using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ADManager : MonoBehaviour
{
    public static ADManager instance;

    private Action adCallBackSuccess;
    private Action adCallBackFail;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }


    public bool IsADEnableToPlay(int type)
    {
        bool isEnable = false;
        if (Application.isMobilePlatform)
        {
            if (ActivityManager.Instance().GetAndriod() != null)
            {
                isEnable = ActivityManager.Instance().GetAndriod().CallStatic<bool>("IsADEnableToPlay", type);
            }
        }
        else if (Application.isEditor)
        {
            isEnable = true;
        }
        return isEnable;
    }

    public void ShowAdByType(ADType type, Action callbackSuccess, Action callbackFail = null)
    {
        if (!PaymentManager.instance.isOpenAD)
        {
            return;
        }
        adCallBackSuccess = callbackSuccess;
        adCallBackFail = callbackFail;
        if (Application.isMobilePlatform)//手机
        {
            if (IsADEnableToPlay((int)type))//广告可以播放
            {
                ActivityManager.Instance().GetAndriod().CallStatic("PlayADByType", (int)type);
            }
            else
            {
                //string tip = LanguageManager.instance.CurType == LanguageType.中文 ? "广告暂不可以播放！" : "This ad is not available!";
                //UIManager.instance.ShowAlertTip(tip);
                if (adCallBackFail != null)
                {
                    adCallBackFail();
                }
            }
        }
        else//当前的编辑器
        {
            if (adCallBackSuccess != null)
            {
                adCallBackSuccess();
            }
        }
    }

    /// <summary>
    /// 奖励性广告
    /// </summary>
    /// <param name="adType"></param>
    public void OnAdsVideoClosed(string finish)
    {
        bool isFinish = (finish == "0") ? false : true;
        if (isFinish)
        {
            adCallBackSuccess();
        }
        else
        {
            adCallBackFail();
        }
    }
    public void OnAdsClosed(string adType)
    {
        adCallBackSuccess();
    }

    /// <summary>
    /// 关闭banner条
    /// </summary>
    public void SwitchRECOMMANDBanner()
    {
        if (Application.isEditor || !PaymentManager.instance.isOpenAD)
        {
            return;
        }
        ActivityManager.Instance().GetAndriod().CallStatic("SwitchRECOMMANDBanner");
    }



    void OnGUI()
    {
        //GUI.skin.textArea.fontSize = 25;
        //GUI.skin.button.fontSize = 25;


        //// add
        //if (GUI.Button(new Rect(0, 150, 250, 100), "add"))
        //{
        //    int sum = androidCall.CallStatic<int>("add", 1, 2);
        //    print("sum is " + sum);
        //}

        //// showMessage
        //if (GUI.Button(new Rect(0, 250, 250, 100), "showMessage"))
        //{
        //    androidCall.CallStatic("showMessage", "显示这一段的文字");
        //}

        //// showAlertView
        //if (GUI.Button(new Rect(0, 350, 250, 100), "showAlertView"))
        //{
        //    androidCall.CallStatic("showAlertView");
        //}

        //if (GUI.Button(new Rect(0, 150, 250, 100), "init"))
        //{
        //    unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        //    currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        //    androidCall = new AndroidJavaClass("com.xc.dzk.ADManager");
        //    androidCall.CallStatic("init", currentActivity);
        //}

        //stringToEdit = GUI.TextField(new Rect(0, 250, 250, 100), stringToEdit);

        //if (GUI.Button(new Rect(0, 350, 250, 100), "Play"))
        //{
        //    CallAdByType((ADType)int.Parse((stringToEdit)));
        //}
        //if (GUI.Button(new Rect(0, 450, 250, 100), "Special"))
        //{
        //    UIManager.instance.ShowAlertTip("Special");
        //    androidCall.CallStatic("PlaySpecialAD");
        //}

    }
}

/// <summary>
/// SDK广告的类型
/// </summary>
public enum ADType
{
    无 = 0,
    /// <summary>
    /// 添加一个AdMob的Banner条。若AdMob的Banner条不可用，该方法则无效。
    /// AddAdMobBanner(int position)
    /// </summary>
    ADS_TYPE_ADMOB_BANNER = 1,
    /// <summary>
    /// 显示一个AdMob的插页广告
    /// ShowAdMobInterstitialAd()
    /// </summary>
    ADS_TYPE_ADMOB_INTERSTITIAL = 2,
    /// <summary>
    /// 
    /// </summary>
    ADS_TYPE_ADMOB_REWARD_VIDEO = 3,
    /// <summary>
    /// 显示一个UnityAds的视频广告，该视频广告可以跳过。
    /// ShowUnityAdsVideoAd()
    /// </summary>
    ADS_TYPE_UNITY_VIDEO = 4,
    /// <summary>
    /// 显示一个UnityAds的奖励性视频广告，该视频广告不可以跳过，必须看完。
    /// ShowUnityAdsRewardVideoAd()
    /// </summary>
    ADS_TYPE_UNITY_REWARD_VIDEO = 5,
    /// <summary>
    /// 添加一个自推荐Banner条广告
    /// AddRecommandSelfBanner(int position)
    /// </summary>
    ADS_TYPE_RECOMMAND_BANNER = 6,
    /// <summary>
    /// 显示一个自推荐的插页式广告。
    /// ResetRecommandSelfBannerPosition(int position)
    /// </summary>
    ADS_TYPE_RECOMMAND_INTERSTITIAL = 7
}