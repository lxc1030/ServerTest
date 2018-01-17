using NetFrame.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    /// 处理线程通过这个队列知道有数据需要处理
    /// </summary>
    /// <typeparam name="ConnCache"></typeparam>
    /// <param name=""></param>
    /// <returns></returns>
    Queue<ConnCache> tokenQueue;




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
    public bool isConnected = false;





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

    Thread dealReceive = null;


    public void Init(Action<SocketError> callback = null)
    {
        hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
        _Socket = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        //SocketError error = Connect(Login);
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
        isConnected = (e.SocketError == SocketError.Success);
        if (isConnected)
        {
            Debug.Log("Socket连接成功");

            MyUserToken = new AsyncUserToken(bufferSize);
            MyUserToken.ConnectSocket = _Socket;

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

                ConnCache connCache = new ConnCache(copy, userToken);
                //处理线程
                ThreadPool.QueueUserWorkItem(new WaitCallback(AnalyzeThrd), connCache);

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
    /// <summary>
    /// 线程处理接收事件
    /// </summary>
    /// <param name="state"></param>
    private void AnalyzeThrd(object state)
    {
        ConnCache connCache = (ConnCache)state;
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
                    if (buffer[0] != MessageXieYi.markStart)
                    {
                        break;
                    }
                    MessageXieYi xieyi = MessageXieYi.FromBytes(buffer);
                    if (xieyi == null)
                    {
                        Log4Debug("奇怪为什么协议为空");
                        break;
                    }
                    int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                    buffer = buffer.Skip(messageLength).ToArray();
                    //将数据包交给前台去处理
                    DealXieYi(xieyi, userToken);
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
            isConnected = false;
            Log4Debug("发送未成功，回调：" + e.SocketError);
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
        if (isConnected)
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
                    Debug.Log("重连帧：" + index);
                    break;
                case MessageConvention.heartBeat:
                    break;
                case MessageConvention.updateName:
                    actor = SerializeHelper.Deserialize<RoomActor>(xieyi.MessageContent);
                    DataController.instance.myInfo = actor;
                    break;
                case MessageConvention.createRoom:
                case MessageConvention.joinRoom:
                    DataController.instance.MyLocateIndex = int.Parse(SerializeHelper.ConvertToString(tempMessageContent));
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
                    break;
                case MessageConvention.quitRoom:
                    break;
                case MessageConvention.getRoommateInfo:
                    error = ClassGroup.CheckIsError(xieyi);
                    if (error != ErrorType.none)
                    {
                        Debug.LogError(error);
                    }
                    else
                    {
                        List<RoomActor> rActors = SerializeHelper.Deserialize<List<RoomActor>>(tempMessageContent);
                        for (int i = 0; i < rActors.Count; i++)
                        {
                            if (DataController.instance.ActorList == null)
                            {
                                DataController.instance.ActorList = new Dictionary<int, RoomActor>();
                            }
                            if (!DataController.instance.ActorList.ContainsKey(rActors[i].UniqueID))
                            {
                                DataController.instance.ActorList.Add(rActors[i].UniqueID, null);
                            }
                            DataController.instance.ActorList[rActors[i].UniqueID] = rActors[i];
                        }
                    }
                    break;
                case MessageConvention.rotateDirection:

                    break;
                case MessageConvention.updateActorAnimation:
                    messageInfo = SerializeHelper.ConvertToString(xieyi.MessageContent);
                    ActorNetAnimation getNetAnimation = new ActorNetAnimation();
                    getNetAnimation.SetSendInfo(messageInfo);
                    if (GameManager.instance.memberGroup.ContainsKey(getNetAnimation.userIndex))
                    {
                        if (GameManager.instance.memberGroup[getNetAnimation.userIndex] != null)
                        {
                            //此处需要修改
                            //GameManager.instance.memberGroup[getNetAnimation.userIndex].NetAnimation = getNetAnimation;
                        }
                        //else if (getNetAnimation.userIndex == DataController.instance.myRoomInfo.MyLocateIndex)//服务器给我设置了
                        //{
                        //    MyController.instance.InitNetSaveInfo(null, null, getNetAnimation);
                        //}
                    }
                    break;
                case MessageConvention.updateActorState:
                    messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                    roomActorUpdate.SetSendInfo(messageInfo);
                    //Debug.Log("更新用户->" + roomActorUpdate.userIndex + " 状态为:" + (RoomActorState)int.Parse(roomActorUpdate.update));
                    DataController.instance.ActorList[roomActorUpdate.userIndex].CurState = (RoomActorState)int.Parse(roomActorUpdate.update);
                    break;
                case MessageConvention.prepareLocalModel:
                    //int waitSecond = int.Parse(SerializeHelper.ConvertToString(tempMessageContent));
                    //DataController.instance.myRoomInfo.CountDownTime = waitSecond / 1000;
                    messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                    roomActorUpdate.SetSendInfo(messageInfo);
                    //Debug.Log("用户站位：" + roomActorUpdate.userIndex + "准备进度：" + roomActorUpdate.update + "%");
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
                    DataController.instance.MyRoomInfo.CurState = RoomActorState.Gaming;
                    break;
                case MessageConvention.gamingTime:
                    messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                    DataController.instance.MyRoomInfo.GameTime = int.Parse(messageInfo) / 1000;
                    //Debug.LogError("游戏时间：" + DataController.instance.myRoomInfo.GameTime);
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
                    List<FrameInfo> fInfos = null;
                    try
                    {
                        fInfos = SerializeHelper.Deserialize<List<FrameInfo>>(tempMessageContent);
                    }
                    catch (Exception e)
                    {
                        string frameError = "帧出错：" + e.Message + "：" + tempMessageContent.Length + "\n";
                        for (int i = 0; i < tempMessageContent.Length; i++)
                        {
                            frameError += tempMessageContent[i] + ",";
                        }
                        Debug.LogError(frameError);
                        return;
                    }
                    if (fInfos == null)
                    {
                        Debug.LogError("请检查，该逻辑不能为空。");
                    }
                    for (int i = 0; i < fInfos.Count; i++)
                    {
                        FrameInfo fInfo = fInfos[i];
                        if (fInfos == null)
                        {
                            Debug.LogError("解析后的数据有空值：" + fInfo.frameIndex);
                        }
                        lock (GameManager.instance.FrameInfos)
                        {
                            if (!GameManager.instance.FrameInfos.ContainsKey(fInfo.frameIndex))
                            {
                                GameManager.instance.FrameInfos.Add(fInfo.frameIndex, fInfo);
                                //Debug.Log("成功保存帧：" + fInfo.frameIndex);
                                while (GameManager.instance.FrameInfos.ContainsKey(DataController.instance.FrameCanIndex + 1))
                                {
                                    DataController.instance.FrameCanIndex++;
                                }
                            }
                            else
                            {
                                //Debug.Log("重复保存帧数据：" + fInfo.frameIndex);
                            }
                        }
                    }
                    break;
                default:
                    Debug.LogError("没有协议：" + xieyi.XieYiFirstFlag + "/MesageLength:" + xieyi.MessageContentLength);
                    break;
            }

            //在数据处理后再执行委托响应脚本
            if (allHandle.ContainsKey((MessageConvention)xieyi.XieYiFirstFlag))
            {
                allHandle[(MessageConvention)xieyi.XieYiFirstFlag](xieyi);
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
        if (!isConnected || MyUserToken.ConnectSocket == null)
        {
            Init(GetSocketBack);
        }
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

    public void DisConnect()
    {
        isConnected = false;
        MyUserToken.ConnectSocket.Shutdown(SocketShutdown.Both);
        MyUserToken.ConnectSocket = null;
    }
    public void Log4Debug(string msg)
    {
        Debug.LogError(msg);
    }

    #region IDisposable Members

    // Disposes the instance of SocketClient.
    public void Dispose()
    {
        if (dealReceive != null)
        {
            dealReceive.Abort();
        }
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
