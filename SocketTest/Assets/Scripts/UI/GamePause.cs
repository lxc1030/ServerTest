using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GamePause : MonoBehaviour
{
    public static GamePause instance;
    public static string Name = "GamePause";

    private void Awake()
    {
        instance = this;
        //ADManager.instance.ShowAdByType(ADType.ADS_TYPE_RECOMMAND_BANNER, null);
        //ADManager.instance.SwitchRECOMMANDBanner();
    }

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }

    public void Init()
    {
        //
    }

    public void OnEnable()
    {
        //ADManager.instance.SwitchRECOMMANDBanner();

    }

    public void OnClose()
    {

    }
    private void Close()
    {
        //ADManager.instance.SwitchRECOMMANDBanner();
        UIManager.instance.HidePanel(Name);
    }

    public void OnClickContinue()
    {
        AudioManager.instance.Play();
        Close();
    }
    public void OnClickNew()
    {
        AudioManager.instance.Play();
        Close();
        //新游戏
    }



}
