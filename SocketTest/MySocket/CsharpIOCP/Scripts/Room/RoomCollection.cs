using NetFrame.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

public class RoomCollection
{
    protected Dictionary<int, SingleRoom> roomList;
    public Dictionary<int, SingleRoom> RoomList { get { return roomList; } }
    public RoomCollection()
    {
        roomList = new Dictionary<int, SingleRoom>() { { 0, null } };

        //// 建立聊天室的名字以及容許人數
        //roomList.Add(new SingleRoom("公開聊天室1", 20));
        //roomList.Add(new SingleRoom("公開聊天室2", 20));
        //roomList.Add(new SingleRoom("公開聊天室3", 20));
        //roomList.Add(new SingleRoom("公開聊天室4", 20));
        //roomList.Add(new SingleRoom("公開聊天室5", 20));
    }




    // 取得房間名稱及會員人數資訊
    public RoomInfo GetRoomInfo(int roomIndex)
    {
        if (!roomList.ContainsKey(roomIndex))
        {
            return null;
        }
        RoomInfo roomInfo = roomList[roomIndex].RoomInfo;
        return roomInfo;
    }

    public byte[] CreateNewRoom(JObject json, AsyncUserToken userToken)
    {
        byte[] backData = null;
        object userID = json[nameof(Register.userID)];
        GameModel roomType = (GameModel)(int)json[nameof(RoomInfo.RoomType)];
        string roomName = (string)json[nameof(RoomInfo.RoomName)];

        //主逻辑
        string userName = userToken.userInfo.Register.name;
        if (roomName == null)
        {
            roomName = userName + "的房间";
        }

        int roomID = roomList.Count;
        int limetCount = GameTypeManager.BackRoomLimet(roomType);
        Log4Debug("当前房间数:" + roomList.Values.Count);
        foreach (KeyValuePair<int, SingleRoom> item in roomList)
        {
            if (item.Key == 0)//0号房间不开放使用，因为玩家的默认房间为0(Protobuf的构造函数必须有才这样);
            {
                continue;
            }
            if (item.Value.IsEmptyRoom())
            {
                roomID = item.Key;
                break;
            }
        }
        if (!roomList.ContainsKey(roomID))
        {
            roomList.Add(roomID, null);
        }
        roomList[roomID] = new SingleRoom(roomID, (string)roomName, roomType, limetCount);
        int localIndex = -1;
        if (roomList[roomID].Join(userToken, out localIndex))
        {
            Log4Debug("创建房间成功,站位：" + localIndex);
            RoomInfo info = GetRoomInfo(roomID);
            backData = SerializeHelper.Serialize<RoomInfo>(info);
        }
        else
        {
            Log4Debug("创建房间失败，检查！！！");
        }
        return backData;
    }
    public byte[] JoinRoom(JObject json, AsyncUserToken userToken)
    {
        byte[] backData = null;
        object userID = json[nameof(Register.userID)];
        object roomID = json[nameof(RoomInfo.RoomID)];
        GameModel roomType = (GameModel)(int)json[nameof(RoomInfo.RoomType)];

        //主逻辑
        if (roomID == null)//加入指定房间
        {
            int rID = (int)roomID;
            int localIndex = -1;
            if (roomList.ContainsKey(rID))
            {
                if (roomList[rID].Join(userToken, out localIndex))//可以加入
                {
                    Log4Debug("加入房间成功。");
                    RoomInfo info = GetRoomInfo(rID);
                    info.MyLocateIndex = localIndex;
                    backData = SerializeHelper.Serialize<RoomInfo>(info);
                    userToken.userInfo = info.ActorList[localIndex];
                }
                else
                {
                    backData = ClassGroup.ErrorBackByType(ErrorType.roomMateFull);
                }
            }
            else
            {
                backData = ClassGroup.ErrorBackByType(ErrorType.roomNotExist);
            }
        }
        else//根据游戏类型遍历所有房间
        {
            foreach (KeyValuePair<int, SingleRoom> item in roomList)
            {
                if (item.Key == 0)
                {
                    continue;
                }
                if (item.Value.RoomInfo.RoomType != roomType)
                    continue;
                int localIndex = -1;
                if (item.Value.Join(userToken, out localIndex))//可以加入
                {
                    RoomInfo info = GetRoomInfo(item.Key);
                    info.MyLocateIndex = localIndex;
                    backData = SerializeHelper.Serialize<RoomInfo>(info);
                    userToken.userInfo = info.ActorList[localIndex];
                    return backData;

                }
            }
            //请求加入的游戏模式暂没有房间，生成新房间
            backData = CreateNewRoom(json, userToken);
        }
        return backData;
    }

    public byte[] UpdateRoom(JObject json)
    {
        byte[] backData = null;

        string userID = (string)json[nameof(Register.userID)];
        int roomID = (int)json[nameof(RoomInfo.RoomID)];
        GameModel roomType = (GameModel)(int)json[nameof(RoomInfo.RoomType)];
        string roomName = (string)json[nameof(RoomInfo.RoomName)];

        //房间存在
        if (roomList.ContainsKey(roomID))
        {
            if (roomList[roomID].IsMaster(userID))//发起者是房主
            {
                roomList[roomID].UpdateRoom(roomType, roomName);
                RoomInfo info = roomList[roomID].RoomInfo;
                backData = SerializeHelper.Serialize<RoomInfo>(info);
            }
        }
        else
        {
            backData = ClassGroup.ErrorBackByType(ErrorType.roomNotExist);
        }
        return backData;
    }



    public void Log4Debug(string msg)
    {
        LogManager.WriteLog(this.GetType().Name + ":" + msg);
    }
}