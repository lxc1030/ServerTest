using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public List<AudioInfo> freeSource = new List<AudioInfo>();

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }
    public void OnEnable()
    {
        PlayMusic(clipMusic);
    }
    public bool IsMusic
    {
        get { return PlayerPref.GetInt("Is_Music", 1) == 1; }
        set
        {
            PlayerPref.SetInt("Is_Music", value ? 1 : 0);
            BGMusic.mute = !value;
            PlayMusic(clipMusic);
        }
    }

    public bool IsSound
    {
        get { return PlayerPref.GetInt("Is_Sound", 1) == 1; }
        set { PlayerPref.SetInt("Is_Sound", value ? 1 : 0); }
    }




    /// <summary>
    /// Music的播放器，专门用来播放背景音乐的
    /// </summary>
    public AudioSource BGMusic;
    /// <summary>
    /// 背景音乐
    /// </summary>
    public AudioClip clipMusic;

    /// <summary>
    /// 按钮点击音效
    /// </summary>
    public AudioClip clipClick;

    public AudioClip clipFill;
    public AudioClip clipLight;

    public AudioClip clipStone_Hit;
    public AudioClip clipStone_Dead;

    public AudioClip clipEffect;
    public AudioClip clipPowerSkill;
    public AudioClip clipCountDown;
    public AudioClip clipLock;
    public AudioClip clipPop;

    public void PlayMusic(AudioClip clip, bool isLoop = true)
    {
        if (!IsMusic)
        {
            return;
        }
        BGMusic.clip = clip;
        BGMusic.loop = isLoop;
        if (clip == null)
        {
            Debug.Log("E播放的音效为空：" + clip);
        }
        BGMusic.Play();
    }

    /// <summary>
    /// 只播Sound类
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="isLoop"></param>
    public void Play(AudioClip clip = null, bool isLoop = false)
    {
        if (!IsSound)
        {
            return;
        }
        if (clip == null)
        {
            clip = clipClick;
        }
        AudioInfo info = GetFreeInfo();
        info.source.clip = clip;
        info.source.loop = isLoop;
        if (clip == null)
        {
            Debug.Log("E播放的音效为空：" + clip);
        }
        info.source.Play();
    }

    public AudioInfo GetFreeInfo()
    {
        for (int i = 0; i < freeSource.Count; i++)
        {
            if (!freeSource[i].IsPlay)
            {
                return freeSource[i];
            }
        }
        AudioInfo info = new AudioInfo() { source = gameObject.AddComponent<AudioSource>() };
        freeSource.Add(info);
        return info;
    }


}


[System.Serializable]
public class AudioInfo
{
    public AudioSource source;
    public bool IsPlay
    {
        get { return source.isPlaying; }
    }

}