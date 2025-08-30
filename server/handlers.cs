using VapSRServer.Attributes;
using VapSRServer.Data.Players;
using VapSRServer.Data.Rooms;
using VapSRServer.Networking.C2S;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;
using VapSRServer.Networking.S2C;

namespace VapSRServer.Server;

static class Handlers {
  internal static Matchmaker MatchmakingInstance;
  internal static Rooms RoomInstance;

  [MessageHandler(C2STypes.LoadingFinished)]
  public static void LoadingFinished(Player player, object data) {
		if (Server.debug)
			Console.WriteLine($"Player {player.name ?? player.UUID} finished loading.");
    MatchmakingInstance.pool.playerLoaded.Invoke(null, player);
  }

  [MessageHandler(C2STypes.StartMatchmaking)]
  public static void StartMatchmaking(Player player, object data) {
    MatchmakingInstance.pool.playerStartedMatchmaking.Invoke(null, player);
    player.SendResponse(S2CTypes.MatchmakingStarted);
  }

  private static void RoomRouteStageFinished(Player player, PlayerCompletedStageC2S data) {
    PrivateRoom room = player.room;
    PlayerCompletedStageS2C stage = new()
    {
      playerName = player.name,
      stage = data.stage
    };
    Player[] players = [room.host, .. room.connected];
    foreach (Player connected in players) {
      if (connected != player)
        connected.SendResponse(S2CTypes.PlayerFinishedStage, stage);
    }
  }

  [MessageHandler(C2STypes.RouteStageFinished)]
  public static void RouteStageFinished(Player player, object data) {
    PlayerCompletedStageC2S info = (PlayerCompletedStageC2S)data;
    if (player.inRoom) {
      RoomRouteStageFinished(player, info);
      return;
    }
    if (player.upAgainst == null)
      return;
    PlayerCompletedStageS2C stage = new() 
    {
      playerName = player.name,
      stage = info.stage
    };
    player.upAgainst.SendResponse(S2CTypes.PlayerFinishedStage, stage);
  }

  [MessageHandler(C2STypes.UserInfo)]
  public static void UserInfoReceived(Player player, object data) {
    UserInfoC2S info = (UserInfoC2S)data;
    player.name = info.username;
  }

  [MessageHandler(C2STypes.RunFinished)]
  public static void RunFinished(Player player, object data) {
    RunFinishedC2S info = (RunFinishedC2S)data;
    player.RunFinished.Invoke(null, info.time);
    //MatchmakingInstance.pool.playerCompletedRun.Invoke(null, (player, info.time));
    if (Server.debug)
			Console.WriteLine($"Player {player.name ?? player.UUID} finished with time {info.time}");
  }

  [MessageHandler(C2STypes.LeftToMenu)]
  public static void PlayerLeft(Player player, object data) {
    if (!player.isInGame)
      return;
    Player p1 = player;
    Player p2 = p1.upAgainst;
    p1.upAgainst = null;
    p2.upAgainst = null;
    p1.isInGame = false;
    p2.isInGame = false;
    p1.isLoaded = false;
    p2.isLoaded = false;
    p1.runFinished = false;
    p2.runFinished = false;
    p1.runStarted = false;
    p2.runStarted = false;
    p1.time = 0f;
    p2.time = 0f;
    MatchmakingInstance.pool.playerNotBusy.Invoke(null, p1);
    MatchmakingInstance.pool.playerNotBusy.Invoke(null, p2);
    p2.SendResponse(S2CTypes.OtherPlayerForfeit, new PlayerResultCommon() { playerName = p1.name });
  }

  [MessageHandler(C2STypes.CancelMatchmaking)]
  public static void CancelMatchmaking(Player player, object data) {
    player.matchmaking = false;
    MatchmakingInstance.pool.playerStoppedMatchmaking.Invoke(null, player);
  }

  [MessageHandler(C2STypes.CreatePrivateRoom)]
  public static void CreatePrivateRoom(Player player, object data) {
    if (player.inRoom)
      return;
    PrivateRoom createdRoom = RoomInstance.CreateRoom(ref player);
    RoomDataCommon creation = new()
    {
      code = createdRoom.code
    };
    player.inRoom = true;
    player.room = createdRoom;
    MatchmakingInstance.pool.playerBusy.Invoke(null, player);
    player.SendResponse(S2CTypes.PrivateRoomCreated, creation);
  }

	private static string[] GrabNames(Player[] connected) 
	{
		string[] connectedNames = [];
		foreach (Player player in connected)
			connectedNames = [ .. connectedNames, player.name ];
		return connectedNames;
	}
	
	internal static void UpdateRoomData(PrivateRoom room) 
	{
		RoomReplicationDataS2C replicationData = new() 
		{
			host = room.host.name,
			code = room.code,
			opponents = GrabNames(room.connected)
		};
		Player[] players = [ room.host, .. room.connected ];
		foreach (Player connectedClient in players)
			connectedClient.SendResponse(S2CTypes.ReplicateRoomData, replicationData);
	}
	
	private static void RoomStarted(PrivateRoom room) 
	{
		if (!room.host.isInGame)
			room.host.SendResponse(S2CTypes.RequestSeed);
		foreach (Player connectedClient in room.connected) 
		{
			if (connectedClient.isInGame)
				continue;
			connectedClient.isInGame = true;
			connectedClient.SendResponse(S2CTypes.PrivateRoomStarted);
		}
	}

  [MessageHandler(C2STypes.JoinPrivateRoom)]
  public static void JoinPrivateRoom(Player player, object data) {
    if (player.inRoom)
      return;
    RoomDataCommon roomData = (RoomDataCommon)data;
    if (RoomInstance.RoomCodeExists(roomData.code)) {
      player.inRoom = true;
      PrivateRoom room = RoomInstance.GetRoom(roomData.code);
      player.room = room;
      MatchmakingInstance.pool.playerBusy.Invoke(null, player);
      Player[] playerUpd = [.. room.connected ];
      RoomReplicationDataS2C replicationData = new()
      {
        host = room.host.name,
        code = room.code,
        opponents = GrabNames([.. playerUpd, player])
      };
      Player[] players = [.. playerUpd, room.host];
      foreach (Player connected in players)
        connected.SendResponse(S2CTypes.ReplicateRoomData, replicationData);
      player.SendResponse(S2CTypes.PrivateRoomJoinAttempt, new RoomJoinAttemptS2C() { RoomJoined = true, replicationData = replicationData });
    }
    else {
      player.SendResponse(S2CTypes.PrivateRoomJoinAttempt, new RoomJoinAttemptS2C() { RoomJoined = false });
    }
  }

  [MessageHandler(C2STypes.RequestCurrentHost)]
  public static void RequestCurrentHost(Player player, object data) {
    if (!player.inRoom)
      return;
    player.SendResponse(S2CTypes.PrivateRoomNewHost, new PlayerResultCommon() { playerName = player.room.host.name });
  }

  [MessageHandler(C2STypes.PrivateRoomStart)]
  public static void StartPrivateRoom(Player player, object data) {
    if (!player.inRoom)
      return;
    PrivateRoom room = player.room;
    if (room.connected.Length < 1)
      return;
    RoomStarted(room);
  }

  internal static void HostLeft(PrivateRoom room) {
    Player? nextHost = room.connected.FirstOrDefault();
    if (nextHost == null) {
      RoomInstance.RemoveRoom(room.code);
      return;
    }
    room.host = nextHost;
    room.connected = [.. room.connected.Where((v) => v.UUID != nextHost.UUID)];
    UpdateRoomData(room);
  }

  [MessageHandler(C2STypes.LeavePrivateRoom)]
  public static void LeavePrivateRoom(Player player, object data) {
    if (!player.inRoom)
      return;
    player.inRoom = false;
    if (player.room.host == player)
      HostLeft(player.room);
    else {
      player.room.connected = [.. player.room.connected.Where((v) => v.UUID != player.UUID)];
      UpdateRoomData(player.room);
      player.room = null;
    }
    MatchmakingInstance.pool.playerNotBusy.Invoke(null, player);
  }

  [MessageHandler(C2STypes.RngSeed)]
  public static void RngSeed(Player player, object data) {
    RngDataCommon rng = (RngDataCommon)data;
		if (Server.debug)
			Console.WriteLine($"{player.name ?? player.UUID}'s rng seed: {rng.seed}");
    if (player.inRoom) {
      player.isInGame = true;
      player.SendResponse(S2CTypes.PrivateRoomStarted);
      Player[] coll = [.. player.room.connected, player.room.host];
      foreach (Player client in coll) {
        if (client.UUID != player.UUID)
          client.SendResponse(S2CTypes.RngSeed, rng);
      }
      return;
    }
    player.upAgainst.SendResponse(S2CTypes.RngSeed, rng);
  }
}