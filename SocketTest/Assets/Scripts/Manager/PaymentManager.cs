using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PaymentManager : MonoBehaviour
{
    public static PaymentManager instance;

    public bool isTestPay;
    public bool isOpenAD;


    private static string XMLName = "ChargeConfig";
    public Dictionary<int, ChargeItem> config = new Dictionary<int, ChargeItem>();

    private static string XMLTotalName = "ChargeConfig";


    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);

        GameEventDispatcher.addListener(LanguageChangeEvent.Language_Change_Event_Tag, LanguageEventLister);
        instance.Init();
    }

    private void OnDestroy()
    {
        GameEventDispatcher.removeListener(LanguageChangeEvent.Language_Change_Event_Tag, LanguageEventLister);
    }
    private void LanguageEventLister(object sender, GameEvent evt)
    {
        LanguageChangeEvent my = (LanguageChangeEvent)evt;
        SetXMLNameByLanguage(my.beType);
    }
    private void Init()
    {
        SetXMLNameByLanguage(LanguageManager.instance.CurType);
    }
    private void SetXMLNameByLanguage(LanguageType curType)
    {
        XMLTotalName = XMLName;
        if (curType == LanguageType.中文)
        {
            XMLTotalName += "_China";
        }
        else if (curType == LanguageType.英文)
        {
            XMLTotalName += "_English";
        }
        else
        {
            Debug.LogError("检查是否没有判断中英文以外的价格配表");
        }

        ReadConfig();
    }



    void ReadConfig()
    {
        List<ChargeItem> temp = new List<ChargeItem>();
        config.Clear();
        FileUtil.loadConfig(XMLTotalName, ref temp, null, false);
        for (int i = 0; i < temp.Count; i++)
        {
            config.Add(temp[i].chargeId, temp[i]);
        }

        if (config.Count <= 0)
        {
            Debug.LogError("价格配表数据为空！");
        }
    }

    public ChargeItem GetChargeItem(int chargeId)
    {
        return config[chargeId];
    }
    public ChargeItem GetChargeItem(int payType, int payNum)
    {
        return GetNearestChargeItem(payType, payNum);
    }

    public ChargeItem GetNearestChargeItem(int payType, int payNum)
    {
        if (config.Count <= 0)
        {
            Init();
        }
        ChargeItem get = null;
        foreach (var item in config.Values)
        {
            if (item.GetAddType() == payType)
            {
                if (get == null || item.GetAddNum() > get.GetAddNum())
                {
                    get = item;
                }
                if (item.GetAddNum() >= payNum)
                {
                    get = item;
                    break;
                }
            }
        }
        return get;
    }

    public ChargeItem GetGiftChargeItemByType(string type)
    {
        if (config.Count <= 0)
        {
            Init();
        }
        ChargeItem get = null;
        foreach (var item in config.Values)
        {
            if (item.type == type)
            {
                get = item;
                break;
            }
        }
        return get;
    }


    private ChargeItem payItem;
    private Action callbackSuccess;
    private Action callbackFail;
    public void DoPay(ChargeItem item, Action _callbackSuccess, Action _callbackFail)
    {
        payItem = item;
        callbackSuccess = _callbackSuccess;
        callbackFail = _callbackFail;
        if (isTestPay)
        {
            PayJavaBack("1");
            return;
        }
        if (Application.isMobilePlatform)
        {
            ActivityManager.Instance().GetAndriod().CallStatic("Pay", item.chargeId.ToString());
            //androidCall.CallStatic("Pay", item.chargeId.ToString());
        }
        else
        {
            PayJavaBack("1");
        }
    }


    public void PayJavaBack(string message)
    {
        if (message == "0")//false
        {
            if (callbackFail != null)
                callbackFail();
        }
        else if (message == "1")//true
        {
            string[] split = payItem.addInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 1)
            {
                for (int i = 0; i < split.Length; i++)
                {
                    string[] split2 = split[i].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split2.Length == 2)
                    {
                        ItemManager.instance.AddItem(int.Parse(split2[0]), int.Parse(split2[1]));
                    }
                }
            }
            if (callbackSuccess != null)
                callbackSuccess();
        }
    }

    public void LoginGoogle()
    {
        ActivityManager.Instance().GetAndriod().CallStatic("ShowLeaderboards");
    }

    public void ReflashRankData(long _score)
    {
        if (Application.isEditor)
            return;
        ActivityManager.Instance().GetAndriod().CallStatic("ReflashRankData", _score);
    }
}


public class ChargeItem
{
    public int chargeId;
    public int price;
    public string addInfo;
    public int isShop;
    public string type;
    public string desc;
    public string notice;

    public int GetAddType()
    {
        string[] nums = SplitOut(addInfo);
        if (nums.Length == 2)
        {
            return int.Parse(nums[0]);
        }
        return -1;
    }

    public int GetAddNum()
    {
        string[] nums = SplitOut(addInfo);
        if (nums.Length == 2)
        {
            return int.Parse(nums[1]);
        }
        return -1;
    }

    public string[] SplitOut(string info)
    {
        string[] split = info.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 1)
        {
            string[] split2 = split[0].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            return split2;
        }
        return null;
    }
}