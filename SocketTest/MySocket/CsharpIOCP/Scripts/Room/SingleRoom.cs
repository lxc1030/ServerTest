using NetFrame.Net;
using System;
using System.Collections.Generic;
using System.Threading;

public class SingleRoom
{
    public int PassedCountDownTime = 0;//倒计时已经过了多久
    public int CountDownTime = 5 * 1000;//倒计时X秒以后开始游戏

    public static int frameTime = 15;//是毫秒，检测间隔0.015s

    public int PassedGameTime = 0;// 游戏已经过了多久


    public RoomInfo RoomInfo { get; set; }//客户端和服务器通用保存房间属性的变量类
    public Dictionary<string, AsyncUserToken> UserTokens { get; set; }

    /// <summary>
    /// 游戏棋子对应的拥有玩家ID--盒子序号，用户站位
    /// </summary>
    public Dictionary<int, BoxInfo> BoxList { get; set; }
    /// <summary>
    /// 队伍信息
    /// </summary>
    public Dictionary<TeamType, List<int>> AllTeamInfo { get; set; }

    public Timer CountDownTimer { get; set; }
    public Timer GameTimer { get; set; }
    public Timer FrameTimer { get; set; }

    /// <summary>
    /// 保存每帧玩家发送来的数据
    /// </summary>
    public Dictionary<int, FrameInfo> FrameGroup { get; set; }

    public SingleRoom(int roomID, string roomName, GameModel roomType, int limit)
    {
        RoomInfo = new RoomInfo(roomID, roomName, roomType, limit);
        UserTokens = new Dictionary<string, AsyncUserToken>();
        //
        switch (roomType)
        {
            case GameModel.组队模式:
                AllTeamInfo = new Dictionary<TeamType, List<int>>()
                {
                    { TeamType.Blue, new List<int>() { } },
                    { TeamType.Red, new List<int>() { } }
                };
                BoxList = GameTypeManager.BackBoxInfosByType(roomType);

                break;
        }

    }

    // 會員加入房間
    public bool Join(AsyncUserToken userToken, string MemberID, string NickName, short Sex, out int UniqueID)
    {
        //if (ActorList.Values.Count < Limit)
        //{
        //    for (int i = 0; i < Limit; i++)
        //    {
        //        ActorList.Add(i, null);
        //    }
        //}
        lock (this)
        {
            UniqueID = -1;
            foreach (KeyValuePair<int, RoomActor> item in RoomInfo.ActorList)
            {
                if (item.Value.MemberID == "")
                {
                    UniqueID = item.Key;
                    break;
                }
            }
            if (UniqueID != -1)
            {
                Log4Debug("账号->" + MemberID + " 用户名->" + NickName + " 加入房间->" + RoomInfo.RoomID + " 站位为->" + UniqueID);
                UserTokens[MemberID] = userToken;

                TeamType myTeam = TeamType.Blue;
                if (UniqueID % 2 == 0)//红蓝两队
                {
                    myTeam = TeamType.Blue;
                }
                else
                {
                    myTeam = TeamType.Red;
                }
                RoomActor actor = new RoomActor(RoomInfo.RoomID, UniqueID, MemberID, NickName, Sex, myTeam);
                userToken.userInfo = actor;
                RoomInfo.ActorList[UniqueID] = actor;
                RoomInfo.ActorList[UniqueID].MyModelInfo.pos = (NetVector3)GameTypeManager.BackStandPos(RoomInfo.RoomType, UniqueID);
                RoomInfo.ActorList[UniqueID].MyModelInfo.rotate = new NetVector3(0, GameTypeManager.BackLookAt(RoomInfo.RoomType, UniqueID), 0);
                RoomInfo.ActorList[UniqueID].MyModelInfo.animation = 0;
                BoardcastActorInfo(UniqueID);
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    #region 判断命令

    // 會員離開房間
    public bool Quit(int unique)
    {
        lock (this)
        {
            if (RoomInfo.ActorList.ContainsKey(unique))
            {
                if (RoomInfo.ActorList[unique].CurState != RoomActorState.Gaming)//当前正在游戏则不能退出房间
                {
                    //
                    RoomInfo.InitRoomActorByIndex(unique);
                    BoardcastActorInfo(unique);
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsEmptyRoom()
    {
        foreach (KeyValuePair<int, RoomActor> item in RoomInfo.ActorList)
        {
            if (item.Value.UniqueID != -1)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsInRoom(string memberID)
    {
        lock (this)
        {
            foreach (KeyValuePair<int, RoomActor> item in RoomInfo.ActorList)
            {
                if (item.Value.MemberID == memberID)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsMaster(string memberID)
    {
        return RoomInfo.ActorList[0].MemberID == memberID;
    }

    #endregion


    public void InitRoom()
    {
        List<RoomActor> allRA = new List<RoomActor>(RoomInfo.ActorList.Values);
        for (int i = 0; i < allRA.Count; i++)
        {
            if (allRA[i].CurState == RoomActorState.Offline)
            {
                //离线到游戏结束
                Quit(allRA[i].UniqueID);
                ServerDataManager.instance.ClearOffLine(allRA[i]);
            }
        }
        ChangeRoomState(RoomActorState.NoReady);
    }
    
    #region 更新信息

    public void UpdateRoom(GameModel roomType, string roomName)
    {
        RoomInfo.RoomType = roomType;
        RoomInfo.RoomName = roomName;
    }


    public void UpdateAnimation(ActorNetAnimation netAniamtion)
    {
        if (RoomInfo.ActorList[netAniamtion.userIndex] == null)
            return;
        RoomInfo.ActorList[netAniamtion.userIndex].MyModelInfo.animation = netAniamtion.animationIndex;
        byte[] message = SerializeHelper.ConvertToByte(netAniamtion.GetSendInfo());
        BoardcastMessage(MessageConvention.updateActorAnimation, message, netAniamtion.userIndex);
    }

    public void UpdateState(RoomActorUpdate roomActorUpdate)
    {
        int index = roomActorUpdate.userIndex;
        RoomActorState upState = (RoomActorState)int.Parse(roomActorUpdate.update);
        if (RoomInfo.ActorList[index] == null)
            return;
        if (RoomInfo.ActorList[index].CurState != RoomActorState.ReConnect)
        {
            Log4Debug("站位 " + index + " 更新当前状态：" + RoomInfo.ActorList[index].CurState + " -> " + (RoomActorState)int.Parse(roomActorUpdate.update));
            RoomInfo.ActorList[index].CurState = upState;
            byte[] message = SerializeHelper.ConvertToByte(roomActorUpdate.GetSendInfo());
            BoardcastMessage(MessageConvention.updateActorState, message);

            if (CheckIsAllFixedState(RoomActorState.Ready))
            {
                ChangeRoomState(RoomActorState.Ready);
            }
            if (RoomInfo.CurState == RoomActorState.Gaming)
            {
                switch (RoomInfo.ActorList[index].CurState)
                {
                    case RoomActorState.WaitForStart:
                        Log4Debug("模型未准备好的玩家准备好进入游戏了。");
                        AsyncIOCPServer.instance.SendMessageToUser(RoomInfo.ActorList[index].MemberID, new byte[] { 1 }, (byte)MessageConvention.startGaming, 0);
                        if (RoomInfo.CurState == RoomActorState.Gaming)
                        {
                            roomActorUpdate = new RoomActorUpdate()
                            {
                                userIndex = index,
                                update = (int)RoomActorState.Gaming + ""
                            };
                            UpdateState(roomActorUpdate);//广播,修改玩家状态用来准备本机数据
                        }
                        break;
                    case RoomActorState.Dead:
                        RoomInfo.ActorList[index].deadTimer = new Timer(new TimerCallback(SetActorDeadToGaming), index, RoomActor.DeadToReviveTime, 0);
                        break;
                }
            }
        }
        else//reConnect时修改状态
        {
            if (upState == RoomActorState.WaitForStart && RoomInfo.CurState == RoomActorState.Gaming)
            {
                RoomInfo.ActorList[index].CurState = RoomActorState.Gaming;
                roomActorUpdate.update = (int)RoomInfo.ActorList[index].CurState + "";
                byte[] message = SerializeHelper.ConvertToByte(roomActorUpdate.GetSendInfo());
                BoardcastMessage(MessageConvention.updateActorState, message);
            }
            else
            {
                Log4Debug("想要在重连时更新状态：" + upState);
            }
        }
    }
    //public void ShootBullet(BulletInfo bullet)
    //{
    //    //子弹的拥有者的旋转角度
    //    Vector3 bulletRotate = ActorList[bullet.userIndex].myRotationInfo.Rotation;
    //    Vector2 bullet2D = new Vector2(bullet.pos.x, bullet.pos.z);
    //    //子弹相对落地点（半径以内的x，y）
    //    Vector2 sPos = new Vector2(
    //        RoomActor.ShootRadius * Mathf.Sin((Mathf.PI / 180) * bulletRotate.y),
    //        RoomActor.ShootRadius * Mathf.Cos((Mathf.PI / 180) * bulletRotate.y));
    //    //子弹世界落地点
    //    Vector2 bulletWorldPos = sPos + bullet2D;


    //    foreach (var item in ActorList)//遍历是否射中其他玩家
    //    {
    //        bool isShooted = false;
    //        if (item.Value == null)
    //            continue;
    //        if (item.Value.UniqueID == bullet.userIndex)//子弹不检测自身
    //            continue;
    //        RoomActor actor = item.Value;
    //        //待碰撞人物的顶视图世界坐标
    //        Vector2 cP = new Vector2(actor.MyMoveInfo.Pos.x, actor.MyMoveInfo.Pos.z);
    //        //子弹发射点和待碰撞人物距离
    //        float bscDistance = Vector2.Distance(bullet2D, cP);

    //        //子弹射击方向世界角度
    //        float shootRotate = bulletRotate.y;
    //        shootRotate = 90 - shootRotate;//转换模型旋转角度到世界坐标角度
    //        BackPositiveOfAngle(ref shootRotate);//转换负数角度成正数角度
    //        //射击点到人物的世界角度
    //        float middleAngle = Mathf.Atan2(actor.MyMoveInfo.Pos.z - bullet.pos.z, actor.MyMoveInfo.Pos.x - bullet.pos.x) * 180 / Mathf.PI;
    //        BackPositiveOfAngle(ref middleAngle);
    //        //射击点到人物边缘的世界角度
    //        float sideAngle = Mathf.Atan2(RoomActor.ModelRadius, bscDistance) * 180 / Mathf.PI;
    //        BackPositiveOfAngle(ref sideAngle);
    //        float angleMin = (middleAngle - sideAngle) > (middleAngle + sideAngle) ? (middleAngle + sideAngle) : (middleAngle - sideAngle);
    //        float angleMax = (middleAngle - sideAngle) > (middleAngle + sideAngle) ? (middleAngle - sideAngle) : (middleAngle + sideAngle);


    //        //判断待射击人物的夹角(计算射击点到人物边缘的角度)子弹在朝向模型
    //        if (shootRotate > angleMin && shootRotate < angleMax)
    //        {
    //            if (bscDistance <= RoomActor.ShootRadius)//待检测人物的中心点在射击半径上或内
    //            {
    //                isShooted = true;
    //            }
    //            else if (bscDistance < RoomActor.ShootRadius + RoomActor.ModelRadius)//子弹落在2个人物之间,正好是射击待碰撞人物中心点，最短距离
    //            {
    //                //判断子弹落地点是否在待检测人物半径内
    //                if (Vector2.Distance(bulletWorldPos, cP) <= RoomActor.ModelRadius)
    //                {
    //                    isShooted = true;
    //                }
    //            }
    //        }
    //        //射中人物
    //        if (isShooted)
    //        {
    //            Log4Debug("射中人物：" + actor.UniqueID);
    //            TeamType bulletType = BackTeamTypeByUnique(bullet.userIndex);
    //            RoomActorUpdate hit = new RoomActorUpdate() { userIndex = actor.UniqueID, update = (int)bulletType + "" };
    //            byte[] message = SerializeHelper.ConvertToByte(hit.GetSendInfo());
    //            //广播
    //            BoardcastMessage(MessageConvention.shootBullet, message);
    //            return;
    //        }
    //    }
    //    //Log4Debug("子弹落地点：" + bulletWorldPos);
    //    //子弹碰撞方块
    //    foreach (KeyValuePair<int, BoxInfo> item in BoxList)
    //    {
    //        Vector2 boxPos = new Vector2(item.Value.position.x, item.Value.position.z);
    //        Vector2 boxScale = new Vector2(item.Value.scale.x, item.Value.scale.z);

    //        if (
    //            bulletWorldPos.x > boxPos.x - (float)(boxScale.x / 2) &&
    //            bulletWorldPos.x <= boxPos.x + (float)(boxScale.x / 2) &&
    //            bulletWorldPos.y > boxPos.y - (float)(boxScale.y / 2) &&
    //            bulletWorldPos.y <= boxPos.y + (float)(boxScale.y / 2)
    //            )//顶视图看是在方格内的
    //        {
    //            Log4Debug("子弹落点：（" + bulletWorldPos.x + "," + bulletWorldPos.y + ") 射中方块坐标：(" + item.Value.position.x + "," + item.Value.position.z + ")");
    //            //设置色块拥有者
    //            item.Value.ownerIndex = bullet.userIndex;
    //            //广播发送消息
    //            RoomActorUpdate hit = new RoomActorUpdate() { userIndex = bullet.userIndex, update = item.Key + "" };
    //            byte[] message = SerializeHelper.ConvertToByte(hit.GetSendInfo());
    //            //射中地板方块
    //            BoardcastMessage(MessageConvention.updateBox, message);
    //            return;
    //        }
    //    }
    //    //
    //}
    public void UpdateBulletInfo(BulletInfo bulletInfo)
    {
        //
        switch (bulletInfo.shootTag)
        {
            case ShootTag.Box:
                //设置色块拥有者
                BoxList[int.Parse(bulletInfo.shootInfo)].ownerIndex = bulletInfo.userIndex;
                break;
            case ShootTag.Character:
                //
                int shootedIndex = int.Parse(bulletInfo.shootInfo);
                if (RoomInfo.ActorList[shootedIndex].CurState == RoomActorState.Gaming)
                {
                    //增加击杀数
                    int killer = bulletInfo.userIndex;
                    RoomInfo.ActorList[killer].KillCount++;
                    if (RoomInfo.CurState == RoomActorState.Gaming)
                    {
                        BoardcastActorInfos();
                    }
                    //改变被射击者状态
                    RoomActorUpdate dead = new RoomActorUpdate()
                    {
                        userIndex = shootedIndex,
                        update = (int)RoomActorState.Dead + ""
                    };
                    UpdateState(dead);
                    //
                    //GameModelData dead = new GameModelData()
                    //{
                    //    userIndex = shootedIndex,
                    //    pos = (NetVector3)GameTypeManager.BackStandPos(RoomType, shootedIndex),
                    //    rotate = new NetVector3(0, GameTypeManager.BackLookAt(RoomType, shootedIndex), 0),
                    //    animation = 0
                    //};
                    //SetRecondFrame(SerializeHelper.Serialize<GameModelData>(dead), FrameIndex);
                }
                else
                {
                    Log4Debug("射击者站位：" + bulletInfo.userIndex + " 正在鞭尸位置->" + shootedIndex);
                    return;
                }

                break;
            case ShootTag.Wall:
                break;
        }

        //广播发送消息
        //byte[] message = SerializeHelper.ConvertToByte(bulletInfo));
        //BoardcastMessage(MessageConvention.bulletInfo, message);
    }

    public void SetActorDeadToGaming(object unique)
    {
        int index = (int)unique;
        RoomInfo.ActorList[index].deadTimer.Dispose();
        Log4Debug("执行回调设置复活。");
        RoomActorUpdate roomActorUpdate = new RoomActorUpdate() { userIndex = index, update = (int)RoomActorState.Gaming + "" };
        UpdateState(roomActorUpdate);
    }

    //public void ShootBullet(ShootInfo shootInfo)
    //{
    //    //广播发送消息
    //    byte[] message = SerializeHelper.ConvertToByte(shootInfo.GetSendInfo());
    //    BoardcastMessage(MessageConvention.shootBullet, message, shootInfo.userIndex);
    //}
    public void UpdatePrepare(RoomActorUpdate roomActorUpdate, AsyncUserToken userToken)
    {
        if (RoomInfo.ActorList[roomActorUpdate.userIndex] == null)
            return;

        int index = roomActorUpdate.userIndex;
        int preLoadProgress = int.Parse(roomActorUpdate.update);
        Log4Debug("站位 " + roomActorUpdate.userIndex + " 当前准备进度为" + preLoadProgress + "%");
        BoardcastMessage(MessageConvention.prepareLocalModel, SerializeHelper.ConvertToByte(roomActorUpdate.GetSendInfo()));

        //玩家自己本地模型加载到100%就给该玩家发送网络数据
        if (preLoadProgress >= 100)
        {
            RoomActorUpdate actorState = new RoomActorUpdate()
            {
                userIndex = index,
                update = (int)RoomActorState.ModelPrepared + ""
            };
            UpdateState(actorState);

            GetRoommateNetData(roomActorUpdate.userIndex, userToken);
        }
    }
    #endregion
    
    #region 帧同步保存和读取

    /// <summary>
    /// 保存各个指令的数据
    /// </summary>
    /// <param name="message"></param>
    /// <param name="index">保存在指定帧内</param>
    public void SetRecondFrame(byte[] message, int index = -1)
    {
        int curIndex = RoomInfo.FrameIndex;
        if (index != -1)
        {
            curIndex = index;
        }
        Log4Debug("存储帧：" + curIndex);
        if (RoomInfo.CurState == RoomActorState.Gaming)
        {
            lock (FrameGroup[curIndex].frameData)
            {
                FrameGroup[curIndex].frameData.Add(message);
            }
        }
    }



    /// <summary>
    /// 广播该帧之前区间frameInterval的值（或者客户端未收到自行请求）
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public byte[] GetBoardFrame(int index)
    {
        List<FrameInfo> infos = new List<FrameInfo>() { };
        int init = index - RoomInfo.frameInterval;
        if (init > FrameGroup.Keys.Count)
        {
            Log4Debug("请求帧：" + init + "超过帧总数：" + FrameGroup.Keys.Count);
        }
        else
        {
            for (int i = index - RoomInfo.frameInterval; i < index; i++)
            {
                infos.Add(FrameGroup[i]);
            }
        }
        return SerializeHelper.Serialize<List<FrameInfo>>(infos);
    }

    /// <summary>
    /// 服务器帧根据时间递增
    /// </summary>
    /// <param name="obj"></param>
    private void GameFrameReconding(object obj)
    {
        if (RoomInfo.CurState == RoomActorState.Gaming)
        {
            int tempIndex = RoomInfo.FrameIndex + 1;
            RoomInfo.FrameIndex = tempIndex;
            if (tempIndex % RoomInfo.frameInterval == 0)//广播前面frameInterval间隔的数据
            {
                byte[] message = GetBoardFrame(tempIndex);
                BoardcastMessage(MessageConvention.frameData, message);
            }
        }
    }

    #endregion

    #region 计算逻辑功能函数

    /// <summary>
    /// 转换负数角度成正数角度
    /// </summary>
    /// <param name="angle"></param>
    private void BackPositiveOfAngle(ref float angle)
    {
        angle = (angle < 0) ? angle + 360 : angle;
    }

    /// <summary>
    /// 根据站位获得用户组别
    /// </summary>
    /// <param name="unique"></param>
    /// <returns></returns>
    private TeamType BackTeamTypeByUnique(int unique)
    {
        foreach (KeyValuePair<TeamType, List<int>> item in AllTeamInfo)
        {
            foreach (var index in item.Value)
            {
                if (index == unique)
                {
                    return item.Key;
                }
            }
        }
        return TeamType.Both;
    }

    private bool CheckIsAllFixedState(RoomActorState state)
    {
        bool isFixed = true;
        for (int i = 0; i < RoomInfo.ActorList.Count; i++)
        {
            if (RoomInfo.ActorList[i] == null || RoomInfo.ActorList[i].CurState != state)
            {
                isFixed = false;
                break;
            }
        }
        return isFixed;
    }
    /// <summary>
    /// 返回胜利队伍
    /// </summary>
    /// <returns></returns>
    private TeamType GetWinnerTeam()
    {
        int[] TwoTeam = new int[2] { 0, 0 };

        foreach (KeyValuePair<int, BoxInfo> item in BoxList)
        {
            if (item.Value.ownerIndex < 0)//初始化BoxList的时候，默认值是-1，代表这个盒子没有拥有者
            {
                continue;
            }
            if (RoomInfo.ActorList[item.Value.ownerIndex].MyTeam == TeamType.Blue)//该盒子的拥有者是哪个队的
            {
                TwoTeam[0] += 1;
            }
            else if (RoomInfo.ActorList[item.Value.ownerIndex].MyTeam == TeamType.Red)
            {
                TwoTeam[1] += 1;
            }
        }
        Log4Debug("色块个数：" + TwoTeam[0] + "//" + TwoTeam[1]);
        if (TwoTeam[0] > TwoTeam[1])
        {
            return TeamType.Blue;
        }
        else if (TwoTeam[0] < TwoTeam[1])
        {
            return TeamType.Red;
        }
        else
        {
            return TeamType.Both;
        }
    }
    #endregion

    #region 时间自动计时

    /// <summary>
    /// 加载倒计时
    /// </summary>
    /// <param name="obj"></param>
    private void GameCountDowning(object obj)
    {
        PassedCountDownTime += 1 * 1000;
        Log4Debug("倒计时时间：" + PassedCountDownTime / 1000);
        if (PassedCountDownTime >= CountDownTime)
        {
            ChangeRoomState(RoomActorState.Gaming);
            CountDownTimer.Dispose();
        }
    }
    /// <summary>
    /// 游戏计时
    /// </summary>
    private void GameTimeIsFinish(object obj)
    {
        PassedGameTime += 1 * 1000;
        Log4Debug("当前时间：" + PassedGameTime / 1000);

        //广播游戏时间
        byte[] message = SerializeHelper.ConvertToByte((RoomInfo.GameTime - PassedGameTime) + "");
        BoardcastMessage(MessageConvention.gamingTime, message);
        if (RoomInfo.CurState == RoomActorState.Gaming && PassedGameTime > RoomInfo.GameTime)
        {
            ChangeRoomState(RoomActorState.GameEnd);
            GameTimer.Dispose();
        }
    }

    #endregion
    
    #region 广播逻辑

    /// <summary>
    /// 向该用户广播该房间的人物模型准备数据
    /// </summary>
    public void GetRoommateNetData(int uniqueID, AsyncUserToken userToken)
    {
        byte[] message = null;
        foreach (var item in RoomInfo.ActorList)
        {
            MessageXieYi xieyi = null;
            if (item.Value == null)
            {
                Log4Debug("检查，房间不该为空");
                continue;
            }
            //准备数据发送完毕，打开UI
            message = SerializeHelper.Serialize<GameModelData>(new GameModelData()
            {
                userIndex = item.Value.UniqueID,
                pos = (NetVector3)GameTypeManager.BackStandPos(RoomInfo.RoomType, item.Value.UniqueID),
                rotate = new NetVector3(0, GameTypeManager.BackLookAt(RoomInfo.RoomType, item.Value.UniqueID), 0),
                animation = 0
            });//此处是初始数据
            xieyi = new MessageXieYi((byte)MessageConvention.updateModelInfo, 0, message);
            AsyncIOCPServer.instance.SaveSendMessage(userToken, xieyi.ToBytes());
        }
        //准备数据发送完毕，打开UI
        message = new byte[] { 1 };//此处的数组应该是初始数据
        MessageXieYi xieyiFinish = new MessageXieYi((byte)MessageConvention.getPreGameData, 0, message);
        AsyncIOCPServer.instance.SaveSendMessage(userToken, xieyiFinish.ToBytes());
    }



    /// <summary>
    /// 广播我的信息给房间其他用户
    /// </summary>
    /// <param name="memberID">除该ID以外都广播</param>
    public void BoardcastActorInfo(int uniqueID)
    {
        //需要修改
        byte[] message = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(RoomInfo.ActorList.Values));
        BoardcastMessage(MessageConvention.getRoommateInfo, message, uniqueID);
    }

    /// <summary>
    /// 广播房间用户信息给所有人
    /// </summary>
    public void BoardcastActorInfos()
    {
        //List<string> allActor = GetRoommateInfo();
        //string info = SerializeHelper.ListCompose(allActor);
        //byte[] message = SerializeHelper.ConvertToByte(info);
        //BoardcastMessage(MessageConvention.getRoommateInfo, message);
        byte[] message = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(RoomInfo.ActorList.Values));
        BoardcastMessage(MessageConvention.getRoommateInfo, message);
    }

    /// <summary>
    /// 广播消息逻辑
    /// </summary>
    /// <param name="convention">广播类型</param>
    /// <param name="message">广播值</param>
    /// <param name="uniqueID">除该用户以外都广播</param>
    private void BoardcastMessage(MessageConvention convention, byte[] message, int uniqueID = -1, byte xieyiSecond = 0)
    {
        for (int i = 0; i < RoomInfo.ActorList.Count; i++)//逐个玩家遍历发送消息
        {
            if (RoomInfo.ActorList[i] == null)
            {
                continue;
            }
            if (RoomInfo.ActorList[i].CurState == RoomActorState.Offline)//离线玩家不发送
            {
                continue;
            }
            if (RoomInfo.ActorList[i].UniqueID != uniqueID)
            {
                AsyncIOCPServer.instance.SendMessageToUser(RoomInfo.ActorList[i].MemberID, message, (byte)convention, xieyiSecond);
            }
        }
    }

    #endregion



    public void ChangeRoomState(RoomActorState state)
    {
        byte[] message = new byte[] { };
        RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
        Log4Debug("房间更改状态为:" + RoomInfo.CurState + "->" + state);
        RoomInfo.CurState = state;
        switch (RoomInfo.CurState)
        {
            case RoomActorState.NoReady:
                foreach (var item in RoomInfo.ActorList)
                {
                    if (item.Value == null)
                    {
                        continue;
                    }
                    //初始化房间玩家部分信息
                    item.Value.InitActor();
                    //广播房间用户准备状态为未准备
                    roomActorUpdate = new RoomActorUpdate()
                    {
                        userIndex = item.Key,
                        update = (int)item.Value.CurState + ""
                    };
                    UpdateState(roomActorUpdate);
                }
                break;
            case RoomActorState.Ready:
                Log4Debug("玩家都准备了。");
                //设置所有玩家默认值
                foreach (var item in RoomInfo.ActorList)
                {
                    if (item.Value == null)
                    {
                        continue;
                    }
                    item.Value.MyModelInfo.pos = (NetVector3)GameTypeManager.BackStandPos(RoomInfo.RoomType, item.Key);
                    item.Value.MyModelInfo.rotate = new NetVector3(0, GameTypeManager.BackLookAt(RoomInfo.RoomType, item.Key), 0);
                    roomActorUpdate = new RoomActorUpdate()
                    {
                        userIndex = item.Key,
                        update = (int)RoomActorState.PrepareModel + ""
                    };
                    UpdateState(roomActorUpdate);//广播,修改玩家状态用来准备本机数据
                }
                //方块初始化
                BoxList = GameTypeManager.BackBoxInfosByType(RoomInfo.RoomType);

                //倒计时进入游戏
                ChangeRoomState(RoomActorState.WaitForStart);
                break;
            case RoomActorState.WaitForStart:
                //
                Log4Debug("开始倒计时");
                //时间初始化
                PassedCountDownTime = 0;
                CountDownTimer = new Timer(new TimerCallback(GameCountDowning), null, 0, 1 * 1000);

                break;
            case RoomActorState.Gaming:
                Log4Debug("开始游戏");

                foreach (var item in RoomInfo.ActorList)
                {
                    if (item.Value == null)
                    {
                        continue;
                    }
                    if (item.Value.CurState == RoomActorState.WaitForStart)
                    {
                        roomActorUpdate = new RoomActorUpdate()
                        {
                            userIndex = item.Key,
                            update = (int)RoomActorState.Gaming + ""
                        };
                        UpdateState(roomActorUpdate);//广播,修改玩家状态用来准备本机数据
                    }
                    else
                    {
                        Log4Debug("账号：" + item.Value.MemberID + "在倒计时期间内未准备好模型。");
                    }
                }
                //保存帧同步
                RoomInfo.FrameIndex = 0;
                int keyNum = (int)(RoomInfo.GameTime / frameTime) + 1;
                FrameGroup = new Dictionary<int, FrameInfo>();
                for (int i = 0; i < keyNum; i++)
                {
                    FrameGroup.Add(i, new FrameInfo() { frameIndex = i, frameData = new List<byte[]>() });
                }

                //ActorMoveDirection md = new ActorMoveDirection() { userIndex = 0, direction = new UnityEngine.Vector3(100, 0, 100), speed = 300 };
                //MessageXieYi tm = new MessageXieYi((byte)MessageConvention.moveDirection, 0, SerializeHelper.ConvertToByte(md.GetSendInfo()));
                //FrameGroup[1].frameData.Add(tm.ToBytes());
                //FrameGroup[1].frameData.Add(tm.ToBytes());

                FrameTimer = new Timer(new TimerCallback(GameFrameReconding), null, 0, frameTime);
                //
                BoardcastMessage(MessageConvention.startGaming, new byte[1] { 1 });
                //发送游戏时间
                PassedGameTime = 0;
                GameTimer = new Timer(new TimerCallback(GameTimeIsFinish), null, 0, 1 * 1000);
                break;
            case RoomActorState.GameEnd:
                //帧同步
                FrameTimer.Dispose();
                Log4Debug("退出计数");


                TeamType winTeam = GetWinnerTeam();
                Log4Debug("游戏结束" + winTeam + "胜利");
                message = SerializeHelper.ConvertToByte((int)winTeam + "");
                BoardcastMessage(MessageConvention.endGaming, message);
                //
                InitRoom();
                break;
        }
    }




    /// <summary>
    /// userToken只有一个MemberID能用
    /// </summary>
    /// <param name="userToken"></param>
    public void ReConnect(AsyncUserToken userToken)
    {
        List<RoomActor> allRA = new List<RoomActor>(RoomInfo.ActorList.Values);
        RoomActor actor = null;
        for (int i = 0; i < allRA.Count; i++)
        {
            if (allRA[i].MemberID == userToken.userInfo.MemberID)
            {
                actor = allRA[i];
                userToken.userInfo = actor;
                //给房间其他玩家发送更新重连状态


                break;
            }
        }

        if (actor != null)
        {
            //Log4Debug("----------ReConnect--------" + userToken.ConnectSocket.RemoteEndPoint);
            //给自己发送重连消息，因为没有先初始化房间
            MessageXieYi xieyi = null;
            xieyi = new MessageXieYi((byte)MessageConvention.reConnect, 0, SerializeHelper.ConvertToByte("开始重连"));
            AsyncIOCPServer.instance.SaveSendMessage(userToken, xieyi.ToBytes());
            //
            switch (RoomInfo.CurState)
            {
                case RoomActorState.WaitForStart:
                case RoomActorState.Gaming:
                    Log4Debug("重连----------" + userToken.ConnectSocket.RemoteEndPoint);
                    byte[] message = null;
                    message = SerializeHelper.Serialize<RoomInfo>(RoomInfo);
                    xieyi = new MessageXieYi((byte)MessageConvention.joinRoom, 0, message);
                    Log4Debug("数据长度：" + xieyi.ToBytes().Length);
                    AsyncIOCPServer.instance.SaveSendMessage(userToken, xieyi.ToBytes());

                    message = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(RoomInfo.ActorList.Values));
                    xieyi = new MessageXieYi((byte)MessageConvention.getRoommateInfo, 0, message);
                    AsyncIOCPServer.instance.SaveSendMessage(userToken, xieyi.ToBytes());

                    RoomActorUpdate roomActorUpdate = null;
                    //roomActorUpdate = new RoomActorUpdate()
                    //{
                    //    userIndex = actor.UniqueID,
                    //    update = (int)RoomActorState.ReConnect + ""
                    //};
                    //UpdateState(roomActorUpdate);

                    //准备模型
                    roomActorUpdate = new RoomActorUpdate()
                    {
                        userIndex = actor.UniqueID,
                        update = (int)RoomActorState.PrepareModel + ""
                    };
                    UpdateState(roomActorUpdate);
                    break;
                default:
                    Log4Debug("检查房间在状态:" + RoomInfo.CurState + "时该执行什么逻辑。");
                    break;
            }
        }

    }





    public void Log4Debug(string msg)
    {
        LogManager.WriteLog(msg);
    }
}

