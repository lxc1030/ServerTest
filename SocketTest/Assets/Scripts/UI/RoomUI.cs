using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    public static RoomUI instance;
    public static string Name = "RoomUI";

    public RoomState curState;


    public Text txRoomID;
    public Text txRoomName;
    public Text txRoomType;
    public Text txRoomLimet;

    public RoomNoReady uiNoReady;

    //其他人
    public List<RoomShowInfo> allMemberShow = new List<RoomShowInfo>();



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
        MessageConvention.getRoommateInfo,
        MessageConvention.quitRoom,
        MessageConvention.updateActorState,
        MessageConvention.updateRoom,
    };
    private void Awake()
    {
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
    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }
    private void Init()
    {
        ChangeUIByState(RoomState.初始化);
    }


    public void ChangeUIByState(RoomState state)
    {
        curState = state;
        switch (curState)
        {
            case RoomState.初始化:
                uiNoReady.all.SetActive(true);
                uiNoReady.btnReady.SetActive(true);

                UpdateUIInfo();
                ReflashRoomInfo();
                ReflashRoomState();
                break;
            case RoomState.倒计时:
                uiNoReady.all.SetActive(false);
                uiNoReady.all.SetActive(false);

                //Debug.LogError("倒计时的Invoke需要修改");
                //if (IsInvoking("TimeCountDown"))
                //{
                //    CancelInvoke("TimeCountDown");
                //}
                //InvokeRepeating("TimeCountDown", 0, 1);
                break;
        }
    }


    private void UpdateUIInfo()
    {
        txRoomID.text = DataController.instance.MyRoomInfo.RoomID.ToString();
        txRoomName.text = DataController.instance.MyRoomInfo.RoomName;
        txRoomType.text = DataController.instance.MyRoomInfo.RoomType.ToString();
    }
    /// <summary>
    /// 刷新用户显示信息
    /// </summary>
    private void ReflashRoomInfo()
    {
        int memberNum = 0;
        //
        for (int i = 0; i < allMemberShow.Count; i++)
        {
            RoomActor info = null;
            if (DataController.instance.MyRoomInfo.ActorList.ContainsKey(i))
            {
                info = DataController.instance.MyRoomInfo.ActorList[i];
            }
            if (info == null || string.IsNullOrEmpty(info.Nickname))
            {
                allMemberShow[i].all.SetActive(false);
            }
            else
            {
                //Debug.LogError(info.GetSendInfo());
                memberNum++;
                allMemberShow[i].all.SetActive(true);
                allMemberShow[i].txName.text = "" + info.Nickname;
                //allMemberShow[i].txKill.Show(info.KillCount + "");
            }
        }
        //
        txRoomLimet.text = memberNum + "/" + DataController.instance.MyRoomInfo.Limit;
    }
    /// <summary>
    /// 刷新用户显示状态
    /// </summary>
    private void ReflashRoomState()
    {
        for (int i = 0; i < allMemberShow.Count; i++)
        {
            RoomActor info = null;
            if (DataController.instance.MyRoomInfo.ActorList.ContainsKey(i))
            {
                info = DataController.instance.MyRoomInfo.ActorList[i];
            }
            if (info != null)
            {
                allMemberShow[i].imaState.SetActive(info.CurState == RoomActorState.Ready);
                //UI
            }
        }
    }


    public void CountDown()
    {
        ChangeUIByState(RoomState.倒计时);
    }



    #region 按钮点击方法

    public void OnClose()
    {

    }

    public static void Close()
    {
        UIManager.instance.HidePanel(Name);
    }

    public void OnClickClose()
    {
        SocketManager.instance.SendSave((byte)MessageConvention.quitRoom, new byte[] { });
    }
    public void OnClickReady()
    {
        GameManager.instance.isPreparedAllFinish = false;
        uiNoReady.btnReady.SetActive(false);
        //
        GameManager.SendState(RoomActorState.Ready);
    }



    public void OnClickSetting()
    {
        CreateRoomUI.Show(RoomControl.修改房间, DataController.instance.MyRoomInfo.RoomType);
    }

    #endregion


    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.getRoommateInfo)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    Debug.Log("根据玩家数据刷新房间UI");
                    ReflashRoomInfo();
                    ReflashRoomState();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.quitRoom)
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
                        Close();
                        HomeUI.Show();
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
                    ReflashRoomState();
                }
            }
            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.updateRoom)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    Debug.LogError(error);
                }
                else
                {
                    UpdateUIInfo();
                }
            }
        }
    }
}


[Serializable]
public class RoomNoReady
{
    public GameObject all;
    public GameObject btnReady;
}





[Serializable]
public class RoomShowInfo
{
    public GameObject all;
    public Image imgIcon;
    public Text txName;
    public ShowNumImage txKill;
    public GameObject imaState;
}


public enum RoomState
{
    初始化,
    游戏预加载,
    倒计时,
}


