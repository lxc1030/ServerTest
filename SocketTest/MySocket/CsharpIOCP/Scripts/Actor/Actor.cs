using System;

public class Actor
{
    public Actor(IntPtr _handle, DateTime heartbeat)
    {
        this.handle = _handle;
        this.heartbeatTime = heartbeat;
        roomID = -1; // 預設值設為-1，代表不在任何房間中
        status = RoomActorState.Online;
    }
    //public Guid guid { get; set; }          // Peer 列表的guid
    public IntPtr handle { get; set; }
    public string userID { get; set; }    // 會員帳號
    public string nickname { get; set; }    // 會員暱稱
    public int sex { get; set; }          // 會員性別
    public DateTime loginTime { get; set; } // 登入時間
    public RoomActorState status { get; set; }       // 狀態 1:上線中 2:遊戲中
    public int roomID { get; set; }    // 在哪個房間，只有status是2時才有效
    public DateTime heartbeatTime { get; set; }// 最新一次心跳时间


}