using System;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Collections.Generic;

public class UdpServer
{
    public static UdpServer instance;

    //以下默认都是私有的成员 
    Socket socket; //目标socket 
    IPEndPoint ipEnd; //侦听端口 
    Thread connectThread; //连接线程
    int receiveLength = 1024;



    Dictionary<string, UDPRoomData> allUDPs;



    //初始化
    public UdpServer(int port)
    {
        instance = this;
        //定义侦听端口,侦听任何IP
        ipEnd = new IPEndPoint(IPAddress.Any, port);
        //定义套接字类型,在主线程中定义
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //服务端需要绑定ip
        socket.Bind(ipEnd);

        allUDPs = new Dictionary<string, UDPRoomData>();
        Log4Debug("初始化UDP服务器");

        //开启一个线程连接，必须的，否则主线程卡死
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }

    public void SocketSend(byte[] sendData, string keyIndex)
    {
        if (string.IsNullOrEmpty(keyIndex))//tcp掉线时会设置udp为空
        {
            return;
        }
        if (!allUDPs.ContainsKey(keyIndex))
        {
            Log4Debug("该地址：" + keyIndex + "未进行登录");
            return;
        }
        SocketSend(sendData, allUDPs[keyIndex].point);
    }

    private void SocketSend(byte[] sendData, EndPoint clientEnd)
    {
        //清空发送缓存
        //byte[] sendData = new byte[sendLength]; //发送的数据，必须为字节 
        //数据类型转换
        //sendData = Encoding.ASCII.GetBytes(sendStr);
        //发送给指定客户端
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, clientEnd);
    }




    //服务器接收
    void SocketReceive()
    {
        //进入接收循环
        while (true)
        {
            //对data清零
            byte[] recvData = new byte[receiveLength];
            //定义客户端
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint clientEnd = (EndPoint)sender;
            try
            {
                //获取客户端，获取客户端数据，用引用给客户端赋值
                int recvLen = socket.ReceiveFrom(recvData, ref clientEnd);//接收的数据长度 
                string key = clientEnd.ToString();
                lock (allUDPs)
                {
                    if (!allUDPs.ContainsKey(key))
                    {
                        //打印客户端信息
                        Log4Debug("message from: " + clientEnd.ToString());
                        allUDPs.Add(key, new UDPRoomData() { point = clientEnd });
                    }
                }
                //输出接收到的数据
                //string recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
                //Log4Debug("我是服务器，接收到客户端的数据" + recvStr);
                byte[] effectiveData = new byte[recvLen];
                Array.Copy(recvData, effectiveData, recvLen);
                byte[] backData = SelectMessage(effectiveData, clientEnd);
                //将接收到的数据经过处理再发送出去
                //string sendStr = "I~m Here. ";
                if (backData != null)
                {
                    SocketSend(backData, clientEnd);
                }
            }
            catch (Exception e)
            {
                Log4Debug("接收->" + e.Message + clientEnd);
            }
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
        Console.WriteLine("disconnect");
    }






    public byte[] SelectMessage(byte[] data, EndPoint endPoint)
    {
        string strPoint = endPoint.ToString();
        byte[] newBuffer = null;
        MessageXieYi xieyi = MessageXieYi.FromBytes(data);
        if (xieyi == null)
            return newBuffer;

        byte[] tempMessageContent = xieyi.MessageContent;
        ActorMoveDirection moveDirection = null;
        SingleRoom room = null;
        UDPLogin login = null;

        if (ServerDataManager.instance.allRoom.RoomList.ContainsKey(allUDPs[strPoint].roomID))
        {
            room = ServerDataManager.instance.allRoom.RoomList[allUDPs[strPoint].roomID];
        }

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.setUDP:
                login = SerializeHelper.Deserialize<UDPLogin>(tempMessageContent);
                login.login = strPoint;
                allUDPs[strPoint].roomID = login.roomID;
                allUDPs[strPoint].unique = login.unique;
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
                BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(tempMessageContent);
                //
                room.UpdateBulletInfo(bulletInfo);//更新
                room.SetRecondFrame(xieyi.ToBytes());
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


    public void Log4Debug(string msg)
    {
        LogManager.instance.WriteLog(this.GetType().Name + ":" + msg);
    }

}

public class UDPRoomData
{
    public EndPoint point;
    public int roomID;
    public int unique;
}