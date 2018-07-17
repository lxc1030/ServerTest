using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;
using Newtonsoft.Json.Linq;
/// <summary>
/// 游戏运行中的相关逻辑
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private static string XMLName = "EndlessHardConfig";
    //public Dictionary<int, EndlessHardConfig> config = new Dictionary<int, EndlessHardConfig>();

    private Dictionary<int, CharacterCommon> memberGroup = new Dictionary<int, CharacterCommon>();
    public Transform transActor;
    public Transform transBullet;
    public Transform transBuff;

    #region 帧同步相关变量
    public FramePlayType CurrentPlayType;
    public int reConnectIndex = 0;//重连时需要复现到的帧编号----//重连不需要则为-1
    public int frameIndex = 0;//当前复现到哪一帧了
    public int frameMaxIndex = 0;
    private int frameWaitCount = 0;//数据缓存等待了帧数

    public int requestFrameIndex;//上次帧不存在请求的是哪个帧



    public Dictionary<int, FrameInfo> FrameInfos = new Dictionary<int, FrameInfo>();


    #endregion

    /// <summary>
    /// 动画
    /// </summary>
    public List<AnimationClip> animationGroup = new List<AnimationClip>();

    private void Awake()
    {
        DontDestroyOnLoad(this);
        instance = this;
        //
        instance.Init();
    }

    #region 注册Socket处理

    /// <summary>
    /// 待处理的Socket信息
    /// </summary>
    private Queue<MessageXieYi> serverEventTCP = new Queue<MessageXieYi>();
    /// <summary>
    /// 该脚本要处理的socket数据类型----每个脚本自己定义自己要侦听的消息类型，只会接收已定义的协议数据
    /// </summary>
    private List<MessageConvention> messageHandleTCP = new List<MessageConvention>()
    {
        MessageConvention.login,
        MessageConvention.getHeartBeatTime,
        MessageConvention.getRoomInfo,
        MessageConvention.reConnectCheck,
        MessageConvention.quitRoom,
        MessageConvention.updateActorState,
        MessageConvention.updateModelInfo,
        MessageConvention.getPreGameData,
        MessageConvention.startGaming,
        MessageConvention.endGaming,
        MessageConvention.frameData,
    };
    //
    private Queue<MessageXieYi> serverEventUDP = new Queue<MessageXieYi>();
    private List<MessageConvention> messageHandleUDP = new List<MessageConvention>()
    {
        MessageConvention.setUDP,
    };

    private void Start()
    {
        SocketManager.ListenDelegate(true, messageHandleTCP, OperationListenInfoTCP);
        UDPManager.ListenDelegate(true, messageHandleUDP, OperationListenInfoUDP);
    }
    private void OnDisable()
    {
        SocketManager.ListenDelegate(false, messageHandleTCP, OperationListenInfoTCP);
        UDPManager.ListenDelegate(false, messageHandleUDP, OperationListenInfoUDP);
    }
    /// <summary>
    /// 添加Socket管理类分发来的需要处理的数据
    /// </summary>
    /// <param name="xieyi"></param>
    public void OperationListenInfoTCP(MessageXieYi xieyi)
    {
        serverEventTCP.Enqueue(xieyi);
    }
    public void OperationListenInfoUDP(MessageXieYi xieyi)
    {
        serverEventUDP.Enqueue(xieyi);
    }

    #endregion



    //private void ReadConfig()
    //{
    //    List<EndlessHardConfig> temp = new List<EndlessHardConfig>();
    //    FileUtil.loadConfig(XMLName, ref temp, null, false);
    //    for (int i = 0; i < temp.Count; i++)
    //    {
    //        config.Add(temp[i].id, temp[i]);
    //    }
    //}




    #region 客户端逻辑

    public void Init()
    {

    }
    public int AnimationIndex(string name)
    {
        for (int i = 0; i < animationGroup.Count; i++)
        {
            if (animationGroup[i].name == name)
            {
                return i;
            }
        }
        UIManager.instance.ShowAlertTip("动画未找到：" + name);
        return 0;
    }

    public void OffLine()
    {
        DataController.instance.MyRoomInfo = null;
        DataController.instance.ActorList = null;
        CurrentPlayType = FramePlayType.游戏未开始;
        for (int i = 0; i < memberGroup.Count; i++)
        {
            memberGroup[i].BeStop();
        }
    }
    public float GetGameTime()
    {
        if (DataController.instance.MyRoomInfo == null)
        {
            return 0;
        }
        int FrameCount = (int)(DataController.instance.MyRoomInfo.GameTime / RoomInfo.frameTime);
        int leaveFrame = FrameCount - frameIndex;
        float time = leaveFrame * RoomInfo.frameTime;
        time = time / 1000;
        time = (time < 0) ? 0 : time;
        return time;
    }

    public CharacterCommon GetMyControl()
    {
        return GetControl(DataController.instance.MyLocateIndex);
    }
    public CharacterCommon GetControl(int index)
    {
        return memberGroup[index];
    }






    public void UIShot()
    {
        int index = DataController.instance.MyLocateIndex;
        if (DataController.instance.ActorList[index].CurState == RoomActorState.Dead)
        {
            return;
        }
        //旋转
        ShootInfo info = new ShootInfo();
        info.userIndex = index;
        info.bulletType = 0;
        info.position = DataController.BackNetLimetByType(GameManager.instance.GetControl(index).cameraParent.position);
        info.direction = DataController.BackNetLimetByType(GameManager.instance.GetControl(index).cameraParent.eulerAngles);
        //
        GetControl(index).ShowBullet(info);
        //发送
        SendNetInfo(info);
    }

    /// <summary>
    /// 子弹射中谁
    /// </summary>
    /// <param name="bulletInfo"></param>
    public void SendNetInfo(BulletInfo bulletInfo)
    {
        //发送
        byte[] message = SerializeHelper.Serialize<BulletInfo>(bulletInfo);
        //SocketManager.instance.SendSave((byte)MessageConvention.bulletInfo, message, false);
        UDPManager.instance.SendSave((byte)MessageConvention.bulletInfo, message);
    }

    public void SendNetInfo(ShootInfo shootInfo)
    {
        byte[] sendData = SerializeHelper.Serialize<ShootInfo>(shootInfo);
        //SocketManager.instance.SendSave((byte)MessageConvention.shootBullet, message, false);
        UDPManager.instance.SendSave((byte)MessageConvention.shootBullet, sendData);
    }
    public void SendNetInfo(ActorJump jumpInfo)
    {
        byte[] sendData = SerializeHelper.Serialize<ActorJump>(jumpInfo);
        UDPManager.instance.SendSave((byte)MessageConvention.jump, sendData);
    }

    #endregion



    #region 协议处理函数

    private void ErrorShow(ErrorType error)
    {
        string info = "";
        switch (error)
        {
            case ErrorType.userOnline:
                info = "账号在线，无法登陆。";
                break;
            case ErrorType.passwordWrong://密码错误
                info = "账号密码错误。";
                break;
            case ErrorType.userIDNotExist://账号不存在
                info = "账号密码错误。";
                break;
        }
        UIManager.instance.ShowAlertTip(info);
    }

    /// <summary>
    ///  请求房间中人物的信息
    /// </summary>
    public static void GetRoommateInfo()
    {
        RoomActorUpdate roommateInfo = new RoomActorUpdate()
        {
            userIndex = DataController.instance.MyLocateIndex,
            update = DataController.instance.MyRoomInfo.RoomID.ToString()
        };
        byte[] message = SerializeHelper.ConvertToByte(roommateInfo.GetSendInfo());
        SocketManager.instance.SendSave((byte)MessageConvention.getRoommateInfo, message);
    }


    /// <summary>
    /// 在座位上没人的时候隐藏模型
    /// </summary>
    private void UpdateMemberHide()
    {
        for (int i = 0; i < transActor.childCount; i++)
        {
            transActor.GetChild(i).gameObject.SetActive(false);
        }
    }
    public static void SendState(RoomActorState state)
    {
        RoomActorUpdate netUpdate = new RoomActorUpdate()
        {
            userIndex = DataController.instance.MyLocateIndex,
            update = (int)state + ""
        };
        string info = netUpdate.GetSendInfo();
        byte[] message = SerializeHelper.ConvertToByte(info);
        SocketManager.instance.SendSave((byte)MessageConvention.updateActorState, message, false);
    }

    public static void GetRoomInfo()
    {
        SocketManager.instance.SendSave((byte)MessageConvention.getRoomInfo, new byte[] { }, false);
    }

    public static void GetReconnectIndex()
    {
        SocketManager.instance.SendSave((byte)MessageConvention.reConnectIndex, new byte[] { }, false);
    }

    /// <summary>
    /// 模型加载完成，设置初始状态
    /// </summary>
    private void SetPrepareData(GameModelData info)
    {
        CharacterCommon member = null;
        member = GameManager.instance.memberGroup[info.userIndex];
        member.SetPosition(SerializeHelper.BackVector(info.pos));
        //member.SetRotate(SerializeHelper.BackVector(info.rotate));
        member.SetAnimation(info.animation);
    }

    private void CheckState(MessageXieYi xieyi)
    {
        string messageInfo = SerializeHelper.ConvertToString(xieyi.MessageContent);
        RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
        roomActorUpdate.SetSendInfo(messageInfo);
        if (roomActorUpdate.userIndex == DataController.instance.MyLocateIndex)
        {
            RoomActorState state = (RoomActorState)int.Parse(roomActorUpdate.update);
            Debug.Log("我的状态：" + state);
            //判断我的状态为预准备
            switch (state)
            {
                case RoomActorState.PrepareModel:
                    GameLoadingUI.Show();
                    //PrepareLocalModel();
                    break;
                case RoomActorState.ModelPrepared:
                    break;
                case RoomActorState.Invincible:
                    GameRunUI.instance.Open();
                    //MyJoystickManager.instance.Open();
                    break;
            }
        }
    }


    //public bool isPreparing = false;
    public void PrepareLocalModel()
    {
        //if (isPreparing)
        //    return;
        //isPreparing = true;
        RoomUI.Close();
        ClearModels();
        //GameRunUI.Show();
        //GameLoadingUI.Show();
        SendLoadProgress(20);
        UpdateRoomActor();
        SendLoadProgress(50);
        BoxManager.instance.Init();
        SendLoadProgress(100);
        //isPreparing = false;
    }
    private void SendLoadProgress(int index)
    {
        //发送准备进度命令
        RoomActorUpdate netUpdate = new RoomActorUpdate()
        {
            userIndex = DataController.instance.MyLocateIndex,
            update = index + ""
        };
        string info = netUpdate.GetSendInfo();
        byte[] message = SerializeHelper.ConvertToByte(info);
        SocketManager.instance.SendSave((byte)MessageConvention.prepareLocalModel, message, false);
    }
    //private void ShowModelUIName()
    //{
    //    foreach (var item in DataController.instance.ActorList)
    //    {
    //        if (item.Key != DataController.instance.MyLocateIndex)
    //        {
    //            memberGroup[item.Value.UniqueID].ShowMyName(item.Value.Register.name);
    //        }
    //    }
    //}

    /// <summary>
    /// 刷新3D模型的显示
    /// </summary>
    private void UpdateRoomActor()
    {
        GameObject obj = null;

        for (int i = 0; i < DataController.instance.MyRoomInfo.Limit; i++)
        {
            RoomActor info = DataController.instance.ActorList[i];

            if (!memberGroup.ContainsKey(i))
            {
                Debug.Log("初始化其他玩家模型站位：" + i);
                memberGroup.Add(i, null);
            }
            CharacterCommon mControl = memberGroup[i];
            if (mControl == null)//初始化生成
            {
                obj = PoolManager.instance.GetPoolObjByType(PreLoadType.Member, transActor);
                mControl = obj.GetComponent<CharacterCommon>();
                memberGroup[i] = mControl;
            }
            if (DataController.instance.ActorList[i] == null)//该位置玩家退出或不存在
            {
                mControl.gameObject.SetActive(false);
            }
            else
            {
                mControl.gameObject.SetActive(true);
                mControl.Init(info.UniqueID);
                mControl.ShowTeam(DataController.instance.ActorList[i].MyTeam);
            }
        }
    }

    public void SetDataInit()
    {
        frameIndex = 0;
        frameMaxIndex = 0;
        FrameInfos.Clear();
    }

    private void StartGaming()
    {
        HomeUI.Close();
        if (CurrentPlayType == FramePlayType.游戏未开始)
        {
            Debug.Log("开始游戏");
            frameIndex = 0;
            CurrentPlayType = FramePlayType.准备UI;
        }
        else
        {
            //这个应该是udp断线时TCP消息到了
        }
    }

    /// <summary>
    /// 断线重连和正常游戏都走这，开始游戏
    /// </summary>
    private void TrueGaming()
    {
        if (reConnectIndex == 0)
        {
            reConnectIndex = -1;
            CurrentPlayType = FramePlayType.游戏中;
            CameraManager.instance.SetCameraEnable(true);
            CameraManager.instance.SetCameraFollow(GameManager.instance.GetMyControl().cameraParent);
            GameRunUI.Show();
            //ShowModelUIName();
            //最后关闭loading
            GameLoadingUI.Close();
        }
    }
    private void EndGaming(MessageXieYi xieyi)
    {
        Debug.Log("结束游戏");
        CurrentPlayType = FramePlayType.游戏未开始;
        frameIndex = 0;
        reConnectIndex = 0;//游戏结束的时候未完成复现，则清除重连记录帧
        CameraManager.instance.SetCameraEnable(false);
        CameraManager.instance.SetCameraFollow(transform);
        foreach (var item in memberGroup)
        {
            item.Value.Init(item.Key);
        }
        GameRunUI.Close();
        TeamType winTeam = (TeamType)int.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
        RoomUI.Show();
        GameOverUI.Show(winTeam);
    }



    #endregion



    #region 帧同步处理逻辑


    public void DoFrameRequest(int startCheckIndex)
    {
        CurrentPlayType = FramePlayType.主动请求数据;
        requestFrameIndex = startCheckIndex;
        FrameInfo info = new FrameInfo() { frameIndex = startCheckIndex, frameData = new List<byte[]>() };
        string debug = "请求帧：" + info.frameIndex;
        UIManager.instance.ShowAlertTip(debug);
        Debug.LogError(debug);
        byte[] message = SerializeHelper.Serialize<FrameInfo>(info);
        SocketManager.instance.SendSave((byte)MessageConvention.frameData, message, false);
    }

    string guiInfo = "0000/0000";
    //public void OnGUI()
    //{
    //    if (DataController.instance.MyRoomInfo != null && DataController.instance.MyRoomInfo.ActorList != null)
    //    {
    //        guiInfo = frameIndex + "/" + DataController.instance.MyRoomInfo.FrameIndex;
    //        int length = (DataController.instance.MyRoomInfo.FrameIndex - frameIndex);
    //        guiInfo = guiInfo + " = " + length;
    //        GUIStyle bb = new GUIStyle();
    //        bb.normal.background = null;    //这是设置背景填充的
    //        bb.normal.textColor = Color.blue;   //设置字体颜色的
    //        bb.fontSize = 40;       //当然，这是字体大小
    //        GUI.Label(new Rect(0, 0, 200, 200), guiInfo, bb);
    //    }
    //}
    public void OnGUI()
    {
        if (DataController.instance.MyRoomInfo != null && DataController.instance.ActorList != null)
        {
            string guiInfo2 = "帧比： " + guiInfo;
            GUIStyle bb = new GUIStyle();
            bb.normal.background = null;    //这是设置背景填充的
            bb.normal.textColor = Color.blue;   //设置字体颜色的
            bb.fontSize = 40;       //当然，这是字体大小
            GUI.Label(new Rect(0, 0, 200, 200), guiInfo2, bb);
        }
    }


    /// <summary>
    /// 这个是每隔0.015s运行一次
    /// </summary>
    private void DoFrameLogin()
    {
        switch (CurrentPlayType)
        {
            case FramePlayType.游戏未开始:
                return;
            case FramePlayType.准备UI:
                TrueGaming();
                break;
            case FramePlayType.游戏中:
                //if (reConnectIndex == -1)
                {
                    guiInfo = "当前帧->" + frameIndex + ",最大帧->" + frameMaxIndex + ",总数据->" + FrameInfos.Values.Count;
                    DealFrameByMax();
                }

                //if (FrameInfos.ContainsKey(frameIndex))
                //{
                //    int forwardNum = 1;
                //    int length = DataController.instance.FrameMaxIndex - frameIndex;
                //    if (length > DataController.instance.MyRoomInfo.FrameDelay)//本地运行帧和接收帧差距超过该值，快进
                //    {
                //        forwardNum = length - DataController.instance.MyRoomInfo.FrameDelay;
                //        string info = "快进：" + frameIndex + "/" + DataController.instance.FrameMaxIndex;
                //        UIManager.instance.ShowAlertTip(info);
                //        Debug.LogError(info);
                //    }
                //    for (int i = 0; i < forwardNum; i++)//快进延迟帧的一般数值
                //    {
                //        FrameMainLogic();
                //    }
                //}
                //else
                //{
                //    CurrentPlayType = FramePlayType.空帧缓冲;
                //    //UIManager.instance.ShowAlertTip("等待帧数据：" + frameIndex);
                //    //float overTime = Time.realtimeSinceStartup - frameIndexTime;
                //    //if (overTime > requestMaxTime)
                //    //{
                //    //    frameIndexTime = Time.realtimeSinceStartup;//将该时间作为处理最后一帧的时间，这样可以重新等待延迟最大时间
                //    //    Debug.LogError("超时：" + overTime + "请求帧：" + frameIndex + "/" + DataController.instance.FrameCanIndex);
                //    //    DoFrameRequest(frameIndex);
                //    //}
                //}
                break;
            case FramePlayType.空帧缓冲:
                if (FrameInfos.ContainsKey(frameIndex))//该帧存在则执行
                {
                    CurrentPlayType = FramePlayType.游戏中;
                }
                else//没有该帧
                {
                    if (frameIndex == requestFrameIndex)//该帧已经TCP请求过了，不再二次请求
                    {
                        return;
                    }
                    frameWaitCount++;
                    if (frameWaitCount >= DataController.instance.MyRoomInfo.FrameDelay)//大于等待时长，用TCP主动请求
                    {
                        frameWaitCount = 0;
                        DoFrameRequest(frameIndex);
                    }
                    else
                    {
                        //string info = "正在缓存帧：" + frameIndex + "/" + DataController.instance.FrameMaxIndex;
                        //UIManager.instance.ShowAlertTip(info);
                        //Debug.LogError(info);
                    }
                }
                break;
            case FramePlayType.断线重连:
                //正在断线重连
                break;
            case FramePlayType.主动请求数据:

                break;
        }


    }

    private void DealFrameByMax()
    {
        int max = 0;
        if (reConnectIndex > 0)
        {
            max = reConnectIndex;
        }
        else
        {
            max = frameMaxIndex;
        }

        int forwardNum = 1;
        int length = max - frameIndex;
        if (length > DataController.instance.MyRoomInfo.FrameDelay)//本地运行帧和接收帧
        {
            forwardNum = length - DataController.instance.MyRoomInfo.FrameDelay / 2;
            string info = "Quick:" + frameIndex + "->" + max + "=" + forwardNum;
            UIManager.instance.ShowAlertTip(info);
        }

        for (int i = 0; i < forwardNum; i++)//快进延迟帧的一般数值
        {
            if (FrameInfos.ContainsKey(frameIndex))
            {
                FrameMainLogic();
            }
            else
            {
                if (reConnectIndex > 0)//断线重连
                {
                    UIManager.instance.ShowAlertTip("断线重连没数据：" + frameIndex);
                    DoFrameRequest(frameIndex);
                }
                else
                {
                    UIManager.instance.ShowAlertTip("快进" + forwardNum + "没数据：" + frameIndex);
                    CurrentPlayType = FramePlayType.空帧缓冲;
                }
                return;
            }
        }
        if (reConnectIndex > 0 && frameIndex >= reConnectIndex)//断线重连并且已复现到该帧
        {
            CurrentPlayType = FramePlayType.准备UI;
            reConnectIndex = 0;
            return;
        }
    }



    /// <summary>
    /// 处理接收数据的主要逻辑（快进也是调该逻辑）
    /// </summary>
    private void FrameMainLogic()
    {
        //if (!FrameInfos.ContainsKey(frameIndex))
        //{
        //    Debug.LogError("该执行帧不存在：" + frameIndex);
        //    return;
        //}
        //Debug.LogError("执行：" + frameIndex);
        FrameInfo info = null;
        lock (FrameInfos)
        {
            info = FrameInfos[frameIndex];
        }
        if (info.frameData != null)//有更新操作，更新数据
        {
            if (info.frameData.Count == 0)
            {
                Debug.LogError("检查指令数据长度为0的情况。");
            }
            for (int i = 0; i < info.frameData.Count; i++)
            {
                MessageXieYi frameXY = MessageXieYi.FromBytes(info.frameData[i]);
                if (frameXY != null)
                {
                    SelectFrameInfo(frameXY);
                }
                else
                {
                    string debug = "";
                    for (int j = 0; j < info.frameData[i].Length; j++)
                    {
                        debug += info.frameData[i][j] + ",";
                    }
                    Debug.LogError("服务器数据无法解析还原:" + debug);
                }
            }
        }
        //子件需要每帧判断的逻辑
        AllFrameObj();
        //
        frameIndex++;
    }

    /// <summary>
    /// 解析所有帧操作指令并在本客户端复现
    /// </summary>
    /// <param name="xieyi"></param>
    private void SelectFrameInfo(MessageXieYi xieyi)
    {
        byte[] tempMessageContent = xieyi.MessageContent;
        string messageInfo = "";
        CharacterCommon member = null;
        BuffInfo buffInfo = null;

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.moveDirection:
                ActorMoveDirection moveDir = SerializeHelper.Deserialize<ActorMoveDirection>(tempMessageContent);
                member = GameManager.instance.memberGroup[moveDir.userIndex];
                //用上次移动操作到这次操作的时间，算出当前位置，并移动到该点
                //member.transform.DOMove(SerializeHelper.BackVector(moveDir.position), DataController.FrameFixedTime);
                //member.SetPosition(SerializeHelper.BackVector(moveDir.position));
                member.SetNetDirection(moveDir);
                break;
            case MessageConvention.rotateDirection:
                ActorRotateDirection rotateDir = SerializeHelper.Deserialize<ActorRotateDirection>(tempMessageContent);
                //判断用户
                //Debug.LogError("玩家接收方向移动：" + messageInfo);
                member = GameManager.instance.memberGroup[rotateDir.userIndex];
                member.SetNetDirection(rotateDir);
                break;
            case MessageConvention.jump:
                ActorJump netJump = SerializeHelper.Deserialize<ActorJump>(tempMessageContent);
                member = GameManager.instance.memberGroup[netJump.userIndex];
                member.SetJump();
                break;
            case MessageConvention.shootBullet:
                ShootInfo shootInfo = SerializeHelper.Deserialize<ShootInfo>(tempMessageContent);
                member = GameManager.instance.memberGroup[shootInfo.userIndex];
                if (shootInfo.userIndex != DataController.instance.MyLocateIndex)//自身在收到服务器消息之前已旋转
                {
                    member.ShowBullet(shootInfo);
                }
                break;
            case MessageConvention.bulletInfo:
                BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(tempMessageContent);
                switch (bulletInfo.shootTag)
                {
                    case ShootTag.Box:
                        BoxManager.instance.SetBulletInfo(bulletInfo);
                        break;
                    case ShootTag.Character:
                        UpdateMemberShoot(xieyi);
                        break;
                    case ShootTag.Wall:
                        Debug.Log("射中Wall：" + bulletInfo.shootInfo);
                        break;
                    case ShootTag.Buff:
                        Debug.LogError("Buff不算是子弹");
                        break;
                }

                break;
            case MessageConvention.createBuff:
                buffInfo = SerializeHelper.Deserialize<BuffInfo>(tempMessageContent);
                GameObject obj = PoolManager.instance.GetPoolObjByType(PreLoadType.Buff, GameManager.instance.transBuff);
                obj.transform.position = BoxManager.instance.GetBoxInfoByIndex(buffInfo.boxIndex).transform.position;
                MagicBuff buff = obj.GetComponent<MagicBuff>();
                buff.Init(buffInfo);
                break;
            case MessageConvention.getBuff:
                buffInfo = SerializeHelper.Deserialize<BuffInfo>(tempMessageContent);
                BoxManager.instance.SetBuffData(buffInfo);
                break;
        }
    }

    /// <summary>
    /// 所有需要计算帧时间的函数
    /// </summary>
    private void AllFrameObj()
    {
        foreach (var item in memberGroup)
        {
            if (item.Value == null)
            {
                continue;
            }
            if (item.Key >= DataController.instance.MyRoomInfo.Limit)
            {
                continue;
            }
            item.Value.DoFrameLogic();
        }
    }


    public void UpdateMemberShoot(MessageXieYi xieyi)
    {
        BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(xieyi.MessageContent);
        int bulletMaster = bulletInfo.userIndex;
        int shootedIndex = int.Parse(bulletInfo.shootInfo);
        //同队不杀
        if (DataController.instance.ActorList[shootedIndex].MyTeam == DataController.instance.ActorList[bulletMaster].MyTeam)
        {
            return;
        }
        //
        if (shootedIndex == DataController.instance.MyLocateIndex)
        {
            GameRunUI.instance.BeShoot();
            //MyJoystickManager.instance.BeShoot();
            UIManager.instance.ShowAlertTip("我被射中。");
        }
        GetControl(shootedIndex).SetOriginal();
        GetControl(bulletMaster).ShowKill(DataController.instance.ActorList[bulletMaster].KillCount);
    }

    #endregion


    #region 游戏模型清理

    private void ClearModels()
    {
        Common.Clear(transBuff);

        ////清除人物
        //for (int i = transActor.childCount - 1; i >= 0; i--)
        //{
        //    PoolManager.instance.SetPoolObjByType(PreLoadType.Member, transActor.GetChild(i).gameObject);
        //}
        ////

    }


    #endregion



    private float AccumilatedTime = 0f;
    public void FixedUpdate()
    {
        //Basically same logic as FixedUpdate, but we can scale it by adjusting FrameLength   
        AccumilatedTime = AccumilatedTime + Time.fixedDeltaTime;
        //in case the FPS is too slow, we may need to update the game multiple times a frame   
        while (AccumilatedTime >= DataController.FrameFixedTime)
        {
            DoFrameLogin();
            AccumilatedTime = AccumilatedTime - DataController.FrameFixedTime;
        }
    }

    public void Update()
    {
        if (serverEventTCP.Count > 0)
        {
            MessageXieYi xieyi = serverEventTCP.Dequeue();
            if (xieyi == null)
            {
                Debug.LogError("有事件操作的协议为空？");
                return;
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.login)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    ErrorShow(error);
                }
                else
                {
                    SocketManager.instance.GetBeatTime();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getHeartBeatTime)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    SocketManager.instance.OpenHeartbeat();
                    //
                    UILogin.Close();
                    HomeUI.Show();
                    //
                    Debug.Log("自身检查是否需要重连。");
                    SocketManager.instance.SendSave((byte)MessageConvention.reConnectCheck, new byte[] { }, false);
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.reConnectCheck)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    ReconnctInfo rcInfo = SerializeHelper.Deserialize<ReconnctInfo>(xieyi.MessageContent);
                    if (rcInfo.isReconnect)
                    {
                        CurrentPlayType = FramePlayType.断线重连;
                        ReConnectUI.Show();
                    }
                    else
                    {
                        CurrentPlayType = FramePlayType.游戏未开始;
                    }
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.quitRoom)//自己退出房间
            {
                QuitInfo qInfo = SerializeHelper.Deserialize<QuitInfo>(xieyi.MessageContent);
                if (qInfo.isQuit)
                {
                    UpdateMemberHide();
                    RoomUI.Close();
                    HomeUI.Show();
                    if (qInfo.userIndex != DataController.instance.MyLocateIndex)
                    {
                        UIManager.instance.ShowAlertTip("您被踢出房间。");
                    }
                }
                else
                {
                    if (qInfo.userIndex == qInfo.quitUnique)
                    {
                        UIManager.instance.ShowAlertTip("退出房间失败。");
                    }
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.updateActorState)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    CheckState(xieyi);
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.updateModelInfo)
            {
                GameModelData modelDate = SerializeHelper.Deserialize<GameModelData>(xieyi.MessageContent);
                SetPrepareData(modelDate);
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getPreGameData)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    Debug.Log("验证本客户端收到游戏前准备数据。客户端响应已收到:" + xieyi.MessageContentLength);
                    SendState(RoomActorState.WaitForStart);
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.startGaming)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    StartGaming();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.endGaming)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    EndGaming(xieyi);
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.frameData)
            {
                Debug.LogError("TCP收到整串操作数据");
                UDPManager.instance.DealFrameData(xieyi);
                if (CurrentPlayType == FramePlayType.断线重连)
                {
                    CurrentPlayType = FramePlayType.游戏中;
                }
                if (CurrentPlayType == FramePlayType.主动请求数据)
                {
                    CurrentPlayType = FramePlayType.游戏中;
                }
            }
        }

        //UDP
        if (serverEventUDP.Count > 0)
        {
            MessageXieYi xieyi = serverEventUDP.Dequeue();
            if (xieyi == null)
            {
                Debug.LogError("有UDP事件操作的协议为空？");
                return;
            }
            if (xieyi.XieYiFirstFlag == (byte)MessageConvention.setUDP)
            {
                UDPManager.instance.IsConnect = true;
                Debug.LogError("用tcp 设置房间 udp");
                //SocketManager.instance.SendSave((byte)MessageConvention.setUDP, xieyi.MessageContent, false);
            }
        }
    }



}

public enum FramePlayType
{
    游戏未开始,
    准备UI,
    游戏中,
    空帧缓冲,
    断线重连,
    主动请求数据,
}