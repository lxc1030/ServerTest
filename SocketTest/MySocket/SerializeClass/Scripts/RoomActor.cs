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
    public string Nickname { get; set; }    // 會員暱稱
    [ProtoBuf.ProtoMember(4)]
    public short Sex { get; set; }          // 會員性別
    [ProtoBuf.ProtoMember(5)]
    public RoomActorState CurState { get; set; } // 人物在房间中的当前游戏状态
    [ProtoBuf.ProtoMember(6)]
    public TeamType MyTeam { get; set; } //我的队伍标识
    [ProtoBuf.ProtoMember(7)]
    public int KillCount { get; set; }   //杀敌数

    public RoomActor()
    {
        
    }

    public RoomActor(IntPtr _handle, DateTime heartbeat)
    {
        this.handle = _handle;
        this.heartbeatTime = heartbeat;
        RoomID = -1; // 預設值設為-1，代表不在任何房間中
        CurState = RoomActorState.Online;
    }
    public RoomActor(int roomID, int uniqueID, string memberID, string nickName, short sex, TeamType myTeam)
    {
        RoomID = roomID;
        UniqueID = uniqueID;                    //记录加入此房间的站位
        MemberID = memberID;
        Nickname = nickName;
        Sex = sex;
        MyTeam = myTeam;

        JoinTime = System.DateTime.Now;    //记录加入此房间的时间
        MyModelInfo = new GameModelData()
        {
            userIndex = uniqueID
        };
        deadTimer = null;
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

    #region 静态值

    /// <summary>
    /// 人物半径
    /// </summary>
    public static float ModelRadius = 0.5f;
    /// <summary>
    /// 相对射击点的射击半径
    /// </summary>
    public static float ShootRadius = 4f;
    /// <summary>
    /// 死亡复活时间（毫秒）
    /// </summary>
    public static int DeadToReviveTime = 3000;
    #endregion
    public IntPtr handle { get; set; }
    public DateTime heartbeatTime { get; set; }// 最新一次心跳时间
    public DateTime JoinTime { get; set; }
    public string MemberID { get; set; }    // 會員帳號
    public Timer deadTimer { get; set; }
    public GameModelData MyModelInfo { get; set; }//模型的相关属性

}
