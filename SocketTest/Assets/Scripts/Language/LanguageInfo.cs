using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LanguageInfo : MonoBehaviour
{
    private Image langImage;
    private Text langText;
    private void Awake()
    {
        GameEventDispatcher.addListener(LanguageChangeEvent.Language_Change_Event_Tag, LanguageEventLister);
        langImage = GetComponent<Image>();
        langText = GetComponent<Text>();
    }

    public void Start()
    {
        ReflashByLanguage(LanguageType.中文, LanguageManager.instance.CurType);
    }

    private void OnDestroy()
    {
        GameEventDispatcher.removeListener(LanguageChangeEvent.Language_Change_Event_Tag, LanguageEventLister);
    }
    private void LanguageEventLister(object sender, GameEvent evt)
    {
        LanguageChangeEvent my = (LanguageChangeEvent)evt;
        ReflashByLanguage(my.lastType, my.beType);
    }
    /// <summary>
    /// 刷新自身显示(增加语言需要修改)
    /// </summary>
    /// <param name="last"></param>
    /// <param name="type"></param>
    private void ReflashByLanguage(LanguageType last, LanguageType type)
    {
        LanguageItem item = null;
        if (langImage != null)
        {
            item = LanguageManager.instance.GetItemByString(last, langImage.sprite.name);
            if (item == null)
            {
                Debug.LogError("E该图片的语言版本信息未找到：" + name + "/" + langImage.sprite.name);
                return;
            }
            string info = LanguageManager.GetInfoByType(item, type);
            string spName = DataController.iconPathSkill + info;
            langImage.sprite = Resources.Load(spName, typeof(Sprite)) as Sprite;
            langImage.SetNativeSize();
        }
        else if (langText != null)
        {
            if (string.IsNullOrEmpty(langText.text))
            {
                return;
            }
            item = LanguageManager.instance.GetItemByString(last, langText.text);
            if (item == null)
            {
                Debug.LogError("E该文字的语言版本信息未找到：" + name + "/" + langText.text);
                return;
            }
            string info = LanguageManager.GetInfoByType(item, type);
            langText.text = info;
        }


    }

}
