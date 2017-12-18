using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivityManager : MonoBehaviour
{

    private static ActivityManager instance;
    public static ActivityManager Instance()
    {
        if (instance == null)
        {
            instance = new ActivityManager();
            //
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            assetManager = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            packageName = assetManager.Call<string>("getPackageName");
            androidCall = new AndroidJavaClass(packageName + ".MainActivity");
        }
        return instance;
    }

    #region java内购
    static string packageName = "";

    static AndroidJavaClass unityPlayer;
    static AndroidJavaObject currentActivity;
    static AndroidJavaClass androidCall;
    static AndroidJavaObject assetManager;
    #endregion



    private void Awake()
    {
        DontDestroyOnLoad(this);
        //if (Application.isMobilePlatform)
        //{
        //    unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        //    currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        //    assetManager = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        //    packageName = assetManager.Call<string>("getPackageName");
        //    androidCall = new AndroidJavaClass(packageName + ".MainActivity");
        //}
    }
    public AndroidJavaClass GetAndriod()
    {
        return androidCall;
    }

  

}
