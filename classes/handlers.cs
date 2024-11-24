using VapSRServer;

public class Handlers 
{
	[MessageHandler(ReceivingMessageType.LoadingFinished)]
	public static void LoadingFinished(ref Player player, object data) 
	{
		Console.WriteLine($"Player uuid {player.UUID} finished loading.");
		player.isLoaded = true;
	}
	
	[MessageHandler(ReceivingMessageType.StartMatchmaking)]
	public static void StartMatchmaking(ref Player player, object data) 
	{
		player.matchmaking = true;
		Response response = new(SendingMessageType.MatchmakingStarted);
		Console.WriteLine($"Starting matchmaking for player with uuid {player.UUID}");
		player.SendResponse(response);
	}
	
	private static void RoomRouteStageFinished(ref Player player, PlayerCompletedStageInfo data) 
	{
		PrivateRoom room = player.room;
		PlayerCompletedStage stage = new() 
		{
			playerName = player.name,
			stage = data.stage
		};
		room.host.SendResponse(SendingMessageType.PlayerFinishedStage, stage);
		Player[] players = [ room.host, ..room.connected ];
		foreach (Player connectedClient in players) 
		{
			if (connectedClient.UUID != player.UUID)
				connectedClient.SendResponse(SendingMessageType.PlayerFinishedStage, stage);
		}
	}
	
	[MessageHandler(ReceivingMessageType.RouteStageFinished)]
	public static void RouteStageFinished(ref Player player, object data) 
	{
		PlayerCompletedStageInfo info = (PlayerCompletedStageInfo)data;
		if (player.inRoom) 
		{
			RoomRouteStageFinished(ref player, info);
			return;
		}
		if (player.upAgainst == null)
			return;
		PlayerCompletedStage stage = new() 
		{
			playerName = player.name,
			stage = info.stage
		};
		player.upAgainst.SendResponse(SendingMessageType.PlayerFinishedStage, stage);
	}
	
	[MessageHandler(ReceivingMessageType.UserInfo)]
	public static void UserInfoReceived(ref Player player, object data) 
	{
		UserInfo info = (UserInfo)data;
		player.name = info.username;
		//player.id = info.id;
	}
	
	[MessageHandler(ReceivingMessageType.RunFinished)]
	public static void RunFinished(ref Player player, object data) 
	{
		RunFinishedInfo info = (RunFinishedInfo)data;
		player.runFinished = true;
		player.time = info.time;
		player.RunCompleted();
		Console.WriteLine($"Player {player.name} finished with time {info.time}");
	}
	
	[MessageHandler(ReceivingMessageType.LeftToMenu)]
	public static void PlayerLeft(ref Player player, object data) 
	{
		if (!player.isInGame)
			return;
		Player player2 = player.upAgainst;
		player.upAgainst = null;
		player2.upAgainst = null;
		player.isInGame = false;
		player.isLoaded = false;
		player2.isInGame = false;
		player2.isLoaded = false;
		player.runFinished = false;
		player2.runFinished = false;
		player.runStarted = false;
		player2.runStarted = false;
		player.time = 0f;
		player2.time = 0f;
		player2.SendResponse(SendingMessageType.OtherPlayerForfeit, new MatchFoundResult() { playerName = player.name });
	}
	
	[MessageHandler(ReceivingMessageType.CancelMatchmaking)]
	public static void CancelMatchmaking(ref Player player, object data) 
	{
		player.matchmaking = false;
	}
	
	[MessageHandler(ReceivingMessageType.CreatePrivateRoom)]
	public static void CreatePrivateRoom(ref Player player, object data) 
	{
		if (player.inRoom)
			return;
		PrivateRoom createdRoom = Rooms.CreateRoom(ref player);
		RoomData creation = new() 
		{
			code = createdRoom.code
		};
		player.inRoom = true;
		player.room = createdRoom;
		player.SendResponse(SendingMessageType.PrivateRoomCreated, creation);
	}

	private static string[] GrabNames(Player[] connected) 
	{
		string[] connectedNames = [];
		foreach (Player player in connected)
			connectedNames = [ .. connectedNames, player.name ];
		return connectedNames;
	}
	
	private static void UpdateRoomData(PrivateRoom room) 
	{
		RoomReplicationData replicationData = new() 
		{
			host = room.host.name,
			code = room.code,
			opponents = GrabNames(room.connected)
		};
		Player[] players = [ room.host, .. room.connected ];
		foreach (Player connectedClient in players)
			connectedClient.SendResponse(SendingMessageType.ReplicateRoomData, replicationData);
	}
	
	private static void RoomStarted(PrivateRoom room) 
	{
		if (!room.host.isInGame)
			room.host.SendResponse(SendingMessageType.RequestSeed);
		foreach (Player connectedClient in room.connected) 
		{
			if (connectedClient.isInGame)
				continue;
			connectedClient.isInGame = true;
			connectedClient.SendResponse(SendingMessageType.PrivateRoomStarted);
		}
	}
	
	[MessageHandler(ReceivingMessageType.JoinPrivateRoom)]
	public static void JoinPrivateRoom(ref Player player, object data) 
	{
		if (player.inRoom)
			return;
		RoomData roomData = (RoomData)data;
		if (Rooms.RoomCodeExists(roomData.code)) 
		{
			player.inRoom = true;
			PrivateRoom room = Rooms.GetRoom(roomData.code);
			player.room = room;
			Player[] playerUpd = [ .. room.connected, player ];
			RoomReplicationData replicationData = new() 
			{
				host = room.host.name,
				code = room.code,
				opponents = GrabNames(playerUpd)
			};
			room.connected = playerUpd;
			Player[] players = [ room.host, .. room.connected ];
			Player plr = player;
			players = [ .. players.Where((v) => v.UUID != plr.UUID)];
			foreach (Player connectedClient in players)
				connectedClient.SendResponse(SendingMessageType.ReplicateRoomData, replicationData);
			player.SendResponse(SendingMessageType.PrivateRoomJoinAttempt, new RoomJoinAttempt() { RoomJoined = true, replicationData = replicationData });
		}
		else 
		{
			player.SendResponse(SendingMessageType.PrivateRoomJoinAttempt, new RoomJoinAttempt() { RoomJoined = false });
		}
	}
	
	[MessageHandler(ReceivingMessageType.PrivateRoomStart)]
	public static void StartPrivateRoom(ref Player player, object data) 
	{
		if (!player.inRoom)
			return;
		PrivateRoom room = player.room;
		if (room.connected.Length < 1)
			return;
		RoomStarted(room);
		//room.player1.SendResponse(new Response(SendingMessageType.PrivateRoomStarted));
		//room.player2.SendResponse(new Response(SendingMessageType.PrivateRoomStarted));
	}
	
	private static void HostLeft(PrivateRoom room) 
	{
		Player? nextHost = room.connected.FirstOrDefault();
		if (nextHost == null) 
		{
			Rooms.RemoveRoom(room.code);
			return;
		}
		room.host = nextHost;
		UpdateRoomData(room);
	}
	
	[MessageHandler(ReceivingMessageType.LeavePrivateRoom)]
	public static void LeavePrivateRoom(ref Player player, object data) 
	{
		if (!player.inRoom)
			return;
		player.inRoom = false;
		if (player.room.host == player)
			HostLeft(player.room);
		else 
		{
			Player plr = player;
			player.room.connected = player.room.connected.Where((v) => v.UUID != plr.UUID).ToArray();
			UpdateRoomData(player.room);
		}
	}
	
	[MessageHandler(ReceivingMessageType.RngSeed)]
	public static void RngSeed(ref Player player, object data) 
	{
		RngData rng = (RngData)data;
		Console.WriteLine($"{player.name}'s rng seed: {rng.seed}");
		if (player.inRoom) 
		{
			player.isInGame = true;
			player.SendResponse(SendingMessageType.PrivateRoomStarted);
			foreach (Player client in player.room.connected) 
			{
				if (client.UUID != player.UUID)
					client.SendResponse(SendingMessageType.RngSeedSet, rng);
			}
			return;
		}
		player.upAgainst.SendResponse(SendingMessageType.RngSeedSet, rng);
	}
}