using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameManager : MonoBehaviour
{
    private static FrameManager instance;

    public delegate void FrameEventHandler();
    public static event FrameEventHandler FrameEvent;


    public bool IsStartRecord;
    public float lastTime;
    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(this);
        instance = this;
        instance.Init();
    }

    public static void ListenDelegate(bool isListen, Action action)
    {
        if (isListen)
        {
            FrameEvent += new FrameEventHandler(action);
        }
        else
        {
            FrameEvent -= new FrameEventHandler(action);
        }
    }

    public void Init()
    {
        IsStartRecord = true;
        //FrameManager.ListenDelegate(true, () => { Debug.LogError("1"); });
        //FrameManager.ListenDelegate(true, () => { Debug.LogError("2"); });
    }




    /// <summary>
    /// Update->大概0.015s一帧，时间不固定
    /// FixedUpdate->保证在1秒内运行60帧，时间的间隔可能差距很大（0.5-0.0005-0.0005-....）
    /// </summary>
    void Update()
    {
        if (IsStartRecord)
        {
            float curTime = Time.realtimeSinceStartup;
            int count = 0;
            if (lastTime == 0)
            {
                lastTime = curTime;
                count = 1;
            }
            else
            {
                float length = curTime - lastTime;//当前帧的时间和上一帧的时间差距。
                count = (int)(length / DataController.FrameFixedTime);//差距除以帧间隔时间 就是计算多少帧
                lastTime += count * DataController.FrameFixedTime;
            }
            for (int i = 0; i < count; i++)
            {
                DoFrame();
            }
        }
    }

    private void DoFrame()
    {
        if (FrameEvent != null)
        {
            FrameEvent();
        }
    }

}
