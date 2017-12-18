using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager instance;
    private static string XMLName = "LanguageConfig";
    public List<LanguageItem> config = new List<LanguageItem>();

    #region 获取当前版本和默认版本



    private string langPrefs = "CurrnetLanguage";
    public LanguageType CurType
    {
        get
        {
LanguageType NormalType = LanguageType.中文;
#if SanWang
        NormalType = LanguageType.中文;
#elif GooglePlay
        NormalType = LanguageType.英文;
#endif
            return (LanguageType)PlayerPref.GetInt(langPrefs, (int)NormalType);
        }
        set
        {
            PlayerPref.SetInt(langPrefs, (int)value);
        }
    }
    #endregion
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
        GameEventDispatcher.addListener(LanguageChangeEvent.Language_Change_Event_Tag, LanguageEventLister);
        instance.ReadConfig();
    }
    private void OnDestroy()
    {
        GameEventDispatcher.removeListener(LanguageChangeEvent.Language_Change_Event_Tag, LanguageEventLister);
    }
    private void LanguageEventLister(object sender, GameEvent evt)
    {
        LanguageChangeEvent my = (LanguageChangeEvent)evt;
        CurType = my.beType;
    }


    void ReadConfig()
    {
        FileUtil.loadConfig(XMLName, ref config, null, false);
    }
    /// <summary>
    /// 获取一条数据根据语言种类和文字(增加语言需要修改)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public LanguageItem GetItemByString(LanguageType type, string info)
    {
        foreach (var item in config)
        {
            switch (type)
            {
                case LanguageType.中文:
                    if (item.chinese == info)
                    {
                        return item;
                    }
                    break;
                case LanguageType.英文:
                    if (item.english == info)
                    {
                        return item;
                    }
                    break;
            }
        }
        return null;
    }
    /// <summary>
    /// 根据类型获得指定信息内的对应信息(增加语言需要修改)
    /// </summary>
    /// <param name="item"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetInfoByType(LanguageItem item, LanguageType type)
    {
        string back = "";
        if (item == null)
        {
            Debug.Log("E检查是否有个物体初始值未设置为中文！");
        }
        else
        {
            switch (type)
            {
                case LanguageType.中文:
                    back = item.chinese;
                    break;
                case LanguageType.英文:
                    back = item.english;
                    break;
            }
        }
        return back;
    }

}
public class LanguageItem
{
    public int id;
    public string chinese;
    public string english;
    public string desc;
}


public class LanguageChangeEvent : GameEvent
{
    public const string Language_Change_Event_Tag = "LanguageChangeEventTag";
    public LanguageType lastType;
    public LanguageType beType;

    public LanguageChangeEvent(LanguageType _last, LanguageType _be) : base(Language_Change_Event_Tag)
    {
        lastType = _last;
        beType = _be;
    }

}

public enum LanguageType
{
    中文,
    英文,
}