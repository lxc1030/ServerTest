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
    public int MyLocateIndex { get; set; }//这个值的是在数组中的序号，发送给用户，用户所在的位置

    public RoomInfo()
    {

    }
    public RoomInfo(int roomID, string roomName, GameModel roomType, int limit)
    {
        ActorList = new Dictionary<int, RoomActor>() { };
        for (int i = 0; i < limit; i++)
        {
            InitRoomActorByIndex(i);
        }

        RoomID = roomID;
        RoomName = roomName;
        RoomType = roomType;
        Limit = limit;
        switch (RoomType)
        {
            case GameModel.组队模式:
                GameTime = 2 * 60 * 1000;//分钟
                //GameTime = 1 * 30 * 1000;
                break;
        }
        //
        CurState = RoomActorState.NoReady;
    }

    /// <summary>
    /// 房間中的會員列表
    /// </summary>
    public Dictionary<int, RoomActor> ActorList { get; set; }

    public int FrameIndex { get; set; }
    /// <summary>
    /// 用户操作数据存储后，间隔该帧返回
    /// </summary>
    public static int frameInterval = 2;
    /// <summary>
    /// 客户端运行和接收数据相差该帧就快进
    /// </summary>
    public static int frameDiffer = 15;


    public void InitRoomActorByIndex(int unique)
    {
        if (!ActorList.ContainsKey(unique))
        {
            ActorList.Add(unique, null);
        }
        ActorList[unique] = new RoomActor(RoomID, unique, null, TeamType.Both);
    }










}