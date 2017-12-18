using UnityEngine;
using Umeng;
using System.Runtime.InteropServices;


public class UmengCase : MonoBehaviour
{
    private static string appKey = "591187464ad1566ed3000241";
    private static string storeName
    {
#if SanWang
        get { return "SanWang"; }
#elif GooglePlay
            get { return "GooglePlay"; }
#endif
    }
    // Use this for initialization
    void Start()
    {


        //JSONNode N = new Json


        //N[1] = "Hello world";
        //N[1] = "string";



        //Debug.Log("JSON: " + N.ToString());




        //设置Umeng Appkey
        GA.StartWithAppKeyAndChannelId(appKey, storeName);





        //调试时开启日志
        GA.SetLogEnabled(true);

        GA.SetLogEncryptEnabled(true);






        //GA.ProfileSignIn("fkdafjadklfjdklf");

        //GA.ProfileSignIn("jfkdajfdakfj", "app strore");

        //print("GA.ProfileSignOff();");

        //GA.ProfileSignOff();

    }



    //void OnGUI()
    //{
    //    if (GUI.Button(new Rect(150, 100, 500, 100), "Event"))
    //    {
    //        GA.SetUserLevel(2);
    //        string[] arrayC21 = new string[3];
    //        arrayC21[0] = "one";
    //        arrayC21[1] = "1234567890123456000";
    //        arrayC21[2] = "one";
    //        GA.Event(arrayC21, 2, "label");
    //        //GA.GetDeviceInfo ();
    //    }
    //}
}


