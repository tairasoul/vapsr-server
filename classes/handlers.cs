using Newtonsoft.Json;
using VapSRServer;

public class Handlers 
{
	[MessageHandler(ReceivingMessageType.LoadingFinished)]
	public static void LoadingFinished(ref Player player, string? data) 
	{
		Console.WriteLine($"Player uuid {player.UUID} finished loading.");
		player.isLoaded = true;
	}
	
	[MessageHandler(ReceivingMessageType.StartMatchmaking)]
	public static void StartMatchmaking(ref Player player, string? data) 
	{
		player.matchmaking = true;
		Response response = new(SendingMessageType.MatchmakingStarted);
		Console.WriteLine($"Starting matchmaking for player with uuid {player.UUID}");
		player.SendResponse(response);
	}
	
	[MessageHandler(ReceivingMessageType.RouteStageFinished)]
	public static void RouteStageFinished(ref Player player, string data) 
	{
		PlayerCompletedStageInfo info = JsonConvert.DeserializeObject<PlayerCompletedStageInfo>(data);
		PlayerCompletedStage stage = new() 
		{
			playerName = player.name,
			stage = info.stage
		};
		Response response = new(SendingMessageType.PlayerFinishedStage, stage);
		player.upAgainst.SendResponse(response);
	}
	
	[MessageHandler(ReceivingMessageType.UserInfo)]
	public static void UserInfoReceived(ref Player player, string data) 
	{
		UserInfo info = JsonConvert.DeserializeObject<UserInfo>(data);
		player.name = info.username;
		//player.id = info.id;
	}
	
	[MessageHandler(ReceivingMessageType.RunFinished)]
	public static void RunFinished(ref Player player, string data) 
	{
		RunFinishedInfo info = JsonConvert.DeserializeObject<RunFinishedInfo>(data);
		player.runFinished = true;
		player.time = info.time;
	}
	
	[MessageHandler(ReceivingMessageType.LeftToMenu)]
	public static void PlayerLeft(ref Player player, string data) 
	{
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
		player.upAgainst.SendResponse(new(SendingMessageType.OtherPlayerForfeit, new MatchFoundResult() { playerName = player.name }));
	}
	
	[MessageHandler(ReceivingMessageType.RngSeed)]
	public static void RngSeed(ref Player player, string data) 
	{
		RngData rng = JsonConvert.DeserializeObject<RngData>(data);
		player.upAgainst.SendResponse(new Response(SendingMessageType.RngSeedSet, rng));
	}
}