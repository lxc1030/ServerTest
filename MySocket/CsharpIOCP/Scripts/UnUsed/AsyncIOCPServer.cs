using System;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;

namespace NetFrame.Net
{
    /// <summary>
    /// 异步Socket服务器(IOCP)----（教授说：异步接收，同步发送）
    /// </summary>
    public class AsyncIOCPServer
    {
        public static AsyncIOCPServer instance;

        #region Fields
        /// <summary>
        /// 服务器程序允许的最大客户端连接数
        /// </summary>
        private int _maxClient;

        /// <summary>
        /// 监听Socket，用于接受客户端的连接请求
        /// </summary>
        private Socket _serverSock;

        /// <summary>
        /// 当前的连接的客户端数
        /// </summary>
        private int _clientCount;

        /// <summary>
        /// 用于Socket操作的数据临时接收缓冲区大小
        /// </summary>
        private int _bufferSize = 1024;

        /// <summary>
        /// 信号量
        /// </summary>
        Semaphore _maxAcceptedClients;

        /// <summary>
        /// 对象池（空的对象池待申请使用，使用时从对象池移除）
        /// </summary>
        AsyncUserTokenPool _userTokenPool;

        /// <summary>
        /// 心跳检测间隔秒数
        /// </summary>
        public const int HeartbeatSecondTime = 60;

        private bool disposed = false;

        #endregion

        #region Properties
        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 监听的IP地址
        /// </summary>
        public IPAddress Address { get; private set; }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public int Port { get; private set; }

        public int BufferSize
        {
            get { return _bufferSize; }
            set { _bufferSize = value; }
        }




        #endregion

        #region Ctors

        /// <summary>
        /// 异步Socket TCP服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        /// <param name="maxClient">最大客户端数量</param>
        public AsyncIOCPServer(IPAddress localIPAddress, int listenPort, int maxClient)
        {
            instance = this;
            this.Address = localIPAddress;
            this.Port = listenPort;

            _maxClient = maxClient;
            _serverSock = new Socket(localIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _userTokenPool = new AsyncUserTokenPool(_maxClient);
            _maxAcceptedClients = new Semaphore(_maxClient, _maxClient);

            instance.Start();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化函数
        /// </summary>
        public void Init()
        {
            AsyncUserToken userToken;
            for (int i = 0; i < _maxClient; i++)
            {
                userToken = new AsyncUserToken(BufferSize);
                _userTokenPool.Push(userToken);
            }
            //心跳检测
            Thread tCheckClientHeartbeat = new Thread(CheckClientHeartbeat);
            tCheckClientHeartbeat.IsBackground = true;
            tCheckClientHeartbeat.Start();
        }
        #endregion

        #region Start
        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            try
            {
                if (!IsRunning)
                {
                    Init();
                    IsRunning = true;
                    IPEndPoint localEndPoint = new IPEndPoint(Address, Port);
                    // 创建监听socket
                    _serverSock = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // 配置监听socket为 dual-mode (IPv4 & IPv6) 
                        // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,
                        _serverSock.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                        _serverSock.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
                    }
                    else
                    {
                        _serverSock.Bind(localEndPoint);
                    }
                    // 开始监听
                    _serverSock.Listen(this._maxClient);
                    // 在监听Socket上投递一个接受请求。
                    StartAccept(null);
                    _serverSock.Accept();
                }
            }
            catch (Exception e)
            {
                Log4Debug("Start:" + e.Message);
            }
        }
        #endregion

        #region Stop

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _serverSock.Close();
                //TODO 关闭对所有客户端的连接
            }
        }

        #endregion

        #region Accept

        /// <summary>
        /// 从客户端开始接受一个连接操作
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs asyniar)
        {
            if (asyniar == null)
            {
                asyniar = new SocketAsyncEventArgs();
                asyniar.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                //socket must be cleared since the context object is being reused
                asyniar.AcceptSocket = null;
            }
            _maxAcceptedClients.WaitOne();
            if (!_serverSock.AcceptAsync(asyniar))
            {
                ProcessAccept(asyniar);
                //如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件
                //此时I/O操作同步完成，不会触发Asyn_Completed事件，所以指定BeginAccept()方法
            }
        }

        /// <summary>
        /// accept 操作完成时回调函数
        /// </summary>
        /// <param name="sender">Object who raised the event.</param>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 监听Socket接受处理
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket sock = e.AcceptSocket;//和客户端关联的socket
            if (e.SocketError == SocketError.Success)
            {
                if (sock.Connected)
                {
                    Log4Debug("连接用户的Handle：" + sock.Handle);
                    try
                    {
                        Interlocked.Increment(ref _clientCount);//原子操作加1
                        AsyncUserToken userToken = _userTokenPool.Pop();
                        userToken.Init();
                        userToken.ConnectSocket = sock;

                        //创建处理数据线程
                        //Thread handle = new Thread(new ParameterizedThreadStart(Handle));
                        //handle.Start(userToken);

                        //心跳时间
                        userToken.userInfo = new RoomActor(DateTime.Now);

                        Log4Debug(String.Format("客户 {0} 连入, 共有 {1} 个连接。", sock.RemoteEndPoint.ToString(), _clientCount));

                        ProcessReceive(userToken);//投递接收请求
                    }
                    catch (SocketException ex)
                    {
                        Log4Debug(String.Format("接收客户 {0} 数据出错, 异常信息： {1} 。", sock.RemoteEndPoint, ex.ToString()));
                        //TODO 异常处理
                    }
                    //投递下一个接受请求
                    StartAccept(e);
                }
            }
        }

        #endregion

        #region 发送数据
        /// <summary>
        /// 存储要发送的消息并判断是否发送
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        public void SaveSendMessage(AsyncUserToken userToken, byte[] data)
        {
            //string INFO = "保存待发送:";
            //for (int i = 0; i < data.Length; i++)
            //{
            //    INFO += "_" + data[i];
            //}
            //Log4Debug(INFO);

            lock (userToken.SendBuffer)
            {
                //存值
                for (int i = 0; i < data.Length; i++)
                {
                    //将buffer保存到队列
                    userToken.SendBuffer.Enqueue(data[i]);
                }
            }
            if (!userToken.isDealSend)
            {
                userToken.isDealSend = true;
                Send(userToken);
            }

        }

        ///// <summary>
        ///// 同步发送方法
        ///// </summary>
        ///// <param name="e"></param>
        ///// <param name="timeout">同步发送的延迟毫秒</param>
        //public void Send(AsyncUserToken e, byte[] data)
        //{
        //    AsyncUserToken userToken = e;
        //    try
        //    {
        //        //if (!userToken.ConnectSocket.Connected)
        //        //{
        //        //    OffLineClientSocket(userToken);
        //        //}

        //        userToken.ConnectSocket.SendTimeout = 0;
        //        //int startTickCount = Environment.TickCount;

        //        try
        //        {
        //            userToken.ConnectSocket.Send(data, data.Length, SocketFlags.None);
        //        }
        //        catch (SocketException ex)
        //        {
        //            if (
        //                ex.SocketErrorCode == SocketError.WouldBlock
        //                || ex.SocketErrorCode == SocketError.IOPending
        //                || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable
        //                )
        //            {
        //                // socket buffer is probably full, wait and try again
        //                Thread.Sleep(30);
        //                //
        //                Log4Debug("发送失败？");
        //            }
        //            else
        //            {
        //                OffLineClientSocket(userToken);
        //                /*throw ex;*/ // any serious error occurr
        //            }
        //        }
        //    }
        //    catch (Exception error)
        //    {
        //        Log4Debug("SendError:" + error.Message);
        //        OffLineClientSocket(userToken);
        //    }
        //}




        public void Send(AsyncUserToken userToken)
        {
            Socket socket = userToken.ConnectSocket;
            //判断Socket是否存在以及是否掉线
            if (socket == null)
            {
                if (userToken.userInfo != null)
                {
                    instance.Log4Debug("玩家掉线：" + userToken.userInfo.Register.name);
                    userToken.userInfo.CurState = RoomActorState.Offline;
                }
                return;
            }
            else if (!socket.Connected)//发送数据时检测到Socket掉线
            {
                return;
            }

            //开始发送
            socket.SendTimeout = 10000;//设置发送后判断超时的时长
            while (userToken.SendBuffer.Count > 0)
            {
                byte[] mix = new byte[userToken.SendBuffer.Count];
                lock (userToken.SendBuffer)
                {
                    Array.Copy(userToken.SendBuffer.ToArray(), 0, mix, 0, userToken.SendBuffer.Count);
                    userToken.ClearSend();
                }

                //int startTickCount = Environment.TickCount;
                while (mix.Length > 0)
                {
                    int curIndex = userToken.sendIndex;
                    MessageOperation oper = MessageOperation.FromBytes(curIndex, mix);
                    byte[] buffer = null;

                    buffer = oper.ToBytes();
                    userToken.sendIndex++;
                    mix = mix.Skip(buffer.Length).ToArray();
                    string sendIfo = "userToken.sendIndex:" + curIndex + "----";
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        sendIfo += buffer[i] + ",";
                    }
                    Log4Debug(sendIfo);




                    //byte[] idBuffer = BitConverter.GetBytes(curIndex);// 将 int 转换成字节数组
                    //byte[] lengthBuffer = new byte[4];
                    //userToken.sendIndex++;
                    //int dealLength = 0;
                    //if (mix.Length > 1020)
                    //{
                    //    dealLength = 1020;
                    //}
                    //else
                    //{
                    //    dealLength = mix.Length;
                    //}
                    //byte[] buffer = new byte[idBuffer.Length + dealLength];
                    //Array.Copy(idBuffer, 0, buffer, 0, idBuffer.Length);//4
                    //Array.Copy(mix, 0, buffer, idBuffer.Length, dealLength);//dealLength
                    //mix = mix.Skip(buffer.Length).ToArray();
                    //string sendIfo = "userToken.sendIndex:" + curIndex + "----";
                    //for (int i = 0; i < buffer.Length; i++)
                    //{
                    //    sendIfo += buffer[i] + ",";
                    //}
                    //Log4Debug(sendIfo);
                    //
                    int sent = 0; // how many bytes is already sent
                    do
                    {
                        try
                        {
                            sent += socket.Send(buffer, sent, buffer.Length - sent, SocketFlags.None);
                        }
                        catch (SocketException ex)
                        {
                            Log4Debug("sendError:" + ex.SocketErrorCode);
                            //
                            if (ex.SocketErrorCode == SocketError.WouldBlock ||
                            ex.SocketErrorCode == SocketError.IOPending ||
                            ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                            {
                                // socket buffer is probably full, wait and try again
                                Thread.Sleep(30);
                                Log4Debug("睡眠30秒后继续发送。");
                            }
                            else
                            {
                                if (ex.SocketErrorCode == SocketError.ConnectionAborted)//您的主机中的软件中止了一个已建立的连接。
                                {
                                    return;
                                }
                                //throw ex; // any serious error occurr
                                Log4Debug("send错误：" + ex.Message);
                            }
                        }
                    } while (sent < buffer.Length);
                    //Log4Debug("发送用时毫秒：" + (Environment.TickCount - startTickCount));
                }
            }
            userToken.isDealSend = false;
        }
        #endregion

        #region 接收数据

        /// <summary>
        /// 异步接收Socket数据
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public void ProcessReceive(AsyncUserToken e)
        {
            AsyncUserToken userToken = e;
            Socket socket = userToken.ConnectSocket;

            socket.BeginReceive(userToken.AsyncReceiveBuffer, 0, userToken.AsyncReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), userToken);
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            AsyncUserToken userToken = (AsyncUserToken)ar.AsyncState;
            Socket socket = userToken.ConnectSocket;
            try
            {
                if (socket == null || !socket.Connected)
                {
                    return;
                }
                lock (userToken.ReceiveBuffer)
                {
                    //从远程设备读取数据
                    int read = socket.EndReceive(ar);
                    if (read > 0)
                    {
                        //byte[] buffer = new byte[read];
                        ////将getBuffer数组的前read个字节拷贝到buffer数组中
                        //Array.Copy(userToken.AsyncReceiveBuffer, 0, buffer, 0, read);
                        //userToken.userInfo.heartbeatTime = DateTime.Now;
                        ////接收数据保存以后继续接收
                        //ProcessReceive(userToken);

                        //lock (userToken.ReceiveBuffer)
                        //{
                        //    //存值
                        //    for (int i = 0; i < buffer.Length; i++)
                        //    {
                        //        //将buffer保存到队列
                        //        userToken.ReceiveBuffer.Enqueue(buffer[i]);
                        //    }
                        //}
                        //if (!userToken.isDealReceive)
                        //{
                        //    userToken.isDealReceive = true;
                        //    Handle(userToken);
                        //}



                        byte[] buffer = new byte[read];
                        //将getBuffer数组的前read个字节拷贝到buffer数组中
                        Array.Copy(userToken.AsyncReceiveBuffer, 0, buffer, 0, read);
                        //接收数据保存以后继续接收
                        ProcessReceive(userToken);

                        string info = "";
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            info += buffer[i] + ",";
                        }
                        //Log4Debug("接收数据：" + info);

                        MessageOperation oper = MessageOperation.FromBytes(buffer);
                        userToken.outOrders.Add(oper.GetId(), oper.Message);


                        //byte[] intBuff = new byte[4] { buffer[0], buffer[1], buffer[2], buffer[3] };
                        //int index = BitConverter.ToInt32(intBuff, 0);           // 从字节数组转换成 int

                        //byte[] dealBuffer = new byte[buffer.Length - intBuff.Length];
                        //Array.Copy(buffer, intBuff.Length, dealBuffer, 0, dealBuffer.Length);
                        //userToken.outOrders.Add(index, dealBuffer);
                        //while (userToken.outOrders.ContainsKey(userToken.sendIndex))
                        //{
                        //    //存值
                        //    for (int i = 0; i < userToken.outOrders[userToken.sendIndex].Length; i++)
                        //    {
                        //        //将buffer保存到队列
                        //        userToken.ReceiveBuffer.Enqueue(userToken.outOrders[userToken.sendIndex][i]);
                        //    }
                        //    userToken.sendIndex++;
                        //}
                        if (!userToken.isDealReceive)
                        {
                            userToken.isDealReceive = true;
                            Handle(userToken);
                        }











                    }
                    else//接收数据小于等于0
                    {
                        CloseClientSocket(userToken);
                        return;
                    }
                }
            }
            catch (Exception error)
            {
                Log4Debug("ReceiveError:" + error.Message);
                CloseClientSocket(userToken);
            }
        }




        public void Handle(object obj)
        {
            //AsyncUserToken userToken = (AsyncUserToken)obj;
            //while (userToken.ReceiveBuffer.Count > 0)
            //{
            //    //userToken.isOnLoop = true;
            //    byte[] buffer = new byte[] { };
            //    if (userToken.HalfMessage == null)//上一次循环的数据处理完毕
            //    {
            //        int startLength = MessageXieYi.XieYiLength + 1;
            //        //TODO 处理数据
            //        if (userToken.ReceiveBuffer.Count < startLength)
            //        {
            //            Log4Debug("剩余长度:" + userToken.ReceiveBuffer.Count() + "小于协议默认长度:" + startLength);
            //            break;
            //        }
            //        //查找开头标识
            //        byte markStart = 0;
            //        lock (userToken.ReceiveBuffer)
            //        {
            //            do
            //            {
            //                markStart = userToken.ReceiveBuffer.Dequeue();
            //            }
            //            while (markStart != MessageXieYi.markStart);//此处有可能删除数据
            //        }
            //        //
            //        //至少6位数据  解析传输数据长度
            //        buffer = new byte[MessageXieYi.XieYiLength];
            //        lock (userToken.ReceiveBuffer)
            //        {
            //            for (int i = 0; i < buffer.Length; i++)
            //            {
            //                buffer[i] = userToken.ReceiveBuffer.Dequeue();
            //            }
            //        }
            //        userToken.HalfMessage = MessageXieYi.BackMessageType(buffer);// 读取协议长度的数值来判断该协议中数据长度的数值
            //    }
            //    if (userToken.HalfMessage.IsLengthCanFillMessage(userToken.ReceiveBuffer))//长度是否足够填充信息（接收数据是否够完成本次）
            //    {
            //        lock (userToken.ReceiveBuffer)
            //        {
            //            userToken.HalfMessage.FillMessageContent(userToken.ReceiveBuffer);
            //            //检查填充完成的下一位是否是结尾符
            //            byte end = userToken.ReceiveBuffer.Peek();
            //            if (end == MessageXieYi.markEnd)//一致的话清除结尾符
            //            {
            //                userToken.ReceiveBuffer.Dequeue();
            //            }
            //            else
            //            {
            //                Log4Debug("检查->处理数据结束后的markEnd不一致:" + end);
            //            }
            //        }
            //        byte[] backInfo = ServerDataManager.instance.SelectMessage(userToken.HalfMessage, userToken); //判断逻辑
            //        userToken.HalfMessage = null;
            //        if (backInfo != null)//用户需要服务器返回值的话
            //        {
            //            //存储要发送的消息并判断是否发送
            //            AsyncIOCPServer.instance.SaveSendMessage(userToken, backInfo);
            //        }
            //    }
            //    else
            //    {
            //        string info = "接收长度不够填充完整处理，保留HalfMessage。";
            //        Log4Debug(info);
            //        break;
            //    }
            //}
            //userToken.isOnLoop = false;

            ///////////////2
            //AsyncUserToken userToken = (AsyncUserToken)obj;
            //while (userToken.ReceiveBuffer.Count > 0)
            //{
            //    byte[] mix = new byte[userToken.halfReceiveMessage.Length + userToken.ReceiveBuffer.Count];
            //    Array.Copy(userToken.halfReceiveMessage, 0, mix, 0, userToken.halfReceiveMessage.Length);
            //    //lock (userToken.ReceiveBuffer)//锁住以后Copy并且置空
            //    {
            //        Array.Copy(userToken.ReceiveBuffer.ToArray(), 0, mix, userToken.halfReceiveMessage.Length, userToken.ReceiveBuffer.Count);
            //        userToken.ClearReceive();
            //    }
            //    do
            //    {
            //        MessageXieYi xieyi = MessageXieYi.FromBytes(mix);
            //        if (xieyi != null)
            //        {
            //            int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
            //            byte[] backInfo = ServerDataManager.instance.SelectMessage(xieyi, userToken); //判断逻辑
            //            if (backInfo != null)//用户需要服务器返回值的话
            //            {
            //                //存储要发送的消息并判断是否发送
            //                AsyncIOCPServer.instance.SaveSendMessage(userToken, backInfo);
            //            }
            //            mix = mix.Skip(messageLength).ToArray();
            //        }
            //        else
            //        {
            //            string info = "sy:";
            //            for (int i = 0; i < mix.Length; i++)
            //            {
            //                info += mix[i] + ",";
            //            }
            //            Log4Debug("剩余未处理数据长度：" + mix.Length + "/" + info);
            //            break;
            //        }

            //    } while (mix.Length > 0);
            //    userToken.halfReceiveMessage = new byte[mix.Length];
            //    userToken.halfReceiveMessage = mix;//保存未处理的数据长度
            //}
            //userToken.isDealReceive = false;
            //////////////2


            AsyncUserToken userToken = (AsyncUserToken)obj;
            while (userToken.outOrders.ContainsKey(userToken.receiveIndex))
            {
                byte[] buffer = userToken.outOrders[userToken.receiveIndex];
                userToken.outOrders.Remove(userToken.receiveIndex);
                byte[] mix = new byte[userToken.halfReceiveMessage.Length + buffer.Length];
                userToken.halfReceiveMessage.CopyTo(mix, 0);
                Array.Copy(buffer, 0, mix, userToken.halfReceiveMessage.Length, buffer.Length);
                userToken.halfReceiveMessage = new byte[] { };
                userToken.receiveIndex++;

                MessageXieYi xieyi = MessageXieYi.FromBytes(mix);
                if (xieyi != null)
                {
                    int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                    //Log4Debug("快速处理协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
                    DealReceive(xieyi, userToken);

                    mix = mix.Skip(messageLength).ToArray();
                    //if (mix.Length > 0)
                    //{
                    //    byte[] intBuff = new byte[4] { mix[0], mix[1], mix[2], mix[3] };
                    //    int index = BitConverter.ToInt32(intBuff, 0);// 从字节数组转换成 int
                    //    mix = mix.Skip(intBuff.Length).ToArray();
                    //    userToken.outOrders.Add(index, mix);
                    //    userToken.receiveIndex = index;
                    //    continue;
                    //}
                    //else
                    //{
                    //    break;
                    //}
                }
                else
                {
                    Array.Copy(mix, 0, userToken.halfReceiveMessage, 0, mix.Length);
                    string info = "sy:";
                    for (int i = 0; i < mix.Length; i++)
                    {
                        info += mix[i] + ",";
                    }
                    //Debug.LogError("剩余未处理数据长度：" + mix.Length + "当前帧：" + GameManager.instance.frameIndex + "/" + DataController.instance.MyRoomInfo.FrameIndex + info);
                    break;
                }


            }
            userToken.isDealReceive = false;

        }



        private void DealReceive(MessageXieYi xieyi, AsyncUserToken userToken)
        {
            byte[] backInfo = ServerDataManager.instance.SelectMessage(xieyi, userToken); //判断逻辑
            if (backInfo != null)//用户需要服务器返回值的话
            {
                //存储要发送的消息并判断是否发送
                AsyncIOCPServer.instance.SaveSendMessage(userToken, backInfo);
            }
        }




        #endregion

        #region 心跳检测
        /// <summary>
        /// 客户端心跳检测
        /// </summary>
        private void CheckClientHeartbeat()
        {
            while (true)
            {
                try
                {
                    int heartbeatTime = HeartbeatSecondTime * 1000; //1000是毫秒，检测间隔
                    Thread.Sleep(heartbeatTime);
                    Log4Debug("开始心跳检测" + DateTime.Now);
                    _userTokenPool.CheckIsConnected(heartbeatTime, CloseClientSocket);
                }
                catch (Exception e)
                {
                    Log4Debug("心跳检测错误:" + e.Message);
                }
            }
        }

        #endregion

        #region Close

        ///// <summary>
        ///// 设置断线
        ///// </summary>
        ///// <param name="userToken"></param>
        //public void OffLineClientSocket(AsyncUserToken userToken)
        //{
        //    if (userToken.userInfo.CurState != RoomActorState.Offline)
        //    {
        //        ServerDataManager.instance.SetOffLine(userToken.userInfo);

        //        Log4Debug("设置玩家ID：" + userToken.userInfo.Register.userID + "/断线等待。");
        //        userToken.userInfo.CurState = RoomActorState.Offline;
        //        if (userToken.userInfo.CurState == RoomActorState.Gaming)//游戏中的话，等房间游戏结束自动清理
        //        {

        //        }
        //        else//非游戏中，等待一段时间，超时则清除
        //        {
        //            userToken.userInfo.offLineTimer = new Timer(new TimerCallback(OfflineTimeCount), userToken, RoomActor.OfflineCloseTime, 0);
        //        }
        //    }
        //}

        //public void OfflineTimeCount(object obj)
        //{
        //    AsyncUserToken userToken = (AsyncUserToken)obj;
        //    userToken.userInfo.offLineTimer.Dispose();
        //    //从房间中移除
        //    ServerDataManager.instance.ForceQuit(userToken.userInfo);
        //    //
        //    CloseClientSocket(userToken);
        //}


        /// <summary>
        /// 关闭socket连接
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed send/receive operation.</param>
        public void CloseClientSocket(AsyncUserToken userToken)
        {
            if (userToken.ConnectSocket == null)
                return;

            try
            {
                ServerDataManager.instance.SetOffLineByState(userToken);
                Log4Debug(String.Format("客户 {0} 清理链接!", userToken.ConnectSocket.RemoteEndPoint.ToString()));
                //
                userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // Throw if client has closed, so it is not necessary to catch.
            }
            finally
            {

            }
            Interlocked.Decrement(ref _clientCount);
            userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源
            userToken.userInfo = null;
            _maxAcceptedClients.Release();
            _userTokenPool.Push(userToken);
        }

        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release 
        /// both managed and unmanaged resources; <c>false</c> 
        /// to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Stop();
                        if (_serverSock != null)
                        {
                            _serverSock = null;
                        }
                    }
                    catch (SocketException ex)
                    {
                        //TODO 事件
                    }
                }
                disposed = true;
            }
        }
        #endregion


        #region 用户逻辑

        /// <summary>
        /// 将需要发送给用户的协议发送
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="message"></param>
        /// <param name="xieyiFirst"></param>
        /// <param name="xieyiSecond"></param>
        public void SendMessageToUser(string userID, byte[] message, byte xieyiFirst, byte xieyiSecond)
        {
            AsyncUserToken userToken = GetTokenByMemberID(userID);
            if (userToken != null)
            {
                //  创建一个发送缓冲区。   
                MessageXieYi msgXY = new MessageXieYi(xieyiFirst, xieyiSecond, message);
                //Log4Debug("给 ID:" + userID + "/发送消息协议号：" + (MessageConvention)xieyiFirst + "/大小：" + message.Length);
                SaveSendMessage(userToken, msgXY.ToBytes());
            }
        }

        /// <summary>
        /// 获取指定用户
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public AsyncUserToken GetTokenByMemberID(string userID)
        {
            return _userTokenPool.GetTokenByMemberID(userID);
        }

        #endregion



        public void Log4Debug(string msg)
        {
            LogManager.WriteLog(this.GetType().Name + ":" + msg);
        }
    }
}
