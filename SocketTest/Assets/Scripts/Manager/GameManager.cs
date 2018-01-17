using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    public const float timeActorMoveSpan = 0.03f; //每隔30帧发送一次坐标
    public const float myActorMoveSpeed = 5;//人物移动速度
    public const float myCameraMoveSpeed = 100;
    public const float gravity = 9.8f;

    public const int uiMoveIndex = 1;//ui移动用到的保留小数点位数
    public const int uiSpeedIndex = 1;//ui移动速度用到的保留小数点位数
    public const int uiRotateIndex = 1;//ui旋转大于该区间才发送数据


    public Dictionary<int, CharacterCommon> memberGroup = new Dictionary<int, CharacterCommon>();
    public Transform transActor;
    public Transform transBullet;

    #region 帧同步相关变量

    public int frameIndex = 0;//当前复现到哪一帧了
    public float frameIndexTime;//运行到这帧时的时间

    public bool isReconnect = false;
    public int reConnectIndex = 0;//重连时需要复现到的帧编号----//重连不需要则为-1



    public Dictionary<int, FrameInfo> FrameInfos = new Dictionary<int, FrameInfo>();

    /// <summary>
    /// 重连时客户端一帧复现X帧的数据
    /// </summary>
    private int reConnectSpan = 50;


    public bool isOnFrame;//是否正在处理帧逻辑
    private float requestMaxTime = 5f;//等待数据时长
    public int frameEmpty = 0;//判断重连时，服务器广播了哪一帧的数据，用来请求前面的数据

    private float FrameFixedTime;//一帧转换成时间
    public float timeDeal;//游戏时间存储数值
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
    private Queue<MessageXieYi> serverEvent = new Queue<MessageXieYi>();
    /// <summary>
    /// 该脚本要处理的socket数据类型----每个脚本自己定义自己要侦听的消息类型，只会接收已定义的协议数据
    /// </summary>
    private List<MessageConvention> messageHandle = new List<MessageConvention>()
    {
        MessageConvention.login,
        MessageConvention.getHeartBeatTime,
        MessageConvention.heartBeat,
        MessageConvention.reConnectCheck,
        MessageConvention.getRoommateInfo,
        MessageConvention.quitRoom,
        MessageConvention.updateActorState,
        MessageConvention.updateModelInfo,
        MessageConvention.getPreGameData,
        MessageConvention.startGaming,
        MessageConvention.endGaming,
    };

    private void Start()
    {
        SocketManager.ListenDelegate(true, messageHandle, OperationListenInfo);
    }
    private void OnDisable()
    {
        SocketManager.ListenDelegate(false, messageHandle, OperationListenInfo);
    }
    /// <summary>
    /// 添加Socket管理类分发来的需要处理的数据
    /// </summary>
    /// <param name="xieyi"></param>
    public void OperationListenInfo(MessageXieYi xieyi)
    {
        serverEvent.Enqueue(xieyi);
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
        FrameFixedTime = (float)RoomInfo.frameTime / 1000;//初始化-帧转秒
    }

    public void InitRoom()
    {
        BoxManager.instance.Init();
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
        for (int i = 0; i < memberGroup.Count; i++)
        {
            memberGroup[i].BeStop();
        }
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
    /// 重连逻辑
    /// </summary>
    /// <param name="state"></param>
    private void ReConnectLogin()
    {
        Debug.Log("重连：" + isReconnect);
        GameLoadingUI.Show();
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
        if (info.userIndex == DataController.instance.MyLocateIndex)
        {
            member = MyController.instance;
        }
        else
        {
            member = GameManager.instance.memberGroup[info.userIndex];
        }
        member.SetPosition(SerializeHelper.BackVector(info.pos));
        member.SetRotate(SerializeHelper.BackVector(info.rotate));
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
                    MyJoystickManager.instance.Open();
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
    private void ShowModelUIName()
    {
        foreach (var item in DataController.instance.ActorList)
        {
            memberGroup[item.Value.UniqueID].ShowMyName(item.Value.Register.name);
        }
    }

    /// <summary>
    /// 刷新3D模型的显示
    /// </summary>
    private void UpdateRoomActor()
    {
        GameObject obj = null;
        int myIndex = DataController.instance.MyLocateIndex;

        for (int i = 0; i < DataController.instance.MyRoomInfo.Limit; i++)
        {
            RoomActor info = DataController.instance.ActorList[i];

            if (!memberGroup.ContainsKey(i))
            {
                Debug.Log("初始化其他玩家模型站位：" + i);
                memberGroup.Add(i, null);
            }
            if (i == myIndex)
            {
                CharacterCommon mControl = memberGroup[i];
                if (MyController.instance == null || !MyController.instance.IsShowHierarchy())//原来没有
                {
                    obj = PoolManager.instance.GetPoolObjByType(PreLoadType.Character, transActor);
                    mControl = obj.GetComponent<CharacterCommon>();
                    memberGroup[i] = mControl;
                    obj.SetActive(true);

                    MyController.instance.Init(info.UniqueID);
                    MyController.instance.ShowTeam(info.MyTeam);
                }
            }
            else
            {
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
    }

    private void StartGaming()
    {
        HomeUI.Close();
        frameIndex = 0;
        Debug.Log("开始游戏");
        isOnFrame = true;
        frameIndexTime = Time.realtimeSinceStartup;
    }




    private void EndGaming(MessageXieYi xieyi)
    {
        isOnFrame = false;
        frameIndex = 0;
        reConnectIndex = 0;//游戏结束的时候未完成复现，怎清除重连记录帧
        MyJoystickManager.instance.Close();
        Debug.Log("结束游戏");
        foreach (var item in memberGroup)
        {
            item.Value.Init(item.Key);
        }
        GameRunUI.Close();
        FrameInfos = new Dictionary<int, FrameInfo>();
        TeamType winTeam = (TeamType)int.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
        RoomUI.Show();
        GameOverUI.Show(winTeam);
    }



    #endregion



    #region 帧同步处理逻辑


    public void DoFrameRequest(int startCheckIndex)
    {
        for (int i = startCheckIndex; i >= 0; i++)
        {
            if (!FrameInfos.ContainsKey(i))
            {
                frameEmpty = i;
                break;
            }
        }
        FrameInfo info = new FrameInfo() { frameIndex = frameEmpty, frameData = new List<byte[]>() };
        string debug = "请求帧：" + info.frameIndex;
        UIManager.instance.ShowAlertTip(debug);
        //Debug.LogError(debug);
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
    //public void OnGUI()
    //{
    //    if (DataController.instance.MyRoomInfo != null && DataController.instance.MyRoomInfo.ActorList != null)
    //    {
    //        guiInfo ="站位：" + DataController.instance.MyLocateIndex;
    //        GUIStyle bb = new GUIStyle();
    //        bb.normal.background = null;    //这是设置背景填充的
    //        bb.normal.textColor = Color.blue;   //设置字体颜色的
    //        bb.fontSize = 40;       //当然，这是字体大小
    //        GUI.Label(new Rect(0, 0, 200, 200), guiInfo, bb);
    //    }
    //}


    /// <summary>
    /// 这个是每隔0.015s运行一次
    /// </summary>
    private void DoFrameLogin()
    {
        if (FrameInfos.ContainsKey(frameIndex))
        {
            int forwardNum = 1;
            int length = DataController.instance.FrameCanIndex - frameIndex;
            if (length > DataController.instance.MyRoomInfo.FrameDelay)//本地运行帧和接收帧差距超过该值，快进
            {
                forwardNum = length;
                string info = "快进：" + frameIndex + "/" + DataController.instance.FrameCanIndex;
                //Debug.LogError(info);
                UIManager.instance.ShowAlertTip(info);
            }
            for (int i = 0; i < forwardNum; i++)//快进延迟帧的一般数值
            {
                FrameMainLogic();
            }
        }
        else
        {
            float overTime = Time.realtimeSinceStartup - frameIndexTime;
            if (overTime > requestMaxTime)
            {
                frameIndexTime = Time.realtimeSinceStartup;//将该时间作为处理最后一帧的时间，这样可以重新等待延迟最大时间
                Debug.LogError("超时：" + overTime + "请求帧：" + frameIndex + "/" + DataController.instance.FrameCanIndex);
                DoFrameRequest(frameIndex);
            }
        }

    }

    /// <summary>
    /// 处理接收数据的主要逻辑（快进也是调该逻辑）
    /// </summary>
    private void FrameMainLogic()
    {
        if (!FrameInfos.ContainsKey(frameIndex))
        {
            Debug.Log("该执行帧不存在：" + frameIndex);
            return;
        }
        frameIndexTime = Time.realtimeSinceStartup;

        FrameInfo info = FrameInfos[frameIndex];
        if (info.frameData != null)//有更新操作，更新数据
        {
            if (info.frameData.Count == 0)
            {
                Debug.LogError("检查长度为0的情况。");
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
        lock (FrameInfos)
        {
            FrameInfos.Remove(frameIndex);
            frameIndex++;
        }
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

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.moveDirection:
                ActorMoveDirection moveDir = SerializeHelper.Deserialize<ActorMoveDirection>(tempMessageContent);

                //Debug.LogError("玩家接收方向移动：" + messageInfo);
                member = GameManager.instance.memberGroup[moveDir.userIndex];
                member.SetNetDirection(moveDir);
                break;
            case MessageConvention.rotateDirection:
                ActorRotateDirection rotateDir = SerializeHelper.Deserialize<ActorRotateDirection>(tempMessageContent);
                //判断用户
                //Debug.LogError("玩家接收方向移动：" + messageInfo);
                member = GameManager.instance.memberGroup[rotateDir.userIndex];
                member.SetNetDirection(rotateDir);
                break;
            case MessageConvention.shootBullet:
                int shootIndex = int.Parse(SerializeHelper.ConvertToString(tempMessageContent));
                if (shootIndex == DataController.instance.MyLocateIndex)
                {
                    MyController.instance.ShowBullet(true);
                }
                else
                {
                    member = GameManager.instance.memberGroup[shootIndex];
                    member.ShowBullet(false);
                }
                break;
            case MessageConvention.bulletInfo:
                BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(tempMessageContent);

                switch (bulletInfo.shootTag)
                {
                    case ShootTag.Box:
                        BoxManager.instance.UpdateBulletInfo(xieyi);
                        break;
                    case ShootTag.Character:
                        UpdateMemberShoot(xieyi);
                        break;
                    case ShootTag.Wall:
                        Debug.Log("射中Wall：" + bulletInfo.shootInfo);
                        break;
                }

                break;
        }
    }

    /// <summary>
    /// 所有需要计算帧时间的函数
    /// </summary>
    private void AllFrameObj()
    {
        ///按数值操作
        foreach (var item in memberGroup)
        {
            if (item.Value == null)
            {
                continue;
            }
            if (!item.Value.IsShowHierarchy())
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
        if (shootedIndex != DataController.instance.MyLocateIndex)
        {
            //此处需要修改
            memberGroup[shootedIndex].BeShoot();
            UIManager.instance.ShowAlertTip("玩家：" + DataController.instance.ActorList[shootedIndex].Register.name + " 被射中。");
        }
        else
        {
            MyJoystickManager.instance.BeShoot();
            MyController.instance.BeShoot();
            UIManager.instance.ShowAlertTip("我被射中。");
        }
        memberGroup[bulletMaster].ShowKill(DataController.instance.ActorList[bulletMaster].KillCount);
    }

    #endregion


    public void FixedUpdate()
    {
        if (isOnFrame)
        {
            if (reConnectIndex <= 0)
            {
                if (timeDeal == 0)
                {
                    timeDeal = Time.realtimeSinceStartup;
                }
                float length = Time.realtimeSinceStartup - timeDeal;
                //Debug.LogError("本帧:" + Time.realtimeSinceStartup + "与上帧的差距：" + length);
                int count = (int)(length / FrameFixedTime);
                //Debug.LogError("需要在本帧运算次数：" + count);
                for (int i = 0; i < count; i++)
                {
                    DoFrameLogin();
                }
                timeDeal += count * FrameFixedTime;
            }
            else
            {
                int length = 0;
                if (reConnectIndex >= reConnectSpan)
                {
                    length = reConnectSpan;
                }
                else
                {
                    length = reConnectIndex;
                }
                for (int i = 0; i < length; i++, reConnectIndex--)
                {
                    FrameMainLogic();
                }
            }
            if (isReconnect || reConnectIndex == 0)
            {
                isReconnect = false;
                frameIndexTime = Time.realtimeSinceStartup;
                reConnectIndex = -1;
                GameLoadingUI.Close();
                GameRunUI.Show();
                ShowModelUIName();
            }
        }


    }


    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
            if (xieyi.XieYiFirstFlag == (byte)MessageConvention.login)
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
            if (xieyi.XieYiFirstFlag == (byte)MessageConvention.getHeartBeatTime)
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
                    isReconnect = int.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent)) == 1 ? true : false;
                    if (isReconnect)
                    {
                        ReConnectLogin();
                    }
                }
            }
            //if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getRoommateInfo)
            //{
            //    ErrorType error = ClassGroup.CheckIsError(xieyi);
            //    if (error != ErrorType.none)
            //    {
            //        Debug.LogError(error);
            //    }
            //    else
            //    {
            //        HomeUI.instance.Close();
            //        if (DataController.instance.MyRoomInfo.CurState != RoomActorState.Gaming)
            //        {
            //            RoomUI.Show();
            //        }
            //        else
            //        {
            //            if (reConnectIndex > 0)
            //            {
            //                PrepareLocalModel();
            //            }
            //        }
            //    }
            //}
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.quitRoom)//自己退出房间
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    QuitInfo qInfo = SerializeHelper.Deserialize<QuitInfo>(xieyi.MessageContent);
                    if (qInfo.isQuit)
                    {
                        UpdateMemberHide();
                    }
                    else
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

        }
    }



}
