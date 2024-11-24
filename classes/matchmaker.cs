namespace VapSRServer;

public class Matchmaker 
{
	private static void MatchFound(ref Player player1, ref Player player2) 
	{
		player1.matchmaking = false;
		player2.matchmaking = false;
		player1.isInGame = true;
		player2.isInGame = true;
		PlayerResult result1 = new() 
		{
			playerName = player2.name
		};
		PlayerResult result2 = new() 
		{
			playerName = player1.name
		};
		Response response1 = new(SendingMessageType.MatchFound, result1);
		Response response2 = new(SendingMessageType.MatchFound, result2);
		player1.upAgainst = player2;
		player2.upAgainst = player1;
		player1.SendResponse(response1);
		SeedRequest(player1);
		player2.SendResponse(response2);
	}
	
	private static async Task SeedRequest(Player player1) 
	{
		await Task.Delay(2000);
		player1.SendResponse(SendingMessageType.RequestSeed);
	}
	
	public static async Task MatchDoneLoop() 
	{
		while (true) 
		{
			await Task.Delay(20);
			Player[] playersInGame = [];
			foreach (Player player in PlayerPool.players) 
			{
				if (!player.inRoom && player.isLoaded && player.runStarted && playersInGame.FirstOrDefault((v) => v.UUID == player.upAgainst.UUID) == null)
					playersInGame = [ .. playersInGame, player ];
			}
			foreach (Player player in playersInGame) 
			{
				MatchDone(player, player.upAgainst);
			}
		}
	}
	
	private static void MatchDone(Player player1, Player player2) 
	{
		if (player1.runFinished && player2.runFinished) 
		{
			RunFinishedRelayInfo info1 = new() 
			{
				playerName = player2.name,
				time = player2.time,
				youWon = player1.time < player2.time
			};
			RunFinishedRelayInfo info2 = new() 
			{
				playerName = player1.name,
				time = player1.time,
				youWon = player2.time < player1.time
			};
			player1.upAgainst = null;
			player2.upAgainst = null;
			player1.isInGame = false;
			player1.isLoaded = false;
			player2.isInGame = false;
			player2.isLoaded = false;
			player1.runFinished = false;
			player2.runFinished = false;
			player1.runStarted = false;
			player2.runStarted = false;
			player1.time = 0f;
			player2.time = 0f;
			player1.SendResponse(SendingMessageType.RunStopped, info1);
			player2.SendResponse(SendingMessageType.RunStopped, info2);
		}
	}
	
	public static async Task ReadyLoop() 
	{
		while (true) 
		{
			await Task.Delay(20);
			Player[] playersInGame = [];
			foreach (Player player in PlayerPool.players) 
			{
				if (!player.inRoom && player.isInGame && !player.runStarted && playersInGame.FirstOrDefault((v) => v.UUID == player.upAgainst.UUID) == null)
					playersInGame = [ .. playersInGame, player ];
			}
			foreach (Player player in playersInGame) 
			{
				VersusReady(player, player.upAgainst);
			}
		}
	}
	
	private static void VersusReady(Player player1, Player player2) 
	{
		if (player1.isLoaded && player2.isLoaded) 
		{
			player1.runStarted = true;
			player2.runStarted = true;
			player1.SendResponse(SendingMessageType.StartRun);
			player2.SendResponse(SendingMessageType.StartRun);
		}
	}
	
	public static async Task MatchmakingLoop() 
	{
		while (true) 
		{
			await Task.Delay(20);
			Player[] playersMatchmaking = [];
			foreach (Player player in PlayerPool.players) 
			{
				if (!player.inRoom && player.matchmaking && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - player.lastResponseTime < 5)
					playersMatchmaking = [ .. playersMatchmaking, player ];
			}
			if (playersMatchmaking.Length > 1) 
			{
				Player[] players = playersMatchmaking.OrderBy(x => Random.Shared.Next()).Take(2).ToArray();
				MatchFound(ref players[0], ref players[1]);
			}
		}
	}
	
	private static void ProcessRoom_Ready(PrivateRoom room) 
	{
		Player[] players = [ room.host, .. room.connected ];
		bool allReady = players.All((v) => v.isLoaded);
		if (allReady) 
			foreach (Player player in players) 
			{
				if (player.runStarted)
					continue;
				player.runStarted = true;
				player.SendResponse(SendingMessageType.StartRun);
				player.RunFinished += (object _, RunFinishedArgs args) => ProcessRoom_Finished(player, args.time);
			}
	}
	
	private static void ProcessRoom_Finished(Player player, float time) 
	{
		if (ServerHandler.debug)
			Console.WriteLine($"Player {player.name} finished run in private room with time {time}");
		player.ClearFinishedListeners();
		PrivateRoom room = player.room;
		Player[] players = [ room.host, .. room.connected ];
		foreach (Player plr in players) 
		{
			if (plr.runFinished && plr.UUID != player.UUID) 
				plr.SendResponse(SendingMessageType.PrivateRoomRunFinished, new RoomRunFinished() { player = player.name, time = time });
		}
		Run[] runs = [];
		foreach (Player plr in players) 
		{
			if (plr.runFinished && plr.UUID != player.UUID) 
			{
				Run run = new()
				{
					name = plr.name,
					time = plr.time
				};
				runs = [ .. runs, run ];
			}
		}
		BatchRoomRunsFinished batch = new()
		{
			times = runs
		};
		if (runs.Length != 0)
			player.SendResponse(SendingMessageType.PrivateRoomBatchRunsFinished, batch);
		Task.Run(() => RoomFinishedCheck(room));
	}
	
	private static async Task RoomFinishedCheck(PrivateRoom room) 
	{
		Player[] players = [ room.host, .. room.connected ];
		await Task.Delay(2000);
		bool allFinished = players.All((v) => v.runFinished);
		if (allFinished) 
			foreach (Player plr in players)
				plr.SendResponse(SendingMessageType.PrivateRoomEveryoneCompleted);
	}

	public static async Task RoomReadyLoop() 
	{
		while (true) 
		{
			await Task.Delay(20);
			foreach (PrivateRoom room in Rooms.rooms)
				ProcessRoom_Ready(room);
		}
	}
}