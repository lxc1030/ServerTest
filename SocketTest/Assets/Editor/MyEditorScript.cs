using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class MyEditorScript
{
    //得到工程中所有场景名称
    static string[] SCENES = FindEnabledEditorScenes();


    //一系列批量build的操作
    [MenuItem("Custom/Build Test")]
    static void PerformTest()
    {
        BulidTarget("SocketTest", "Test");
    }


    [MenuItem("Custom/Build Android QQ")]
    static void PerformAndroidQQBuild()
    {
        BulidTarget("QQ", "Android");
    }

    [MenuItem("Custom/Build Android UC")]
    static void PerformAndroidUCBuild()
    {
        BulidTarget("UC", "Android");
    }

    [MenuItem("Custom/Build Android CMCC")]
    static void PerformAndroidCMCCBuild()
    {
        BulidTarget("CMCC", "Android");
    }

    [MenuItem("Custom/Build Android ALL")]
    static void PerformAndroidALLBuild()
    {
        BulidTarget("QQ", "Android");
        BulidTarget("UC", "Android");
        BulidTarget("CMCC", "Android");
    }
    [MenuItem("Custom/Build iPhone QQ")]
    static void PerformiPhoneQQBuild()
    {
        BulidTarget("QQ", "IOS");
    }

    [MenuItem("Custom/Build iPhone QQ")]
    static void PerformiPhoneUCBuild()
    {
        BulidTarget("UC", "IOS");
    }

    [MenuItem("Custom/Build iPhone CMCC")]
    static void PerformiPhoneCMCCBuild()
    {
        BulidTarget("CMCC", "IOS");
    }

    [MenuItem("Custom/Build iPhone ALL")]
    static void PerformiPhoneALLBuild()
    {
        BulidTarget("QQ", "IOS");
        BulidTarget("UC", "IOS");
        BulidTarget("CMCC", "IOS");
    }

    //这里封装了一个简单的通用方法。
    static void BulidTarget(string name, string target)
    {
        string app_name = name;
        string target_dir = Application.dataPath + "/TargetAndroid";
        string target_name = app_name + ".apk";
        BuildTargetGroup targetGroup = BuildTargetGroup.Android;
        BuildTarget buildTarget = BuildTarget.Android;
        string applicationPath = Application.dataPath.Replace("/Assets", "");

        if (target == "Android")
        {
            target_dir = applicationPath + "/TargetAndroid";
            targetGroup = BuildTargetGroup.Android;
        }
        if (target == "IOS")
        {
            target_dir = applicationPath + "/TargetIOS";
            target_name = app_name;
            targetGroup = BuildTargetGroup.iOS;
            buildTarget = BuildTarget.iOS;
        }
        if (target == "Test")
        {
            target_dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            targetGroup = BuildTargetGroup.Android;
            buildTarget = BuildTarget.Android;
        }

        //每次build删除之前的残留
        if (Directory.Exists(target_dir))
        {
            if (File.Exists(target_name))
            {
                File.Delete(target_name);
            }
        }
        else
        {
            Directory.CreateDirectory(target_dir);
        }

        //==================这里是比较重要的东西=======================
        switch (name)
        {
            case "QQ":
                PlayerSettings.applicationIdentifier = "com.game.qq";
                PlayerSettings.bundleVersion = "v0.0.1";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "QQ");
                break;
            case "UC":
                PlayerSettings.applicationIdentifier = "com.game.uc";
                PlayerSettings.bundleVersion = "v0.0.1";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "UC");
                break;
            case "CMCC":
                PlayerSettings.applicationIdentifier = "com.game.cmcc";
                PlayerSettings.bundleVersion = "v0.0.1";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "CMCC");
                break;
            case "SocketTest":
                PlayerSettings.applicationIdentifier = "com.xc.Test";
                string version = PlayerSettings.bundleVersion;
                int ver = int.Parse(version.Split(new string[] { "v0.0." }, StringSplitOptions.RemoveEmptyEntries)[0]);
                ver++;
                version = "v0.0." + ver;
                BundleVersionChecker.CreateNewBuildVersionClassFile(version);
                PlayerSettings.bundleVersion = version;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "SanWang");
                break;
        }

        //==================这里是比较重要的东西=======================

        //开始Build场景，等待吧～
        GenericBuild(SCENES, target_dir + "/" + target_name, targetGroup, buildTarget, BuildOptions.None);

    }

    private static string[] FindEnabledEditorScenes()
    {
        List<string> EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            EditorScenes.Add(scene.path);
        }
        return EditorScenes.ToArray();
    }

    static void GenericBuild(string[] scenes, string target_dir, BuildTargetGroup build_group, BuildTarget build_target, BuildOptions build_options)
    {
        try
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(build_group, build_target);
            string res = BuildPipeline.BuildPlayer(scenes, target_dir, build_target, build_options);
            if (res.Length > 0)
            {
                throw new Exception("BuildPlayer failure: " + res);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

}