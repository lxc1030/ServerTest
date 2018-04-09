using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLoadingUI : MonoBehaviour
{
    public static string Name = "GameLoadingUI";
    public static GameLoadingUI instance;

    public Transform transTeamBlue;
    public Transform transTeamRed;

    public GameObject loadPrefab;
    public CheckLoading curState;
    public Dictionary<int, PrefabLoad> allLoad = new Dictionary<int, PrefabLoad>();


    private void Awake()
    {
        instance = this;
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
        MessageConvention.getRoomInfo,
        MessageConvention.getRoommateInfo,
        MessageConvention.prepareLocalModel,
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

    public void OnClose()
    {
        Close();
    }
    public static void Close()
    {
        UIManager.instance.HidePanel(Name, true);
        MyJoystickManager.instance.Open();
    }

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }
    private void Init()
    {
        allLoad = new Dictionary<int, PrefabLoad>();
        Common.Clear(transTeamBlue);
        Common.Clear(transTeamRed);

        ChangeState(CheckLoading.房间信息);
    }

    private void ChangeState(CheckLoading state)
    {
        curState = state;
        switch (curState)
        {
            case CheckLoading.房间信息:
                if (DataController.instance.MyRoomInfo == null)
                {
                    GameManager.GetRoomInfo();
                }
                else
                {
                    Have_RoomInfo();
                }
                break;
            case CheckLoading.人物信息:
                if (DataController.instance.ActorList == null)
                {
                    GameManager.GetRoommateInfo();
                }
                else
                {
                    Have_MateInfo();
                }
                break;
            case CheckLoading.注册UDP:
                if (!UDPManager.instance.IsConnect)
                {
                    InvokeRepeating("MakeUDPEnable", 0, 0.1f);
                }
                else
                {
                    Have_UDPInfo();
                }
                break;
            case CheckLoading.取重连帧:
                if (GameManager.instance.CurrentPlayType == FramePlayType.断线重连)
                {
                    Debug.LogError("重连时，取重连帧。");
                    GameManager.GetReconnectIndex();
                }
                break;
        }
    }

    private void Have_RoomInfo()
    {
        ChangeState(CheckLoading.人物信息);
    }
    private void Have_MateInfo()
    {
        ChangeState(CheckLoading.注册UDP);
    }
    private void Have_UDPInfo()
    {
        GenerateUserUI();
        GameManager.instance.PrepareLocalModel();
        ChangeState(CheckLoading.取重连帧);
    }
    /// <summary>
    /// Invoke
    /// </summary>
    private void MakeUDPEnable()
    {
        Debug.LogError("udp 连接状态:" + UDPManager.instance.IsConnect);
        if (!UDPManager.instance.IsConnect)
        {
            UDPManager.instance.InitSocket();
        }
        else
        {
            CancelInvoke("MakeUDPEnable");
            Have_UDPInfo();
        }
    }


    private void GenerateUserUI()
    {
        foreach (var item in DataController.instance.ActorList.Values)
        {
            if (item == null)
            {
                Debug.LogError("不该有空值，请检查。");
                continue;
            }
            if (!string.IsNullOrEmpty(item.Register.name))
            {
                if (!allLoad.ContainsKey(item.UniqueID))
                {
                    Generate(item);
                }
                else
                {
                    allLoad[item.UniqueID].Init(item);
                }
            }
        }
    }

    private void Generate(RoomActor actor)
    {
        TeamType team = actor.MyTeam;
        GameObject obj = Common.Generate(loadPrefab, team == TeamType.Blue ? transTeamBlue : transTeamRed);
        PrefabLoad info = obj.GetComponent<PrefabLoad>();
        info.Init(actor);
        if (!allLoad.ContainsKey(actor.UniqueID))
        {
            allLoad.Add(actor.UniqueID, info);
        }
    }




    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getRoomInfo)//房间信息
            {
                Have_RoomInfo();
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getRoommateInfo)//房间中人物信息
            {
                Have_MateInfo();
            }

            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.prepareLocalModel)
            {
                RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
                roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(xieyi.MessageContent));
                if (allLoad.ContainsKey(roomActorUpdate.userIndex))
                {
                    allLoad[roomActorUpdate.userIndex].UpdateSlider(int.Parse(roomActorUpdate.update));
                }
                else
                {
                    Debug.LogError("在该UI未准备好的时候，收到了该用户的进度。");
                }
            }
        }

    }
}

public enum CheckLoading
{
    房间信息,
    人物信息,
    注册UDP,
    取重连帧,
}