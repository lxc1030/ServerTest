using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using Network_Kcp;

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
    /// Socket-Server
    /// </summary>
    Socket _Socket;
    /// <summary>
    /// 对象池
    /// </summary>
    AsyncUserTokenPool userTokenPool;
    /// <summary>
    /// SocketEventArgs 用到的缓冲区管理类
    /// </summary>
    BufferManager bufferManager;

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
            _Socket.NoDelay = true;
            hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
            _Socket.Bind(hostEndPoint);
            _Socket.Listen(maxClient);

            Init();

            Thread tCheckClientHeartbeat = new Thread(CheckClientHeartbeat);
            tCheckClientHeartbeat.IsBackground = true;
            tCheckClientHeartbeat.Start();
            
            StartAccept(null);
            Log4Debug("初始化TCP Socket成功。");
        }
        catch (Exception error)
        {
            Log4Debug(error.Message);
        }
    }
    private void Init()
    {
        bufferManager = new BufferManager(maxClient * 4 * bufferSize, bufferSize);
        userTokenPool = new AsyncUserTokenPool(maxClient);
        for (int i = 0; i < maxClient; i++) //填充SocketAsyncEventArgs池
        {
            AsyncUserToken userToken = new AsyncUserToken(bufferSize);
            userToken.SAEA_Receive.UserToken = userToken;
            userToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            bufferManager.SetBuffer(userToken.SAEA_Receive);

            userTokenPool.Push(userToken);
        }
    }

    #region Init
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
            saea_Accept.AcceptSocket = null;  //重用前进行对象清理--貌似会导致套接字正在使用的bug
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
                accept.Dispose();
                string sClientIP = ((IPEndPoint)s.RemoteEndPoint).ToString();
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
        StartAccept(null);
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

                lock (userToken.ReceiveBuffer)
                {
                    userToken.ReceiveBuffer.AddRange(copy);
                }

                if (!userToken.ConnectSocket.ReceiveAsync(e))
                    ProcessReceive(userToken);


                //处理线程
                ThreadPool.QueueUserWorkItem(new WaitCallback(Handle), userToken);

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
    /// <summary>
    /// 线程处理接收数据
    /// </summary>
    /// <param name="state"></param>
    private void Handle(object state)
    {
        AsyncUserToken userToken = (AsyncUserToken)state;
        lock (userToken.LockHanding)
        {
            while (userToken.ReceiveBuffer.Count > sizeof(int))//包整长判断
            {
                int packageLen = 0;
                byte[] completeMessage = null;
                lock (userToken.ReceiveBuffer)
                {
                    byte[] lenBytes = userToken.ReceiveBuffer.GetRange(0, sizeof(int)).ToArray();
                    packageLen = BitConverter.ToInt32(lenBytes, 0);
                    if (packageLen <= userToken.ReceiveBuffer.Count - sizeof(int))//数据够长
                    {
                        completeMessage = userToken.ReceiveBuffer.GetRange(sizeof(int), packageLen).ToArray();
                        userToken.ReceiveBuffer.RemoveRange(0, packageLen + sizeof(int));
                    }
                    else//数据不够长
                    {
                        continue;
                    }
                }
                //处理Complete
                MessageXieYi xieyi = MessageXieYi.FromBytes(completeMessage);
                if (xieyi == null)
                {
                    Log4Debug("完整长度数据不能反序列化成MessageXieYi，本段数据丢弃。");
                }
                else
                {
                    object[] all = new object[] { userToken, xieyi };
                    ThreadPool.QueueUserWorkItem(new WaitCallback(XieYiThrd), all);
                }
            }
        }
    }
    private void XieYiThrd(object state)
    {
        object[] all = (object[])state;
        AsyncUserToken userToken = (AsyncUserToken)all[0];
        MessageXieYi xieyi = (MessageXieYi)all[1];



        //将接收到的数据经过处理再发送出去
        byte[] backInfo = ServerDataManager.instance.SelectMessage(xieyi, userToken); //判断逻辑
        if (backInfo != null)
        {
            SendSave(userToken, backInfo);
        }
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
        else if (e.SocketError == SocketError.Shutdown)
        {
            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
            CloseClientSocket(userToken);
            Log4Debug("Socket已断线");
        }
    }
    private void Send(AsyncUserToken userToken, byte[] send)
    {
        try
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
            lock (listArgs)//要锁定,不锁定让别的线程抢走了就不妙了.  
            {
                sendArgs = listArgs.Find(a => a.IsUsing == false);
                if (sendArgs == null)
                {
                    sendArgs = initSendArgs();
                }
                sendArgs.IsUsing = true;
            }
            sendArgs.SetBuffer(buffer, 0, buffer.Length);
            sendArgs.UserToken = userToken;

            Socket s = userToken.ConnectSocket;
            if (!s.SendAsync(sendArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
            {
                // 同步发送时处理发送完成事件  
                ProcessSend(sendArgs);
            }
        }
        catch (Exception e)
        {
            CloseClientSocket(userToken);
        }
    }

    #endregion

    #region 服务器发送接收逻辑

    public void SendSave(AsyncUserToken userToken, byte[] data)
    {
        Send(userToken, data);
    }

    //public void SendSave(string userID, byte[] message, byte xieyiFirst, byte xieyiSecond)
    //{
    //    AsyncUserToken userToken = GetTokenByMemberID(userID);
    //    if (userToken != null)
    //    {
    //        //创建一个发送缓冲区。   
    //        MessageXieYi msgXY = new MessageXieYi(xieyiFirst, xieyiSecond, message);
    //        //Log4Debug("给 ID:" + userID + "/发送消息协议号：" + (MessageConvention)xieyiFirst + "/大小：" + message.Length);
    //        SendSave(userToken, msgXY.ToBytes());
    //    }
    //}


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
        NetworkDebuger.Log(this.GetType().Name + ":" + msg);
    }
}
