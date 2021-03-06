﻿using Network_Kcp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class SingleRoom
{
    private Dictionary<int, AsyncUserToken> UserTokenInfo { get; set; }

    public int PassedCountDownTime = 0;//倒计时已经过了多久
    public int CountDownTime = 5 * 1000;//倒计时X秒以后开始游戏


    public int PassedGameTime = 0;// 游戏已经过了多久
    private int FrameCount;
    public RoomInfo RoomInfo { get; set; }//客户端和服务器通用保存房间属性的变量类
    public RoomActorState CurState { get; set; }//房间当前状态
    public int FrameIndex { get; set; }//房间当前运行帧

    /// <summary>
    /// 房間中的會員列表
    /// </summary>
    public ConcurrentDictionary<int, RoomActor> ActorList { get; set; }
    /// <summary>
    /// 游戏棋子对应的拥有玩家ID--盒子序号，用户站位
    /// </summary>
    public ConcurrentDictionary<int, BoxInfo> BoxList { get; set; }
    /// <summary>
    /// 所有生成的buff信息
    /// </summary>
    public ConcurrentDictionary<int, BuffInfo> BuffList { get; set; }
    /// <summary>
    /// 记录一共生成了多少个Buff
    /// </summary>
    public int BuffIndex { get; set; }

    /// <summary>
    /// 队伍信息
    /// </summary>
    public ConcurrentDictionary<TeamType, List<int>> AllTeamInfo { get; set; }

    public Timer CountDownTimer { get; set; }

    /// <summary>
    /// 最后一次广播的帧序号
    /// </summary>
    public int FrameCastIndex { get; set; }

    /// <summary>
    /// 保存每帧玩家发送来的数据
    /// </summary>
    private ConcurrentDictionary<int, FrameInfo> FrameGroup;


    public udpUser[] udpUserInfo { get; set; }

    /// <summary>
    /// 每帧广播用到的线程
    /// </summary>
    public Thread ThFrameCount;
    

    public SingleRoom(int roomID, string roomName, GameModel roomType)
    {
        RoomInfo = new RoomInfo(roomID, roomName, roomType);
        CurState = RoomActorState.NoReady;
        //
        UserTokenInfo = new Dictionary<int, AsyncUserToken>();
        for (int i = 0; i < RoomInfo.Limit; i++)
        {
            UserTokenInfo.Add(i, null);
        }
        ActorList = new ConcurrentDictionary<int, RoomActor>() { };
        for (int i = 0; i < RoomInfo.Limit; i++)
        {
            InitRoomActorByIndex(i);
        }
        udpUserInfo = new udpUser[RoomInfo.Limit];
        //
        switch (roomType)
        {
            case GameModel.组队模式:
                AllTeamInfo = new ConcurrentDictionary<TeamType, List<int>>();
                AllTeamInfo.AddOrUpdate(TeamType.Blue, new List<int>() { }, (key, oldValue) => new List<int>() { });
                AllTeamInfo.AddOrUpdate(TeamType.Red, new List<int>() { }, (key, oldValue) => new List<int>() { });
                BoxList = new ConcurrentDictionary<int, BoxInfo>();
                BuffList = new ConcurrentDictionary<int, BuffInfo>();
                break;
        }
    }

    // 會員加入房間
    public bool Join(AsyncUserToken userToken, out int UniqueID)
    {
        UniqueID = -1;
        lock (ActorList)
        {
            foreach (KeyValuePair<int, RoomActor> item in ActorList)
            {
                if (item.Value.Register == null)
                {
                    UniqueID = item.Key;
                    item.Value.Register = userToken.userInfo.Register;//先占位，放开lock，允许其他人加入。
                    break;
                }
            }
        }
        if (UniqueID != -1)
        {
            Log4Debug("账号->" + userToken.userInfo.Register.userID + " 用户名->" + userToken.userInfo.Register.name + " 加入房间->" + RoomInfo.RoomID + " 站位为->" + UniqueID);
            //
            TeamType myTeam = TeamType.Both;
            switch (RoomInfo.RoomType)
            {
                case GameModel.组队模式:
                    if (UniqueID % 2 == 0)//红蓝两队
                    {
                        myTeam = TeamType.Blue;
                    }
                    else
                    {
                        myTeam = TeamType.Red;
                    }
                    break;
                case GameModel.Boss模式:
                    myTeam = TeamType.Blue;
                    break;
            }
            //

            RoomActor actor = new RoomActor(RoomInfo.RoomID, UniqueID, userToken.userInfo.Register, myTeam);
            actor.MyModelInfo.pos = (NetVector3)GameTypeManager.BackStandPos(RoomInfo.RoomType, UniqueID);
            actor.MyModelInfo.rotate = new NetVector3(0, GameTypeManager.BackLookAt(RoomInfo.RoomType, UniqueID), 0);
            actor.MyModelInfo.animation = 0;

            userToken.userInfo = actor;
            lock (UserTokenInfo)
            {
                UserTokenInfo[UniqueID] = userToken;
            }
            lock (ActorList)
            {
                ActorList[UniqueID] = actor;
            }
            BoardcastActorInfo(UniqueID);
            //广播房间信息
            byte[] message = SerializeHelper.Serialize<RoomInfo>(RoomInfo);
            MessageXieYi xieyi = new MessageXieYi((byte)MessageConvention.getRoomInfo, 0, message);
            AsyncIOCPServer.instance.SendSave(userToken, xieyi.ToBytes());
            return true;
        }
        else
        {
            return false;
        }
    }


    #region 判断命令

    public void CheckQuit(AsyncUserToken userToken, QuitInfo quitInfo)
    {
        if (quitInfo.userIndex != 0 && quitInfo.userIndex != quitInfo.quitUnique)//不是房主，并且踢的也不是自己
        {
            Log4Debug("无权踢人");
            quitInfo.isQuit = false;
        }
        else
        {
            quitInfo.isQuit = Quit(quitInfo.quitUnique);
        }
        byte[] message = SerializeHelper.Serialize<QuitInfo>(quitInfo);
        MessageXieYi xieyi = new MessageXieYi((byte)MessageConvention.quitRoom, 0, message);
        AsyncIOCPServer.instance.SendSave(UserTokenInfo[quitInfo.quitUnique], xieyi.ToBytes());
    }
    // 會員離開房間
    public bool Quit(int unique)
    {
        if (ActorList.ContainsKey(unique))
        {
            if (ActorList[unique].CurState != RoomActorState.Gaming)//当前正在游戏则不能退出房间
            {
                //
                InitRoomActorByIndex(unique);
                BoardcastActorInfo(unique);
                return true;
            }
        }
        return false;
    }

    public bool IsEmptyRoom()
    {
        foreach (KeyValuePair<int, RoomActor> item in ActorList)
        {
            if (item.Value.Register != null)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsMaster(string memberID)
    {
        return ActorList[0].Register.userID == memberID;
    }

    #endregion


    public void InitRoom()
    {
        List<RoomActor> allRA = new List<RoomActor>(ActorList.Values);
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

    public void UpdateUDP(int unique, udpUser user)
    {
        Log4Debug("站位：" + unique + " udp地址：" + user.lastPoint);
        lock (udpUserInfo)
        {
            udpUserInfo[unique] = user;
        }
    }
    public void ClearUDP(int unique)
    {
        udpUserInfo[unique] = null;
    }

    public void UpdateAnimation(ActorNetAnimation netAniamtion)
    {
        if (ActorList[netAniamtion.userIndex] == null)
            return;
        ActorList[netAniamtion.userIndex].MyModelInfo.animation = netAniamtion.animationIndex;
        byte[] message = SerializeHelper.ConvertToByte(netAniamtion.GetSendInfo());
        BoardcastMessage(MessageConvention.updateActorAnimation, message, netAniamtion.userIndex);
    }

    public void UpdateState(RoomActorUpdate roomActorUpdate)
    {
        int index = roomActorUpdate.userIndex;
        RoomActorState upState = (RoomActorState)int.Parse(roomActorUpdate.update);
        if (ActorList[index] == null)
            return;
        //if (ActorList[index].CurState != RoomActorState.ReConnect)
        {
            Log4Debug("站位 " + index + " 更新当前状态：" + ActorList[index].CurState + " -> " + (RoomActorState)int.Parse(roomActorUpdate.update));
            ActorList[index].CurState = upState;
            byte[] message = SerializeHelper.ConvertToByte(roomActorUpdate.GetSendInfo());
            BoardcastMessage(MessageConvention.updateActorState, message);

            if (CheckIsAllFixedState(RoomActorState.Ready))
            {
                ChangeRoomState(RoomActorState.Ready);
            }
            if (CurState == RoomActorState.Gaming)
            {
                switch (ActorList[index].CurState)
                {
                    case RoomActorState.ReConnect:
                        break;
                    case RoomActorState.WaitForStart:
                        Log4Debug("模型未准备好的玩家准备好进入游戏了。");
                        string starttime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        byte[] start = SerializeHelper.ConvertToByte(starttime);
                        MessageXieYi xieyi = new MessageXieYi((byte)MessageConvention.startGaming, 0, start);
                        AsyncIOCPServer.instance.SendSave(UserTokenInfo[index], xieyi.ToBytes());

                        roomActorUpdate = new RoomActorUpdate()
                        {
                            userIndex = index,
                            update = (int)RoomActorState.Gaming + ""
                        };
                        UpdateState(roomActorUpdate);//广播,修改玩家状态用来准备本机数据

                        break;
                    case RoomActorState.Dead:
                        ActorList[index].timerDead = new Timer(new TimerCallback(SetAfterDead), index, RoomActor.DeadLastTime, 0);
                        break;
                }
            }
        }
        //else//reConnect时修改状态
        //{
        //    if (upState == RoomActorState.WaitForStart && RoomInfo.CurState == RoomActorState.Gaming)
        //    {
        //        ActorList[index].CurState = RoomActorState.Gaming;
        //        roomActorUpdate.update = (int)ActorList[index].CurState + "";
        //        byte[] message = SerializeHelper.ConvertToByte(roomActorUpdate.GetSendInfo());
        //        BoardcastMessage(MessageConvention.updateActorState, message);
        //    }
        //    else
        //    {
        //        Log4Debug("想要在重连时更新状态：" + upState);
        //    }
        //}
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
        byte[] message = null;
        MessageXieYi xieyi = null;
        int boxIndex = -1;
        //
        switch (bulletInfo.shootTag)
        {
            case ShootTag.Box:
                //设置色块拥有者
                lock (BoxList)
                {
                    boxIndex = int.Parse(bulletInfo.shootInfo);

                    BoxInfo boxInfo = new BoxInfo() { ownerIndex = -1, myIndex = boxIndex };
                    BoxList.AddOrUpdate(boxIndex, boxInfo, (key, oldValue) => boxInfo);

                    if (BoxList[boxIndex].ownerIndex < 0)
                    {
                        BoxList[boxIndex].ownerIndex = bulletInfo.userIndex;
                        //生成buff
                        BuffInfo buffInfo = new BuffInfo() { ownerIndex = -1, myIndex = BuffIndex };
                        lock (BuffList)
                        {
                            buffInfo.boxIndex = boxIndex;
                            buffInfo.type = RandomBuffType();//随机一个buff类型
                            BuffList.AddOrUpdate(BuffIndex, buffInfo, (key, oldValue) => buffInfo);
                            BuffIndex++;
                        }
                        Log4Debug("在盒子编号->" + boxIndex + " 掉落Buff,编号->" + buffInfo.myIndex + ",类型->" + buffInfo.type);
                        //保存子弹消息
                        message = SerializeHelper.Serialize<BulletInfo>(bulletInfo);
                        xieyi = new MessageXieYi((byte)MessageConvention.bulletInfo, 0, message);
                        SetRecondFrame(xieyi.ToBytes());
                        //保存Buff消息
                        message = SerializeHelper.Serialize<BuffInfo>(buffInfo);
                        xieyi = new MessageXieYi((byte)MessageConvention.createBuff, 0, message);
                        SetRecondFrame(xieyi.ToBytes());
                    }
                    else//该色块已被其他人击碎
                    {
                        return;
                    }
                }
                break;
            case ShootTag.Character:
                //
                int shootedIndex = int.Parse(bulletInfo.shootInfo);
                if (ActorList[shootedIndex].MyTeam == ActorList[bulletInfo.userIndex].MyTeam)
                {
                    return;
                }
                if (ActorList[shootedIndex].CurState == RoomActorState.Gaming)
                {
                    //增加击杀数
                    ActorList[bulletInfo.userIndex].KillCount++;
                    if (CurState == RoomActorState.Gaming)
                    {
                        message = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(ActorList.Values));
                        BoardcastMessage(MessageConvention.getRoommateInfo, message);
                    }
                    //改变被射击者状态
                    RoomActorUpdate dead = new RoomActorUpdate()
                    {
                        userIndex = shootedIndex,
                        update = (int)RoomActorState.Dead + ""
                    };
                    UpdateState(dead);
                }
                else if (ActorList[shootedIndex].CurState == RoomActorState.Invincible)
                {
                    Log4Debug("射击者站位：" + bulletInfo.userIndex + " 攻击无敌站位：->" + shootedIndex);
                }
                else
                {
                    Log4Debug("射击者站位：" + bulletInfo.userIndex + " 正在鞭尸位置->" + shootedIndex);
                }
                //保存子弹消息
                message = SerializeHelper.Serialize<BulletInfo>(bulletInfo);
                xieyi = new MessageXieYi((byte)MessageConvention.bulletInfo, 0, message);
                SetRecondFrame(xieyi.ToBytes());
                break;
            case ShootTag.Wall:
                //打中墙的消息就不存了
                break;
            case ShootTag.Buff:
                int buffIndex = int.Parse(bulletInfo.shootInfo);
                //Log4Debug("站位：" + bulletInfo.userIndex + " 请求拾取Buff->" + buffIndex);
                lock (BuffList)
                {
                    if (BuffList[buffIndex].ownerIndex < 0)
                    {
                        BuffList[buffIndex].ownerIndex = bulletInfo.userIndex;
                        Log4Debug("站位：" + bulletInfo.userIndex + " 拾取了Buff->" + buffIndex);
                    }
                    else//该buff已被其他人加成
                    {
                        return;
                    }
                }
                //保存Buff消息
                message = SerializeHelper.Serialize<BuffInfo>(BuffList[buffIndex]);
                xieyi = new MessageXieYi((byte)MessageConvention.getBuff, 0, message);
                SetRecondFrame(xieyi.ToBytes());
                break;
        }


        //广播发送消息
        //byte[] message = SerializeHelper.ConvertToByte(bulletInfo));
        //BoardcastMessage(MessageConvention.bulletInfo, message);
    }

    private void SetAfterDead(object unique)
    {
        int index = (int)unique;
        ActorList[index].timerDead.Dispose();
        if (CurState == RoomActorState.Gaming)//游戏中计时器才可以切换状态
        {
            //Log4Debug("执行回调设置复活，状态无敌。");
            RoomActorUpdate roomActorUpdate = new RoomActorUpdate() { userIndex = index, update = (int)RoomActorState.Invincible + "" };
            UpdateState(roomActorUpdate);
            ActorList[index].timerInvincible = new Timer(new TimerCallback(SetAfterInvincible), index, RoomActor.InvincibleLastTime, 0);
        }
    }
    private void SetAfterInvincible(object unique)
    {
        int index = (int)unique;
        ActorList[index].timerInvincible.Dispose();
        if (CurState == RoomActorState.Gaming)//游戏中计时器才可以切换状态
        {
            //Log4Debug("执行回调取消无敌。");
            RoomActorUpdate roomActorUpdate = new RoomActorUpdate() { userIndex = index, update = (int)RoomActorState.Gaming + "" };
            UpdateState(roomActorUpdate);
        }
    }

    public void UpdatePrepare(RoomActorUpdate roomActorUpdate, AsyncUserToken userToken)
    {
        if (ActorList[roomActorUpdate.userIndex] == null)
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
    public void SetRecondFrame(byte[] message, int setIndex = -1)
    {
        if (message == null)
        {
            Log4Debug("检查为什么存值为null");
            return;
        }
        int curIndex = FrameIndex;
        if (setIndex != -1)
        {
            curIndex = setIndex;
        }
        if (curIndex >= FrameCount)
        {
            Log4Debug("存储帧：" + curIndex + "大于等于总长：" + FrameCount);
            return;
        }
        //Log4Debug("存储帧：" + curIndex);
        if (CurState == RoomActorState.Gaming)
        {
            FrameInfo frameInfo = new FrameInfo() { frameIndex = curIndex, frameData = new List<byte[]>() };
            lock (FrameGroup.GetOrAdd(curIndex, frameInfo).frameData)
            {
                FrameGroup[curIndex].frameData.Add(message);
            }
        }
    }



    public byte[] GetBoardFrame(int start)
    {
        return GetBoardFrame(start, start);
    }

    public byte[] GetBoardFrame(int start, int end)
    {
        List<FrameInfo> infos = new List<FrameInfo>() { };
        if (start < 0)
        {
            Log4Debug("请求帧start：" + start + " 小于0");
        }
        else if (start >= FrameCount || end >= FrameCount)
        {
            Log4Debug("请求帧start：" + start + " end:" + end + " 超过帧总数：" + FrameCount + "，请检查代码逻辑");
        }
        else if (end > FrameIndex)
        {
            Log4Debug("请求帧start：" + start + " end:" + end + " 超过当前运行帧数：" + FrameIndex);
        }
        else
        {
            for (int i = start; i <= end; i++)
            {
                infos.Add(FrameGroup.GetOrAdd(i, new FrameInfo() { frameIndex = i, frameData = new List<byte[]>() }));
            }
            return SerializeHelper.Serialize<List<FrameInfo>>(infos);
        }
        return null;
    }

    private DateTime lastRecondTime { get; set; }
    /// <summary>
    /// 服务器帧根据时间递增
    /// </summary>
    /// <param name="obj"></param>
    private void GameFrameReconding(object obj)
    {
        lastRecondTime = DateTime.Now;
        Thread.Sleep(RoomInfo.frameTime - (RoomInfo.frameTime / 2));
        while (CurState == RoomActorState.Gaming)//需要在游戏结束的时候，不再运行广播线程
        {
            DateTime cur = DateTime.Now;
            double sub = cur.Subtract(lastRecondTime).TotalMilliseconds;
            if ((int)sub >= RoomInfo.frameTime)
            {
                lastRecondTime = lastRecondTime.Add(TimeSpan.FromMilliseconds(RoomInfo.frameTime));
                //Log4Debug("增加15-》" + addTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                //ThreadPool.QueueUserWorkItem(new WaitCallback(FrameLogic), null);
                FrameLogic(null);
                if ((int)(sub - RoomInfo.frameTime) == 0)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    //Log4Debug("间隔->" + sub);
                }
            }
        }
    }

    private void FrameLogic(object obj)
    {
        int end = FrameIndex;
        int tempIndex = FrameIndex + 1;
        FrameIndex = tempIndex;
        int start = tempIndex - RoomInfo.frameInterval;

        byte[] message = GetBoardFrame(start, end);//当前帧的前2帧一起发送，是用来防止udp丢包的
        if (message != null)
        {
            //BoardcastMessage(MessageConvention.frameData, message);
            Log4Debug("udp广播:" + start + "/" + end);
            BoardcastMessage(MessageConvention.frameData, message, -1, 0, ProtocolType.Udp);
        }
        else
        {
            Log4Debug("帧数据为空，广播（" + start + "->" + end + "）失败");
        }
        if (tempIndex == FrameCount)//判断游戏是否结束。
        {
            ChangeRoomState(RoomActorState.GameEnd);
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
        for (int i = 0; i < ActorList.Count; i++)
        {
            if (ActorList[i] == null || ActorList[i].CurState != state)
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
            int teamIndex = BuffList[item.Value.ownerIndex].ownerIndex % 2;

            if (teamIndex == 0)
            {
                TwoTeam[0] += 1;
            }
            else if (teamIndex == 1)
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


    public void InitRoomActorByIndex(int unique)
    {
        RoomActor roomActor = new RoomActor(RoomInfo.RoomID, unique, null, TeamType.Both);
        ActorList.AddOrUpdate(unique, roomActor, (key, oldValue) => roomActor);
    }


    private BuffType RandomBuffType()
    {
        BuffType type = BuffType.Score;
        Random rad = new Random();//实例化随机数产生器rad
        int value = rad.Next(0, 100);//用rad生成大于等于0，小于100的随机数；
        if (value > 0 && value < 80)
        {
            type = BuffType.Score;
        }
        else
        {
            type = BuffType.CanKill;
        }
        return type;
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
            CountDownTimer.Dispose();
            ChangeRoomState(RoomActorState.Gaming);
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
        foreach (var item in ActorList)
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
            AsyncIOCPServer.instance.SendSave(userToken, xieyi.ToBytes());
        }
        //准备数据发送完毕，打开UI
        message = new byte[] { 1 };//此处的数组应该是初始数据
        MessageXieYi xieyiFinish = new MessageXieYi((byte)MessageConvention.getPreGameData, 0, message);
        AsyncIOCPServer.instance.SendSave(userToken, xieyiFinish.ToBytes());
    }



    /// <summary>
    /// 广播房间人物信息给房间其他用户
    /// </summary>
    /// <param name="memberID">除该ID以外都广播</param>
    public void BoardcastActorInfo(int uniqueID)
    {
        //需要修改
        byte[] message = SerializeHelper.Serialize<List<RoomActor>>(new List<RoomActor>(ActorList.Values));
        BoardcastMessage(MessageConvention.getRoommateInfo, message, uniqueID);
    }

    /// <summary>
    /// 广播消息逻辑
    /// </summary>
    /// <param name="convention">广播类型</param>
    /// <param name="message">广播值</param>
    /// <param name="uniqueID">除该用户以外都广播</param>
    private void BoardcastMessage(MessageConvention convention, byte[] message, int uniqueID = -1, byte xieyiSecond = 0, ProtocolType netType = ProtocolType.Tcp)
    {
        MessageXieYi msgXY = new MessageXieYi((byte)convention, xieyiSecond, message);
        for (int i = 0; i < ActorList.Count; i++)//逐个玩家遍历发送消息
        {
            if (ActorList[i].Register == null)
            {
                continue;
            }
            if (ActorList[i].CurState == RoomActorState.Offline)//离线玩家不发送
            {
                continue;
            }
            if (ActorList[i].UniqueID != uniqueID)
            {
                if (netType == ProtocolType.Tcp)
                {
                    AsyncIOCPServer.instance.SendSave(UserTokenInfo[ActorList[i].UniqueID], msgXY.ToBytes());
                }
                else if (netType == ProtocolType.Udp)
                {
                    if (udpUserInfo[i] == null)
                    {
                        Log4Debug("广播时，站位{0}用户udp未连接。" + i);
                        continue;
                    }
                    UdpServer.Instance.SendMessage(udpUserInfo[i].lastPoint, msgXY.ToBytes());
                }
            }
        }
    }

    #endregion



    public void ChangeRoomState(RoomActorState state)
    {
        byte[] message = new byte[] { };
        RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
        Log4Debug("房间更改状态为:" + CurState + "->" + state);
        CurState = state;
        switch (CurState)
        {
            case RoomActorState.NoReady:
                foreach (var item in ActorList)
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
                foreach (var item in ActorList)
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
                BoxList = new ConcurrentDictionary<int, BoxInfo>();

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
                Log4Debug("开始游戏:" + FrameIndex);

                foreach (var item in ActorList)
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
                        Log4Debug("用户：" + item.Value.Register.name + "在倒计时期间内未准备好模型。");
                    }
                }

                //保存帧同步
                FrameIndex = 0;
                FrameCount = (int)(RoomInfo.GameTime / RoomInfo.frameTime);
                FrameGroup = new ConcurrentDictionary<int, FrameInfo>();
                //FrameGroup = new Dictionary<int, FrameInfo>();
                //for (int i = 0; i < FrameCount; i++)
                //{
                //    FrameGroup.Add(i, new FrameInfo() { frameIndex = i, frameData = new List<byte[]>() });
                //}
                //线程池调用时间间隔逻辑
                ThFrameCount = new Thread(GameFrameReconding);
                ThFrameCount.IsBackground = true;
                ThFrameCount.Start();
                //ThreadPool.QueueUserWorkItem(new WaitCallback(GameFrameReconding), null);
                //
                string starttime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                byte[] start = SerializeHelper.ConvertToByte(starttime);
                BoardcastMessage(MessageConvention.startGaming, start);
                //发送游戏时间
                PassedGameTime = 0;
                break;
            case RoomActorState.GameEnd:
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
        List<RoomActor> allRA = new List<RoomActor>(ActorList.Values);
        RoomActor actor = null;
        for (int i = 0; i < allRA.Count; i++)
        {
            if (allRA[i].Register.userID == userToken.userInfo.Register.userID)
            {
                actor = allRA[i];
                userToken.userInfo = actor;
                userToken.userInfo.CurState = RoomActorState.Online;
                UserTokenInfo[userToken.userInfo.UniqueID] = userToken;
                break;
            }
        }
        //房间信息


        MessageXieYi xieyi = new MessageXieYi((byte)MessageConvention.joinRoom, 0, SerializeHelper.ConvertToByte(actor.UniqueID + ""));
        AsyncIOCPServer.instance.SendSave(userToken, xieyi.ToBytes());
    }
    public void GetReConnectFrameData(int unique)
    {
        AsyncUserToken userToken = UserTokenInfo[unique];
        int curFrame = FrameIndex;
        byte[] message = null;
        MessageXieYi xieyi = null;

        xieyi = new MessageXieYi((byte)MessageConvention.reConnectIndex, 0, SerializeHelper.ConvertToByte(curFrame + ""));
        AsyncIOCPServer.instance.SendSave(userToken, xieyi.ToBytes());
        Log4Debug("重连标记帧：" + curFrame);

        //帧数据
        int limetNum = 1800;
        int count = curFrame / limetNum;
        count += 1;
        for (int i = 0; i < count; i++)
        {
            int min = i * limetNum;
            int max = (i + 1) * limetNum + 1;//多加1帧
            if (max > curFrame)
            {
                max = curFrame;
            }
            message = GetBoardFrame(min, max);
            if (message == null)
            {
                Log4Debug("重连数据中有空数据->(" + min + "," + max + ")");
            }
            xieyi = new MessageXieYi((byte)MessageConvention.frameData, 0, message);
            AsyncIOCPServer.instance.SendSave(userToken, xieyi.ToBytes());
            Log4Debug("发送重连数据->(" + min + "," + max + ")");
        }
    }




    public void Log4Debug(string msg)
    {
        NetworkDebuger.Log(this.GetType().Name + ":" + msg);
    }
}