﻿using NetFrame.Net;
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
    /// 用于每个I/O Socket操作的缓冲区大小 默认1024
    /// </summary>
    private int BufferSize = 1024;

    // Listener endpoint.
    private IPEndPoint hostEndPoint;

    // Signals a connection.
    private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);

    private static Thread handleReceive = null;



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
        //
        MyUserToken = new AsyncUserToken(BufferSize);

    }
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
    private void OnDisable()
    {
        Dispose();
    }


    #region 初始化
    public void Init(Action<SocketError> callback = null)
    {
        hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
        MyUserToken.ConnectSocket = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //SocketError error = Connect(Login);
        SocketError error = Connect();
        if (callback != null)
        {
            callback(error);
        }
    }
    public void DisConnect()
    {
        isConnected = false;
        MyUserToken.ConnectSocket.Shutdown(SocketShutdown.Both);
        MyUserToken.ConnectSocket = null;
    }

    /// <summary>
    /// 连接到主机
    /// </summary>
    /// <returns>0.连接成功, 其他值失败,参考SocketError的值列表</returns>
    internal SocketError Connect()
    {
        SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        connectArgs.UserToken = MyUserToken.ConnectSocket;
        connectArgs.RemoteEndPoint = hostEndPoint;
        connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);
        //connectArgs.Completed += (SocketAsyncEventArgs, Action) =>
        //{
        //    OnConnect(callback, connectArgs);
        //};
        MyUserToken.ConnectSocket.ConnectAsync(connectArgs);
        autoConnectEvent.WaitOne();

        return connectArgs.SocketError;
    }

    // Calback for connect operation
    private void OnConnect(object sender, SocketAsyncEventArgs e)
    {
        // Signals the end of connection.
        autoConnectEvent.Set(); //释放阻塞.
        // Set the flag for socket connected.
        isConnected = (e.SocketError == SocketError.Success);
        if (isConnected)
        {
            Debug.Log("Socket连接成功");
            ProcessReceive(MyUserToken);
            //((Action)sender)();
        }
        else
        {
            Debug.LogError("Socket连接失败:" + e.SocketError);
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
        Debug.Log("开始心跳检测" + DateTime.Now);
        if (isConnected)
        {
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

    #region 发送数据

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
        SendSave(sendBuffer);
    }

    /// <summary>
    /// 存储要发送的消息并判断是否发送
    /// </summary>
    /// <param name="data"></param>
    private void SendSave(byte[] data)
    {
        AsyncUserToken userToken = MyUserToken;

        string INFO = "保存待发送:";
        for (int i = 0; i < data.Length; i++)
        {
            INFO += "_" + data[i];
        }
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


    /// <summary>
    /// 同步发送方法--同步方法适用于等待回复的功能上
    /// </summary>
    /// <param name="e"></param>
    /// <param name="timeout">同步发送的延迟毫秒</param>
    //http://www.cnblogs.com/luofuxian/archive/2012/03/06/2382147.html
    //http://www.cnblogs.com/tianzhiliang/archive/2011/03/02/1969187.html
    private void Send(AsyncUserToken e, byte[] data)
    {
        AsyncUserToken userToken = e;
        try
        {
            if (!userToken.ConnectSocket.Connected)
            {
                return;
            }
            userToken.ConnectSocket.SendTimeout = 0;
            //int startTickCount = Environment.TickCount;

            try
            {
                userToken.ConnectSocket.Send(data, data.Length, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                if (
                    ex.SocketErrorCode == SocketError.WouldBlock
                    || ex.SocketErrorCode == SocketError.IOPending
                    || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable
                    )
                {
                    // socket buffer is probably full, wait and try again
                    Thread.Sleep(30);
                    //
                    Debug.LogError("发送错误？");
                }
                else
                {
                    Debug.LogError("发送错误：" + ex.Message);
                    /*throw ex;*/ // any serious error occurr
                }
            }
        }
        catch (Exception error)
        {
            Debug.LogError("为什么不能发送：" + error.Message);
        }
    }

    //private void Send(AsyncUserToken userToken)
    //{
    //    Socket socket = userToken.ConnectSocket;
    //    if (socket != null)
    //    {
    //        if (!socket.Connected)
    //        {
    //            instance.DisConnect();
    //            SocketConnectUI.instance.OffLine();
    //            return;
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError("Socket清空以后请求了发送，不处理。");
    //        return;
    //    }
    //    //开始发送
    //    socket.SendTimeout = 10000;//设置发送后判断超时的时长
    //    while (userToken.SendBuffer.Count > 0)
    //    {
    //        byte[] buffer = null;
    //        lock (userToken.SendBuffer)
    //        {
    //            buffer = userToken.SendBuffer.ToArray();
    //            userToken.ClearSend();
    //        }

    //        int startTickCount = Environment.TickCount;
    //        int sent = 0; // how many bytes is already sent
    //        do
    //        {
    //            try
    //            {
    //                sent += socket.Send(buffer, sent, buffer.Length - sent, SocketFlags.None);
    //            }
    //            catch (SocketException ex)
    //            {
    //                Log4Debug("sendError:" + ex.SocketErrorCode);
    //                //
    //                if (ex.SocketErrorCode == SocketError.WouldBlock ||
    //                ex.SocketErrorCode == SocketError.IOPending ||
    //                ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
    //                {
    //                    // socket buffer is probably full, wait and try again
    //                    Thread.Sleep(30);
    //                    Log4Debug("睡眠30秒后继续发送。");
    //                }
    //                else
    //                {
    //                    if (ex.SocketErrorCode == SocketError.ConnectionAborted)//您的主机中的软件中止了一个已建立的连接。
    //                    {
    //                        return;
    //                    }
    //                    //throw ex; // any serious error occurr
    //                    Log4Debug("send错误：" + ex.Message);
    //                }
    //            }
    //        } while (sent < buffer.Length);
    //        //Log4Debug("发送用时毫秒：" + (Environment.TickCount - startTickCount));
    //    }
    //    userToken.isDealSend = false;
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
                //Log4Debug(sendIfo);




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
    private void ProcessReceive(AsyncUserToken e)
    {
        AsyncUserToken userToken = e as AsyncUserToken;
        Socket socket = userToken.ConnectSocket;
        //

        socket.BeginReceive(userToken.AsyncReceiveBuffer, 0, userToken.AsyncReceiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), userToken);
    }

    public float kb;
    public int zijie;
    private void ReceiveCallback(IAsyncResult ar)
    {
        AsyncUserToken userToken = (AsyncUserToken)ar.AsyncState;
        Socket socket = userToken.ConnectSocket;

        //lock (userToken.ReceiveBuffer)
        {
            //从远程设备读取数据
            int read = socket.EndReceive(ar);
            if (read > 0)
            {
                zijie += read;
                kb = zijie / 1024;
                //
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
                Log4Debug("接收数据：" + info);



                while (buffer.Length > 0)
                {
                    if (buffer[0] == MessageOperation.markStart)//检查是否是传输标头
                    {
                        MessageOperation oper = MessageOperation.FromBytes(buffer);
                        if (oper != null)
                        {
                            lock (userToken.outOrders)
                            {
                                userToken.outOrders.Add(oper.GetId(), oper.Message);
                            }
                            buffer = buffer.Skip(MessageOperation.lengthID + MessageOperation.lengthLength + oper.Message.Length).ToArray();
                        }
                        else//是断包前面部分
                        {
                            if (userToken.halfReceiveMessage.Length == 0)
                            {
                                userToken.halfReceiveMessage = new byte[buffer.Length];
                                buffer.CopyTo(userToken.halfReceiveMessage, 0);
                                DealHalf(userToken);
                            }
                            else
                            {
                                Log4Debug("断包前数据保存时，存储区有数值。");
                            }
                            break;
                        }
                    }
                    else//是断包后面部分
                    {
                        if (userToken.halfReceiveMessage.Length == 0)
                        {
                            Log4Debug("断包后数据保存时，存储区无数值。");
                        }
                        else
                        {
                            int halfLength = userToken.halfReceiveMessage.Length;
                            byte[] mix = userToken.halfReceiveMessage;
                            userToken.halfReceiveMessage = new byte[halfLength + buffer.Length];
                            mix.CopyTo(userToken.halfReceiveMessage, 0);
                            Array.Copy(buffer, 0, userToken.halfReceiveMessage, halfLength, buffer.Length);
                            DealHalf(userToken);
                        }
                        break;
                    }
                }


                //byte[] intBuff = new byte[4] { buffer[0], buffer[1], buffer[2], buffer[3] };
                //int index = BitConverter.ToInt32(intBuff, 0);           // 从字节数组转换成 int
                //byte[] dealBuffer = new byte[buffer.Length - intBuff.Length];
                //Array.Copy(buffer, intBuff.Length, dealBuffer, 0, dealBuffer.Length);

                //lock (userToken.outOrders)
                //{
                //    userToken.outOrders.Add(index, dealBuffer);
                //}
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
                isConnected = false;
                MyUserToken.ConnectSocket.Shutdown(SocketShutdown.Both);
                Debug.LogError("接收数据小于等于0" + socket.SocketType);
            }
        }
    }





    private void DealHalf(AsyncUserToken userToken)
    {

    }







    public void Handle(object obj)
    {
        AsyncUserToken userToken = (AsyncUserToken)obj;
        while (true)
        {
            if (userToken.outOrders.ContainsKey(userToken.receiveIndex))
            {
                Debug.LogError("准备处理序号：" + userToken.receiveIndex);
                byte[] buffer = userToken.outOrders[userToken.receiveIndex];
                lock (userToken.outOrders)
                {
                    userToken.outOrders.Remove(userToken.receiveIndex);
                }
                byte[] mix = new byte[userToken.halfReceiveMessage.Length + buffer.Length];
                userToken.halfReceiveMessage.CopyTo(mix, 0);
                Array.Copy(buffer, 0, mix, userToken.halfReceiveMessage.Length, buffer.Length);
                userToken.halfReceiveMessage = new byte[] { };
                userToken.receiveIndex++;

                while (mix.Length > 0)
                {
                    MessageXieYi xieyi = MessageXieYi.FromBytes(mix);
                    if (xieyi != null)
                    {
                        int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                        Log4Debug("--处理协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
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
                        Debug.LogError("剩余未处理数据：" + info);
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError("不包含该序号：" + userToken.receiveIndex);
                break;
            }
        }
        userToken.isDealReceive = false;






        //while (userToken.ReceiveBuffer.Count > 0)
        //{
        //    byte[] mix = new byte[userToken.halfReceiveMessage.Length + userToken.ReceiveBuffer.Count];
        //    Array.Copy(userToken.halfReceiveMessage, 0, mix, 0, userToken.halfReceiveMessage.Length);

        //    Array.Copy(userToken.ReceiveBuffer.ToArray(), 0, mix, userToken.halfReceiveMessage.Length, userToken.ReceiveBuffer.Count);
        //    userToken.ClearReceive();

        //    do
        //    {
        //        MessageXieYi xieyi = MessageXieYi.FromBytes(mix);
        //        if (xieyi != null)
        //        {
        //            int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
        //            DoReceiveEvent(xieyi);
        //            mix = mix.Skip(messageLength).ToArray();
        //        }
        //        else
        //        {
        //            string info = "sy:";
        //            for (int i = 0; i < mix.Length; i++)
        //            {
        //                info += mix[i] + ",";
        //            }
        //            Debug.LogError("剩余未处理数据长度：" + mix.Length + "当前帧：" + GameManager.instance.frameIndex + "/" + DataController.instance.MyRoomInfo.FrameIndex + info);
        //            break;
        //        }
        //    } while (mix.Length > 0);
        //    userToken.halfReceiveMessage = new byte[mix.Length];
        //    userToken.halfReceiveMessage = mix;//保存未处理的数据长度
        //}
        //userToken.isDealReceive = false;
    }
    private void DealReceive(MessageXieYi xieyi, AsyncUserToken userToken)
    {
        DoReceiveEvent(xieyi);
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

    public DateTime startGamTime;
    #endregion

    public string log = "";

    #region 接收数据处理
    /// <summary>
    /// 判断并通知事件回调
    /// </summary>
    /// <param name="buff"></param>
    private void DoReceiveEvent(System.Object obj)
    {
        MessageXieYi xieyi = (MessageXieYi)obj;
        log += xieyi.XieYiFirstFlag + ",";
        //Debug.LogError("处理协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
        byte[] tempMessageContent = xieyi.MessageContent;
        string messageInfo = "";
        ErrorType error = ErrorType.none;
        RoomActor actor = null;
        RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
        if (tempMessageContent.Length > 200)
        {
            Debug.Log((MessageConvention)xieyi.XieYiFirstFlag + "单次接收数据超过200/" + tempMessageContent.Length);
        }
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
                break;
            case MessageConvention.reConnect:
                break;
            case MessageConvention.heartBeat:
                break;
            case MessageConvention.updateName:
                actor = SerializeHelper.Deserialize<RoomActor>(xieyi.MessageContent);
                DataController.instance.myInfo = actor;
                break;
            case MessageConvention.createRoom:
            case MessageConvention.joinRoom:
            case MessageConvention.updateRoom:
                Debug.Log((MessageConvention)xieyi.XieYiFirstFlag + "数据长度：" + xieyi.MessageContent.Length);
                error = ClassGroup.CheckIsError(xieyi);
                if (error == ErrorType.none)
                {
                    DataController.instance.MyRoomInfo = SerializeHelper.Deserialize<RoomInfo>(tempMessageContent);
                }
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
                        if (DataController.instance.MyRoomInfo.ActorList == null)
                        {
                            DataController.instance.MyRoomInfo.ActorList = new Dictionary<int, RoomActor>();
                        }
                        if (!DataController.instance.MyRoomInfo.ActorList.ContainsKey(rActors[i].UniqueID))
                        {
                            DataController.instance.MyRoomInfo.ActorList.Add(rActors[i].UniqueID, null);
                        }
                        DataController.instance.MyRoomInfo.ActorList[rActors[i].UniqueID] = rActors[i];
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
                Debug.Log("更新用户->" + roomActorUpdate.userIndex + " 状态为:" + (RoomActorState)int.Parse(roomActorUpdate.update));
                DataController.instance.MyRoomInfo.ActorList[roomActorUpdate.userIndex].CurState = (RoomActorState)int.Parse(roomActorUpdate.update);
                break;
            case MessageConvention.prepareLocalModel:
                //int waitSecond = int.Parse(SerializeHelper.ConvertToString(tempMessageContent));
                //DataController.instance.myRoomInfo.CountDownTime = waitSecond / 1000;
                messageInfo = SerializeHelper.ConvertToString(tempMessageContent);
                roomActorUpdate.SetSendInfo(messageInfo);
                Debug.Log("用户站位：" + roomActorUpdate.userIndex + "准备进度：" + roomActorUpdate.update + "%");
                break;
            case MessageConvention.updateModelInfo:

                break;
            case MessageConvention.getPreGameData:
                Debug.LogError("getPreGameData已收到。");
                break;
            case MessageConvention.startGaming:
                string time = SerializeHelper.ConvertToString(tempMessageContent);
                Debug.LogError("开始游戏时间：" + time);
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
                //FrameInfo fInfo = SerializeHelper.Deserialize<FrameInfo>(tempMessageContent);
                //if (DataController.instance.myRoomInfo.FrameIndex < fInfo.frameIndex)
                //{
                //    DataController.instance.myRoomInfo.FrameIndex = fInfo.frameIndex;
                //    if (fInfo.frameData != null)
                //    {
                //        Debug.LogError("frameDataCount:" + fInfo.frameData.Count);
                //        for (int i = 0; i < fInfo.frameData.Count; i++)
                //        {
                //            MessageXieYi frameXY = MessageXieYi.FromBytes(fInfo.frameData[i], true);
                //            DoReceiveEvent(frameXY);
                //        }
                //    }
                //}
                //else
                //{
                //    //Debug.LogError("已重复请求同一帧域。");
                //}
                List<FrameInfo> fInfos = null;
                try
                {
                    fInfos = SerializeHelper.Deserialize<List<FrameInfo>>(tempMessageContent);
                }
                catch (Exception e)
                {
                    Debug.LogError("帧出错：" + e.Message);
                }
                if (fInfos == null)
                {
                    Debug.LogError("请检查，该逻辑不能为空。" + DataController.instance.MyRoomInfo.FrameIndex);
                }
                for (int i = 0; i < fInfos.Count; i++)
                {
                    FrameInfo fInfo = fInfos[i];
                    if (fInfos == null)
                    {
                        Debug.LogError("解析后的数据有空值：" + fInfo.frameIndex);
                    }
                    if (!GameManager.instance.FrameInfos.ContainsKey(fInfo.frameIndex))
                    {
                        lock (GameManager.instance.FrameInfos)
                        {
                            GameManager.instance.FrameInfos.Add(fInfo.frameIndex, null);
                        }
                    }
                    else
                    {
                        Debug.Log("重复保存帧数据：" + fInfo.frameIndex);
                    }
                    lock (GameManager.instance.FrameInfos)
                    {
                        GameManager.instance.FrameInfos[fInfo.frameIndex] = fInfo;
                        //Debug.Log("成功保存帧：" + fInfo.frameIndex);
                    }
                    if (fInfo.frameIndex > DataController.instance.MyRoomInfo.FrameIndex)
                    {
                        DataController.instance.MyRoomInfo.FrameIndex = fInfo.frameIndex;
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



    #endregion



    #region 错误处理

    private string errorDetail = "";

    // Close socket in case of failure and throws
    // a SockeException according to the SocketError.
    private void ProcessError(AsyncUserToken e)
    {
        //Debug.LogError("检查什么导致了这个进程错误：" + errorDetail);
        return;
        AsyncUserToken userToken = e;
        Socket s = userToken.ConnectSocket;
        if (s.Connected)
        {
            // close the socket associated with the client
            try
            {
                s.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // throws if client process has already closed
            }
            finally
            {
                if (s.Connected)
                {
                    s.Close();
                }
                isConnected = false;
            }
        }
    }


    #endregion

    #region IDisposable Members

    // Disposes the instance of SocketClient.
    public void Dispose()
    {
        if (handleReceive != null)
        {
            handleReceive.Abort();
        }
        autoConnectEvent.Close();
        if (MyUserToken.ConnectSocket.Connected)
        {
            MyUserToken.ConnectSocket.Close();
        }
    }

    #endregion
    public void Log4Debug(string msg)
    {
        Debug.LogError(msg);
    }

}
