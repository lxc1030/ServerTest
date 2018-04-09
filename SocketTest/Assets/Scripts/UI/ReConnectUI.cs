using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReConnectUI : MonoBehaviour
{
    public static ReConnectUI instance;
    public static string Name = "ReConnectUI";

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }

    void Init ()
    {
		
	}
	
	// Update is called once per frame
	void Awake ()
    {
        instance = this;
	}



    public static void Close()
    {
        UIManager.instance.HidePanel(Name);
    }




    public void OnClickSure()
    {
        GameLoadingUI.Show();
        Close();
    }

    public void OnClickCancel()
    {
        Close();
    }
}
