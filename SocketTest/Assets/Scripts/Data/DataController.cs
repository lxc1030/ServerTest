#undef SanWang
#undef GooglePlay

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Umeng;

/// <summary>
/// 全局数据保存
/// </summary>
public class DataController : MonoBehaviour
{
    public static DataController instance;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

    // Use this for initialization
    void Start()
    {
        ServerTime = DateTime.Now;
        //FPS
        Application.targetFrameRate = 60;
    }

    public string UserID
    {
        get
        {
            return PlayerPref.GetString("My_Current_UserID");
        }
        set
        {
            PlayerPref.SetString("My_Current_UserID", value);
        }
    }
    public string Password
    {
        get
        {
            return PlayerPref.GetString("My_Current_Password");
        }
        set
        {
            PlayerPref.SetString("My_Current_Password", value);
        }
    }

    public RoomActor myInfo = new RoomActor();

    public Dictionary<int, RoomActor> ActorList;

    private RoomInfo myRoomInfo = null;

    public RoomInfo MyRoomInfo
    {
        get
        {
            return myRoomInfo;
        }
        set
        {
            myRoomInfo = value;
        }
    }
    public int MyLocateIndex = -1;

    /// <summary>
    /// 一帧转换成时间
    /// </summary>j
    public static float FrameFixedTime
    {
        get { return (float)RoomInfo.frameTime / 1000; }
    }



    public const int bulletAutoDestory = 100;

    //UI
    public const string iconPathSkill = "Image/";

    //prefab
    public const string prefPath_Character = "prefab/Character";
    public const string prefPath_Member = "prefab/Member";
    public const string prefabPath_Bullet = "prefab/PrefabBullet";
    public const string prefabPath_Ground = "prefab/Grounds/";
    public const string prefabPath_PeopleInfo = "prefab/PeopleInfo";
    public const string prefabPath_Buff = "prefab/PrefabBuff";

    //matrial
    public const string materialPathBox = "prefab/Material/Box/";
    public const string materialPathKillCount = "prefab/Material/KillCount/";



    //方块图案图片名称前缀
    public const string fillColorName = "Color_";


    /// <summary>
    /// 结算增加金币数（没有计算公式）
    /// </summary>
    public const int finishAddCoin = 1;
    /// <summary>
    /// 复活需要的金币数
    /// </summary>
    public const int resurrectionCostCoin = 20;
    /// <summary>
    /// 看广告奖励的金币
    /// </summary>
    public const int adAddCoin = 5;



    private static string XMLName = "EffectConfig";
    //public Dictionary<StoneEffectType, EffectConfig> config = new Dictionary<StoneEffectType, EffectConfig>();

    private int maxScore = 0;
    public int maxScoreEver
    {
        get
        {
            if (maxScore == 0)
            {
                maxScore = PlayerPref.GetInt("MaxScoreEver", 0);
            }
            return maxScore;
        }
        set
        {
            PlayerPref.SetInt("MaxScoreEver", value);
            maxScore = value;
#if SanWang

#elif GooglePlay
            //更新排行榜
            PaymentManager.instance.ReflashRankData(score);
#endif
        }
    }
    public bool IsFristEnterGame
    {
        get { return PlayerPref.GetInt("IsFristEnterGame", 1) == 1 ? true : false; }
        set { PlayerPref.SetInt("IsFristEnterGame", value ? 1 : 0); }
    }


    private float CurPassedTime;
    private float serverMarkTime;
    private DateTime serverTime;

    public DateTime ServerTime
    {
        get
        {
            return serverTime.AddSeconds(CurPassedTime - serverMarkTime);
        }
        set
        {
            serverMarkTime = CurPassedTime;
            serverTime = value;
        }
    }
    


    /// <summary>
    /// 结算加金币
    /// </summary>
    /// <param name="num"></param>
    public void SetCoinAdd(int num)
    {
        ItemManager.instance.AddItem(ItemID.Coin, num);
    }

    public void DealPay(ItemID id, int num, Action callback, Action backFail = null, bool isCalculate = true)
    {
        Item item = ItemManager.instance.GetItem(id);
        if (item.Count < num)
        {
            if (LanguageManager.instance.CurType == LanguageType.中文)
            {
                UIManager.instance.ShowAlertTip(item.name + "不足。");
            }
            else
            {
                UIManager.instance.ShowAlertTip("Not enough to resurrect.");
            }
            //ShopUI.Show();
            //return;
            if (backFail != null)
            {
                backFail();
            }
        }
        else
        {
            if (isCalculate)
            {
                ItemManager.instance.AddItem(id, -num);
            }
            callback();
        }
    }
    #region 材质

    private Dictionary<TeamType, Material> teamMaterials = new Dictionary<TeamType, Material>();
    /// <summary>
    /// 替换材质
    /// </summary>
    /// <param name="hp"></param>
    /// <returns></returns>
    public Material GetMaterialOfTeamType(TeamType type)
    {
        if (teamMaterials.ContainsKey(type))
        {
            if (teamMaterials[type] != null)
            {
                return teamMaterials[type];
            }
        }
        else
        {
            teamMaterials.Add(type, null);
        }
        teamMaterials[type] = (Material)Resources.Load(DataController.materialPathBox + type);
        return teamMaterials[type];
    }

    private Dictionary<int, Material> killMaterials = new Dictionary<int, Material>();

    public Material GetMaterialOfKillCount(int killCount)
    {
        if (killMaterials.ContainsKey(killCount))
        {
            if (killMaterials[killCount] != null)
            {
                return killMaterials[killCount];
            }
        }
        else
        {
            killMaterials.Add(killCount, null);
        }
        killMaterials[killCount] = (Material)Resources.Load(DataController.materialPathKillCount + killCount);
        return killMaterials[killCount];
    }

    #endregion

    public const float Gravity = 10f;
    public const float JumpSpeed = -6f;
    public const float MyActorMoveSpeed = 5;//人物移动速度
    public static float BackBulletSpeed(int index)
    {
        float speed = 0;
        if (index == 0)
        {
            speed = 50;
        }
        return speed;
    }
    public static float BackShootSpan(int index)
    {
        float span = 0;
        if (index == 0)
        {
            span = 0.4f;
        }
        return span;
    }


    /// <summary>
    /// 多位浮点转换成网络传输的少量浮点
    /// </summary>
    /// <param name="org"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static NetVector3 BackNetLimetByType(Vector3 org, NetLimetType type = NetLimetType.保留2位)
    {
        NetVector3 back = new NetVector3(0, 0, 0);
        float x = BackNetLimetByType(org.x, type);
        float y = BackNetLimetByType(org.y, type);
        float z = BackNetLimetByType(org.z, type);
        back = new NetVector3(x, y, z);
        return back;
    }
    public static float BackNetLimetByType(float f, NetLimetType type = NetLimetType.保留2位)
    {
        float temp = 0;
        int index = (int)type;
        temp = (float)Math.Round(f, index);
        return temp;
    }


    public void Update()
    {
        CurPassedTime = Time.realtimeSinceStartup;
    }
}



public enum NetLimetType
{
    保留0位,
    保留1位,
    保留2位,
    保留3位,
}







/// <summary>
/// 对应的item.xml
/// 是ItemID的延伸
/// </summary>
public enum ItemId
{
    体力 = 4,
}
/// <summary>
/// 需要在Inspector上设置
/// </summary>
public enum Tag
{
    Box,
    Bullet,
    Member,
    Buff,
    Ground,
}