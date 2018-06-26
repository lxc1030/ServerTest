using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class UdpServer
{
    public static UdpServer instance;
    IPAddress ip;
    int port;
    UdpClient myClient;
    Thread thrRecv;//连接线程
    Thread thrHeart;//心跳线程

    /// <summary>
    /// 心跳检测间隔秒数
    /// </summary>
    public const int HeartbeatSecondTime = 60;




    private List<udpUser> allUDPs = new List<udpUser>();



    public UdpServer(string _ip, int _port)
    {
        ip = IPAddress.Parse(_ip);
        port = _port;
        instance = this;

        IPEndPoint localIpep = new IPEndPoint(ip, 12000); // 本机IP和监听端口号
        myClient = new UdpClient(localIpep);
        thrRecv = new Thread(ReceiveMessage);
        thrRecv.Start();

        thrHeart = new Thread(CheckHeartBeat);
        thrHeart.Start();
        Log4Debug("UDP监听器已成功启动");

    }


    private void CheckHeartBeat(object obj)
    {
        Thread.Sleep(HeartbeatSecondTime * 1000);
        List<udpUser> all = null;
        lock (allUDPs)
        {
            all = new List<udpUser>(allUDPs);
        }
        for (int i = 0; i < all.Count; i++)
        {
            object[] infos = new object[] { Encoding.Unicode.GetBytes("HeartBeat"), all[i].endPoint };
            SendMessage(infos);

        }
        Log4Debug("心跳检测结束时间:" + DateTime.Now);
        CheckHeartBeat(obj);
    }

    /// <summary>
    /// 接收数据
    /// </summary>
    /// <param name="obj"></param>
    private void ReceiveMessage(object obj)
    {
        IPEndPoint remoteIpep = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                byte[] bytRecv = myClient.Receive(ref remoteIpep);
                udpDatas data = new udpDatas() { byteRecv = bytRecv, endPoint = remoteIpep };
                ThreadPool.QueueUserWorkItem(new WaitCallback(XieYiThrd), data);
            }
            catch (Exception ex)
            {
                Log4Debug(ex.Message);
                break;
            }
        }
    }

    private void XieYiThrd(object all)
    {
        udpDatas data = all as udpDatas;
        IPEndPoint remoteIpep = data.endPoint;
        udpUser user = null;

        lock (allUDPs)
        {
            if (!allUDPs.Exists(t => t.endPoint.ToString() == remoteIpep.ToString()))//该IP不存在
            {
                //打印客户端信息
                Log4Debug("新udp连接: " + remoteIpep.ToString());
                allUDPs.Add(new udpUser() { endPoint = remoteIpep, heartTime = DateTime.Now });
            }
            user = allUDPs.Find(t => t.endPoint.ToString() == remoteIpep.ToString());
        }


        //将数据包交给前台去处理
        byte[] backData = SelectMessage(data, user);
        //将接收到的数据经过处理再发送出去
        //string sendStr = "I~m Here. ";
        if (backData != null)
        {
            object[] infos = new object[] { backData, user.endPoint };
            SendMessage(infos);
        }
    }


    public byte[] SelectMessage(udpDatas data, udpUser user)
    {
        byte[] newBuffer = null;
        MessageXieYi xieyi = MessageXieYi.FromBytes(data.byteRecv);
        if (xieyi == null)
            return newBuffer;

        byte[] tempMessageContent = xieyi.MessageContent;
        ActorMoveDirection moveDirection = null;
        SingleRoom room = null;
        UDPLogin login = null;

        //该处RoomList没有加锁
        if (ServerDataManager.instance.allRoom.RoomList.ContainsKey(user.roomID))
        {
            room = ServerDataManager.instance.allRoom.RoomList[user.roomID];
        }

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.setUDP:
                login = SerializeHelper.Deserialize<UDPLogin>(tempMessageContent);
                login.login = user.endPoint.ToString();
                user.roomID = login.roomID;
                user.unique = login.unique;
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



    /// <summary>
    /// 发送信息
    /// </summary>
    /// <param name="obj"></param>
    public void SendMessage(object obj)
    {
        try
        {
            object[] all = obj as object[];
            UdpClient send = myClient;
            byte[] sendbytes = (byte[])all[0];
            IPEndPoint remoteIpep = (IPEndPoint)all[1];
            send.Send(sendbytes, sendbytes.Length, remoteIpep);
        }
        catch (Exception ex)
        {
            Log4Debug("发送错误：" + ex.Message);
        }
    }










    //连接关闭
    void SocketQuit()
    {
        //关闭线程
        if (thrRecv != null)
        {
            thrRecv.Interrupt();
            thrRecv.Abort();
        }
        ////最后关闭socket
        //if (socket != null)
        //    socket.Close();
        Log4Debug("udpSocket Disconnect");
    }

    private void Log4Debug(string msg)
    {
        LogManager.instance.WriteLog(this.GetType().Name + ":" + msg);
    }


}

public class udpUser
{
    public IPEndPoint endPoint;//通过地址互相获取相关数据
    public string userId;
    public int roomID;
    public int unique;
    public DateTime heartTime;

}
public class udpDatas
{
    public byte[] byteRecv;
    public IPEndPoint endPoint;//通过地址互相获取相关数据
}