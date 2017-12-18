using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class PowerManager : MonoBehaviour
{

    public static PowerManager instance;

    /// <summary>
    /// 最大体力数
    /// </summary>
    public const int powerMaxNum = 100;
    /// <summary>
    /// 恢复1点体力需要的秒数
    /// </summary>
    public const float powerSpanTime = 60;

    #region 保存体力变化时的时间
    private string powerPrefs = "PowerTime";

    public DateTime savePowerOnTime
    {
        get
        {
            DateTime time = DateTime.Parse(PlayerPref.GetString(powerPrefs, DateTime.MinValue.ToString()));
            if (time == DateTime.MinValue)
            {
                time = DateTime.Now;
            }
            return time;
        }
        set
        {
            PlayerPref.SetString(powerPrefs, value.ToString());
        }
    }
    #endregion

    #region 保存体力变化时剩余的时间
    private string leftTimePrefs = "LeftTime";
    public int LeftSecond
    {
        get
        {
            return PlayerPref.GetInt(leftTimePrefs, 0);
        }
        set
        {
            PlayerPref.SetInt(leftTimePrefs, value);
        }
    }
    #endregion

    /// <summary>
    /// 倒计时存储时间
    /// </summary>
    public float allShowTime;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        GameEventDispatcher.addListener(ItemCountChangeEvent.ITEM_COUNT_CHAGE_EVENT_TAG, ItemChangeEventLister);
    }
    private void OnDestroy()
    {
        GameEventDispatcher.removeListener(ItemCountChangeEvent.ITEM_COUNT_CHAGE_EVENT_TAG, ItemChangeEventLister);
    }
    private void ItemChangeEventLister(object sender, GameEvent evt)
    {
        ItemCountChangeEvent me = (ItemCountChangeEvent)evt;
        Item item = ItemManager.instance.GetItem(me.itemId);
        if (item.itemId == (int)ItemId.体力)
        {
            savePowerOnTime = DateTime.Now;
            if (ItemManager.instance.GetItem((ItemID)ItemId.体力).Count >= powerMaxNum)
            {
                allShowTime = 0;
            }
            else
            {
                allShowTime = (powerMaxNum - ItemManager.instance.GetItem((ItemID)ItemId.体力).Count) * powerSpanTime;
            }
            LeftSecond = (int)allShowTime;
        }
    }
    public void Start()
    {
        Init();
    }
    /// <summary>
    /// 进游戏以后计算体力增长值，并增加体力
    /// </summary>
    public void Init()
    {
        Item item = ItemManager.instance.GetItem((ItemID)ItemId.体力);
        if (item.Count < powerMaxNum)
        {
            TimeSpan passTime = DateTime.Now.Subtract(savePowerOnTime);//当前时间减去存储时间
            allShowTime = LeftSecond;
            //if (passTime.TotalSeconds > 0)
            {
                int addPowerNum = (int)passTime.TotalSeconds / (int)powerSpanTime;
                //AddPower(addPowerNum);
                //allShowTime -= (passTime.TotalSeconds - addPowerNum * powerSpanTime);
                ////if (addPowerNum > 0)//超过1点体力就加1
                {
                    if (item.Count + addPowerNum < powerMaxNum)
                    {
                        allShowTime -= (float)passTime.TotalSeconds;
                        AddPower(addPowerNum);
                    }
                    else
                    {
                        allShowTime = 0;
                        AddPower(powerMaxNum - item.Count);
                    }
                }
                ////else
                ////{
                ////    allShowTime -= (float)passTime.TotalSeconds;
                ////}
            }
        }
        Invoke("ReduceTime", 1);
    }



    /// <summary>
    /// 获取当前应该显示的时间
    /// </summary>
    /// <returns></returns>
    public string GetShowTime()
    {
        return "" + TimeSpan.FromSeconds((int)allShowTime);
    }

    /// <summary>
    /// 减1秒
    /// </summary>
    private void ReduceTime()
    {
        if (allShowTime > 0)
        {
            allShowTime -= 1;
            TimeSpan span = TimeSpan.FromSeconds(allShowTime);
            if (span.Seconds == 0)
            {
                if ((int)span.TotalSeconds % powerSpanTime == 0)
                {
                    ShowTimeReduceToAddPower();
                }
            }
        }
        Invoke("ReduceTime", 1);
    }
    /// <summary>
    /// 减体力
    /// </summary>
    /// <param name="num"></param>
    public void ReducePower(int num)
    {
        AddPower(-num);

    }
    public void AddPower(int num)
    {
        ItemManager.instance.AddItem((ItemID)ItemId.体力, num);
    }
    /// <summary>
    /// 时间到0 加1
    /// </summary>
    private void ShowTimeReduceToAddPower()
    {
        AddPower(1);
    }

}
