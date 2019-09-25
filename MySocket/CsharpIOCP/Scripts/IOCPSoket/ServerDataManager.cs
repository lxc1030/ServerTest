using Network_Kcp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;

public class ServerDataManager
{
    public static ServerDataManager instance;
    public RoomCollection allRoom;
    public ConcurrentDictionary<string, int> OffLineRooms;

    public static void Init()
    {
        if (instance == null)
        {
            instance = new ServerDataManager();
        }
    }
    public ServerDataManager()
    {
        //房间初始化
        allRoom = new RoomCollection();
        OffLineRooms = new ConcurrentDictionary<string, int>();
        //
        Log4Debug("数据处理准备就绪。");
    }

    public SingleRoom GetSingleRoomByID(int roomID)
    {
        SingleRoom room = null;
        lock (allRoom.RoomList)
        {
            if (allRoom.RoomList.ContainsKey(roomID))
            {
                room = allRoom.RoomList[roomID];
            }
        }
        return room;
    }


    #region 处理接收来的协议拆分和判断

    /// <summary>
    /// tcp
    /// </summary>
    /// <param name="xieyi"></param>
    /// <param name="userToken"></param>
    /// <returns></returns>
    public byte[] SelectMessage(MessageXieYi xieyi, AsyncUserToken userToken)
    {
        JObject json = null;
        byte[] newBuffer = null;
        byte[] tempMessageContent = xieyi.MessageContent;
        int roomID = userToken.userInfo.RoomID;
        SingleRoom room = GetSingleRoomByID(roomID);
        Register login = null;
        RoomActorUpdate roomActorUpdate = null;
        ActorMoveDirection moveDirection = null;
        if (userToken == null)
        {
            Log4Debug("该用户已被清理，不处理接收数据。");
            return newBuffer;
        }
        if (allRoom.RoomList.ContainsKey(userToken.userInfo.RoomID))
        {
            room = allRoom.RoomList[userToken.userInfo.RoomID];
        }

        //Log4Debug("/" + xieyi.MessageContentLength);
        //try
        {
            switch ((MessageConvention)xieyi.XieYiFirstFlag)
            {
                case MessageConvention.login:
                    login = SerializeHelper.Deserialize<Register>(tempMessageContent);
                    newBuffer = Login(login, userToken);
                    break;
                case MessageConvention.getHeartBeatTime:
                    HeartbeatTime hbTime = new HeartbeatTime() { time = AsyncIOCPServer.HeartbeatSecondTime };
                    newBuffer = SerializeHelper.Serialize<HeartbeatTime>(hbTime);
                    break;
                case MessageConvention.reConnectCheck:
                    //检查是否是掉线用户
                    ReconnctInfo rcInfo = new ReconnctInfo();
                    if (OffLineRooms.ContainsKey(userToken.userInfo.Register.userID))
                    {
                        int offRoomID = OffLineRooms[userToken.userInfo.Register.userID];
                        userToken.userInfo.RoomID = offRoomID;
                        allRoom.RoomList[offRoomID].ReConnect(userToken);

                        int removeCount = 0;
                        OffLineRooms.TryRemove(userToken.userInfo.Register.userID, out removeCount);
                        rcInfo.isReconnect = true;
                    }
                    else
                    {
                        rcInfo.isReconnect = false;
                    }
                    newBuffer = SerializeHelper.Serialize<ReconnctInfo>(rcInfo);
                    break;
                case MessageConvention.reConnectIndex:
                    room.GetReConnectFrameData(userToken.userInfo.UniqueID);
                    break;
                case MessageConvention.heartBeat:
                    //心跳检测客户端传递服务器时间
                    //Log4Debug("接收到 " + userToken.userInfo.Register.name + " 的心跳检测");
                    //newBuffer = SerializeHelper.DateTimeToBytes(DateTime.Now);
                    //newBuffer = SerializeHelper.Serialize<HeartbeatTime>(new HeartbeatTime() { time = AsyncIOCPServer.HeartbeatSecondTime });
                    break;
                case MessageConvention.updateName:
                    string updateName = SerializeHelper.ConvertToString(tempMessageContent);
                    Log4Debug("修改人物信息：" + updateName);
                    newBuffer = UpdateName(userToken, updateName);
                    break;
                case MessageConvention.createRoom:
                    json = JObject.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
                    newBuffer = allRoom.CreateNewRoom(json, userToken);
                    break;
                case MessageConvention.joinRoom:
                    json = JObject.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
                    newBuffer = allRoom.JoinRoom(json, userToken);
                    break;
                case MessageConvention.updateRoom:
                    Log4Debug("更新房间");
                    json = JObject.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
                    newBuffer = allRoom.UpdateRoom(json);
                    break;
                case MessageConvention.getRoomInfo:
                    newBuffer = SerializeHelper.Serialize<RoomInfo>(room.RoomInfo);
                    break;
                case MessageConvention.getRoommateInfo:
                    Log4Debug("请求房间人物信息：" + SerializeHelper.ConvertToString(tempMessageContent));
                    roomActorUpdate = new RoomActorUpdate();
                    roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                    //
                    newBuffer = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(room.ActorList.Values));
                    break;
                case MessageConvention.quitRoom:
                    QuitInfo qInfo = SerializeHelper.Deserialize<QuitInfo>(tempMessageContent);
                    if (room != null)
                    {
                        room.CheckQuit(userToken, qInfo);
                    }
                    break;
                case MessageConvention.updateActorAnimation:
                    ActorNetAnimation netAnimation = new ActorNetAnimation();
                    netAnimation.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                    //Log4Debug("set->" + netAnimation.animationIndex + "");
                    room.UpdateAnimation(netAnimation);//更新
                    break;
                case MessageConvention.updateActorState:
                    roomActorUpdate = new RoomActorUpdate();
                    roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                    Log4Debug("站位：" + userToken.userInfo.UniqueID + "/" + userToken.userInfo.Register.name
                        + "用户站位：" + roomActorUpdate.userIndex +
                        "请求修改状态为->" + (RoomActorState)int.Parse(roomActorUpdate.update));
                    //
                    room.UpdateState(roomActorUpdate);//更新
                    break;
                case MessageConvention.prepareLocalModel:
                    roomActorUpdate = new RoomActorUpdate();
                    roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                    //
                    room.UpdatePrepare(roomActorUpdate, userToken);//更新
                    break;
                case MessageConvention.getPreGameData:

                    break;
                case MessageConvention.startGaming:

                    break;
                case MessageConvention.endGaming:

                    break;
                case MessageConvention.frameData:
                    FrameInfo frame = SerializeHelper.Deserialize<FrameInfo>(tempMessageContent);
                    newBuffer = room.GetBoardFrame(frame.frameIndex);
                    //Log4Debug("用户" + userToken.userInfo.Register.name + "/请求帧数据：" + frame.frameIndex + "/" + room.RoomInfo.FrameIndex + "数据总长：" + newBuffer.Length);
                    break;
                //case MessageConvention.setUDP:
                //    UDPLogin loginUDP = SerializeHelper.Deserialize<UDPLogin>(tempMessageContent);
                //    Log4Debug("收到登录UDP账号：" + loginUDP.login);
                //    room.UpdateUDP(userToken.userInfo.UniqueID, loginUDP);
                //    break;
                default:
                    Log4Debug("TCP是否判断该协议：" + (MessageConvention)xieyi.XieYiFirstFlag);
                    break;
            }
        }
        //catch (Exception error)
        //{
        //    Log4Debug("处理逻辑错误：" + error.Message);
        //}
        //  创建一个发送缓冲区。   
        byte[] sendBuffer = null;
        if (newBuffer != null)//用户需要服务器返回值给自己的话
        {
            xieyi = new MessageXieYi(xieyi.XieYiFirstFlag, xieyi.XieYiSecondFlag, newBuffer);
            sendBuffer = xieyi.ToBytes();
        }
        return sendBuffer;
    }




    /// <summary>
    /// udp
    /// </summary>
    /// <param name="data"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public byte[] SelectMessage(udpDatas data, udpUser user)
    {
        byte[] newBuffer = null;
        MessageXieYi xieyi = MessageXieYi.FromBytes(data.byteRecv);
        if (xieyi == null)
            return newBuffer;

        byte[] tempMessageContent = xieyi.MessageContent;
        ActorMoveDirection moveDirection = null;
        int roomID = user.roomId;
        SingleRoom room = GetSingleRoomByID(roomID);
        UDPLogin login = null;

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.setUDP:
                login = SerializeHelper.Deserialize<UDPLogin>(tempMessageContent);
                login.login = user.lastPoint.ToString();
                user.roomId = login.roomID;
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








    #endregion

    #region 逻辑部分


    public byte[] Login(Register login, AsyncUserToken userToken)
    {
        byte[] backData = null;
        string sql = "";

        sql = SqlManager.instance.SelectWhere(TableName.register,
            new string[] { nameof(Register.userID) },
            new string[] { "=" },
            new string[] { login.userID });

        List<Register> regs = SqlManager.instance.DataRead(sql, new Func<SqlDataReader, List<Register>>(Register.BackDatas));
        if (regs.Count <= 0)//验证账号存不存在 --不存在
        {
            backData = ClassGroup.ErrorBackByType(ErrorType.userIDNotExist);
        }
        else
        {
            Register register = regs[0];
            //主逻辑
            string name = register.name;
            if (login.password != register.password)//密码错误
            {
                backData = ClassGroup.ErrorBackByType(ErrorType.passwordWrong);
            }
            else//密码正确
            {
                AsyncUserToken getToken = AsyncIOCPServer.instance.GetTokenByMemberID(login.userID);
                if (getToken != null)//在线
                {
                    backData = ClassGroup.ErrorBackByType(ErrorType.userOnline);
                }
                else
                {
                    userToken.userInfo.LoginSet(register);
                    //返回用户信息
                    backData = SerializeHelper.Serialize<RoomActor>(userToken.userInfo);
                }
            }
        }
        return backData;
    }

    public void SetOffLineByState(AsyncUserToken userToken)
    {
        if (userToken.userInfo.RoomID != -1)
        {
            SingleRoom room = allRoom.RoomList[userToken.userInfo.RoomID];
            if (room.CurState != RoomActorState.NoReady)
            {
                Log4Debug("用户账号：" + userToken.userInfo.Register.userID + " 掉线前保存房间号：" + userToken.userInfo.RoomID);
                OffLineRooms.AddOrUpdate(userToken.userInfo.Register.userID, -1, (key, oldValue) => userToken.userInfo.RoomID);
                //更新掉线用户的状态
                RoomActorUpdate roomActorUpdate = new RoomActorUpdate()
                {
                    userIndex = userToken.userInfo.UniqueID,
                    update = (int)RoomActorState.Offline + ""
                };
                room.ClearUDP(userToken.userInfo.UniqueID);
                room.UpdateState(roomActorUpdate);
            }
            else
            {
                room.Quit(userToken.userInfo.UniqueID);
                Log4Debug("用户:" + userToken.userInfo.Register.userID + " 掉线，房间不保存。房间状态：" + room.CurState);
            }
        }
    }
    public void ClearOffLine(RoomActor actor)
    {
        lock (OffLineRooms)
        {
            int removeCount = 0;
            OffLineRooms.TryRemove(actor.Register.userID, out removeCount);
        }
    }

    public byte[] UpdateName(AsyncUserToken userToken, string name)
    {

        string sql = SqlManager.instance.UpdateInto(
            TableName.register,
            nameof(Register.name), name,
            nameof(Register.userID), userToken.userInfo.Register.userID
            );
        int count = SqlManager.instance.DataWrite(sql);
        if (count > 0)
        {
            userToken.userInfo.Register.name = name;
        }
        return SerializeHelper.Serialize<RoomActor>(userToken.userInfo);
    }
    #endregion


    public void Log4Debug(string msg)
    {
        NetworkDebuger.Log(this.GetType().Name + ":" + msg);
    }
}

