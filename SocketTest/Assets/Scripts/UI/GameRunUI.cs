using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRunUI : MonoBehaviour
{
    public static GameRunUI instance;
    public static string Name = "GameRunUI";

    public Transform transName;
    /// <summary>
    /// 比赛时的进度条
    /// </summary>
    public Slider showScore;
    public ShowNumImage showBlue;
    public ShowNumImage showRed;
    public ShowNumImage showTime;


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
        SetNameUIPool();
    }
    public void OnClose()
    {
        Close();
    }
    public static void Close()
    {
        UIManager.instance.HidePanel(Name);
    }


    /// <summary>
    /// 游戏中显示游戏时间
    /// </summary>
    private void SetShowTime(float time)
    {
        showTime.Show(SecondToString(time));
    }

    private string SecondToString(float time)
    {
        DateTime utcdt = DateTime.Parse(DateTime.UtcNow.ToString("1970-01-01 00:00:00")).AddSeconds(time);
        //转成本地时间  
        DateTime localdt = utcdt.ToLocalTime();
        string timeformat = localdt.ToString("mm:ss");
        return timeformat;
    }

    private void SetScorePercent()
    {
        showScore.value = BoxManager.instance.BackScorePercent();
        showBlue.Show(BoxManager.instance.teamBlue + "");
        showRed.Show(BoxManager.instance.teamRed + "");
    }
    private void SetNameUIPool()
    {
        for (int i = transName.childCount - 1; i >= 0; i--)
        {
            PoolManager.instance.SetPoolObjByType(PreLoadType.PeopleInfo, transName.GetChild(i).gameObject);
        }
    }
    
    public void Update()
    {
        SetShowTime(DataController.instance.MyRoomInfo.GameTime);
        SetScorePercent();
    }

}

