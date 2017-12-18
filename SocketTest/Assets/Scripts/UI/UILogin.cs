using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
public class UILogin : MonoBehaviour
{
    private static UILogin instance;
    public static string Name = "UILogin";

    public InputField txUserID;
    public InputField txPassword;

    public GameObject btnLogin;


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

    public void Init()
    {

    }

    public void OnClickLogin()
    {
        LoginHttp(txUserID.text, txPassword.text, GetServerBack);
    }

    public void OnClickWantRegister()
    {
        UIRegister.Show();
        Close();
    }

    public void GetServerBack(string receive)
    {
        JObject jons = JObject.Parse(receive);
        string comm = (string)jons[nameof(Comm)];

        if (comm != null && comm == (int)Comm.error + "")
        {
            ErrorType error = (ErrorType)int.Parse((string)jons[nameof(ErrorType)]);
            ErrorShow(error);
        }
        else//登录成功
        {
            DataController.instance.myInfo = new Register()
            {
                userID = (string)jons[nameof(Register.userID)],
                password = (string)jons[nameof(Register.password)],
                name = (string)jons[nameof(Register.name)],
                iconID = (int)jons[nameof(Register.iconID)],
                coin = (int)jons[nameof(Register.coin)],
                fightCount = (int)jons[nameof(Register.fightCount)],
                winCount = (int)jons[nameof(Register.winCount)],
                winLast = (int)jons[nameof(Register.winLast)]
            };
            //初始化tcp
            //SocketManager.instance.Init(GetSocketBack);
            //登录
            SocketManager.instance.Login();
        }
    }

    public static void LoginHttp(string userID, string password, Action<string> callback)
    {
        string[] column = new string[] { nameof(Comm), nameof(Register.userID), nameof(Register.password) };
        string[] value = new string[] { "" + (int)Comm.login, "" + userID, "" + password };
        string sendData = ClassGroup.StringToHttp(column, value);

        HttpClient.instance.SendComm(sendData, callback);
    }


    public void Close()
    {
        UIManager.instance.HidePanel(Name);
    }


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
                    Debug.LogError(error);
                }
                else
                {
                    HeartbeatTime beatTime = SerializeHelper.Deserialize<HeartbeatTime>(xieyi.MessageContent);
                    Debug.LogError("登录账号成功,心跳检测时间:" + beatTime.time + "/此处需要加上场景和UI初始化逻辑");
                    //打开心跳检测
                    SocketManager.instance.heartbeatSecondTime = beatTime.time - 1;//-1防止和服务器心跳时间一致的时候会导致偏差
                    SocketManager.instance.OpenHeartbeat();
                    Close();
                    HomeUI.Show();
                    //
                    Debug.Log("自身检查是否需要重连。");
                    SocketManager.instance.SendSave((byte)MessageConvention.reConnect, new byte[] { }, false);
                }
            }
        }
    }




}
