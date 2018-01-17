using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

public class LogManager : MonoBehaviour
{
    public List<string> mWriteTxt = new List<string>();
    public string path;
    public string fileName;

    void Start()
    {
        DontDestroyOnLoad(this);
        //Application.persistentDataPath Unity中只有这个路径是既可以读也可以写的。
        //path = Application.persistentDataPath + "/outLog.txt";

        Check(out path, out fileName);

        //每次启动客户端删除之前保存的Log
        //if (System.IO.File.Exists(outpath))
        //{
        //    File.Delete(outpath);
        //}
        //在这里做一个Log的监听
        //Application.logMessageReceived += HandleLog;

        if (!Application.isEditor)
        {
            Application.logMessageReceivedThreaded += HandleLog;
        }
    }
    void Update()
    {
        //因为写入文件的操作必须在主线程中完成，所以在Update中哦给你写入文件。
        if (mWriteTxt.Count > 0)
        {
            string[] temp = mWriteTxt.ToArray();
            foreach (string t in temp)
            {
                using (StreamWriter writer = new StreamWriter(path + "/" + fileName, true, Encoding.UTF8))
                {
                    writer.WriteLine(t);
                    writer.Close();
                }
            }
            lock (mWriteTxt)
            {
                mWriteTxt.RemoveRange(0, temp.Length);
            }
        }
    }
    private void OnDestroy()
    {
        if (!Application.isEditor)
        {
            Application.logMessageReceivedThreaded -= HandleLog;
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            string all = "\n";
            all += DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + ":\n" + Log(logString);
            all += "\n" + new System.Diagnostics.StackTrace().ToString();
            lock (mWriteTxt)
            {
                mWriteTxt.Add(all);
            }
        }
    }

    //这里我把错误的信息保存起来，用来输出在手机屏幕上
    public string Log(params object[] objs)
    {
        string text = "";
        for (int i = 0; i < objs.Length; ++i)
        {
            if (i == 0)
            {
                text += objs[i].ToString();
            }
            else
            {
                text += ", " + objs[i].ToString();
            }
        }
        return text;
    }

    //string guiInfo = "";
    //void OnGUI()
    //{
    //    //guiInfo = path + "/" + name;
    //    GUIStyle bb = new GUIStyle();
    //    bb.normal.background = null;    //这是设置背景填充的
    //    bb.normal.textColor = Color.blue;   //设置字体颜色的
    //    bb.fontSize = 40;       //当然，这是字体大小
    //    GUI.Label(new Rect(0, 0, 200, 600), guiInfo, bb);
    //}


    void Check(out string path, out string fileName)
    {
        path = Application.persistentDataPath + "/Log";
        fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

        if (!Directory.Exists(path))//生成文件夹
        {
            Directory.CreateDirectory(path);
        }

        DirectoryInfo TheFolder = new DirectoryInfo(path);
        FileInfo[] child = TheFolder.GetFiles();
        for (int i = child.Length - 1; i >= 0; i--)
        {
            if (child[i].Name != fileName)
            {
                File.Delete(child[i].FullName);
            }
        }

        string fp = path + "/" + fileName;
        if (!File.Exists(fp))
        {
            File.Create(path + "\\" + fileName);
        }
    }
}








