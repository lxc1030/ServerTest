using System;
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
    /// 对象池
    /// </summary>
    AsyncUserTokenPool userTokenPool;

    /// <summary>
    /// 每个Socket套接字缓冲区大小
    /// </summary>
    int bufferSize = 1024;
    /// <summary>
    /// 心跳检测间隔秒数
    /// </summary>
    public const int HeartbeatSecondTime = 5;

    /// <summary>
    /// 侦听客户端
    /// </summary>
    public AsyncIOCPServer(string IP, int portNo, int maxClient)
    {
        instance = this;
        try
        {
            IPAddress ipAddress = IPAddress.Parse(IP);
            s_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s_Server.Bind(new IPEndPoint(ipAddress, portNo));
            s_Server.Listen(maxClient);

            userTokenPool = new AsyncUserTokenPool(maxClient);
            for (int i = 0; i < maxClient; i++) //填充SocketAsyncEventArgs池
            {
                AsyncUserToken userToken = new AsyncUserToken(bufferSize);

                userToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                userToken.SAEA_Receive.UserToken = userToken;
                userToken.SAEA_Send.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                userToken.SAEA_Send.UserToken = userToken;

                userTokenPool.Push(userToken);
            }

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
            saea_Accept.AcceptSocket = null;  //重用前进行对象清理

        if (!s_Server.AcceptAsync(saea_Accept))
            ProcessAccept(saea_Accept);
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
            catch { }
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
        try
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                userToken.HeartbeatTime = DateTime.Now;
                string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).Address.ToString();
                try
                {
                    byte[] buffer = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, buffer, 0, e.BytesTransferred);

                    string info = "";
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        info += buffer[i] + ",";
                    }
                    //Log4Debug("From the " + sClientIP + " to receive " + e.BytesTransferred + " bytes of data：" + info);

                    lock (userToken.ReceiveBuffer)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            userToken.ReceiveBuffer.Enqueue(buffer[i]);
                        }
                    }
                    //
                    if (!userToken.isDealReceive)
                    {
                        userToken.isDealReceive = true;
                        Handle(userToken);
                    }

                }
                catch (Exception error)
                {
                    Log4Debug(error.Message);
                }
                finally
                {
                    if (!userToken.ConnectSocket.ReceiveAsync(e))
                        ProcessReceive(userToken);
                }
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }
        catch { }
    }

    private void Handle(AsyncUserToken userToken)
    {
        while (userToken.ReceiveBuffer.Count > 0)
        {
            byte[] mix = null;
            byte[] buffer = null;
            lock (userToken.ReceiveBuffer)
            {
                buffer = userToken.ReceiveBuffer.ToArray();
                userToken.Clear(userToken.ReceiveBuffer);
            }

            mix = new byte[userToken.halfReceiveMessage.Length + buffer.Length];
            userToken.halfReceiveMessage.CopyTo(mix, 0);
            userToken.Clear(userToken.halfReceiveMessage);
            Array.Copy(buffer, 0, mix, userToken.halfReceiveMessage.Length, buffer.Length);

            while (mix.Length > 0)
            {
                MessageXieYi xieyi = MessageXieYi.FromBytes(mix);
                if (xieyi != null)
                {
                    int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                    //Log4Debug("快速处理协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
                    DealReceive(xieyi, userToken);
                    mix = mix.Skip(messageLength).ToArray();
                }
                else
                {
                    userToken.halfReceiveMessage = new byte[mix.Length];
                    userToken.halfReceiveMessage = mix;
                    //
                    string info = "sy:";
                    for (int i = 0; i < mix.Length; i++)
                    {
                        info += mix[i] + ",";
                    }
                    //Debug.LogError("剩余未处理数据长度：" + mix.Length + "当前帧：" + GameManager.instance.frameIndex + "/" + DataController.instance.MyRoomInfo.FrameIndex + info);
                    break;
                }
            }
        }
        userToken.isDealReceive = false;
    }

    #endregion

    #region Send

    /// <summary>
    /// 异步发送操作完成后调用该方法
    /// </summary>
    private void ProcessSend(AsyncUserToken userToken)
    {
        SocketAsyncEventArgs e = userToken.SAEA_Send;

        if (e.SocketError == SocketError.Success)
        {
            //TODO
            userToken.isSending = false;
            if (userToken.SendBuffer.Count > 0)
            {
                Send(userToken);
            }
        }
        else
        {
            Log4Debug("发送回调：" + e.SocketError);
        }
    }
    public void Send(AsyncUserToken userToken)
    {
        userToken.isSending = true;

        byte[] buffer = null;
        lock (userToken.SendBuffer)
        {
            buffer = userToken.SendBuffer.ToArray();
            userToken.Clear(userToken.SendBuffer);
        }

        string info = "";
        for (int i = 0; i < buffer.Length; i++)
        {
            info += buffer[i] + ",";
        }
        Log4Debug("send bytes of data：" + info);

        userToken.SAEA_Send.SetBuffer(buffer, 0, buffer.Length);
        Socket s = userToken.ConnectSocket;
        SocketAsyncEventArgs e = userToken.SAEA_Send;

        if (!s.SendAsync(e))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
        {
            // 同步发送时处理发送完成事件  
            ProcessSend(userToken);
        }
    }

    #endregion

    #region 服务器发送接收逻辑
    
    private void DealReceive(MessageXieYi xieyi, AsyncUserToken userToken)
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
        userToken.SendSave(data);
        if (!userToken.isSending)
        {
            Send(userToken);
        }
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
                userToken = (AsyncUserToken)e.UserToken;
                ProcessSend(userToken);
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
            Log4Debug("Free Client total：" + userTokenPool.Count());
        }
        catch
        {

        }
        userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源
        userToken.userInfo = null;
        userTokenPool.Push(userToken);
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
        LogManager.WriteLog(this.GetType().Name + ":" + msg);
    }

}

