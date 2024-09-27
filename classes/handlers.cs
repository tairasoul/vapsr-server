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
}