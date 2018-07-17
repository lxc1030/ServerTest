using Network_Kcp;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;

public class UdpServer
{
    private static UdpServer instance;
    public static UdpServer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UdpServer();
            }
            return instance;
        }
        set
        {
            value = instance;
        }
    }


    public static string ConnectInfo = "ConnectRequest";

    public ConcurrentDictionary<IPEndPoint, udpUser> ListUserKCP;

    private KCPPlayer kcpServer;

    public UdpServer()
    {
        ListUserKCP = new ConcurrentDictionary<IPEndPoint, udpUser>();
    }


    public void Init(string IP, int port)
    {
        //开启非可靠传输udp监听客户端连接
        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse(IP), port);

        kcpServer = new KCPPlayer();
        //p1.Init(localIpep, new IPEndPoint(IPAddress.Parse("192.168.1.111"), 12345));
        kcpServer.Init(localIpep, new IPEndPoint(IPAddress.Any, 0), OnReceive);

        Thread th = new Thread(SetUpdate);
        th.IsBackground = true;
        th.Start();

        Log4Debug("初始化UDP Socket成功。");
    }

    private void SetUpdate(object obj)
    {
        while (true)
        {
            kcpServer.OnFixedUpdate();
            kcpServer.OnUpdate();
            System.Threading.Thread.Sleep(10);
        }
    }

    private void OnReceive(byte[] buffer, int size, IPEndPoint remotePoint)
    {
        if (remotePoint.Address.Equals(IPAddress.Parse("192.168.1.129")))
        {
            return;
        }

        Log4Debug("OnReceive() " + remotePoint + "/长度->" + size);

        udpUser info = null;
        if (ListUserKCP.ContainsKey(remotePoint))
        {
            info = ListUserKCP[remotePoint];
        }
        else//配表没有该用户信息
        {
            info = ListUserKCP.GetOrAdd(remotePoint, new udpUser() { lastPoint = remotePoint, roomId = -1 });
        }

        byte[] sendBytes = SelectMessage(info, buffer);
        if (sendBytes != null)
        {
            SendMessage(info.lastPoint, sendBytes);
        }
    }
    public void SendMessage(IPEndPoint remotePoint, byte[] message)
    {
        if (remotePoint == null)
        {
            Log4Debug("向NULL地址发送长度{0}的数据" + message.Length);
            return;
        }
        kcpServer.SendMessage(remotePoint, message);
    }


    public byte[] SelectMessage(udpUser user, byte[] data)
    {
        byte[] newBuffer = null;
        MessageXieYi xieyi = MessageXieYi.FromBytes(data);
        if (xieyi == null)
            return newBuffer;

        byte[] tempMessageContent = xieyi.MessageContent;
        ActorMoveDirection moveDirection = null;
        SingleRoom room = null;
        UDPLogin login = null;

        //该处RoomList没有加锁
        if (ServerDataManager.instance.allRoom.RoomList.ContainsKey(user.roomId))
        {
            room = ServerDataManager.instance.allRoom.RoomList[user.roomId];
        }

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.setUDP:
                login = SerializeHelper.Deserialize<UDPLogin>(tempMessageContent);
                user.roomId = login.roomID;
                user.unique = login.unique;
                ListUserKCP.AddOrUpdate(user.lastPoint, user, (key, oldValue) => user);
                ServerDataManager.instance.allRoom.RoomList[user.roomId].UpdateUDP(user.unique, user);
                Log4Debug("UDP login 房间号：" + login.roomID);
                newBuffer = SerializeHelper.Serialize<UDPLogin>(login);
                break;
            case MessageConvention.moveDirection:
                moveDirection = SerializeHelper.Deserialize<ActorMoveDirection>(tempMessageContent);
                if (room.ActorList[moveDirection.userIndex].CurState != RoomActorState.Dead)
                {
                    //Log4Debug("将历史帧：" + moveDirection.frameIndex + "保存到" + (moveDirection.frameIndex + room.RoomInfo.frameInterval) + "/" + room.RoomInfo.FrameIndex);
                    room.SetRecondFrame(xieyi.ToBytes());
                    //Log4Debug("站位：" + moveDirection.userIndex + " 更新了方向：" + "["
                    //    + moveDirection.direction.x + ","
                    //    + moveDirection.direction.y + ","
                    //    + moveDirection.direction.z + "]"
                    //    + "/速度:" + moveDirection.speed);
                }
                else
                {
                    Log4Debug("死亡用户不更新移动。");
                }
                break;
            case MessageConvention.rotateDirection:
                ActorRotateDirection netRotation = SerializeHelper.Deserialize<ActorRotateDirection>(tempMessageContent);
                if (room.ActorList[netRotation.userIndex].CurState != RoomActorState.Dead)
                {
                    room.SetRecondFrame(xieyi.ToBytes());
                    //Log4Debug("站位：" + netRotation.userIndex + " 更新了旋转：" + netRotation.rotateY);
                }
                else
                {
                    Log4Debug("死亡用户不更新旋转。");
                }
                break;
            case MessageConvention.jump:
                ActorJump netJump = SerializeHelper.Deserialize<ActorJump>(tempMessageContent);
                if (room.ActorList[netJump.userIndex].CurState != RoomActorState.Dead)
                {
                    room.SetRecondFrame(xieyi.ToBytes());
                }
                else
                {
                    Log4Debug("死亡用户不更新跳跃。");
                }
                break;
            case MessageConvention.shootBullet:
                ShootInfo shootInfo = SerializeHelper.Deserialize<ShootInfo>(tempMessageContent);
                if (room.ActorList[shootInfo.userIndex].CurState != RoomActorState.Dead)
                {
                    room.SetRecondFrame(xieyi.ToBytes());
                    //Log4Debug("站位：" + netRotation.userIndex + " 更新了旋转：" + netRotation.rotateY);
                }
                else
                {
                    Log4Debug("死亡用户不更新射击。");
                }
                break;
            case MessageConvention.bulletInfo:
                //
                BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(xieyi.MessageContent);
                room.UpdateBulletInfo(bulletInfo);//更新
                //room.SetRecondFrame(xieyi.ToBytes());
                break;
            default:
                Log4Debug("检查协议->" + (MessageConvention)xieyi.XieYiFirstFlag);
                break;
        }

        byte[] sendBuffer = null;
        if (newBuffer != null)//用户需要服务器返回值给自己的话
        {
            xieyi = new MessageXieYi(xieyi.XieYiFirstFlag, xieyi.XieYiSecondFlag, newBuffer);
            sendBuffer = xieyi.ToBytes();
        }
        return sendBuffer;
    }

    private void Log4Debug(string msg)
    {
        NetworkDebuger.Log(this.GetType().Name + ":" + msg);
    }

}

/// <summary>
/// 保存连入用户数据
/// </summary>
public class udpUser
{
    public IPEndPoint lastPoint;//通过地址互相获取相关数据
    public string userId;
    public int roomId;
    public int unique;
    public DateTime heartTime;
}

/// <summary>
/// 接收到数据结构
/// </summary>
public class udpDatas
{
    public int index;
    public byte[] byteRecv;
    public IPEndPoint endPoint;//通过地址互相获取相关数据
    public udpDatas(byte[] recv)
    {
        byte[] id = new byte[sizeof(int)];
        Array.Copy(recv, 0, id, 0, sizeof(int));
        byteRecv = new byte[recv.Length - sizeof(int)];
        Array.Copy(recv, 0, byteRecv, 0, byteRecv.Length);
    }
}

