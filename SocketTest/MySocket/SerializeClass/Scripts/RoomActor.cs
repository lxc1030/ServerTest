using System;
using System.Threading;

[ProtoBuf.ProtoContract]
public class RoomActor// 因為要多記錄加入房間時間的屬性，因此用繼承的方式建立房間用的會員類別
{
    [ProtoBuf.ProtoMember(1)]
    public int UniqueID { get; set; }      //人物在房间中的座位编号
    [ProtoBuf.ProtoMember(2)]
    public int RoomID { get; set; }        //在哪個房間，只有status是2時才有效
    [ProtoBuf.ProtoMember(3)]
    public Register Register { get; set; }          // 會員性別
    [ProtoBuf.ProtoMember(4)]
    public RoomActorState CurState { get; set; } // 人物在房间中的当前游戏状态
    [ProtoBuf.ProtoMember(5)]
    public TeamType MyTeam { get; set; } //我的队伍标识
    [ProtoBuf.ProtoMember(6)]
    public int KillCount { get; set; }   //杀敌数

    public RoomActor()
    {

    }

    public RoomActor(DateTime heartbeat)
    {
        this.heartbeatTime = heartbeat;
        Register = null;
        RoomID = -1; // 預設值設為-1，代表不在任何房間中
        CurState = RoomActorState.Online;
    }
    public RoomActor(int roomID, int uniqueID, Register register, TeamType myTeam)
    {
        Register = register;
        RoomID = roomID;
        UniqueID = uniqueID;                    //记录加入此房间的站位
        MyTeam = myTeam;

        JoinTime = System.DateTime.Now;    //记录加入此房间的时间
        MyModelInfo = new GameModelData()
        {
            userIndex = uniqueID
        };
        timerDead = null;
        this.InitActor();
    }

    /// <summary>
    /// 游戏结束设置用户部分数据初始化
    /// </summary>
    public void InitActor()
    {
        KillCount = 0;
        CurState = RoomActorState.NoReady;
    }
    /// <summary>
    /// 登录的时候保存用户信息
    /// </summary>
    /// <param name="login"></param>
    public void LoginSet(Register login)
    {
        Register = login;
        Register.password = "";//清除密码，防止将密码发送给客户端
    }


    #region 静态值

    /// <summary>
    /// 人物半径
    /// </summary>
    public static float ModelRadius = 0.5f;
    /// <summary>
    /// 相对射击点的射击半径
    /// </summary>
    public static float ShootRadius = 4f;

    #endregion
    public DateTime heartbeatTime { get; set; }// 最新一次心跳时间
    public DateTime JoinTime { get; set; }

    #region 计时器变量
    /// <summary>
    /// 死亡复活时间（毫秒）
    /// </summary>
    public static int DeadLastTime = 3000;
    public Timer timerDead { get; set; }
    /// <summary>
    /// 无敌时间（毫秒）
    /// </summary>
    public static int InvincibleLastTime = 1000;
    public Timer timerInvincible { get; set; }
    #endregion
    public GameModelData MyModelInfo { get; set; }//模型的相关属性

}
