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
        MessageConvention.reConnectIndex,
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
        UIManager.instance.HidePanel(Name);
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
        GameManager.GetRoomInfo();
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
                GameManager.GetRoommateInfo();
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getRoommateInfo)//房间中人物信息
            {
                GenerateUserUI();
                if (GameManager.instance.isReconnect)
                {
                    Debug.Log("重连请求重连帧。");
                    GameManager.GetReconnectIndex();
                }
                else
                {
                    GameManager.instance.PrepareLocalModel();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.reConnectIndex)//获取重连帧编号
            {
                //GameManager.instance.DoFrameRequest(GameManager.instance.reConnectIndex);
                GameManager.instance.PrepareLocalModel();//准备本地模型加载
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
                    Debug.LogError("在自己UI未准备好的时候，收到了其他用户的进度。");
                }
            }
        }
    }
}
