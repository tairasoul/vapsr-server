namespace VapSRServer;

public class Matchmaker 
{
	private static void MatchFound(ref Player player1, ref Player player2) 
	{
		player1.matchmaking = false;
		player2.matchmaking = false;
		player1.isInGame = true;
		player2.isInGame = true;
		MatchFoundResult result1 = new() 
		{
			playerName = player2.name
		};
		MatchFoundResult result2 = new() 
		{
			playerName = player1.name
		};
		Response response1 = new(SendingMessageType.MatchFound, result1);
		Response response2 = new(SendingMessageType.MatchFound, result2);
		player1.upAgainst = player2;
		player2.upAgainst = player1;
		player1.SendResponse(response1);
		player2.SendResponse(response2);
	}
	
	public static async Task MatchDoneLoop() 
	{
		while (true) 
		{
			await Task.Delay(20);
			Player[] playersInGame = [];
			foreach (Player player in PlayerPool.players) 
			{
				if (player.isLoaded && player.runStarted && playersInGame.FirstOrDefault((v) => v.UUID == player.upAgainst.UUID) == null)
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
			player1.time = 0f;
			player2.time = 0f;
			player1.SendResponse(new Response(SendingMessageType.RunStopped, info1));
			player2.SendResponse(new Response(SendingMessageType.RunStopped, info2));
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
				if (player.isInGame && !player.runStarted && playersInGame.FirstOrDefault((v) => v.UUID == player.upAgainst.UUID) == null)
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
			Response response = new(SendingMessageType.StartRun);
			player1.SendResponse(response);
			player2.SendResponse(response);
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
				if (player.matchmaking)
					playersMatchmaking = [ .. playersMatchmaking, player ];
			}
			if (playersMatchmaking.Length > 1) 
			{
				Player[] players = playersMatchmaking.OrderBy(x => Random.Shared.Next()).Take(2).ToArray();
				MatchFound(ref players[0], ref players[1]);
			}
		}
	}
}