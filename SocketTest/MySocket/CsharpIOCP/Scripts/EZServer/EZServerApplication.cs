using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

public class EZServerApplication : ApplicationBase
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    public ActorCollection Actors;
    public RoomCollection Rooms;
    public Lobby lobbyActors;

    private bool broadcastActive = false;
    private int lobbyBoradcastCounter;


    protected override PeerBase CreatePeer(InitRequest initRequest)
    {
        // 建立連線並回傳給Photon Server
        return new EZServerPeer(initRequest.Protocol, initRequest.PhotonPeer, this);
    }

    protected override void Setup()
    {
        // 初始化GameServer

        log4net.GlobalContext.Properties["Photon:ApplicationLogPath"] = Path.Combine(this.ApplicationRootPath, "log");

        // log4net
        string path = Path.Combine(this.BinaryPath, "log4net.config");
        var file = new FileInfo(path);
        if (file.Exists)
        {
            LogManager.SetLoggerFactory(Log4NetLoggerFactory.Instance);
            XmlConfigurator.ConfigureAndWatch(file);
        }

        Log.Info("EZServer is running...");

        Actors = new ActorCollection();
        Rooms = new RoomCollection();
        lobbyActors = new Lobby();

        lobbyBoradcastCounter = 0;

        // create broadcast threading
        broadcastActive = true;
        ThreadPool.QueueUserWorkItem(this.LobbyBroadcast);
    }

    protected override void TearDown()
    {
        // 關閉GameServer並釋放資源
        broadcastActive = false;
    }

    private void LobbyBroadcast(object state)
    {
        while (broadcastActive)
        {
            try
            {
                RoomCollection.RoomInfo[] rInfos = Rooms.GetAllRoomInfo();

                var parameter = new Dictionary<byte, object>();

                for (int i = 0; i < rInfos.Length; i++)
                {
                    var roomparameter = new Dictionary<byte, object> {
                                              { (byte)GetRoomInfoEventCode.RoomIndex, rInfos[i].RoomIndex }, {(byte)GetRoomInfoEventCode.RoomName, rInfos[i].RoomName}, {(byte)GetRoomInfoEventCode.Limit, rInfos[i].Limit}, {(byte)GetRoomInfoEventCode.ActorCount, rInfos[i].ActorCount}
                                           };

                    parameter.Add((byte)i, roomparameter);

                }

                var eventData = new EventData(
                    (byte)EZProtocol.EventCode.LobbyBroadcast, parameter);


                for (int i = 0; i < 20; i++) // 每一回合廣播20個玩家
                {
                    if (lobbyBoradcastCounter < lobbyActors.ActorUniqueIDs.Count)
                    {
                        Actor actor = Actors.GetActor(lobbyActors.ActorUniqueIDs[lobbyBoradcastCounter]);

                        if (actor != null)
                        {
                            EZServerPeer peer = Actors.TryGetPeer(actor.guid);

                            if (peer != null)
                            {
                                peer.SendEvent(eventData, new SendParameters());
                            }
                        }
                        lobbyBoradcastCounter++;
                    }
                    else  // 若廣播完一輪後計數器歸0後結束這一回合
                    {
                        lobbyBoradcastCounter = 0;
                        break;
                    }

                }
            }
            catch (Exception EX)
            {
                Log.Error("Lobby Broadcast Exception : " + EX.Message);
            }

            Thread.Sleep(500);
        }
    }

    // 將所有玩家資料廣播給一個玩家
    public void BroadcastRoomActorAllToOne(int RoomIndex, int ActorUniqueID)
    {
        // get peer fo actor
        Actor actor = Actors.GetActor(ActorUniqueID);

        if (actor != null)
        {
            EZServerPeer peer = Actors.TryGetPeer(actor.guid);

            if (peer != null)
            {
                for (int i = 0; i < Rooms.RoomList[RoomIndex].ActorList.Count; i++)
                {
                    var parameter = new Dictionary<byte, object> {
                        { (byte)RoomActorActionInfo.MemberUniqueID, Rooms.RoomList[RoomIndex].ActorList[i].uniqueID },
                        { (byte)RoomActorActionInfo.NickName, Rooms.RoomList[RoomIndex].ActorList[i].nickname },
                        { (byte)RoomActorActionInfo.PosX, Rooms.RoomList[RoomIndex].ActorList[i].PosX },
                        { (byte)RoomActorActionInfo.PosY, Rooms.RoomList[RoomIndex].ActorList[i].PosY },
                        { (byte)RoomActorActionInfo.PosZ, Rooms.RoomList[RoomIndex].ActorList[i].PosZ },
                        { (byte)RoomActorActionInfo.Direct, Rooms.RoomList[RoomIndex].ActorList[i].Direct },
                        { (byte)RoomActorActionInfo.ActionNum, Rooms.RoomList[RoomIndex].ActorList[i].ActionNum }
                    };

                    var eventData = new EventData(
                    (byte)EZProtocol.EventCode.RoomBroadcastActorAction, parameter);
                    peer.SendEvent(eventData, new SendParameters());
                }
            }
        }
    }

    // 將一個玩家資料廣播給所有玩家
    public void BroadcastRoomActorOneToAll(int RoomIndex, int ActorUniqueID, string Nickname, float PosX, float PosY, float PosZ, short Direct, short ActionNum)
    {
        var parameter = new Dictionary<byte, object> {
                { (byte)RoomActorActionInfo.MemberUniqueID, ActorUniqueID },
                { (byte)RoomActorActionInfo.NickName, Nickname },
                { (byte)RoomActorActionInfo.PosX, PosX },
                { (byte)RoomActorActionInfo.PosY, PosY },
                { (byte)RoomActorActionInfo.PosZ, PosZ },
                { (byte)RoomActorActionInfo.Direct, Direct },
                { (byte)RoomActorActionInfo.ActionNum, ActionNum }
            };

        //Log.Info("BBBBBBBBBBB   nick = " + Nickname);
        var eventData = new EventData(
        (byte)EZProtocol.EventCode.RoomBroadcastActorAction, parameter);

        for (int i = 0; i < Rooms.RoomList[RoomIndex].ActorList.Count; i++)
        {
            // get peer
            Actor actor = Actors.GetActor(Rooms.RoomList[RoomIndex].ActorList[i].uniqueID);
            if (actor != null)
            {
                EZServerPeer peer = Actors.TryGetPeer(actor.guid);

                if (peer != null)
                {
                    peer.SendEvent(eventData, new SendParameters());
                }
            }
        }
    }

    // 將玩家離開的事件廣播給房間所有人
    public void BroadcastRoomActorQuit(int RoomIndex, int ActorUniqueID)
    {
        var parameter = new Dictionary<byte, object> {
                {(byte)RoomActorQuit.MemberUniqueID, ActorUniqueID}
            };
        var eventData = new EventData((byte)EZProtocol.EventCode.RoomBroadcastActorQuit, parameter);
        for (int i = 0; i < Rooms.RoomList[RoomIndex].ActorList.Count; i++)
        {
            // get peer
            Actor actor = Actors.GetActor(Rooms.RoomList[RoomIndex].ActorList[i].uniqueID);

            if (actor != null)
            {
                EZServerPeer peer = Actors.TryGetPeer(actor.guid);

                if (peer != null)
                {
                    peer.SendEvent(eventData, new SendParameters());
                }
            }
        }
    }

    // 將某玩家講話的內容廣播給房間所有人
    public void BroadcastRoomSpeak(int RoomIndex, int ActorUniqueID, string TalkString)
    {
        var parameter = new Dictionary<byte, object> {
                { (byte)RoomActorSpeak.MemberUniqueID, ActorUniqueID },
                { (byte)RoomActorSpeak.TalkString, TalkString },
            };

        var eventData = new EventData((byte)EZProtocol.EventCode.RoomBroadcastActorSpeak, parameter);

        for (int i = 0; i < Rooms.RoomList[RoomIndex].ActorList.Count; i++)
        {
            // get peer
            Actor actor = Actors.GetActor(Rooms.RoomList[RoomIndex].ActorList[i].uniqueID);
            EZServerPeer peer = Actors.TryGetPeer(actor.guid);

            if (peer != null)
            {
                peer.SendEvent(eventData, new SendParameters());
            }
        }
    }
}
