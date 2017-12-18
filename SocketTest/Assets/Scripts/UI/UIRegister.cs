using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRegister : MonoBehaviour
{
    private static UIRegister instance;
    public static string Name = "UIRegister";

    public InputField txUserID;
    public InputField txPassword;
    public InputField txPswCheck;
    public bool isRegister;

    private void Awake()
    {
        instance = this;
    }

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }

    public void OnClose()
    {
        Close();
    }
    private void Close()
    {
        UIManager.instance.HidePanel(Name);
    }
    public void Init()
    {

    }


    public void OnClickBackLogin()
    {
        Close();
        UILogin.Show();
    }



    public void OnClickRegister()
    {
        string error = "";
        if (string.IsNullOrEmpty(txUserID.text))
        {
            error = "账号未填写";
        }
        else if (string.IsNullOrEmpty(txPassword.text) || string.IsNullOrEmpty(txPswCheck.text))
        {
            error = "密码未填写";
        }
        else if (txPassword.text != txPswCheck.text)
        {
            error = "两次密码不一致";
        }
        if (!string.IsNullOrEmpty(error))//有错误
        {
            UIManager.instance.ShowAlertTip(error);
            return;
        }


        string[] column = new string[] { nameof(Comm), nameof(Register.userID), nameof(Register.password) };
        string[] value = new string[] { "" + (int)Comm.register, "" + txUserID.text, "" + txPassword.text };
        string sendData = ClassGroup.StringToHttp(column, value);

        HttpClient.instance.SendComm(sendData, GetServerBack);
    }

    public void GetServerBack(string receive)
    {
        JObject jons = JObject.Parse(receive);
        string comm = (string)jons[nameof(Comm)];
        if (comm != null && comm == (int)Comm.error + "")
        {
            ErrorType error = (ErrorType)(int)jons[nameof(ErrorType)];
            string info = "";
            switch (error)
            {
                case ErrorType.userIDShort:
                    info = "用户名长度小于6位或大于10位";
                    break;
                case ErrorType.passwordShort:
                    info = "密码长度小于6位或大于10位";
                    break;
                case ErrorType.format:
                    info = "格式错误";
                    break;
                case ErrorType.userIDExist:
                    info = "此账号已存在";
                    break;
            }
            UIManager.instance.ShowAlertTip(info);
        }
        else
        {
            UIManager.instance.ShowAlertTip("注册成功！");
            UILogin.Show();
            Close();
        }

    }


    public static void GetRegister(string msg)
    {
        Debug.LogError(msg);
        instance.isRegister = true;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (isRegister)
        //{
        //    isRegister = false;
        //    UIManager.instance.HidePanel(UIRegister.Name);
        //    UIManager.instance.ShowPanel(UILogin.Name);
        //}
    }
}

