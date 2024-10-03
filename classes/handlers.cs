using MessagePack;
using Newtonsoft.Json;
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
	
	[MessageHandler(ReceivingMessageType.RouteStageFinished)]
	public static void RouteStageFinished(ref Player player, object data) 
	{
		if (player.upAgainst == null)
			return;
		PlayerCompletedStageInfo info = (PlayerCompletedStageInfo)data;
		PlayerCompletedStage stage = new() 
		{
			playerName = player.name,
			stage = info.stage
		};
		Response response = new(SendingMessageType.PlayerFinishedStage, stage);
		player.upAgainst.SendResponse(response);
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
		player2.SendResponse(new(SendingMessageType.OtherPlayerForfeit, new MatchFoundResult() { playerName = player.name }));
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
		player.SendResponse(new(SendingMessageType.PrivateRoomCreated, creation));
	}
	
	[MessageHandler(ReceivingMessageType.JoinPrivateRoom)]
	public static void JoinPrivateRoom(ref Player player, object data) 
	{
		if (player.inRoom)
			return;
		RoomData roomData = (RoomData)data;
		player.inRoom = true;
		if (Rooms.RoomCodeExists(roomData.code)) 
		{
			PrivateRoom room = Rooms.GetRoom(roomData.code);
			room.player2 = player;
			player.room = room;
			RoomReplicationData replicationData1 = new() 
			{
				localPlayerName = room.player1.name,
				opponentName = room.player2.name,
				code = roomData.code
			};
			RoomReplicationData replicationData2 = new() 
			{
				opponentName = room.player1.name,
				localPlayerName = room.player2.name,
				code = roomData.code
			};
			room.player1.SendResponse(new Response(SendingMessageType.ReplicateRoomData, replicationData1));
			player.SendResponse(new Response(SendingMessageType.PrivateRoomJoinAttempt, new RoomJoinAttempt() { RoomJoined = true, replicationData = replicationData2 }));
		}
		else 
		{
			player.SendResponse(new Response(SendingMessageType.PrivateRoomJoinAttempt, new RoomJoinAttempt() { RoomJoined = false }));
		}
	}
	
	[MessageHandler(ReceivingMessageType.PrivateRoomStart)]
	public static void StartPrivateRoom(ref Player player, object data) 
	{
		if (!player.inRoom)
			return;
		PrivateRoom room = player.room;
		if (room.player2 == null)
			return;
		room.player1.upAgainst = room.player2;
		room.player2.upAgainst = room.player1;
		room.player1.isInGame = true;
		room.player2.isInGame = true;
		room.player1.SendResponse(new Response(SendingMessageType.PrivateRoomStarted));
		room.player2.SendResponse(new Response(SendingMessageType.PrivateRoomStarted));
	}
	
	[MessageHandler(ReceivingMessageType.LeavePrivateRoom)]
	public static void LeavePrivateRoom(ref Player player, object data) 
	{
		if (!player.inRoom)
			return;
		if (player.room.player1 == player)
			if (player.room.player2 != null)
				player.room.player2.SendResponse(new Response(SendingMessageType.ReplicateRoomData, new RoomReplicationData() { localPlayerName = player.room.player2.name, opponentName = null, code = player.room.code }));
			else
				Rooms.RemoveRoom(player.room.code);
		if (player.room.player2 == player)
			if (player.room.player1 != null)
				player.room.player1.SendResponse(new Response(SendingMessageType.ReplicateRoomData, new RoomReplicationData() { localPlayerName = player.room.player1.name, opponentName = null, code = player.room.code }));
			else
				Rooms.RemoveRoom(player.room.code);
	}
	
	[MessageHandler(ReceivingMessageType.RngSeed)]
	public static void RngSeed(ref Player player, object data) 
	{
		RngData rng = (RngData)data;
		Console.WriteLine($"{player.name}'s rng seed: {rng.seed}");
		player.upAgainst.SendResponse(new Response(SendingMessageType.RngSeedSet, rng));
	}
}