using Network_Kcp;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class RoomCollection
{
    public ConcurrentDictionary<int, SingleRoom> RoomList { get; set; }
    public RoomCollection()
    {
        RoomList = new ConcurrentDictionary<int, SingleRoom>();
        SingleRoom singleRoom = new SingleRoom(0, "", GameModel.组队模式);
        RoomList.AddOrUpdate(0, singleRoom, (key, oldValue) => singleRoom);
        //// 建立聊天室的名字以及容許人數
        //roomList.Add(new SingleRoom("公開聊天室1", 20));
        //roomList.Add(new SingleRoom("公開聊天室2", 20));
        //roomList.Add(new SingleRoom("公開聊天室3", 20));
        //roomList.Add(new SingleRoom("公開聊天室4", 20));
        //roomList.Add(new SingleRoom("公開聊天室5", 20));
    }


    public byte[] CreateNewRoom(JObject json, AsyncUserToken userToken)
    {
        byte[] backData = null;
        JoinRoom joinInfo = new JoinRoom() { error = ErrorType.none, roomID = -1, unique = -1 };
        GameModel roomType = (GameModel)(int)json[nameof(RoomInfo.RoomType)];
        string roomName = (string)json[nameof(RoomInfo.RoomName)];

        //主逻辑
        string userName = userToken.userInfo.Register.name;
        if (roomName == null)
        {
            roomName = userName + "的房间";
        }
        Log4Debug("当前房间数:" + RoomList.Values.Count);

        foreach (var item in RoomList)
        {
            if (item.Key == 0)//0号房间为默认房间，防止出错，不作为用户房间使用
            {
                continue;
            }
            SingleRoom singleRoom = new SingleRoom(item.Key, (string)roomName, roomType);
            if (RoomList.GetOrAdd(item.Key, (value) => singleRoom).IsEmptyRoom())//创建房间成功
            {
                RoomList.TryGetValue(item.Key, out singleRoom);
                if (singleRoom.Join(userToken, out joinInfo.unique))
                {
                    joinInfo.roomID = item.Key;
                    Log4Debug("空房间{0}作为新建房间成功。" + item.Key);
                    break;
                }
                else
                {
                    Log4Debug("该空房间{0}不可加入，请检查。" + item.Key);
                }
            }
        }
        while (joinInfo.roomID < 0)//遍历存在键值后依旧没有房间可用
        {
            int roomID = -1;
            SingleRoom singleRoom = null;
            lock (RoomList)
            {
                roomID = RoomList.Count;
                singleRoom = new SingleRoom(roomID, (string)roomName, roomType);
                RoomList.GetOrAdd(roomID, (value) => singleRoom);
                //
                joinInfo.roomID = roomID;
            }

            if (singleRoom.Join(userToken, out joinInfo.unique))
            {
                joinInfo.roomID = roomID;
                Log4Debug("创建并加入新房间{0}成功" + roomID);
                break;
            }
        }
        backData = SerializeHelper.Serialize<JoinRoom>(joinInfo);
        return backData;
    }
    public byte[] JoinRoom(JObject json, AsyncUserToken userToken)
    {
        byte[] backData = null;
        JoinRoom joinInfo = new JoinRoom() { error = ErrorType.none, roomID = -1, unique = -1 };
        string roomID = (string)json[nameof(RoomInfo.RoomID)];
        GameModel roomType = (GameModel)(int)json[nameof(RoomInfo.RoomType)];

        //主逻辑
        if (!string.IsNullOrEmpty(roomID))//加入指定房间
        {
            joinInfo.roomID = int.Parse(roomID);
            SingleRoom singleRoom = null;
            if (RoomList.TryGetValue(joinInfo.roomID, out singleRoom))
            {
                if (singleRoom.CurState == RoomActorState.Gaming)
                {
                    joinInfo.error = ErrorType.roomIsGaming;
                }
                else if (singleRoom.Join(userToken, out joinInfo.unique))//可以加入
                {
                    Log4Debug("加入指定ID房间成功。");
                }
                else
                {
                    joinInfo.error = ErrorType.roomMateFull;
                }
            }
            else
            {
                joinInfo.error = ErrorType.roomNotExist;
            }
        }
        else//根据游戏类型遍历所有房间
        {
            foreach (var item in RoomList)
            {
                if (item.Key == 0)
                {
                    continue;
                }
                if (item.Value.RoomInfo.RoomType != roomType)
                    continue;
                if (item.Value.Join(userToken, out joinInfo.unique))//可以加入
                {
                    joinInfo.roomID = item.Key;
                    Log4Debug("加入游戏类型房间成功。");
                    continue;
                }
            }
            if (joinInfo.roomID < 0)
            {
                //请求加入的游戏模式暂没有房间，生成新房间
                backData = CreateNewRoom(json, userToken);
            }
        }
        backData = SerializeHelper.Serialize<JoinRoom>(joinInfo);
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
        if (RoomList.ContainsKey(roomID))
        {
            SingleRoom singleRoom = null;
            if (RoomList.TryGetValue(roomID, out singleRoom) && singleRoom.IsMaster(userID))//发起者是房主
            {
                singleRoom.UpdateRoom(roomType, roomName);
                RoomInfo info = singleRoom.RoomInfo;
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
        NetworkDebuger.Log(this.GetType().Name + ":" + msg);
    }
}