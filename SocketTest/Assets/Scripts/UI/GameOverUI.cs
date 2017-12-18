using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI instance;
    public static string Name = "GameOverUI";

    public TeamType winTeam;

    public Text txWin;


    private void Awake()
    {
        instance = this;
    }
    public void OnClose()
    {
        Close();
    }
    public void Close()
    {
        UIManager.instance.HidePanel(Name);
    }
    public static void Show(TeamType winTeam)
    {
        UIManager.instance.ShowPanel(Name);
        instance.winTeam = winTeam;
        instance.Init();
    }
    private void Init()
    {
        string winWord = winTeam.ToString() + " Win";
        txWin.text = winWord;
        switch (winTeam)
        {
            case TeamType.Blue:

                break;
            case TeamType.Red:

                break;
            case TeamType.Both:

                break;
        }

    }

    #region 按钮方法

    public void OnClickClose()
    {
        Close();
    }

    #endregion
}
