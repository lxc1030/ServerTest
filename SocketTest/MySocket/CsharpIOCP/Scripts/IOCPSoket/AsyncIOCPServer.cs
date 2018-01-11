﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class AsyncIOCPServer
{
    public static AsyncIOCPServer instance;
    /// <summary>
    /// Socket-Server
    /// </summary>
    Socket s_Server;
    /// <summary>
    /// Listener endpoint.
    /// </summary>
    private IPEndPoint hostEndPoint;

    /// <summary>
    /// 对象池
    /// </summary>
    AsyncUserTokenPool userTokenPool;
    /// <summary>
    /// 发送与接收的MySocketEventArgs变量定义.
    /// </summary>
    private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();

    int maxClient;
    /// <summary>
    /// 每个Socket套接字缓冲区大小
    /// </summary>
    int bufferSize = 1024;
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
            s_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
            s_Server.Bind(hostEndPoint);
            s_Server.Listen(maxClient);

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

    #region Init

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
        sendArg.UserToken = s_Server;
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
        if (!s_Server.AcceptAsync(saea_Accept))
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
                    userTokenPool.AddUsed(userToken);
                    userToken.userInfo = new RoomActor(DateTime.Now);

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
                //
                lock (userToken.ReceiveBuffer)
                {
                    userToken.ReceiveBuffer.AddRange(copy);
                }
                if (!userToken.ConnectSocket.ReceiveAsync(e))
                    ProcessReceive(userToken);

                if (!userToken.isDealReceive)
                {
                    userToken.isDealReceive = true;
                    Handle(userToken);
                }
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
        while (userToken.ReceiveBuffer.Count > 0)
        {
            byte[] receive = null;
            lock (userToken.ReceiveBuffer)
            {
                receive = userToken.ReceiveBuffer.ToArray();
                userToken.ReceiveBuffer.Clear();
            }
            userToken.DealBuffer.AddRange(receive);
            //
            while (userToken.DealBuffer.Count > 0)
            {
                MessageXieYi xieyi = MessageXieYi.FromBytes(userToken.DealBuffer.ToArray());
                int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                userToken.DealBuffer.RemoveRange(0, messageLength);
                //将数据包交给前台去处理
                DoReceiveEvent(userToken, xieyi);
            }
        }
        userToken.isDealReceive = false;
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
        string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).ToString();
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
        object[] all = new object[] { userToken, xieyi };
        object obj = (object)all;
        //用新的线程,这样不拖延接收新数据. 
        Thread thread = new Thread(new ParameterizedThreadStart(ThreadDealReceive));
        thread.IsBackground = true;
        thread.Start(obj);
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
            try
            {
                int heartbeatTime = HeartbeatSecondTime * 1000; //1000是毫秒，检测间隔
                Thread.Sleep(heartbeatTime);
                //Log4Debug("开始心跳检测" + DateTime.Now);
                userTokenPool.CheckIsConnected(heartbeatTime, CloseClientSocket);
            }
            catch (Exception e)
            {
                Log4Debug("心跳检测错误:" + e.Message);
            }
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

