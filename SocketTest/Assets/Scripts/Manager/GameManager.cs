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
    public const float myActorMoveSpeed = 3;//人物移动速度
    public const float myCameraMoveSpeed = 100;
    public const float gravity = 9.8f;

    public const int uiMoveIndex = 1;//ui移动用到的保留小数点位数
    public const int uiSpeedIndex = 1;//ui移动速度用到的保留小数点位数
    public const int uiRotateIndex = 1;//ui旋转大于该区间才发送数据


    public Dictionary<int, CharacterCommon> memberGroup = new Dictionary<int, CharacterCommon>();
    public Transform transActor;
    public Transform transBullet;

    #region 帧同步相关变量

    public bool isReconnect;

    public FrameType CurFrameType;
    public int frameIndex = 0;//当前复现到哪一帧了
    public int frameEmpty = 0;//判断重连时，服务器广播了哪一帧的数据，用来请求前面的数据
    public int requestCount = 0;
    public Dictionary<int, FrameInfo> FrameInfos = new Dictionary<int, FrameInfo>();

    #endregion

    /// <summary>
    /// 动画
    /// </summary>
    public List<AnimationClip> animationGroup = new List<AnimationClip>();




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
        MessageConvention.reConnect,
        MessageConvention.createRoom,
        MessageConvention.joinRoom,
        MessageConvention.getRoommateInfo,
        MessageConvention.quitRoom,
        MessageConvention.updateActorState,
        MessageConvention.updateModelInfo,
        MessageConvention.getPreGameData,
        MessageConvention.startGaming,
        MessageConvention.endGaming,
    };
    private void Awake()
    {
        DontDestroyOnLoad(this);
        instance = this;
        //
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
        CurFrameType = FrameType.无;
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
        Debug.LogError("重连。");
        //RoomUI.Show();
    }


    /// <summary>
    ///  请求房间中人物的信息
    /// </summary>
    private static void GetRoommateInfo()
    {
        RoomActorUpdate roommateInfo = new RoomActorUpdate()
        {
            userIndex = DataController.instance.MyRoomInfo.MyLocateIndex,
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
            userIndex = DataController.instance.MyRoomInfo.MyLocateIndex,
            update = (int)state + ""
        };
        string info = netUpdate.GetSendInfo();
        byte[] message = SerializeHelper.ConvertToByte(info);
        SocketManager.instance.SendSave((byte)MessageConvention.updateActorState, message, false);
    }

    /// <summary>
    /// 模型加载完成，设置初始状态
    /// </summary>
    private void SetPrepareData(GameModelData info)
    {
        CharacterCommon member = null;
        if (info.userIndex == DataController.instance.MyRoomInfo.MyLocateIndex)
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
        if (roomActorUpdate.userIndex == DataController.instance.MyRoomInfo.MyLocateIndex)
        {
            RoomActorState state = (RoomActorState)int.Parse(roomActorUpdate.update);
            Debug.Log("我的状态：" + state);
            //判断我的状态为预准备
            switch (state)
            {
                case RoomActorState.PrepareModel:
                    Debug.Log("判断我的状态为预准备");
                    PrepareLocalModel();
                    break;
                case RoomActorState.ModelPrepared:
                    Debug.Log("模型准备好了");
                    break;
                case RoomActorState.Gaming:
                    //MyJoystickManager.instance.ReLife();
                    break;
            }
        }
    }


    public bool isPreparing = false;
    private void PrepareLocalModel()
    {
        if (isPreparing)
            return;
        isPreparing = true;
        RoomUI.Close();
        GameRunUI.Show();
        GameLoadingUI.Show();
        SendLoadProgress(0);
        UpdateRoomActor();
        SendLoadProgress(50);
        BoxManager.instance.Init();
        SendLoadProgress(100);
        isPreparing = false;
        ShowModelUIName();
    }
    private void SendLoadProgress(int index)
    {
        //发送准备进度命令
        RoomActorUpdate netUpdate = new RoomActorUpdate()
        {
            userIndex = DataController.instance.MyRoomInfo.MyLocateIndex,
            update = index + ""
        };
        string info = netUpdate.GetSendInfo();
        byte[] message = SerializeHelper.ConvertToByte(info);
        SocketManager.instance.SendSave((byte)MessageConvention.prepareLocalModel, message, false);
    }
    private void ShowModelUIName()
    {
        foreach (var item in DataController.instance.MyRoomInfo.ActorList)
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
        int myIndex = DataController.instance.MyRoomInfo.MyLocateIndex;

        for (int i = 0; i < DataController.instance.MyRoomInfo.Limit; i++)
        {
            RoomActor info = DataController.instance.MyRoomInfo.ActorList[i];

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
                if (DataController.instance.MyRoomInfo.ActorList[i] == null)//该位置玩家退出或不存在
                {
                    mControl.gameObject.SetActive(false);
                }
                else
                {
                    mControl.gameObject.SetActive(true);
                    mControl.Init(info.UniqueID);
                    mControl.ShowTeam(DataController.instance.MyRoomInfo.ActorList[i].MyTeam);
                }
            }
        }
    }




    private void StartGaming()
    {
        if (isReconnect)
        {
            isReconnect = false;
            ReConnectFrameRequest();
        }
        Debug.LogError("开始游戏");
        CurFrameType = FrameType.处理数据;
        //不是断线重连，可以直接关闭Loading
        if (frameEmpty == 0)
        {
            GameLoadingUI.Close();
        }
    }
    private void EndGaming(MessageXieYi xieyi)
    {
        MyJoystickManager.instance.Close();
        Debug.LogError("结束游戏");
        foreach (var item in memberGroup)
        {
            item.Value.Init(item.Key);
        }
        GameRunUI.Close();
        CurFrameType = FrameType.无;
        frameIndex = 0;
        FrameInfos = new Dictionary<int, FrameInfo>();
        TeamType winTeam = (TeamType)int.Parse(SerializeHelper.ConvertToString(xieyi.MessageContent));
        RoomUI.Show();
        GameOverUI.Show(winTeam);
    }



    #endregion



    #region 帧同步处理逻辑

    public string show;
    private void FixedUpdate()
    {
        if (DataController.instance.MyRoomInfo != null)
        {
            show = frameIndex + "/" + DataController.instance.MyRoomInfo.FrameIndex;
        }
        DoFrameLogin();
    }

    /// <summary>
    /// 主动请求帧数据
    /// </summary>
    public void DoFrameRequest(int index)
    {
        RoomActorState myState = DataController.instance.MyRoomInfo.ActorList[DataController.instance.MyRoomInfo.MyLocateIndex].CurState;
        if (myState == RoomActorState.Gaming)
        {
            FrameInfo info = new FrameInfo() { frameIndex = index + RoomInfo.frameInterval, frameData = new List<byte[]>() };
            byte[] message = SerializeHelper.Serialize<FrameInfo>(info);
            SocketManager.instance.SendSave((byte)MessageConvention.frameData, message, false);
        }
        else
        {
            Debug.LogError("请求数据的时候，自身状态为：" + myState);
        }
    }
    private void ReConnectFrameRequest()
    {
        frameIndex = 0;
        frameEmpty = DataController.instance.MyRoomInfo.FrameIndex;
        for (int i = 0; i < frameEmpty;)
        {
            if (!FrameInfos.ContainsKey(i))
            {
                FrameInfo info = new FrameInfo() { frameIndex = i + RoomInfo.frameInterval, frameData = new List<byte[]>() };
                byte[] message = SerializeHelper.Serialize<FrameInfo>(info);
                SocketManager.instance.SendSave((byte)MessageConvention.frameData, message, false);
            }
            i += RoomInfo.frameInterval;
        }




        //int start = frameIndex;
        //frameEmpty = frameIndex;//缺失的帧编号
        //for (int i = frameIndex; i < maxIndex; i++)
        //{
        //    if (FrameInfos.ContainsKey(i))
        //    {
        //        frameEmpty = i;
        //        break;
        //    }
        //}
        //Debug.LogError("重连请求帧：" + frameEmpty + "->" + maxIndex);
        //for (int i = 0; i < (frameEmpty - start) / RoomInfo.frameInterval; i++)
        //{
        //    FrameInfo info = new FrameInfo() { frameIndex = start + i * RoomInfo.frameInterval, frameData = new List<byte[]>() };
        //    Debug.Log("请求帧：" + info.frameIndex);
        //    byte[] message = SerializeHelper.Serialize<FrameInfo>(info);
        //    SocketManager.instance.SendSave((byte)MessageConvention.frameData, message, false);
        //}
    }


    /// <summary>
    /// 最低延迟帧数
    /// </summary>
    private int FastCount = 2;
    /// <summary>
    /// 最低延迟帧数*该值为设置掉线需要的总帧数
    /// </summary>
    private int OffLineCount = 3;



    /// <summary>
    /// 这个是每隔0.015s运行一次
    /// </summary>
    private void DoFrameLogin()
    {
        switch (CurFrameType)
        {
            case FrameType.无:
                break;
            case FrameType.处理数据:
                if (FrameInfos.ContainsKey(frameIndex))
                {
                    FrameMainLogic();
                    //本地运行帧和接收帧差距超过RoomInfo.frameDiffer*，快进
                    if (DataController.instance.MyRoomInfo.FrameIndex - frameIndex > RoomInfo.frameDiffer * FastCount)
                    {
                        //Debug.Log("快进：" + frameIndex + "/" + DataController.instance.MyRoomInfo.FrameIndex);
                        for (int i = 0; i < RoomInfo.frameDiffer; i++)//快进延迟帧的一般数值
                        {
                            FrameMainLogic();
                        }
                    }
                }
                else
                {
                    CurFrameType = FrameType.请求数据;
                }
                break;
            case FrameType.请求数据:
                CurFrameType = FrameType.等待数据;
                requestCount = 0;
                break;
            case FrameType.等待数据:
                requestCount++;
                if (FrameInfos.ContainsKey(frameIndex))//已收到数据，下一帧处理
                {
                    CurFrameType = FrameType.处理数据;
                }
                if (requestCount % RoomInfo.frameDiffer == 0)//超过延迟帧就主动请求
                {
                    if (requestCount / RoomInfo.frameDiffer == OffLineCount)
                    {
                        //设置自身掉线
                        CurFrameType = FrameType.通讯中断;
                        SocketManager.instance.DisConnect();
                    }
                    else
                    {
                        //请求数据
                        Debug.LogError("请求帧：" + (frameIndex + RoomInfo.frameInterval) + "/" + DataController.instance.MyRoomInfo.FrameIndex);
                        DoFrameRequest(frameIndex);
                    }
                }
                break;
            case FrameType.通讯中断:
                break;
        }
    }

    /// <summary>
    /// 处理接收数据的主要逻辑（快进也是调该逻辑）
    /// </summary>
    private void FrameMainLogic()
    {
        if (FrameInfos.ContainsKey(frameIndex))
        {
            //Debug.Log("当前执行帧：" + frameIndex);
            FrameInfo info = FrameInfos[frameIndex];
            //
            if (info.frameData != null)//有更新操作，更新数据
            {
                if (info.frameData.Count == 0)
                {
                    Debug.LogError("检查长度为0的情况。");
                }
                for (int i = 0; i < info.frameData.Count; i++)
                {
                    MessageXieYi frameXY = MessageXieYi.FromBytes(info.frameData[i], true);
                    SelectFrameInfo(frameXY);
                }
            }
            else
            {
                //
                AllFrameObj();
            }
            //
            frameIndex++;

            if (frameIndex >= frameEmpty && frameEmpty > 0)
            {
                frameEmpty = 0;
                GameLoadingUI.Close();
            }
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

        switch ((MessageConvention)xieyi.XieYiFirstFlag)
        {
            case MessageConvention.moveDirection:
                ActorMoveDirection moveDir = SerializeHelper.Deserialize<ActorMoveDirection>(tempMessageContent);

                //判断用户
                if (moveDir.userIndex == DataController.instance.MyRoomInfo.MyLocateIndex)
                {
                    //Debug.LogError("自接收方向移动：" + messageInfo);
                    MyController.instance.SetNetDirection(moveDir);
                    MyController.instance.DoFrameLogic();
                }
                else
                {
                    //Debug.LogError("玩家接收方向移动：" + messageInfo);
                    CharacterCommon member = GameManager.instance.memberGroup[moveDir.userIndex];
                    member.SetNetDirection(moveDir);
                    member.DoFrameLogic();
                }
                break;
            case MessageConvention.rotateDirection:
                ActorRotateDirection rotateDir = SerializeHelper.Deserialize<ActorRotateDirection>(tempMessageContent);
                //判断用户
                if (rotateDir.userIndex == DataController.instance.MyRoomInfo.MyLocateIndex)
                {
                    //Debug.LogError("自接收方向移动：" + messageInfo);
                    //MyController.instance.SetNetDirection(rotateDir);
                }
                else
                {
                    //Debug.LogError("玩家接收方向移动：" + messageInfo);
                    CharacterCommon member = GameManager.instance.memberGroup[rotateDir.userIndex];
                    member.SetNetDirection(rotateDir);
                }
                break;
            case MessageConvention.shootBullet:
                int shootIndex = int.Parse(SerializeHelper.ConvertToString(tempMessageContent));
                if (shootIndex == DataController.instance.MyRoomInfo.MyLocateIndex)
                {
                    MyController.instance.ShowBullet(true);
                }
                else
                {
                    CharacterCommon member = GameManager.instance.memberGroup[shootIndex];
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
                        Debug.LogError("射中Wall：" + bulletInfo.shootInfo);
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
        //
        if (shootedIndex != DataController.instance.MyRoomInfo.MyLocateIndex)
        {
            //此处需要修改
            memberGroup[shootedIndex].BeShoot();
            UIManager.instance.ShowAlertTip("玩家：" + DataController.instance.MyRoomInfo.ActorList[shootedIndex].Register.name + " 被射中。");
        }
        else
        {
            MyJoystickManager.instance.BeShoot();
            MyController.instance.BeShoot();
            UIManager.instance.ShowAlertTip("我被射中。");
        }
        memberGroup[bulletMaster].ShowKill(DataController.instance.MyRoomInfo.ActorList[bulletMaster].KillCount);
    }

    #endregion


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
                    SocketManager.instance.SendSave((byte)MessageConvention.reConnect, new byte[] { }, false);
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.reConnect)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    string reConnect = SerializeHelper.ConvertToString(xieyi.MessageContent);
                    isReconnect = true;
                    ReConnectLogin();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.createRoom
              || (MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.joinRoom
              )
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    UIManager.instance.ShowAlertTip("加入房间失败" + error);
                }
                else
                {
                    CreateRoomUI.Close();
                    HomeUI.instance.Close();
                    //获取房间中人物信息
                    GetRoommateInfo();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getRoommateInfo)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    if (DataController.instance.MyRoomInfo.CurState != RoomActorState.Gaming)
                    {
                        RoomUI.Show();
                    }
                }
            }
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
/// <summary>
/// 帧同步游戏数据操作状态
/// </summary>
public enum FrameType
{
    无,
    处理数据,
    请求数据,
    等待数据,
    通讯中断,
}
