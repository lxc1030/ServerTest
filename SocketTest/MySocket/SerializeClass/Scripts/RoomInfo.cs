using System;
using System.Collections.Generic;
using UnityEngine;

[ProtoBuf.ProtoContract]
public class RoomInfo
{
    [ProtoBuf.ProtoMember(1)]
    public int RoomID { get; set; }// 房间编号
    [ProtoBuf.ProtoMember(2)]
    public GameModel RoomType { get; set; }// 房间类型
    [ProtoBuf.ProtoMember(3)]
    public string RoomName { get; set; }// 房間名稱
    [ProtoBuf.ProtoMember(4)]
    public int Limit { get; set; }// 人數上限
    [ProtoBuf.ProtoMember(5)]
    public float GameTime { get; set; }// 游戏分钟,值是毫秒
    [ProtoBuf.ProtoMember(6)]
    public RoomActorState CurState { get; set; }// 房间当前状态
    [ProtoBuf.ProtoMember(7)]
    public int FrameDelay { get; set; }

    public RoomInfo()
    {

    }
    public RoomInfo(int roomID, string roomName, GameModel roomType, int limit)
    {

        RoomID = roomID;
        RoomName = roomName;
        RoomType = roomType;
        Limit = limit;
        switch (RoomType)
        {
            case GameModel.组队模式:
                frameInterval = 8;
                FrameDelay = 10;
                GameTime = 2 * 60 * 1000;//分钟
                //GameTime = 1 * 30 * 1000;
                break;
        }
        //
        CurState = RoomActorState.NoReady;
    }



    public int FrameIndex { get; set; }
    /// <summary>
    /// 用户操作数据存储后，间隔该帧返回
    /// </summary>
    public int frameInterval;
    /// <summary>
    /// 一帧对应的时间
    /// </summary>
    public static int frameTime = 15;//是毫秒，检测间隔0.015s











}