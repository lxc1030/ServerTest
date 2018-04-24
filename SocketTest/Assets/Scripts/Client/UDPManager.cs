using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPManager : MonoBehaviour
{
    public static UDPManager instance;

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

    public EndPoint myEndPoint;

    public const string ip = "192.168.1.110";
    public const int portUDP = 12000;

    public void Awake()
    {
        instance = this;
        instance.Init();
    }

    //以下默认都是私有的成员
    Socket socket; //目标socket
    EndPoint serverEnd; //服务端
    IPEndPoint ipEnd; //服务端端口
    Thread connectThread; //连接线程
    int receiveLength = 4096;

    public bool IsConnect;

    void Init()
    {
        IsConnect = false;
        //InitSocket(); //在这里初始化
    }

    //初始化
    public void InitSocket()
    {
        if (connectThread != null)
        {
            connectThread.Abort();
        }
        //定义连接的服务器ip和端口，可以是本机ip，局域网，互联网
        ipEnd = new IPEndPoint(IPAddress.Parse(ip), portUDP);
        //定义套接字类型,在主线程中定义
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //定义服务端
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        serverEnd = (EndPoint)sender;
        Debug.LogError("sending UDP dgram");

        //建立初始连接，这句非常重要，第一次连接初始化了serverEnd后面才能收到消息
        //Send(new byte[] { 1, 2, 3 });

        UDPLogin login = new UDPLogin()
        {
            login = DataController.instance.UserID,
            roomID = DataController.instance.MyRoomInfo.RoomID,
            unique = DataController.instance.MyLocateIndex
        };
        byte[] message = SerializeHelper.Serialize<UDPLogin>(login);
        //因为SendSave限制在断线重连时的重连，所以得绕过判断
        MessageXieYi msgxy = new MessageXieYi((byte)MessageConvention.setUDP, 0, message);
        byte[] sendBuffer = msgxy.ToBytes();
        Send(sendBuffer);

        //开启一个线程连接，必须的，否则主线程卡死
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
        connectThread.IsBackground = true;

    }



    //服务器接收
    void SocketReceive()
    {
        try
        {
            while (true)
            {
                //对data清零
                byte[] recvData = new byte[receiveLength];
                //定义客户端
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint clientEnd = (EndPoint)sender;

                //获取客户端，获取客户端数据，用引用给客户端赋值
                int recvLen = socket.ReceiveFrom(recvData, ref clientEnd);//接收的数据长度 
                                                
                //输出接收到的数据
                byte[] effectiveData = new byte[recvLen];
                Array.Copy(recvData, effectiveData, recvLen);

                object[] all = new object[] { clientEnd, effectiveData };
                ThreadPool.QueueUserWorkItem(new WaitCallback(XieYiThrd), all);

            }
        }
        catch (Exception e)
        {
            Debug.LogError("接收->" + e.Message);
        }
        finally
        {
            SocketQuit();
            IsConnect = false;
        }

        //try
        //{
        //    //进入接收循环
        //    while (true)
        //    {
        //        //对data清零
        //        recvData = new byte[receiveLength];
        //        //获取客户端，获取服务端端数据，用引用给服务端赋值，实际上服务端已经定义好并不需要赋值
        //        recvLen = socket.ReceiveFrom(recvData, ref serverEnd);
        //        guiInfo = recvLen + "";

        //        byte[] effectiveData = new byte[recvLen];
        //        Array.Copy(recvData, effectiveData, recvLen);
        //        byte[] backData = SelectMessage(effectiveData, myEndPoint);

        //        if (backData != null)
        //        {
        //            Send(backData);
        //        }

        //        ////打印服务端信息
        //        //Debug.LogError("message from: " + serverEnd.ToString());
        //        ////输出接收到的数据
        //        //recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
        //        //Debug.LogError("我是客户端，接收到服务器的数据" + recvStr);
        //    }
        //}
        //catch (Exception e)
        //{
        //    Debug.LogError(recvLen + "/" + e.Message);
        //}
        //finally
        //{
        //    SocketQuit();
        //    IsConnect = false;
        //}
    }
    private void XieYiThrd(object state)
    {
        object[] all = (object[])state;
        EndPoint clientEnd = (EndPoint)all[0];
        byte[] data = (byte[])all[1];
        //将数据包交给前台去处理
        byte[] backData = SelectMessage(data, clientEnd);
        //将接收到的数据经过处理再发送出去
        //string sendStr = "I~m Here. ";
        if (backData != null)
        {
            Send(backData);
        }
    }

    //连接关闭
    void SocketQuit()
    {
        //关闭线程
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        //最后关闭socket
        if (socket != null)
            socket.Close();
    }


    public void OnApplicationQuit()
    {
        SocketQuit();
    }
    private void Send(byte[] sendData)
    {
        if (sendData == null)
            return;
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, ipEnd);
        socket.BeginSendTo(sendData, 0, sendData.Length, SocketFlags.None, ipEnd, new AsyncCallback(SendCallback), socket);
    }

    public void SendSave(byte xieYiFirstFlage, byte[] message)
    {
        if (GameManager.instance.CurrentPlayType == FramePlayType.断线重连)
        {
            return;
        }
        MessageXieYi msgxy = new MessageXieYi(xieYiFirstFlage, 0, message);
        byte[] sendBuffer = msgxy.ToBytes();
        Send(sendBuffer);
    }

    private void SendCallback(IAsyncResult ar)
    {
        ((Socket)ar.AsyncState).EndSendTo(ar);
        //RaiseCompletedSend(null);
    }



    public byte[] SelectMessage(byte[] data, EndPoint endPoint)
    {
        byte[] backData = null;
        try
        {
            MessageXieYi xieyi = MessageXieYi.FromBytes(data);
            if (xieyi == null)
                return backData;

            byte[] tempMessageContent = xieyi.MessageContent;
            switch ((MessageConvention)xieyi.XieYiFirstFlag)
            {
                case MessageConvention.frameData:
                    DealFrameData(xieyi);
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
        catch
        {

        }
        return backData;
    }

    public void DealFrameData(MessageXieYi xieyi)
    {
        try
        {
            byte[] tempMessageContent = xieyi.MessageContent;

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
                UIManager.instance.ShowAlertTip(frameError);
                return;
            }

            if (fInfos == null)
            {
                Debug.LogError("请检查，该逻辑不能为空。");
            }

            lock (GameManager.instance.FrameInfos)
            {
                for (int i = 0; i < fInfos.Count; i++)
                {
                    FrameInfo fInfo = fInfos[i];
                    if (fInfo == null)
                    {
                        Debug.LogError("帧数据解析后有空值");
                    }
                    if (!GameManager.instance.FrameInfos.ContainsKey(fInfo.frameIndex))
                    {
                        GameManager.instance.FrameInfos.Add(fInfo.frameIndex, fInfo);
                        //Debug.Log("成功保存帧：" + fInfo.frameIndex);
                        if (GameManager.instance.frameMaxIndex < fInfo.frameIndex)
                        {
                            GameManager.instance.frameMaxIndex = fInfo.frameIndex;
                        }
                    }
                    else
                    {
                        //Debug.Log("重复保存帧数据：" + fInfo.frameIndex);
                    }
                }
            }
        }
        catch (Exception e)
        {

        }
    }


    string guiInfo = "000";
    public void OnGUI()
    {
        if (DataController.instance.MyRoomInfo != null && DataController.instance.ActorList != null)
        {
            string guiInfo2 = "udp： " + ((connectThread == null) ? "" : connectThread.IsAlive + "");
            GUIStyle bb = new GUIStyle();
            bb.normal.background = null;    //这是设置背景填充的
            bb.normal.textColor = Color.blue;   //设置字体颜色的
            bb.fontSize = 40;       //当然，这是字体大小
            GUI.Label(new Rect(0, 50, 200, 200), guiInfo2, bb);
        }
    }

}

