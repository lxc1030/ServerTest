using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
public class LoadingScene : MonoBehaviour
{
    public static LoadingScene instance;
    public static string Name = "LoadingScene";
    public static Action callback;
    public Image progressImage;
    //public Slider progressBar;
    //public UISlider progressBar;
    public Transform particle;
    /// <summary>
    /// 在加载时显示的tip的lab
    /// </summary>
    //public UILabel labTip;
    public Text labTip;

    public List<LoadingTipCfg> cfg = new List<LoadingTipCfg>();

    /// <summary>
    /// 其他脚本传过来的加载进度
    /// </summary>
    public static float loadProgress = 0;
    /// <summary>
    /// tip显示的间隔时间
    /// </summary>
    public float tipShowDelayTime = 1f;
    /// <summary>
    /// 当前显示进度
    /// </summary>
    float progress;
    /// <summary>
    /// 当前进度条度数0.0-1.0
    /// </summary>
    float uptime;
    /// <summary>
    /// 是否加载成功
    /// </summary>
    bool isLoadFinish;
    /// <summary>
    /// 进度条自增的最大值
    /// </summary>
    private float selfAddProgress = 0.5f;
    /// <summary>
    /// 进度条自增的速度
    /// </summary>
    private float selfAddSpeed = 0.001f;

    public bool isShowHealthyGameTip;
    public GameObject objHealthy;
    public float timeHealthyStart;
    private int timeHealthyDelay = 3;

    private void Awake()
    {
        instance = this;
#if SanWang
        objHealthy.SetActive(true);
        isShowHealthyGameTip = true;
#elif GooglePlay
        objHealthy.SetActive(false);
        isShowHealthyGameTip = false;
#endif
    }

    void OnEnable()
    {
        FileUtil.loadConfig("LoadingTipCfg", ref cfg, null, false);
        isLoadFinish = false;
        TipShow();
    }
    void TipShow()
    {
        int random = UnityEngine.Random.Range(0, cfg.Count);
        string info = cfg[random].info;
        labTip.text = info;
    }

    void Update()
    {
        if (isShowHealthyGameTip)
        {
            if (timeHealthyStart == 0)
            {
                timeHealthyStart = Time.realtimeSinceStartup;
            }
            if (Time.realtimeSinceStartup > timeHealthyStart + timeHealthyDelay)
            {
                isShowHealthyGameTip = false;
            }
            return;
        }
        //选取uptime和loadProgress的最大数作为进度条度数
        progress = Mathf.Clamp01(Mathf.Max(uptime, loadProgress));
        //uptime自增
        if (uptime < selfAddProgress)
        {
            uptime += Time.deltaTime * selfAddSpeed;
        }
        //设置进度条度数
        SetProgress(progress);
    }

    void OnDisable()
    {
        progressImage.fillAmount = 1;
    }

    private void SetProgress(float p)
    {
        progressImage.fillAmount = p;
        if (particle != null)
        {
            if (particle.localPosition.x < 305.0f)
            {
                particle.localPosition = new Vector3(particle.localPosition.x + p * (305.0f - particle.localPosition.x), particle.localPosition.y, particle.localPosition.z);
            }
        }
        //加载完成
        if (p == 1 && !isLoadFinish && !isShowHealthyGameTip)
        {
            isLoadFinish = true;
            callback();
            Destroy(gameObject);
        }
    }

    public void LoadFinish()
    {
        Destroy(gameObject);
    }

}

public class LoadingTipCfg
{
    public int id;
    public string info;
    public string desc;
    public string notice;
}