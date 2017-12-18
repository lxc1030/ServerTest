using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            Login login = null;
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
            try
            {
                switch ((MessageConvention)xieyi.XieYiFirstFlag)
                {
                    case MessageConvention.login:
                        tempMessageContent = xieyi.MessageContent;
                        login = SerializeHelper.Deserialize<Login>(tempMessageContent);
                        newBuffer = Login(login, userToken);
                        break;
                    case MessageConvention.reConnect:
                        //检查是否是掉线用户
                        if (OffLineRooms.ContainsKey(userToken.userInfo.MemberID))
                        {
                            int roomID = OffLineRooms[userToken.userInfo.MemberID];
                            userToken.userInfo.RoomID = roomID;
                            allRoom.RoomList[roomID].ReConnect(userToken);
                            OffLineRooms.Remove(userToken.userInfo.MemberID);
                        }
                        break;
                    case MessageConvention.heartBeat:
                        //心跳检测客户端传递服务器时间
                        Log4Debug("接收到 " + userToken.userInfo.MemberID + " 的心跳检测");
                        newBuffer = SerializeHelper.DateTimeToBytes(DateTime.Now);
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
                        //List<RoomActor> rr = SerializeHelper.Deserialize<List<RoomActor>>(newBuffer);
                        //List<string> allActor = room.GetRoommateInfo();
                        //string info = SerializeHelper.ListCompose(allActor);
                        //byte[] message = SerializeHelper.ConvertToByte(info);
                        //newBuffer = message;
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
                        Log4Debug("站位：" + userToken.userInfo.UniqueID + "/" + userToken.userInfo.Nickname
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
                        Log4Debug("用户" + userToken.userInfo.MemberID + "/请求帧数据：" + frame.frameIndex);
                        break;

                    default:
                        Log4Debug("该协议是否需要判断：" + xieyi.XieYiFirstFlag);
                        break;
                }
            }
            catch (Exception error)
            {
                Log4Debug("处理逻辑错误：" + error.Message);
            }
            //  创建一个发送缓冲区。   
            byte[] sendBuffer = newBuffer;
            if (newBuffer != null)//用户需要服务器返回值给自己的话
            {
                xieyi = new MessageXieYi(xieyi.XieYiFirstFlag, xieyi.XieYiSecondFlag, sendBuffer);
                sendBuffer = xieyi.ToBytes();
            }
            return sendBuffer;
        }


        public byte[] Login(Login login, AsyncUserToken userToken)
        {
            byte[] backData = null;
            string sendData = "";
            string sql = "";

            //
            sql = SqlManager.instance.SelectWhere(TableName.register,
                new string[] { nameof(Register.userID) },
                new string[] { "=" },
                new string[] { login.userID });
            SqlDataReader sqReader = SqlManager.instance.UserAccept(login.userID, sql);
            sql = ClassGroup.ReaderToJson(sqReader);
            if (string.IsNullOrEmpty(sql))//验证账号存不存在 --不存在
            {
                backData = ClassGroup.ErrorBackByType(ErrorType.userIDNotExist);
            }
            else//验证密码
            {
                JObject get = JObject.Parse(sql);
                string name = (string)get[nameof(Register.name)];
                if (login.password != (string)get[nameof(Register.password)])//密码错误
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
                        //sendData = AsyncIOCPServer.HeartbeatSecondTime.ToString();//将心跳检测时间返回给客户端
                        backData = SerializeHelper.Serialize<HeartbeatTime>(new HeartbeatTime() { time = AsyncIOCPServer.HeartbeatSecondTime });
                        userToken.userInfo.MemberID = login.userID;///保存本IP的客户信息
                    }
                }
            }
            //byte[] byteArray = SerializeHelper.ConvertToByte(sendData);
            //return byteArray;
            return backData;
        }

        public void SetOffLineByState(AsyncUserToken userToken)
        {
            if (userToken.userInfo.RoomID != -1)
            {
                SingleRoom room = allRoom.RoomList[userToken.userInfo.RoomID];
                if (room.RoomInfo.CurState != RoomActorState.NoReady)
                {
                    Log4Debug("用户账号：" + userToken.userInfo.MemberID + " 掉线前保存房间号：" + userToken.userInfo.RoomID);
                    if (!OffLineRooms.ContainsKey(userToken.userInfo.MemberID))
                    {
                        OffLineRooms.Add(userToken.userInfo.MemberID, -1);
                    }
                    OffLineRooms[userToken.userInfo.MemberID] = userToken.userInfo.RoomID;
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
                    Log4Debug("用户:" + userToken.userInfo.MemberID + " 掉线，房间不保存。房间状态：" + room.RoomInfo.CurState);
                }
            }
        }
        public void ClearOffLine(RoomActor actor)
        {
            if (OffLineRooms.ContainsKey(actor.MemberID))
            {
                OffLineRooms.Remove(actor.MemberID);
            }
        }


        #endregion




        public void Log4Debug(string msg)
        {
            LogManager.WriteLog(msg);
        }
    }
}
