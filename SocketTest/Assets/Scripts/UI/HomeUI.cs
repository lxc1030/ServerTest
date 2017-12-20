using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeUI : MonoBehaviour
{
    public static HomeUI instance;
    public static string Name = "HomeUI";
    private static Action callback;


    public ShowNumImage imgFightCount;
    public ShowNumImage imgWinCount;
    public ShowNumImage imgWinLast;
    public ShowNumImage imgCoin;

    public Text txName;
    public InputField inputName;


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
        MessageConvention.updateName
    };
    private void Awake()
    {
        instance = this;
        //
        SocketManager.ListenDelegate(true, messageHandle, OperationListenInfo);
        GameEventDispatcher.addListener(ItemCountChangeEvent.ITEM_COUNT_CHAGE_EVENT_TAG, ItemChangeEventLister);
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

    private void OnDestroy()
    {
        GameEventDispatcher.removeListener(ItemCountChangeEvent.ITEM_COUNT_CHAGE_EVENT_TAG, ItemChangeEventLister);
    }
    private void OnEnable()
    {
        ADManager.instance.ShowAdByType(ADType.ADS_TYPE_RECOMMAND_BANNER, null);
    }

    private void ItemChangeEventLister(object sender, GameEvent evt)
    {
        ItemCountChangeEvent me = (ItemCountChangeEvent)evt;
        Item item = ItemManager.instance.GetItem(me.itemId);
        if (item.tag == ItemTag.currency.ToString())
        {
            ReflashShow();
        }
    }

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }

    public void Close()
    {
        UIManager.instance.HidePanel(Name);
    }



    public void Init()
    {
        ReflashShow();
        //
    }
    public void ReflashShow()
    {
        imgFightCount.Show("" + DataController.instance.myInfo.Register.fightCount);
        imgWinCount.Show("" + DataController.instance.myInfo.Register.winCount);
        imgWinLast.Show("" + DataController.instance.myInfo.Register.winLast);
        imgCoin.Show("" + DataController.instance.myInfo.Register.coin);
        txName.text = DataController.instance.myInfo.Register.name;
    }


    #region 按钮点击方法

    public void OnClickPause()
    {
        AudioManager.instance.Play();
        GamePause.Show();
    }
    public void OnClickSetting()
    {
        AudioManager.instance.Play();
        Setting.Show();
    }
    public void OnClickRename()
    {
        string name = inputName.text;
        inputName.text = "";

        if (!string.IsNullOrEmpty(name))
        {
            if (name.Length <= 6 * 2)
            {
                byte[] message = SerializeHelper.ConvertToByte(name);
                SocketManager.instance.SendSave((byte)MessageConvention.updateName, message);
            }
            else
            {
                UIManager.instance.ShowAlertTip("名称太长。");
            }
        }
        else
        {
            UIManager.instance.ShowAlertTip("名称为空。");
        }
    }

    public void OnClickCharacter()
    {
        UIManager.instance.ShowAlertTip("暂未开放");
    }
    public void OnClickItem()
    {
        UIManager.instance.ShowAlertTip("暂未开放");
    }

    public void OnClickMain()
    {
        AudioManager.instance.Play();
        BackToMain();
    }

    /// <summary>
    /// 返回主界面
    /// </summary>
    private void BackToMain()
    {
        SocketManager.instance.DisConnect();
        //清除
        Close();
        MainUI.Show();
    }


    public void OnClickAD()
    {
        if (PaymentManager.instance.isOpenAD)
        {
            ADManager.instance.ShowAdByType(ADType.ADS_TYPE_UNITY_REWARD_VIDEO, CallBackCoin, ADFailPlay);
        }
    }


    public void OnClickSelectModel()
    {
        CreateRoomUI.Show(RoomControl.创建房间, GameModel.组队模式);
    }



    #endregion





    /// <summary>
    /// 广告加金币
    /// </summary>
    private void CallBackCoin()
    {
        DataController.instance.SetCoinAdd(DataController.adAddCoin);
    }
    /// <summary>
    /// 看广告失败
    /// </summary>
    private void ADFailPlay()
    {
        if (LanguageManager.instance.CurType == LanguageType.中文)
        {
            UIManager.instance.ShowAlertTip("暂时不能获得免费金币。");
        }
        else
        {
            UIManager.instance.ShowAlertTip("Unable to obtain free gold coins temporarily.");
        }
    }


    private void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();

            if ((MessageConvention)xieyi.XieYiFirstFlag == MessageConvention.updateName)
            {
                ErrorType error = ClassGroup.CheckIsError(xieyi);
                if (error != ErrorType.none)
                {
                    UIManager.instance.ShowAlertTip("更新昵称失败：" + error);
                }
                else
                {
                    UIManager.instance.ShowAlertTip("更新昵称成功");
                    ReflashShow();
                }
            }
        }
    }

}

