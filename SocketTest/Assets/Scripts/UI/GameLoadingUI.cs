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
        foreach (var item in DataController.instance.MyRoomInfo.ActorList.Values)
        {
            if (!string.IsNullOrEmpty(item.Nickname))
            {
                Generate(item);
            }
        }
    }

    private void Generate(RoomActor actor)
    {
        TeamType team = actor.MyTeam;
        GameObject obj = Common.Generate(loadPrefab, team == TeamType.Blue ? transTeamBlue : transTeamRed);
        PrefabLoad info = obj.GetComponent<PrefabLoad>();
        info.Init(actor);
        allLoad.Add(actor.UniqueID, info);
    }




    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.prepareLocalModel)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    RoomActorUpdate roomActorUpdate = new RoomActorUpdate();
                    roomActorUpdate.SetSendInfo(SerializeHelper.ConvertToString(xieyi.MessageContent));
                    allLoad[roomActorUpdate.userIndex].UpdateSlider(int.Parse(roomActorUpdate.update));
                }
            }
        }
    }
}
