using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

public class AsyncIOCPServer
{
    public static AsyncIOCPServer instance;

    /// <summary>
    /// Listener endpoint.
    /// </summary>
    private IPEndPoint hostEndPoint;
    /// <summary>
    /// 发送与接收的MySocketEventArgs变量定义.
    /// </summary>
    private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();
    /// <summary>
    /// 每个Socket套接字缓冲区大小
    /// </summary>
    int bufferSize = 1024;
    /// <summary>
    /// 处理线程通过这个队列知道有数据需要处理
    /// </summary>
    /// <typeparam name="ConnCache"></typeparam>
    /// <param name=""></param>
    /// <returns></returns>
    Queue<ConnCache> tokenQueue;
    /// <summary>
    /// 接收到数据后，同时通知处理线程处理数据
    /// </summary>
    ManualResetEvent tokenEvent;




    /// <summary>
    /// Socket-Server
    /// </summary>
    Socket _Socket;
    /// <summary>
    /// 对象池
    /// </summary>
    AsyncUserTokenPool userTokenPool;

    int maxClient;

    /// <summary>
    /// 心跳检测间隔秒数
    /// </summary>
    public const int HeartbeatSecondTime = 60;


    /// <summary>
    /// 侦听客户端
    /// </summary>
    public AsyncIOCPServer(string IP, int portNo, int _maxClient)
    {
        instance = this;
        try
        {
            maxClient = _maxClient;
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
            _Socket.Bind(hostEndPoint);
            _Socket.Listen(maxClient);

            Init();

            Thread tCheckClientHeartbeat = new Thread(CheckClientHeartbeat);
            tCheckClientHeartbeat.IsBackground = true;
            tCheckClientHeartbeat.Start();

            StartAccept(null);
            Log4Debug("初始化服务器。");
        }
        catch (Exception error)
        {
            Log4Debug(error.Message);
        }
    }

    private void Init()
    {
        userTokenPool = new AsyncUserTokenPool(maxClient);
        for (int i = 0; i < maxClient; i++) //填充SocketAsyncEventArgs池
        {
            AsyncUserToken userToken = new AsyncUserToken(bufferSize);
            userToken.SAEA_Receive.UserToken = userToken;
            userToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);

            userTokenPool.Push(userToken);
        }
        tokenQueue = new Queue<ConnCache>();
        tokenEvent = new ManualResetEvent(false);
        //处理线程
        ThreadPool.QueueUserWorkItem(new WaitCallback(AnalyzeThrd), null);
    }

    #region Init
    /// <summary>
    /// 线程处理接收事件
    /// </summary>
    /// <param name="state"></param>
    private void AnalyzeThrd(object state)
    {
        //AsyncUserToken userToken;
        ConnCache connCache;

        while (true)
        {
            Monitor.Enter(((ICollection)tokenQueue).SyncRoot);
            if (tokenQueue.Count > 0)
            {
                //userToken = tokenQueue.Dequeue();
                connCache = tokenQueue.Dequeue();
                Monitor.Exit(((ICollection)tokenQueue).SyncRoot);
            }
            else
            {
                Monitor.Exit(((ICollection)tokenQueue).SyncRoot);
                //如果没有需要处理的数据，等待x毫秒后再运行
                tokenEvent.WaitOne();
                continue;
            }

            AsyncUserToken userToken = connCache.UserToken;
            lock (userToken.AnalyzeLock)
            {
                lock (userToken.ReceiveBuffer)
                {
                    userToken.ReceiveBuffer.AddRange(connCache.RecvBuffer);
                }
                Handle(userToken);
            }
        }
    }
    int tagCount = 0;
    /// <summary>  
    /// 初始化发送参数MySocketEventArgs  
    /// </summary>  
    /// <returns></returns>  
    MySocketEventArgs initSendArgs()
    {
        MySocketEventArgs sendArg = new MySocketEventArgs();
        sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
        sendArg.UserToken = _Socket;
        sendArg.RemoteEndPoint = hostEndPoint;
        sendArg.IsUsing = false;
        Interlocked.Increment(ref tagCount);
        sendArg.ArgsTag = tagCount;
        lock (listArgs)
        {
            listArgs.Add(sendArg);
        }
        return sendArg;
    }
    #endregion





    #region 服务器特有的Accept

    /// <summary>
    /// 接受来自客户机的连接请求操作
    /// </summary>
    private void StartAccept(SocketAsyncEventArgs saea_Accept)
    {
        if (saea_Accept == null)
        {
            saea_Accept = new SocketAsyncEventArgs();
            saea_Accept.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
        }
        else
        {
            saea_Accept.AcceptSocket = null;  //重用前进行对象清理
        }
        if (!_Socket.AcceptAsync(saea_Accept))
        {
            ProcessAccept(saea_Accept);
        }
    }

    /// <summary>
    /// 异步连接操作完成后调用该方法
    /// </summary>
    private void ProcessAccept(SocketAsyncEventArgs accept)
    {
        Socket s = accept.AcceptSocket;
        if (s != null)
        {
            try
            {
                string sClientIP = ((IPEndPoint)s.RemoteEndPoint).Address.ToString();
                Log4Debug(sClientIP + " Client Accept");

                AsyncUserToken userToken = userTokenPool.Pop();

                if (userToken != null)
                {
                    userToken.ConnectSocket = s;
                    userToken.userInfo = new RoomActor(DateTime.Now);
                    userTokenPool.AddUsed(userToken);

                    Log4Debug("Free Client total：" + userTokenPool.Count());
                    SocketAsyncEventArgs e = userToken.SAEA_Receive;
                    if (!userToken.ConnectSocket.ReceiveAsync(e))
                    {
                        ProcessReceive(userToken);
                    }
                }
                else
                {
                    s.Close();
                    Log4Debug(sClientIP + " Can't connect server,because connection pool has been finished ！");
                }
            }
            catch (Exception e)
            {
                Log4Debug("ProcessAccept:" + e.Message);
            }
        }
        StartAccept(accept);
    }
    #endregion

    #region 客户端特有的Connect

    // Calback for connect operation
    private void ProcessConnected(SocketAsyncEventArgs e)
    {

    }
    #endregion

    #region Receive
    private void ProcessReceive(AsyncUserToken userToken)
    {
        tokenEvent.Set();
        SocketAsyncEventArgs e = userToken.SAEA_Receive;
        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            if (userToken.userInfo != null)
            {
                userToken.userInfo.heartbeatTime = DateTime.Now;
            }
            string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).Address.ToString();
            //try
            {
                byte[] copy = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, copy, 0, e.BytesTransferred);

                ConnCache cache = new ConnCache(copy, userToken);
                Monitor.Enter(((ICollection)tokenQueue).SyncRoot);
                tokenQueue.Enqueue(cache);
                Monitor.Exit(((ICollection)tokenQueue).SyncRoot);

                if (!userToken.ConnectSocket.ReceiveAsync(e))
                    ProcessReceive(userToken);
            }
            //catch (Exception error)
            //{
            //    Log4Debug(error.Message);
            //}
        }
        else
        {
            CloseClientSocket(userToken);
        }
    }
    private void Handle(AsyncUserToken userToken)
    {
        do
        {
            byte[] lenBytes = userToken.ReceiveBuffer.GetRange(0, sizeof(int)).ToArray();
            int packageLen = BitConverter.ToInt32(lenBytes, 0);
            if (packageLen <= userToken.ReceiveBuffer.Count - sizeof(int))
            {
                //包够长时,则提取出来,交给后面的程序去处理  
                byte[] buffer = userToken.ReceiveBuffer.GetRange(sizeof(int), packageLen).ToArray();
                //从数据池中移除这组数据,为什么要lock,你懂的  
                lock (userToken.ReceiveBuffer)
                {
                    userToken.ReceiveBuffer.RemoveRange(0, packageLen + sizeof(int));
                }

                while (buffer.Length > 0)
                {
                    MessageXieYi xieyi = MessageXieYi.FromBytes(buffer);
                    if (xieyi == null)
                    {
                        Log4Debug("奇怪为什么协议为空");
                        break;
                    }
                    int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                    buffer = buffer.Skip(messageLength).ToArray();
                    //将数据包交给前台去处理
                    DoReceiveEvent(userToken, xieyi);
                }
            }
            else
            {   //长度不够,还得继续接收,需要跳出循环  
                break;
            }
        } while (userToken.ReceiveBuffer.Count > sizeof(int));
    }

    #endregion

    #region Send

    /// <summary>
    /// 异步发送操作完成后调用该方法
    /// </summary>
    private void ProcessSend(SocketAsyncEventArgs arg)
    {
        MySocketEventArgs e = (MySocketEventArgs)arg;
        //SocketAsyncEventArgs e = userToken.SAEA_Send;
        e.IsUsing = false;
        if (e.SocketError == SocketError.Success)
        {

        }
        else
        {
            Log4Debug("发送未成功，回调：" + e.SocketError);
        }
    }
    private void Send(AsyncUserToken userToken, byte[] send)
    {
        byte[] buffer = AsyncUserToken.GetSendBytes(send);
        //string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).ToString();
        //string info = "";
        //for (int i = 0; i < buffer.Length; i++)
        //{
        //    info += buffer[i] + ",";
        //}
        //Log4Debug("From the " + sClientIP + " to send " + buffer.Length + " bytes of data：" + info);

        //查找有没有空闲的发送MySocketEventArgs,有就直接拿来用,没有就创建新的.So easy!  
        MySocketEventArgs sendArgs = null;
        lock (listArgs)
        {
            sendArgs = listArgs.Find(a => a.IsUsing == false);
            if (sendArgs == null)
            {
                sendArgs = initSendArgs();
            }
            sendArgs.IsUsing = true;
        }

        //Log4Debug("发送所用的套接字编号：" + sendArgs.ArgsTag);
        //lock (sendArgs) //要锁定,不锁定让别的线程抢走了就不妙了.  
        {
            sendArgs.SetBuffer(buffer, 0, buffer.Length);
        }
        Socket s = userToken.ConnectSocket;
        if (!s.SendAsync(sendArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
        {
            // 同步发送时处理发送完成事件  
            ProcessSend(sendArgs);
        }
    }

    #endregion

    #region 服务器发送接收逻辑

    /// <summary>  
    /// 使用新进程通知事件回调  
    /// </summary>  
    /// <param name="buff"></param>  
    private void DoReceiveEvent(AsyncUserToken userToken, MessageXieYi xieyi)
    {
        //object[] all = new object[] { userToken, xieyi };
        //object obj = (object)all;
        ////用新的线程,这样不拖延接收新数据. 
        //Thread thread = new Thread(new ParameterizedThreadStart(ThreadDealReceive));
        //thread.IsBackground = true;
        //thread.Start(obj);
        DealXieYi(xieyi, userToken);
    }

    private void ThreadDealReceive(object obj)
    {
        object[] all = (object[])obj;
        AsyncUserToken userToken = (AsyncUserToken)all[0];
        MessageXieYi xieyi = (MessageXieYi)all[1];
        DealXieYi(xieyi, userToken);
    }

    private void DealXieYi(MessageXieYi xieyi, AsyncUserToken userToken)
    {
        byte[] backInfo = ServerDataManager.instance.SelectMessage(xieyi, userToken); //判断逻辑

        //用户需要服务器返回值的话
        if (backInfo != null)
        {
            SendSave(userToken, backInfo);
        }
    }



    public void SendSave(AsyncUserToken userToken, byte[] data)
    {
        Send(userToken, data);
    }

    public void SendSave(string userID, byte[] message, byte xieyiFirst, byte xieyiSecond)
    {
        AsyncUserToken userToken = GetTokenByMemberID(userID);
        if (userToken != null)
        {
            //创建一个发送缓冲区。   
            MessageXieYi msgXY = new MessageXieYi(xieyiFirst, xieyiSecond, message);
            //Log4Debug("给 ID:" + userID + "/发送消息协议号：" + (MessageConvention)xieyiFirst + "/大小：" + message.Length);
            SendSave(userToken, msgXY.ToBytes());
        }
    }


    #endregion

    #region 套接字回调
    private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
    {
        AsyncUserToken userToken = null;
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Connect:
                ProcessConnected(e);
                break;
            case SocketAsyncOperation.Accept:
                ProcessAccept(e);
                break;
            case SocketAsyncOperation.Receive:
                userToken = (AsyncUserToken)e.UserToken;
                ProcessReceive(userToken);
                break;
            case SocketAsyncOperation.Send:
                //userToken = (AsyncUserToken)e.UserToken;
                ProcessSend(e);
                break;
        }
    }
    #endregion


    #region Close

    /// <summary>
    /// Socket 断开处理
    /// </summary>
    private void CloseClientSocket(AsyncUserToken userToken)
    {
        try
        {
            if (userToken.ConnectSocket == null)
                return;

            ServerDataManager.instance.SetOffLineByState(userToken);
            Log4Debug(String.Format("客户 {0} 清理链接!", userToken.ConnectSocket.RemoteEndPoint.ToString()));
            //
            userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            userToken.ConnectSocket.Close();
            Log4Debug("Free Client total：" + userTokenPool.Count());
        }
        catch
        {

        }
        finally
        {
            userTokenPool.RemoveUsed(userToken);//清除在线
            userToken.Init();//清除该变量
            userTokenPool.Push(userToken);//复存该变量
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
            //try
            {
                int heartbeatTime = HeartbeatSecondTime * 1000; //1000是毫秒，检测间隔
                Thread.Sleep(heartbeatTime);
                //Log4Debug("开始心跳检测" + DateTime.Now);
                userTokenPool.CheckIsConnected(heartbeatTime, CloseClientSocket);
            }
            //catch (Exception e)
            //{
            //    Log4Debug("心跳检测错误:" + e.Message);
            //}
        }
    }
    #endregion




    /// <summary>
    /// 获取指定用户
    /// </summary>
    /// <param name="userID"></param>
    /// <returns></returns>
    public AsyncUserToken GetTokenByMemberID(string userID)
    {
        return userTokenPool.GetTokenByMemberID(userID);
    }
    public void Log4Debug(string msg)
    {
        LogManager.instance.WriteLog(this.GetType().Name + ":" + msg);
    }

}
