using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomUI : MonoBehaviour
{
    public static CreateRoomUI instance;
    public static string Name = "CreateRoomUI";

    public Text txIntroduce;

    public GameObject objRoomType;
    public GameObject objRoomName;
    public GameObject objRoomID;

    public Dropdown downControl;
    public Dropdown downModel;
    public Dropdown downName;
    public Dropdown downID;



    public InputField inputName;
    public InputField inputID;




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
        MessageConvention.createRoom,
        MessageConvention.joinRoom,
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

    public RoomControl curControl;
    public GameModel selectModel;

    public static void Show(RoomControl control, GameModel model)
    {
        UIManager.instance.ShowPanel(Name);
        instance.curControl = control;
        instance.selectModel = model;
        instance.Init();
    }

    [ContextMenu("切换")]
    private void Init()
    {
        txIntroduce.text = "点击模式显示游戏规则。";
        downControl.value = (int)curControl;
        downControl.Select();

        if (curControl == RoomControl.修改房间)
        {
            downControl.interactable = false;
        }
        else
        {
            downControl.interactable = true;
            downControl.options.RemoveAt(downControl.options.Count - 1);
        }

        OnClickRoomSelect(downControl);
    }

    public void OnClose()
    {
        Close();
    }

    public static void Close()
    {
        UIManager.instance.HidePanel(Name, true);
    }

    public void OnClickClose()
    {
        Close();
    }


    public void OnClickRoomSelect(Dropdown down)
    {
        curControl = (RoomControl)down.value;
        switch (curControl)
        {
            case RoomControl.创建房间:
                objRoomName.SetActive(true);
                objRoomID.SetActive(false);
                objRoomType.SetActive(true);
                //OnClickRoomName(downName);
                inputName.text = DataController.instance.myInfo.name + "的房间";
                break;
            case RoomControl.加入房间:
                objRoomName.SetActive(false);
                objRoomID.SetActive(true);
                objRoomType.SetActive(true);
                OnClickRoomID(downID);
                break;
            case RoomControl.修改房间:
                objRoomName.SetActive(true);
                objRoomID.SetActive(false);
                inputName.text = DataController.instance.MyRoomInfo.RoomName;
                //OnClickRoomName(downName);
                break;
        }
    }

    public void OnClickModel(Dropdown down)
    {
        selectModel = (GameModel)down.value;
        txIntroduce.text = "选中模式：" + selectModel;
    }

    public void OnClickRoomID(Dropdown down)
    {
        if (down.value == 0)
        {
            inputID.gameObject.SetActive(false);
            objRoomType.SetActive(true);
        }
        else
        {
            inputID.gameObject.SetActive(true);
            objRoomType.SetActive(false);
            inputID.text = "";
        }
    }



    public void OnClickSure()
    {
        switch (curControl)
        {
            case RoomControl.创建房间:
                OnClickCreateRoom();
                break;
            case RoomControl.加入房间:
                OnClickJoinRoom();
                break;
            case RoomControl.修改房间:
                OnClickChangeInfo();
                break;
        }


    }





    public void OnClickCreateRoom()
    {
        if (selectModel != GameModel.组队模式)
        {
            UIManager.instance.ShowAlertTip("该模式还未开启。");
            return;
        }
        if (string.IsNullOrEmpty(inputName.text))
        {
            UIManager.instance.ShowAlertTip("房间名称为空");
            return;
        }
        //
        //RoomUI.Show();
        string[] colum = new string[]
        {
            nameof(RoomInfo.RoomName),
            nameof(RoomInfo.RoomType),
            nameof(Register.userID)
        };
        string[] value = new string[]
        {
            inputName.text,
            (int)selectModel + "",
            DataController.instance.myInfo.userID
        };
        string tcp = ClassGroup.StringToJosn(colum, value);
        byte[] message = SerializeHelper.ConvertToByte(tcp);
        SocketManager.instance.SendSave((byte)MessageConvention.createRoom, message);
        //
    }
    public void OnClickJoinRoom()
    {
        if (selectModel != GameModel.组队模式)
        {
            UIManager.instance.ShowAlertTip("该模式还未开启。");
            return;
        }
        if (downID.value == 1)//加入指定房间，则需要房间号
        {
            if (string.IsNullOrEmpty(inputID.text))
            {
                UIManager.instance.ShowAlertTip("房间号为空。");
                return;
            }
        }
        //RoomUI.Show();
        //tcp
        string[] colum = new string[]
        {
            nameof(RoomInfo.RoomType),
            nameof(Register.userID),
            nameof(RoomInfo.RoomID)
        };
        string[] value = new string[]
        {
            (int)selectModel + "",
            DataController.instance.myInfo.userID,
            inputID.text
        };
        string tcp = ClassGroup.StringToJosn(colum, value);
        byte[] message = SerializeHelper.ConvertToByte(tcp);
        SocketManager.instance.SendSave((byte)MessageConvention.joinRoom, message);
        //
    }

    public void OnClickChangeInfo()
    {
        if (selectModel != GameModel.组队模式)
        {
            UIManager.instance.ShowAlertTip("该模式还未开启。");
            return;
        }
        //tcp
        string[] colum = new string[]
        {
            nameof(Register.userID),
            nameof(RoomInfo.RoomID),
            nameof(RoomInfo.RoomType),
            nameof(RoomInfo.RoomName)
        };
        string[] value = new string[]
        {
            DataController.instance.myInfo.userID,
            DataController.instance.MyRoomInfo.RoomID + "",
            (int)selectModel + "",
            inputName.text
        };
        string tcp = ClassGroup.StringToJosn(colum, value);
        byte[] message = SerializeHelper.ConvertToByte(tcp);
        SocketManager.instance.SendSave((byte)MessageConvention.updateRoom, message);
    }



 


    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();

            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.updateRoom)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    UIManager.instance.ShowAlertTip("更新房间失败：" + error);
                }
                else
                {
                    UIManager.instance.ShowAlertTip("更新房间成功");
                }
                Close();
            }
        }
    }


}

public enum RoomControl
{
    创建房间 = 0,
    加入房间 = 1,
    修改房间 = 2
}