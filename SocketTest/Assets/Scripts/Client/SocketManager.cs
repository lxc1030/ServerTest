using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class SocketManager : MonoBehaviour
{
    public static SocketManager instance;

    #region 参数
    /// <summary>
    /// Listener endpoint.
    /// </summary>
    private IPEndPoint hostEndPoint;
    /// <summary>
    /// 发送与接收的MySocketEventArgs变量定义.
    /// </summary>
    private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();
    /// <summary>
    /// 用于每个I/O Socket操作的缓冲区大小 默认1024
    /// </summary>
    int bufferSize = 1024;




    /// <summary>  
    /// 连接服务器的socket  
    /// </summary>  
    private Socket _Socket;

    // Signals a connection.
    private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);


    public DateTime startGamTime;

    /// <summary>
    /// 接收发送数据变量
    /// </summary>
    public AsyncUserToken MyUserToken;

    // Flag for connected socket.
    //public bool isConnected = false;





    #endregion

    #region 端口和IP
    //public const string IP = "192.168.0.110";
    //public const int portNo = 500;
    public const string IP = "192.168.1.110";
    public const int portNo = 500;
    #endregion


    public delegate void MessageEventHandler(MessageXieYi xieyi);
    public static event MessageEventHandler MessageEvent;

    public static Dictionary<MessageConvention, MessageEventHandler> allHandle = new Dictionary<MessageConvention, MessageEventHandler>();
    public static void ListenDelegate(bool isListen, List<MessageConvention> listen, Action<MessageXieYi> action)
    {
        for (int i = 0; i < listen.Count; i++)
        {
            MessageConvention temp = listen[i];
            lock (allHandle)
            {
                if (!allHandle.ContainsKey(temp))
                {
                    allHandle.Add(temp, MessageEvent);
                }
                if (isListen)
                {
                    allHandle[temp] += new MessageEventHandler(action);
                }
                else
                {
                    allHandle[temp] -= new MessageEventHandler(action);
                }
            }
        }
    }


    void Awake()
    {
        instance = this;
    }

    private void OnDisable()
    {
        Dispose();
    }
    private void OnDestroy()
    {
        Dispose();
    }


    public void Init(Action<SocketError> callback = null)
    {
        hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
        _Socket = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _Socket.NoDelay = true;
        //SocketError error = Connect(SendLogin);
        SocketError error = Connect();
        if (callback != null)
        {
            callback(error);
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

    private void ProcessAccept(SocketAsyncEventArgs accept)
    {

    }

    #endregion

    #region 客户端特有的Connect

    /// <summary>
    /// 连接到主机
    /// </summary>
    /// <returns>0.连接成功, 其他值失败,参考SocketError的值列表</returns>
    internal SocketError Connect()
    {
        SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        connectArgs.RemoteEndPoint = hostEndPoint;
        connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
        //connectArgs.Completed += (SocketAsyncEventArgs, Action) =>
        //{
        //    OnConnect(callback, connectArgs);
        //};

        MyUserToken = new AsyncUserToken(bufferSize);
        MyUserToken.ConnectSocket = _Socket;

        if (!_Socket.ConnectAsync(connectArgs))
        {
            ProcessConnected(connectArgs);
        }
        autoConnectEvent.WaitOne();

        return connectArgs.SocketError;
    }

    // Calback for connect operation
    private void ProcessConnected(SocketAsyncEventArgs e)
    {
        // Signals the end of connection.
        autoConnectEvent.Set(); //释放阻塞.
                                // Set the flag for socket connected.
        bool isConnected = (e.SocketError == SocketError.Success);
        if (isConnected)
        {
            Debug.Log("Socket连接成功");

            MyUserToken.SAEA_Receive.SetBuffer(new byte[1024], 0, 1024);
            MyUserToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            MyUserToken.SAEA_Receive.UserToken = MyUserToken;

            if (!MyUserToken.ConnectSocket.ReceiveAsync(MyUserToken.SAEA_Receive))
            {
                ProcessReceive(MyUserToken);
            }

        }
        else
        {
            Debug.LogError("Socket连接失败:" + e.SocketError);
        }
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
                    //DealXieYi(xieyi, userToken);
                }
            }
        }
    }
    private void XieYiThrd(object state)
    {
        object[] all = (object[])state;
        AsyncUserToken userToken = (AsyncUserToken)all[0];
        MessageXieYi xieyi = (MessageXieYi)all[1];
        if (xieyi == null)
        {
            Debug.LogError("线程处理协议前，协议已为空。");
        }
        //将数据包交给前台去处理
        DealXieYi(xieyi, userToken);
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

    #region 客户端发送接收逻辑

    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="xieYiFirstFlage"></param>
    /// <param name="message"></param>
    public void SendSave(byte xieYiFirstFlage, byte[] message, bool isMask = true)
    {
        if (isMask)
        {
            SocketConnectUI.instance.Open((MessageConvention)xieYiFirstFlage);
        }
        else
        {
            //Debug.Log("协议：" + (MessageConvention)xieYiFirstFlage + " 不需要显示Loading");
        }
        MessageXieYi msgxy = new MessageXieYi(xieYiFirstFlage, 0, message);
        byte[] sendBuffer = msgxy.ToBytes();
        SendSave(MyUserToken, sendBuffer);
    }

    public void SendSave(AsyncUserToken userToken, byte[] data)
    {
        Send(userToken, data);
    }


    #endregion

    #region 套接字回调
    /// <summary>
    /// 接收或发送完成异步操作回调
    /// </summary>
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
            Log4Debug(String.Format("客户 {0} 清理链接!", userToken.ConnectSocket.RemoteEndPoint.ToString()));
            //
            DisConnect();
        }
        catch
        {

        }
    }

    #endregion

    #region 心跳检测
    public bool isOpenHeartbeat;
    public int heartbeatSecondTime = 0;//从服务器读来的心跳检测时间
    public void OpenHeartbeat()
    {
        //心跳检测
        //if (!isOpenHeartbeat)
        {
            isOpenHeartbeat = true;
            InvokeRepeating("CheckClientHeartbeat", heartbeatSecondTime, heartbeatSecondTime);
        }
    }
    public void CloseHeartbeat()
    {
        Debug.Log("断开心跳检测");
        CancelInvoke("CheckClientHeartbeat");
    }
    /// <summary>
    /// 客户端心跳检测
    /// </summary>
    public void CheckClientHeartbeat()
    {
        if (MyUserToken.ConnectSocket != null)
        {
            Debug.Log("发送心跳协议。");
            SendSave((byte)MessageConvention.heartBeat, new byte[] { }, false);
        }
        else
        {
            Debug.Log("心跳检测：Socket已断线");
            CloseHeartbeat();
        }
    }
    public void GetBeatTime()
    {
        SendSave((byte)MessageConvention.getHeartBeatTime, new byte[] { }, false);
    }

    #endregion

    #region 接收数据处理

    /// <summary>
    /// 判断并通知事件回调
    /// </summary>
    /// <param name="buff"></param>
    private void DealXieYi(MessageXieYi xieyi, AsyncUserToken userToken)
    {
        //Debug.LogError("处理协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
        byte[] tempMessageContent = xieyi.MessageContent;
        string messageInfo = "";
        ErrorType error = ErrorType.none;
        RoomActor actor = null;
        RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
        //if (tempMessageContent.Length > 200)
        //{
        //    Debug.Log((MessageConvention)xieyi.XieYiFirstFlag + "单次接收数据超过200/" + tempMessageContent.Length);
        //}
        try
        {
            //处理数值到DataController
            switch ((MessageConvention)xieyi.XieYiFirstFlag)
            {
                case MessageConvention.error:
                    break;
                case MessageConvention.login:
                    error = ClassGroup.CheckIsError(xieyi);
                    if (error == ErrorType.none)
                    {
                        actor = SerializeHelper.Deserialize<RoomActor>(xieyi.MessageContent);
                        DataController.instance.myInfo = actor;
                    }
                    break;
                case MessageConvention.getHeartBeatTime:
                    HeartbeatTime beatTime = SerializeHelper.Deserialize<HeartbeatTime>(xieyi.MessageContent);
                    heartbeatSecondTime = beatTime.time - 1;//-1防止和服务器心跳时间一致的时候会导致偏差
                    //Debug.Log("心跳间隔：" + heartbeatSecondTime);
                    break;
                case MessageConvention.reConnectCheck:
                    break;
                case MessageConvention.reConnectIndex:
                    int index = int.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
                    GameManager.instance.reConnectIndex = index;
                    break;
                case MessageConvention.heartBeat:
                    break;
                case MessageConvention.updateName:
                    actor = SerializeHelper.Deserialize<RoomActor>(xieyi.MessageContent);
                    DataController.instance.myInfo = actor;
                    break;
                case MessageConvention.createRoom:
                case MessageConvention.joinRoom:
                    JoinRoom joinInfo = new JoinRoom();
                    joinInfo = SerializeHelper.Deserialize<JoinRoom>(tempMessageContent);
                    DataController.instance.MyLocateIndex = joinInfo.unique;
                    break;
                case MessageConvention.updateRoom:
                    //Debug.Log((MessageConvention)xieyi.XieYiFirstFlag + "数据长度：" + xieyi.MessageContent.Length);
                    error = ClassGroup.CheckIsError(xieyi);
                    if (error == ErrorType.none)
                    {
                        DataController.instance.MyRoomInfo = SerializeHelper.Deserialize<RoomInfo>(tempMessageContent);
                    }
                    break;
                case MessageConvention.getRoomInfo:
                    DataController.instance.MyRoomInfo = SerializeHelper.Deserialize<RoomInfo>(tempMessageContent);
                    Debug.LogError("得到房间号:" + DataController.instance.MyRoomInfo.RoomID);
                    break;
                case MessageConvention.quitRoom:
                    QuitInfo qInfo = SerializeHelper.Deserialize<QuitInfo>(xieyi.MessageContent);
                    if (qInfo.isQuit)
                    {
                        DataController.instance.MyRoomInfo = null;
                        DataController.instance.ActorList = null;
                    }
                    break;
                case MessageConvention.getRoommateInfo:
                    List<RoomActor> rActors = SerializeHelper.Deserialize<List<RoomActor>>(tempMessageContent);
                    for (int i = 0; i < rActors.Count; i++)
                    {
                        if (DataController.instance.ActorList == null)
                        {
                            DataController.instance.ActorList = new Dictionary<int, RoomActor>();
                        }
                        lock (DataController.instance.ActorList)
                        {
                            if (!DataController.instance.ActorList.ContainsKey(rActors[i].UniqueID))
                            {
                                DataController.instance.ActorList.Add(rActors[i].UniqueID, null);
                            }
                            DataController.instance.ActorList[rActors[i].UniqueID] = rActors[i];
                        }
                    }
                    Debug.Log("得到房间人物列表。");
                    break;
                case MessageConvention.rotateDirection:

                    break;
                case MessageConvention.updateActorAnimation:
                    messageInfo = SerializeHelper.ConvertToString(xieyi.MessageContent);
                    ActorNetAnimation getNetAnimation = new ActorNetAnimation();
                    getNetAnimation.SetSendInfo(messageInfo);
                    //if (GameManager.instance.memberGroup.ContainsKey(getNetAnimation.userIndex))
                    //{
                    //    if (GameManager.instance.memberGroup[getNetAnimation.userIndex] != null)
                    //    {
                    //        //此处需要修改
                    //        //GameManager.instance.memberGroup[getNetAnimation.userIndex].NetAnimation = getNetAnimation;
                    //    }
                    //    //else if (getNetAnimation.userIndex == DataController.instance.myRoomInfo.MyLocateIndex)//服务器给我设置了
                    //    //{
                    //    //    MyController.instance.InitNetSaveInfo(null, null, getNetAnimation);
                    //    //}
                    //}
                    break;
                case MessageConvention.updateActorState:
                    messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                    roomActorUpdate.SetSendInfo(messageInfo);
                    //Debug.Log("更新用户->" + roomActorUpdate.userIndex + " 状态为:" + (RoomActorState)int.Parse(roomActorUpdate.update));
                    lock (DataController.instance.ActorList)
                    {
                        DataController.instance.ActorList[roomActorUpdate.userIndex].CurState = (RoomActorState)int.Parse(roomActorUpdate.update);
                    }
                    break;
                case MessageConvention.prepareLocalModel:
                    messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                    roomActorUpdate.SetSendInfo(messageInfo);
                    break;
                case MessageConvention.updateModelInfo:

                    break;
                case MessageConvention.getPreGameData:
                    //Debug.Log("getPreGameData已收到。");
                    break;
                case MessageConvention.startGaming:
                    string time = SerializeHelper.ConvertToString(tempMessageContent);
                    Debug.Log("开始游戏时间：" + time);
                    startGamTime = DateTime.Parse(time);
                    break;
                case MessageConvention.shootBullet:
                    break;
                case MessageConvention.bulletInfo:
                    break;
                case MessageConvention.endGaming:
                    messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                    Debug.Log("胜利队伍是：" + (TeamType)int.Parse(messageInfo));
                    break;
                case MessageConvention.moveDirection://GameManager中处理帧同步相应协议

                    break;
                case MessageConvention.frameData:

                    break;
                default:
                    Debug.LogError("没有协议：" + xieyi.XieYiFirstFlag + "/MesageLength:" + xieyi.MessageContentLength);
                    break;
            }

            //在数据处理后再执行委托响应脚本
            lock (allHandle)
            {
                if (allHandle.ContainsKey((MessageConvention)xieyi.XieYiFirstFlag))
                {
                    allHandle[(MessageConvention)xieyi.XieYiFirstFlag](xieyi);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("处理协议错误：" + e.Message + "/协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
        }
    }



    #endregion







    #region 登录逻辑


    private Register LoginInfo()
    {
        Register login = new Register()
        {
            userID = DataController.instance.UserID,
            password = DataController.instance.Password,
        };
        //if (DataController.instance.MyRoomInfo != null)
        //{
        //    login.roomID = DataController.instance.MyRoomInfo.RoomID;
        //}
        //else
        //{
        //    login.roomID = -1;
        //}
        return login;
    }

    //账号登录
    public void Login()
    {
        if (MyUserToken == null || MyUserToken.ConnectSocket.Connected == false)
        {
            Init(GetSocketBack);
        }
    }
    private void SendLogin()
    {
        Register login = LoginInfo();
        byte[] message = SerializeHelper.Serialize<Register>(login);
        SendSave((byte)MessageConvention.login, message);
    }

    #endregion
    private void GetSocketBack(SocketError error)
    {
        string info = "";
        switch (error)
        {
            case SocketError.Success:
                info = "连接服务器成功。";
                SendLogin();
                break;
            case SocketError.ConnectionRefused:
                info = "服务器主动拒绝本次请求。";
                break;
            default:
                info = error.ToString();
                break;
        }
        UIManager.instance.ShowAlertTip(info);
    }
    public void QuitUser()
    {
        DisConnect();
        MyUserToken = null;
    }

    public void DisConnect()
    {
        MyUserToken.ConnectSocket.Shutdown(SocketShutdown.Both);
    }
    public void Log4Debug(string msg)
    {
        Debug.LogError(msg);
    }

    #region IDisposable Members

    // Disposes the instance of SocketClient.
    public void Dispose()
    {
        if (autoConnectEvent != null)
        {
            autoConnectEvent.Close();
        }
        if (MyUserToken != null && MyUserToken.ConnectSocket != null)
        {
            if (MyUserToken.ConnectSocket.Connected)
            {
                MyUserToken.ConnectSocket.Shutdown(SocketShutdown.Both);
                MyUserToken.ConnectSocket.Close();
            }
        }
    }

    #endregion


}
