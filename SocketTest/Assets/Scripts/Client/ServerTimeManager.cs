using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerTimeManager : MonoBehaviour
{
    public static ServerTimeManager instance;
    public static int AllowDelayMS = 20;



    public float checkMarkTime;
    private DateTime serverTime = DateTime.Now;
    public DateTime ServerTime
    {
        get
        {
            TimeSpan last = TimeSpan.FromSeconds(Time.realtimeSinceStartup - checkMarkTime);
            return serverTime.Add(last);
        }
        set
        {
            serverTime = value;
        }
    }






    private void Awake()
    {
        DontDestroyOnLoad(this);
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
        MessageConvention.timeCheck,
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

    public void CheckServerTime()
    {
        ProofreadTime timeCheck = new ProofreadTime();
        timeCheck.UnityRealTime = Time.realtimeSinceStartup;
        //Debug.LogError("发送时：" + DataController.instance.ServerTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        timeCheck.ClientTime = ServerTime;
        timeCheck.ServerTime = DateTime.Now;
        CheckServerTime(timeCheck);
    }
    public void CheckServerTime(ProofreadTime timeCheck)
    {
        byte[] newBuffer = SerializeHelper.Serialize<ProofreadTime>(timeCheck);
        SocketManager.instance.SendSave((byte)MessageConvention.timeCheck, newBuffer, false);
    }


    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
            ErrorType error = ClassGroup.CheckIsError(xieyi);
            switch ((MessageConvention)xieyi.XieYiFirstFlag)
            {
                case MessageConvention.timeCheck:
                    ProofreadTime timeCheck = SerializeHelper.Deserialize<ProofreadTime>(xieyi.MessageContent);
                    if (timeCheck.IsNeedCheck)//服务器强制客户端重新校对时间
                    {
                        timeCheck.UnityRealTime = Time.realtimeSinceStartup;
                        CheckServerTime(timeCheck);
                    }
                    else
                    {
                        float curTime = Time.realtimeSinceStartup;
                        float delayTime = (curTime - timeCheck.UnityRealTime);
                        int delayMS = (int)(delayTime * 1000);
                        if (delayMS <= AllowDelayMS)//本次延迟在允许的范围内。将本次服务器时间整理保存
                        {
                            Debug.LogError("延迟在允许的范围内:" + delayMS + "/" + AllowDelayMS);
                            int milliSecond = (int)(delayTime / 2 * 1000);//来回的延迟一半作为单次传输延迟
                            TimeSpan span = TimeSpan.FromMilliseconds(milliSecond);
                            checkMarkTime = curTime;
                            ServerTime = timeCheck.ServerTime.Add(span);
                        }
                        else
                        {
                            Debug.LogError("延迟不允许:" + delayMS + "/" + AllowDelayMS);
                            CheckServerTime();
                            //if (!IsInvoking(nameof(CheckServerTime)))
                            //{
                            //    Invoke(nameof(CheckServerTime), 1);//延迟1秒调用一次校对
                            //}
                        }


                        ////client 发现 server 的数据包“提前”到达
                        //if (timeCheck.ServerTime.Subtract(DataController.instance.ServerTime).TotalMilliseconds > 0)//重新校对
                        //{
                        //    Debug.LogError("超前");
                        //}



                    }
                    break;
            }
        }
    }

    //string guiInfo = "";
    //public void OnGUI()
    //{
    //    guiInfo = ServerTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
    //    GUIStyle bb = new GUIStyle();
    //    bb.normal.background = null;    //这是设置背景填充的
    //    bb.normal.textColor = Color.blue;   //设置字体颜色的
    //    bb.fontSize = 40;       //当然，这是字体大小
    //    GUI.Label(new Rect(Screen.width / 2 - 100, 0, 200, 200), guiInfo, bb);
    //}
}
