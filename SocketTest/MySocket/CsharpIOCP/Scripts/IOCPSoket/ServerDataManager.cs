using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NetFrame.Net
{
    public class ServerDataManager
    {
        public static ServerDataManager instance;
        public RoomCollection allRoom;
        public Dictionary<string, int> OffLineRooms;
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
            OffLineRooms = new Dictionary<string, int>();
            //
            Log4Debug("数据处理准备就绪。");
        }

        ///// <summary>
        ///// 强制退出房间
        ///// </summary>
        //public void ForceQuit(RoomActor userInfo)
        //{
        //    allRoom.QuitRoom(userInfo);
        //}


        #region 处理接收来的协议拆分和判断

        public byte[] SelectMessage(MessageXieYi xieyi, AsyncUserToken userToken)
        {
            JObject json = null;
            byte[] newBuffer = null;
            byte[] tempMessageContent = null;
            SingleRoom room = null;
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
                        tempMessageContent = xieyi.MessageContent;
                        login = SerializeHelper.Deserialize<Register>(tempMessageContent);
                        newBuffer = Login(login, userToken);
                        break;
                    case MessageConvention.getHeartBeatTime:
                        newBuffer = SerializeHelper.Serialize<HeartbeatTime>(new HeartbeatTime() { time = AsyncIOCPServer.HeartbeatSecondTime });
                        break;
                    case MessageConvention.reConnect:
                        //检查是否是掉线用户
                        if (OffLineRooms.ContainsKey(userToken.userInfo.Register.userID))
                        {
                            int roomID = OffLineRooms[userToken.userInfo.Register.userID];
                            userToken.userInfo.RoomID = roomID;
                            allRoom.RoomList[roomID].ReConnect(userToken);
                            OffLineRooms.Remove(userToken.userInfo.Register.userID);
                        }
                        break;
                    case MessageConvention.heartBeat:
                        //心跳检测客户端传递服务器时间
                        Log4Debug("接收到 " + userToken.userInfo.Register.userID + " 的心跳检测");
                        //newBuffer = SerializeHelper.DateTimeToBytes(DateTime.Now);
                        newBuffer = SerializeHelper.Serialize<HeartbeatTime>(new HeartbeatTime() { time = AsyncIOCPServer.HeartbeatSecondTime });
                        break;
                    case MessageConvention.updateName:
                        tempMessageContent = xieyi.MessageContent;
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
                        Log4Debug(newBuffer.Length + "newBuffer");
                        break;
                    case MessageConvention.quitRoom:
                        QuitInfo quitInfo = new QuitInfo();
                        if (room != null)
                        {
                            Log4Debug("房间：" + room.RoomInfo.RoomID + " 用户站位：" + userToken.userInfo.UniqueID + "请求退出房间");
                            bool isQuit = room.Quit(userToken.userInfo.UniqueID);
                            quitInfo = new QuitInfo() { isQuit = isQuit };
                        }
                        else
                        {
                            quitInfo.isQuit = true;
                        }
                        newBuffer = SerializeHelper.Serialize<QuitInfo>(quitInfo);
                        break;
                    case MessageConvention.getRoommateInfo:
                        tempMessageContent = xieyi.MessageContent;
                        Log4Debug("请求房间人物信息：" + SerializeHelper.ConvertToString(tempMessageContent));
                        roomActorUpdate = new RoomActorUpdate();
                        roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                        //
                        newBuffer = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(room.RoomInfo.ActorList.Values));
                        break;
                    case MessageConvention.updateActorAnimation:
                        tempMessageContent = xieyi.MessageContent;
                        ActorNetAnimation netAnimation = new ActorNetAnimation();
                        netAnimation.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                        //Log4Debug("set->" + netAnimation.animationIndex + "");
                        room.UpdateAnimation(netAnimation);//更新
                        break;
                    case MessageConvention.updateActorState:
                        tempMessageContent = xieyi.MessageContent;
                        roomActorUpdate = new RoomActorUpdate();
                        roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(tempMessageContent));
                        Log4Debug("站位：" + userToken.userInfo.UniqueID + "/" + userToken.userInfo.Register.name
                            + "用户站位：" + roomActorUpdate.userIndex +
                            "请求修改状态为->" + (RoomActorState)int.Parse(roomActorUpdate.update));
                        //
                        room.UpdateState(roomActorUpdate);//更新
                        break;
                    case MessageConvention.shootBullet:
                        tempMessageContent = xieyi.MessageContent;
                        string shootIndex = SerializeHelper.ConvertToString(tempMessageContent);
                        //
                        room.SetRecondFrame(xieyi.ToBytes());
                        break;
                    case MessageConvention.bulletInfo:
                        tempMessageContent = xieyi.MessageContent;
                        BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(tempMessageContent);
                        Log4Debug("bulletIndex:" + bulletInfo.userIndex + "->" + bulletInfo.shootTag + "/" + bulletInfo.shootInfo);
                        //
                        room.UpdateBulletInfo(bulletInfo);//更新
                        room.SetRecondFrame(xieyi.ToBytes());
                        break;
                    case MessageConvention.prepareLocalModel:
                        tempMessageContent = xieyi.MessageContent;
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
                    case MessageConvention.moveDirection:
                        tempMessageContent = xieyi.MessageContent;
                        moveDirection = SerializeHelper.Deserialize<ActorMoveDirection>(tempMessageContent);
                        if (room.RoomInfo.ActorList[moveDirection.userIndex].CurState != RoomActorState.Dead)
                        {
                            room.SetRecondFrame(xieyi.ToBytes());
                            Log4Debug("站位：" + moveDirection.userIndex + " 更新了方向：" + SerializeHelper.BackVector(moveDirection.direction) + "/速度:" + moveDirection.speed);
                        }
                        else
                        {
                            Log4Debug("死亡用户不更新移动。");
                        }
                        break;
                    case MessageConvention.rotateDirection:
                        tempMessageContent = xieyi.MessageContent;
                        ActorRotateDirection netRotation = SerializeHelper.Deserialize<ActorRotateDirection>(tempMessageContent);
                        if (room.RoomInfo.ActorList[netRotation.userIndex].CurState != RoomActorState.Dead)
                        {
                            room.SetRecondFrame(xieyi.ToBytes());
                            Log4Debug("站位：" + netRotation.userIndex + " 更新了旋转：" + netRotation.rotateY);
                        }
                        else
                        {
                            Log4Debug("死亡用户不更新旋转。");
                        }
                        break;
                    case MessageConvention.frameData:
                        tempMessageContent = xieyi.MessageContent;
                        FrameInfo frame = SerializeHelper.Deserialize<FrameInfo>(tempMessageContent);
                        newBuffer = room.GetBoardFrame(frame.frameIndex);
                        Log4Debug("用户" + userToken.userInfo.Register.userID + "/请求帧数据：" + frame.frameIndex + "-" + room.RoomInfo.FrameIndex);
                        break;
                    default:
                        Log4Debug("该协议是否需要判断：" + xieyi.XieYiFirstFlag);
                        break;
                }
            }
            //catch (Exception error)
            //{
            //    Log4Debug("处理逻辑错误：" + error.Message);
            //}
            //  创建一个发送缓冲区。   
            byte[] sendBuffer = newBuffer;
            if (newBuffer != null)//用户需要服务器返回值给自己的话
            {
                xieyi = new MessageXieYi(xieyi.XieYiFirstFlag, xieyi.XieYiSecondFlag, sendBuffer);
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
            SqlDataGroup sqlGroup = null;

            sql = SqlManager.instance.SelectWhere(TableName.register,
                new string[] { nameof(Register.userID) },
                new string[] { "=" },
                new string[] { login.userID });

            sqlGroup = SqlManager.instance.ReaderFindBySql(sql);
            if (sqlGroup.GetReader() == null)//验证账号存不存在 --不存在
            {
                backData = ClassGroup.ErrorBackByType(ErrorType.userIDNotExist);
            }
            else
            {
                Register register = new Register();
                register.Init(sqlGroup.GetReader());
                //关闭连接
                SqlManager.instance.Close(sqlGroup);
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
                if (room.RoomInfo.CurState != RoomActorState.NoReady)
                {
                    Log4Debug("用户账号：" + userToken.userInfo.Register.userID + " 掉线前保存房间号：" + userToken.userInfo.RoomID);
                    if (!OffLineRooms.ContainsKey(userToken.userInfo.Register.userID))
                    {
                        OffLineRooms.Add(userToken.userInfo.Register.userID, -1);
                    }
                    OffLineRooms[userToken.userInfo.Register.userID] = userToken.userInfo.RoomID;
                    //更新掉线用户的状态
                    RoomActorUpdate roomActorUpdate = new RoomActorUpdate()
                    {
                        userIndex = userToken.userInfo.UniqueID,
                        update = (int)RoomActorState.Offline + ""
                    };
                    room.UpdateState(roomActorUpdate);
                }
                else
                {
                    room.Quit(userToken.userInfo.UniqueID);
                    Log4Debug("用户:" + userToken.userInfo.Register.userID + " 掉线，房间不保存。房间状态：" + room.RoomInfo.CurState);
                }
            }
        }
        public void ClearOffLine(RoomActor actor)
        {
            if (OffLineRooms.ContainsKey(actor.Register.userID))
            {
                OffLineRooms.Remove(actor.Register.userID);
            }
        }

        public byte[] UpdateName(AsyncUserToken userToken, string name)
        {

            string sql = SqlManager.instance.UpdateInto(
                TableName.register,
                nameof(Register.name), name,
                nameof(Register.userID), userToken.userInfo.Register.userID
                );
            int count = SqlManager.instance.ReaderUpdateBySql(sql);
            if (count > 0)
            {
                userToken.userInfo.Register.name = name;
            }
            return SerializeHelper.Serialize<RoomActor>(userToken.userInfo);
        }
        #endregion


        public void Log4Debug(string msg)
        {
            LogManager.WriteLog(this.GetType().Name + ":" + msg);
        }
    }
}
