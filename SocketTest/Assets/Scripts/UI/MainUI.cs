using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainUI : MonoBehaviour
{
    public static MainUI instance;
    public static string Name = "MainUI";
    private static Action callback;

    private void Awake()
    {
        instance = this;
    }

    public void OnEnable()
    {
        ADManager.instance.ShowAdByType(ADType.ADS_TYPE_RECOMMAND_BANNER, null);
    }

    public static void Show(Action _callback = null)
    {
        UIManager.instance.ShowPanel(Name);
        callback = _callback;
    }

    public void OnClose()
    {
        Close();
    }
    private void Close()
    {
        UIManager.instance.HidePanel(Name);
    }


    public void OnClickSetting()
    {
        AudioManager.instance.Play();
        Setting.Show();
    }

    public string _userID = "1";
    public string _password = "1";

    public void OnClickLogin()
    {
        UILogin.Show();
        Close();
    }


}
