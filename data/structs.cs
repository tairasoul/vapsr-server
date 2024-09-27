using System.Reflection;
using Newtonsoft.Json;
using TcpSharp;

namespace VapSRServer;

public class Player 
{
	public ConnectedClient client;
	public string UUID;
	public string name;
	public bool isInGame;
	public bool runStarted;
	public bool runFinished;
	public float time;
	public bool isLoaded;
	public bool matchmaking;
	public Player upAgainst;
	public void SendResponse(Response response) 
	{
		client.SendString(response.ToJson());
	}
}

public struct HandlerClassInfo 
{
	public MethodInfo handler;
	public MessageHandler attribute;
}

public struct MatchFoundResult 
{
	public string playerName;
}

public struct PlayerCompletedStage 
{
	public string playerName;
	public string stage;
}

public struct PlayerCompletedStageInfo 
{
	public string stage;
}

public struct RunFinishedInfo 
{
	public float time;
}

public struct RunFinishedRelayInfo 
{
	public string playerName;
	public float time;
	public bool youWon;
}

public struct UserInfo 
{
	public string username;
	//public string id;
}

public struct RngData 
{
	public int seed;
}

public struct Request {
	public string type;
	public string? data;
}

public struct Response 
{
	public Response(SendingMessageType sendType, object? body) 
	{
		type = sendType.ToString();
		data = body;
	}
	public Response(SendingMessageType sendType) 
	{
		type = sendType.ToString();
	}
	public string type;
	public object? data;
	
	public string ToJson() 
	{
		return $"{{\"type\":\"{type}\",\"data\":{JsonConvert.ToString(JsonConvert.SerializeObject(data))}}}";
	}
}

public struct RunConfigInfo 
{
	public string fileName;
	public string path;
	public string json;
}