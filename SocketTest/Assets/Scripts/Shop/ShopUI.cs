using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Umeng;

public class ShopUI : MonoBehaviour
{
    public static string Name = "ShopUI";
    public static ShopUI instance;

    //private static string XMLName = "ShopConfig";
    private static string XMLName = "ChargeConfig";
    public Dictionary<int, ChargeItem> config = new Dictionary<int, ChargeItem>();

    public Text txCoin;
    public Text txPower;
    public Transform transContain;
    public GameObject prefab;

    public GameObject[] adBtns;

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
    }
    private void Awake()
    {
        instance = this;
        GameEventDispatcher.addListener(ItemCountChangeEvent.ITEM_COUNT_CHAGE_EVENT_TAG, ItemChangeEventLister);
        instance.Init();
    }

    private void OnDestroy()
    {
        GameEventDispatcher.removeListener(ItemCountChangeEvent.ITEM_COUNT_CHAGE_EVENT_TAG, ItemChangeEventLister);
    }

    private void ItemChangeEventLister(object sender, GameEvent evt)
    {
        ItemCountChangeEvent me = (ItemCountChangeEvent)evt;
        Item item = ItemManager.instance.GetItem(me.itemId);
        if (item.tag == ItemTag.currency.ToString())
        {
            ReflashCurrecry();
        }
    }

    private void ReflashCurrecry()
    {
        txCoin.text = "" + ItemManager.instance.GetItem(ItemID.Coin).Count;
        txPower.text = "" + ItemManager.instance.GetItem((ItemID)ItemId.体力).Count;
    }

    public void Init()
    {
        ReflashCurrecry();
        ReadConfig();
        ShowInfos();
    }





    #region 读表
    void ReadConfig()
    {
        List<ChargeItem> temp = new List<ChargeItem>();
        string XMLTotalName = XMLName;
        if (LanguageManager.instance.CurType == LanguageType.中文)
        {
            XMLTotalName += "_China";
        }
        else
        {
            XMLTotalName += "_English";
        }
        FileUtil.loadConfig(XMLTotalName, ref temp, null, false);
        for (int i = 0; i < temp.Count; i++)
        {
            config.Add(temp[i].chargeId, temp[i]);
        }
    }

    #endregion

    private void ShowInfos()
    {
        //删除不需要显示的数据
        List<ChargeItem> selectItem = new List<ChargeItem>();
        foreach (var item in config.Values)
        {
            if ((item.isShop == 1))
            {
                selectItem.Add(item);
            }
        }
        //

        GameObject obj = null;
        GridLayoutGroup grid = transContain.GetComponent<GridLayoutGroup>();

        RectTransform trans = transContain.GetComponent<RectTransform>();
        float height = 0;
        height += grid.padding.top;
        height += grid.padding.bottom;
        height += (grid.cellSize.y + grid.spacing.y) * selectItem.Count;

        Vector2 org = trans.sizeDelta;
        trans.sizeDelta = new Vector2(0, height);

        foreach (var item in selectItem)
        {
            obj = Common.Generate(prefab, transContain);
            ShopInfo info = obj.GetComponent<ShopInfo>();
            info.Init(item);
        }
        //AD按钮随机显示一个
        CloseADButtons();
        int ad = Random.Range(0, adBtns.Length);
        adBtns[ad].SetActive(true);

    }

    private void CloseADButtons()
    {
        for (int i = 0; i < adBtns.Length; i++)
        {
            adBtns[i].SetActive(false);
        }
    }

    private ChargeItem CurChargeItem;
    public void DoItemPay(int payType, int payNum)
    {
        CurChargeItem = PaymentManager.instance.GetNearestChargeItem(payType, payNum);
        if (CurChargeItem != null)
        {
            PaymentManager.instance.DoPay(CurChargeItem, PayBackSuccess, PayBackFail);
        }
    }


    private void PayBackSuccess()
    {
        if (!PaymentManager.instance.isTestPay)
        {
            GA.Pay(CurChargeItem.price, GA.PaySource.GAME, CurChargeItem.GetAddNum());
        }
    }
    private void PayBackFail()
    {
        //UIManager.instance.ShowAlertTip("付费失败");
    }






    public void OnClickADCoin()
    {
        ADManager.instance.ShowAdByType(ADType.ADS_TYPE_UNITY_REWARD_VIDEO, CallBackCoin, ADFailPlay);
    }
    private void CallBackCoin()
    {
        DataController.instance.SetCoinAdd(50);
    }

    public void OnClickADPower()
    {
        ADManager.instance.ShowAdByType(ADType.ADS_TYPE_UNITY_REWARD_VIDEO, CallBackPower, ADFailPlay);
    }
    private void CallBackPower()
    {
        PowerManager.instance.AddPower(3);
    }


    private void ADFailPlay()
    {
        CloseADButtons();
    }





    public void OnClose()
    {
        Close();
    }

    public void Close()
    {
        AudioManager.instance.Play();
        UIManager.instance.HidePanel(Name, true);
    }

}
[System.Serializable]
public class ShopItem
{
    public int id;
    public int costType;
    public int costNum;
    public int addType;
    public int addNum;
    public string spriteName;
    public string desc;
    public string notice;
}

public enum CostType
{
    无 = 0,
    钻石 = 1,
    金币 = 2,
    RMB = 3
}