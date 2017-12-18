using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    public static string Name = "Setting";

    public Toggle tMusic;
    public Toggle tSound;

    public GameObject objLanguage;

    /// <summary>
    /// 语言：0=english 1=chinese
    /// </summary>
    public Text txLang;
    public Toggle tChinese;
    public Toggle tEnglish;


    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
    }
    private void OnEnable()
    {
        tMusic.isOn = AudioManager.instance.IsMusic;
        tSound.isOn = AudioManager.instance.IsSound;
        LanguageType curLang = LanguageManager.instance.CurType;
        tChinese.isOn = (curLang == LanguageType.中文);
        tEnglish.isOn = (curLang == LanguageType.英文);

#if SanWang
        objLanguage.gameObject.SetActive(false);
#elif GooglePlay
        objLanguage.gameObject.SetActive(true);
#endif


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

    public void OnClickMusic()
    {
        AudioManager.instance.IsMusic = !AudioManager.instance.IsMusic;
        AudioManager.instance.Play();
    }

    public void OnClickSound()
    {
        AudioManager.instance.IsSound = !AudioManager.instance.IsSound;
        AudioManager.instance.Play();
    }



    public void OnClickLanguage(Toggle lang)
    {
        AudioManager.instance.Play();
        LanguageType curLang = LanguageType.中文;
        if (lang == tChinese)
        {
            curLang = LanguageType.中文;
        }
        if (lang == tEnglish)
        {
            curLang = LanguageType.英文;
        }
        ChangeLanguage(curLang);
    }

    public void ChangeLanguage(LanguageType lang)
    {
        LanguageChangeEvent evt = new LanguageChangeEvent(LanguageManager.instance.CurType, lang);
        GameEventDispatcher.dispatcherEvent(null, evt);
    }
}
