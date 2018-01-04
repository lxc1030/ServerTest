using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class SocketConnectUI : MonoBehaviour
{
    public static SocketConnectUI instance;
    public static string Name = "SocketConnectUI";

    public SocketLoadingUI myLoading;
    public SocketReLogin myReLogin;


    public NetState CurState;
    public float showRoundTime;
    public float showTimeOutTime;

    public MessageConvention curConvention;

    #region 注册Socket处理

    /// <summary>
    /// 待处理的Socket信息
    /// </summary>
    private Queue<MessageXieYi> serverEvent = new Queue<MessageXieYi>();
    /// <summary>
    /// 该脚本要处理的socket数据类型----每个脚本自己定义自己要侦听的消息类型，只会接收已定义的协议数据
    /// </summary>
    private List<MessageConvention> messageHandle = new List<MessageConvention>();
    private void Awake()
    {
        instance = this;
        //
    }
    private void OnDisable()
    {
        SocketManager.ListenDelegate(false, messageHandle, OperationListenInfo);//删除侦听
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

    public void Open(MessageConvention con)
    {
        curConvention = con;
        OnDisable();
        messageHandle.Clear();
        messageHandle.Add(con);
        SocketManager.ListenDelegate(true, messageHandle, OperationListenInfo);//添加侦听
        Debug.Log("协议：" + con + " 显示挡板。");
        ChangeUIByState(NetState.显示挡板);
    }

    public void OffLine()
    {
        OnDisable();
        CancelInvoke();
        ChangeUIByState(NetState.断线重连);
        //
        GameManager.instance.OffLine();
        MyJoystickManager.instance.OffLine();
    }

    #region Invoke调用函数
    private void ShowRound()
    {
        ChangeUIByState(NetState.显示进度);
    }
    private void TimeOut()
    {
        ChangeUIByState(NetState.超时);
    }
    #endregion


    public void ChangeUIByState(NetState state)
    {
        CurState = state;
        switch (CurState)
        {
            case NetState.显示挡板:
                myLoading.all.SetActive(true);
                myLoading.uiBG.gameObject.SetActive(true);
                myLoading.uiBG.color = new Color(1, 1, 1, 0);
                myLoading.uiRound.SetActive(false);
                myLoading.uiBtn.SetActive(false);
                //
                myReLogin.all.SetActive(false);

                Invoke("ShowRound", showRoundTime);
                Invoke("TimeOut", showTimeOutTime);
                break;
            case NetState.显示进度:
                myLoading.uiBG.color = new Color(1, 1, 1, 0.5f);
                myLoading.uiRound.SetActive(true);
                break;
            case NetState.不显示UI:
                CancelInvoke();
                myLoading.all.SetActive(false);
                //
                myReLogin.all.SetActive(false);
                break;
            case NetState.超时:
                myLoading.uiBtn.SetActive(true);
                SocketManager.ListenDelegate(false, messageHandle, OperationListenInfo);//删除侦听
                break;
            case NetState.断线重连:
                myLoading.all.SetActive(false);
                //
                myReLogin.all.SetActive(true);
                myReLogin.uiBtn.SetActive(true);
                break;
        }
    }


    public void OnClickSure()
    {
        switch (CurState)
        {
            case NetState.超时:
                ChangeUIByState(NetState.不显示UI);
                break;
            case NetState.断线重连:
                UIManager.instance.HideAll();
                myReLogin.uiBtn.SetActive(false);
                if (!string.IsNullOrEmpty(DataController.instance.myInfo.Register.userID))
                {
                    SocketManager.instance.Login();
                }
                else
                {
                    //初始化tcp
                    SocketManager.instance.Init();
                    ChangeUIByState(NetState.不显示UI);
                }
                break;
        }
    }

    private void NetReConnected(SocketError error)
    {
        string info = "";
        switch (error)
        {
            case SocketError.Success:
                info = "连接服务器成功。";
                ChangeUIByState(NetState.不显示UI);
                break;
            case SocketError.ConnectionRefused:
                info = "服务器主动拒绝本次请求。";
                break;
            default:
                info = error.ToString();
                break;
        }
        UIManager.instance.ShowAlertTip(info);
    }




    void Update()
    {
        if (
            SocketManager.instance.MyUserToken != null
            && SocketManager.instance.MyUserToken.ConnectSocket != null
            && !SocketManager.instance.isConnected
            )
        {
            if (CurState != NetState.断线重连)
            {
                OffLine();
            }
        }
        else
        {
            if (CurState == NetState.断线重连)
            {
                ChangeUIByState(NetState.不显示UI);
            }
        }
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
            for (int i = 0; i < messageHandle.Count; i++)
            {
                if ((MessageConvention)xieyi.XieYiFirstFlag == messageHandle[i])
                {
                    ChangeUIByState(NetState.不显示UI);
                    break;
                }
            }
        }
    }

}
public enum NetState
{
    显示挡板,
    显示进度,
    不显示UI,
    超时,
    断线重连,
}
[System.Serializable]
public class SocketLoadingUI
{
    public GameObject all;
    public Image uiBG;
    public GameObject uiRound;
    public GameObject uiBtn;
}
[System.Serializable]
public class SocketReLogin
{
    public GameObject all;
    public GameObject uiBtn;
}