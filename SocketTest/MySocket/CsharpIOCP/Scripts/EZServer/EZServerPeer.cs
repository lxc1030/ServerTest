using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;
using EZProtocol;
using ExitGames.Logging;

public class EZServerPeer : PeerBase
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();


    public Guid peerGuid { get; protected set; }
    private EZServerApplication _server;

    #region 建構與解構式

    public EZServerPeer(IRpcProtocol rpcProtocol, IPhotonPeer nativePeer, EZServerApplication ServerApplication)
        : base(rpcProtocol, nativePeer)
    {
        peerGuid = Guid.NewGuid();
        // 儲存ServerApplication
        _server = ServerApplication;

        // 將 Peer 加入連線列表
        _server.Actors.AddConnectedPeer(peerGuid, this);

    }

    #endregion

    protected override void OnDisconnect(PhotonHostRuntimeInterfaces.DisconnectReason reasonCode, string reasonDetail)
    {
        // 失去連線時要處理的事項，例如釋放資源

        Actor actor = _server.Actors.GetActorFromGuid(this.peerGuid);

        if (actor != null)
        {
            if (actor.roomIndex != -1)
            {
                // 呼叫Application廣播給聊天室所有玩家將此玩家移除
                _server.BroadcastRoomActorQuit(actor.roomIndex, actor.uniqueID);

                // 從房間移除
                _server.Rooms.RoomList[actor.roomIndex].Quit(actor.uniqueID);
            }
            else
            {
                // 從大廳移除
                _server.lobbyActors.Remove(actor.uniqueID);
            }
        }


        _server.Actors.ActorOffline(peerGuid);
    }

    protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
    {

        // 取得Client端傳過來的要求並加以處理

        // 若是Debug模式將傳過來的資料全部印出來以方便驗證
        if (Log.IsDebugEnabled)
        {
            Log.Debug("OnOperationRequest取得的資料  key : 值");
            foreach (KeyValuePair<byte, object> item in operationRequest.Parameters)
            {
                Log.DebugFormat(string.Format("{0} : {1}", item.Key, item.Value.ToString()));
            }
        }

        switch (operationRequest.OperationCode)
        {
            #region Login
            case (byte)OperationCode.Login:
                {
                    if (operationRequest.Parameters.Count < 2) // 若參數小於2則返回錯誤
                    {
                        // 返回登入錯誤
                        OperationResponse response = new OperationResponse(operationRequest.OperationCode) { ReturnCode = (short)ErrorCode.InvalidParameter, DebugMessage = "Login Fail" };
                        SendOperationResponse(response, new SendParameters());
                    }
                    else
                    {
                        var memberID = (string)operationRequest.Parameters[(byte)LoginParameterCode.MemberID];
                        var memberPW = (string)operationRequest.Parameters[(byte)LoginParameterCode.MemberPW];

                        if ((memberID == "a1" || memberID == "a2" || memberID == "a3" || memberID == "a4" || memberID == "a5") && memberPW == "1234")
                        {
                            int MemberUniqueID = Convert.ToInt32(memberID.Substring(1));
                            string Nickname = memberID + "Nick";
                            short Sex = 0;
                            ActorCollection.ActorReturn actorRet = _server.Actors.ActorOnline(peerGuid, MemberUniqueID, memberID, Nickname, Sex);


                            if (actorRet.ReturnCode == 1)
                            {
                                var parameter = new Dictionary<byte, object> {
                                              { (byte)LoginResponseCode.Ret, actorRet.ReturnCode }, {(byte)LoginResponseCode.MemberID, memberID}, {(byte)LoginResponseCode.MemberPW, memberPW}, {(byte)LoginResponseCode.Nickname, Nickname}, {(byte)LoginResponseCode.MemberUniqueID, MemberUniqueID}, {(byte)LoginResponseCode.Sex, Sex}
                                           };

                                OperationResponse response = new OperationResponse(operationRequest.OperationCode, parameter) { ReturnCode = (short)ErrorCode.Ok, DebugMessage = "" };

                                SendOperationResponse(response, new SendParameters());

                                // 登入後預設是加入大廳
                                _server.lobbyActors.Add(MemberUniqueID);
                            }
                            else
                            {
                                // 返回自訂錯誤，若有發生錯誤則將暱稱等資料填空白以免Client以為有成功
                                var parameter = new Dictionary<byte, object> {
                                              { (byte)LoginResponseCode.Ret, actorRet.ReturnCode }, {(byte)LoginResponseCode.MemberID, memberID}, {(byte)LoginResponseCode.MemberPW, memberPW}, {(byte)LoginResponseCode.Nickname, ""}, {(byte)LoginResponseCode.MemberUniqueID, 0}, {(byte)LoginResponseCode.Sex, -1}
                                           };
                                OperationResponse response = new OperationResponse(operationRequest.OperationCode, parameter) { ReturnCode = (short)ErrorCode.CustomError, DebugMessage = actorRet.DebugMessage };
                                SendOperationResponse(response, new SendParameters());
                            }

                        }
                        else
                        {
                            OperationResponse response = new OperationResponse(operationRequest.OperationCode) { ReturnCode = (short)ErrorCode.InvalidOperation, DebugMessage = "帳號密碼錯誤；IDまたはパスワードが正しくありません；Wrong id or password  " };
                            SendOperationResponse(response, new SendParameters());
                        }

                    }

                    break;
                }
            #endregion

            #region GetRoomInfo
            case (byte)OperationCode.GetRoomInfo:
                {
                    var roomIndex = (Int32)operationRequest.Parameters[(byte)GetRoomInfoParameterCode.RoomIndex];
                    RoomCollection.RoomInfo rInfo = _server.Rooms.GetRoomInfo(roomIndex);

                    if (rInfo != null)
                    {
                        var parameter = new Dictionary<byte, object> {
                                              { (byte)GetRoomInfoResponseCode.RoomIndex, rInfo.RoomIndex }, {(byte)GetRoomInfoResponseCode.RoomName, rInfo.RoomName}, {(byte)GetRoomInfoResponseCode.Limit, rInfo.Limit}, {(byte)GetRoomInfoResponseCode.ActorCount, rInfo.ActorCount}
                                           };

                        OperationResponse response = new OperationResponse(operationRequest.OperationCode, parameter) { ReturnCode = (short)ErrorCode.Ok, DebugMessage = "" };

                        SendOperationResponse(response, new SendParameters());
                    }
                    else
                    {
                        OperationResponse response = new OperationResponse(operationRequest.OperationCode) { ReturnCode = (short)ErrorCode.InvalidOperation, DebugMessage = "無此房間" };

                        SendOperationResponse(response, new SendParameters());
                    }
                    break;
                }
            #endregion

            #region GetAllRoomInfo
            case (byte)OperationCode.GetAllRoomInfo:
                {
                    RoomCollection.RoomInfo[] rInfos = _server.Rooms.GetAllRoomInfo();

                    var parameter = new Dictionary<byte, object>();

                    for (int i = 0; i < rInfos.Length; i++)
                    {
                        var roomparameter = new Dictionary<byte, object> {
                                              { (byte)GetRoomInfoResponseCode.RoomIndex, rInfos[i].RoomIndex }, {(byte)GetRoomInfoResponseCode.RoomName, rInfos[i].RoomName}, {(byte)GetRoomInfoResponseCode.Limit, rInfos[i].Limit}, {(byte)GetRoomInfoResponseCode.ActorCount, rInfos[i].ActorCount}
                                           };

                        parameter.Add((byte)i, roomparameter);

                    }

                    OperationResponse response = new OperationResponse(operationRequest.OperationCode, parameter) { ReturnCode = (short)ErrorCode.Ok, DebugMessage = "" };

                    SendOperationResponse(response, new SendParameters());

                    break;
                }
            #endregion

            #region JoinRoom
            case (byte)OperationCode.JoinRoom:
                {
                    var roomIndex = (Int32)operationRequest.Parameters[(byte)GetRoomInfoParameterCode.RoomIndex];
                    RoomCollection.RoomInfo rInfo = _server.Rooms.GetRoomInfo(roomIndex);
                    if (rInfo != null)
                    {
                        // 取得會員資料
                        Actor actor = _server.Actors.GetActorFromGuid(this.peerGuid);

                        // 加人房間
                        if (_server.Rooms.RoomList[roomIndex].Join(this.peerGuid, actor.uniqueID, actor.memberID, actor.nickname, actor.sex))
                        {

                            // 更改會員狀態
                            _server.Actors.SetActorStatus(actor.uniqueID, 2, (short)roomIndex);

                            // 回傳房間資訊 
                            var parameter = new Dictionary<byte, object> {
                                              { (byte)JoinRoomResponseCode.RoomIndex, rInfo.RoomIndex }, {(byte)JoinRoomResponseCode.RoomName, rInfo.RoomName}, {(byte)JoinRoomResponseCode.Limit, rInfo.Limit}, {(byte)JoinRoomResponseCode.ActorCount, rInfo.ActorCount + 1}
                                           };

                            OperationResponse response = new OperationResponse(operationRequest.OperationCode, parameter) { ReturnCode = (short)ErrorCode.Ok, DebugMessage = "" };

                            SendOperationResponse(response, new SendParameters());

                            // 從大廳移除(加入房間)
                            _server.lobbyActors.Remove(actor.uniqueID);
                        }
                        else
                        {
                            OperationResponse response = new OperationResponse(operationRequest.OperationCode) { ReturnCode = (short)ErrorCode.CustomError, DebugMessage = "重複加入或房間已滿" };

                            SendOperationResponse(response, new SendParameters());
                        }
                    }
                    else
                    {
                        OperationResponse response = new OperationResponse(operationRequest.OperationCode) { ReturnCode = (short)ErrorCode.InvalidOperation, DebugMessage = "無此房間" };

                        SendOperationResponse(response, new SendParameters());
                    }

                    break;
                }
            #endregion

            #region QuitRoom
            case (byte)OperationCode.QuitRoom:
                {
                    Actor actor = _server.Actors.GetActorFromGuid(this.peerGuid);

                    if (actor != null)
                    {
                        if (actor.roomIndex != -1)
                        {
                            // 呼叫Application廣播給聊天室所有玩家將此玩家移除
                            _server.BroadcastRoomActorQuit(actor.roomIndex, actor.uniqueID);

                            // 從房間移除
                            _server.Rooms.RoomList[actor.roomIndex].Quit(actor.uniqueID);
                        }

                        // 重設會員狀態
                        _server.Actors.SetActorStatus(actor.uniqueID, 1, -1); // status=1 表示回大廳中, roomindex=-1表示不在任何房間中

                        // 重新加入大廳
                        _server.lobbyActors.Add(actor.uniqueID);
                    }
                    break;
                }
            #endregion

            #region RoomActorBorning
            case (byte)OperationCode.RoomActorBorning:
                {
                    try
                    {
                        // 取得會員的行為資訊
                        float PosX = (float)operationRequest.Parameters[(byte)RoomActorActionInfo.PosX];
                        float PosY = (float)operationRequest.Parameters[(byte)RoomActorActionInfo.PosY];
                        float PosZ = (float)operationRequest.Parameters[(byte)RoomActorActionInfo.PosZ];
                        short Direct = (short)operationRequest.Parameters[(byte)RoomActorActionInfo.Direct];
                        short ActionNum = (short)operationRequest.Parameters[(byte)RoomActorActionInfo.ActionNum];

                        // 取得會員資訊
                        Actor actor = _server.Actors.GetActorFromGuid(this.peerGuid);

                        if (actor.roomIndex != -1)
                        {
                            // 更新玩家狀態到Room裡面的玩家列表
                            _server.Rooms.RoomList[actor.roomIndex].UpdateActorAction(actor.uniqueID, PosX, PosY, PosZ, Direct, ActionNum);

                            // 呼叫Application將已在聊天室所有玩家廣播給此玩家
                            _server.BroadcastRoomActorAllToOne(actor.roomIndex, actor.uniqueID);

                            // 呼叫Application將此玩家的行為廣播給聊天室所有玩家
                            _server.BroadcastRoomActorOneToAll(actor.roomIndex, actor.uniqueID, actor.nickname, PosX, PosY, PosZ, Direct, ActionNum);
                        }
                    }
                    catch (Exception EX)
                    {
                        Log.Error("Exception : " + EX.Message);
                    }
                    break;
                }
            #endregion

            #region RoomActorActionUpdate
            case (byte)OperationCode.RoomActorActionUpdate:
                {
                    // 取得會員的行為資訊
                    float PosX = (float)operationRequest.Parameters[(byte)RoomActorActionInfo.PosX];
                    float PosY = (float)operationRequest.Parameters[(byte)RoomActorActionInfo.PosY];
                    float PosZ = (float)operationRequest.Parameters[(byte)RoomActorActionInfo.PosZ];
                    short Direct = (short)operationRequest.Parameters[(byte)RoomActorActionInfo.Direct];
                    short ActionNum = (short)operationRequest.Parameters[(byte)RoomActorActionInfo.ActionNum];

                    // 取得會員資訊
                    Actor actor = _server.Actors.GetActorFromGuid(this.peerGuid);

                    if (actor.roomIndex != -1)
                    {
                        // 更新玩家狀態到Room裡面的玩家列表
                        _server.Rooms.RoomList[actor.roomIndex].UpdateActorAction(actor.uniqueID, PosX, PosY, PosZ, Direct, ActionNum);

                        // 呼叫Application將此玩家的行為廣播給聊天室所有玩家
                        _server.BroadcastRoomActorOneToAll(actor.roomIndex, actor.uniqueID, actor.nickname, PosX, PosY, PosZ, Direct, ActionNum);
                    }

                    break;
                }
            #endregion

            #region Speak
            case (byte)OperationCode.RoomSpeak:
                {
                    String TalkString = (string)operationRequest.Parameters[(byte)RoomActorSpeak.TalkString];

                    // 取得會員資訊
                    Actor actor = _server.Actors.GetActorFromGuid(this.peerGuid);

                    // 將發言內容廣播給聊天室所有玩家
                    if (actor.roomIndex != -1)
                    {
                        _server.BroadcastRoomSpeak(actor.roomIndex, actor.uniqueID, TalkString);
                    }
                    break;
                }
                #endregion
        }

    }
}
