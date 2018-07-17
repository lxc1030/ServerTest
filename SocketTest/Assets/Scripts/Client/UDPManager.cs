using Network_Kcp;
using System;
using System.Collections.Generic;
using System.Net;
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

    public bool IsConnect;

    public KCPPlayer kcpClient;
    IPEndPoint remoteIpep;
    public const string ip = "192.168.1.110";
    public const int portUDP = 12000;
    public const int udpResend = 10;

    public void Awake()
    {
        instance = this;
        instance.Init();
    }
    void Update()
    {
        if (kcpClient != null)
        {
            kcpClient.OnUpdate();
        }
    }
    private void FixedUpdate()
    {
        //if (kcpClient != null)
        //{
        //    kcpClient.OnFixedUpdate();
        //}
    }

    private void OnApplicationQuit()
    {
        Dispose();
    }
    private void Dispose()
    {
        try
        {
            kcpClient.Dispose();
            NetworkDebuger.EnableSave = false;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    void Init()
    {
        NetworkDebuger.IsUnity = true;
        NetworkDebuger.EnableLog = true;
        NetworkDebuger.EnableSave = true;
        NetworkDebuger.Log("Awake()");

    }

    public void ConnectedToServer()
    {
        //获取UdpClient的发送端口
        //k_Socket = new KCPSocket(12345, 1, AddressFamily.InterNetwork);
        //IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse(Network.player.ipAddress), 12000);
        IPEndPoint localIpep = new IPEndPoint(IPAddress.Any, 0);
        remoteIpep = new IPEndPoint(IPAddress.Parse("192.168.1.110"), 12000);
        kcpClient = new KCPPlayer();
        //p2.Init("Player", IPAddress.Parse(Network.player.ipAddress), 12345, 12000);
        kcpClient.Init(localIpep, remoteIpep, OnReceive);

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
        SendMessage(sendBuffer);

    }


    public void SendSave(byte xieYiFirstFlage, byte[] message)
    {
        Debug.LogError("发送协议：" + (MessageConvention)xieYiFirstFlage);
        if (GameManager.instance.CurrentPlayType == FramePlayType.断线重连)
        {
            return;
        }
        MessageXieYi msgxy = new MessageXieYi(xieYiFirstFlage, 0, message);
        byte[] sendBuffer = msgxy.ToBytes();
        SendMessage(sendBuffer);
    }
    public void SendMessage(byte[] message)
    {
        kcpClient.SendMessage(remoteIpep, message);
    }




    private void OnReceive(byte[] buffer, int size, IPEndPoint remotePoint)
    {


        byte[] sendBytes = SelectMessage(buffer, remotePoint);
        if (sendBytes != null)
        {
            kcpClient.SendMessage(remotePoint, sendBytes);
        }
    }
    private byte[] SelectMessage(byte[] data, EndPoint endPoint)
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
            string guiInfo2 = "udp： ";
            GUIStyle bb = new GUIStyle();
            bb.normal.background = null;    //这是设置背景填充的
            bb.normal.textColor = Color.blue;   //设置字体颜色的
            bb.fontSize = 40;       //当然，这是字体大小
            GUI.Label(new Rect(0, 50, 200, 200), guiInfo2, bb);
        }
    }

}

