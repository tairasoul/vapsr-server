using System.Reflection;
using Newtonsoft.Json;
using TcpSharp;
using MessagePack;
namespace VapSRServer;

public class Player 
{
	public ConnectedClient client;
	public string UUID;
	public string name;
	public PrivateRoom room;
	public long lastResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	public bool inRoom;
	public bool isInGame;
	public bool runStarted;
	public bool runFinished;
	public float time;
	public bool isLoaded;
	public bool matchmaking;
	public Player upAgainst;
	public void SendResponse(Response response) 
	{
		client.SendBytes(response.Bytes());
	}
}

public class PrivateRoom 
{
	public Player player1;
	public Player player2;
	public string code;
}

public struct HandlerClassInfo 
{
	public MethodInfo handler;
	public MessageHandler attribute;
}

[MessagePackObject(true)]
public class MatchFoundResult 
{
	public MatchFoundResult() { }
	public string playerName;
}

[MessagePackObject(true)]
public class PlayerCompletedStage 
{
	public PlayerCompletedStage() { }
	public string playerName;
	public string stage;
}

[MessagePackObject(true)]
public class PlayerCompletedStageInfo 
{
	public PlayerCompletedStageInfo() { }
	public string stage;
}

[MessagePackObject(true)]
public class RunFinishedInfo 
{
	public RunFinishedInfo() { }
	public float time;
}

[MessagePackObject(true)]
public class RunFinishedRelayInfo 
{
	public RunFinishedRelayInfo() { }
	public string playerName;
	public float time;
	public bool youWon;
}

[MessagePackObject(true)]
public class UserInfo 
{
	public UserInfo() { }
	public string username;
	//public string id;
}

[MessagePackObject(true)]
public class RngData 
{
	public RngData() { }
	public int seed;
}

[MessagePackObject(true)]
public class Request 
{
	public Request() { }
	public string type;
	public object? data;
}

[MessagePackObject(true)]
public class Response 
{
	
	public Response() { }
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
	
	public byte[] Bytes() 
	{
		return MessagePackSerializer.Serialize(this);
	}
}

[MessagePackObject(true)]
public class RoomData 
{
	public RoomData() {}
	public string code;
}

[MessagePackObject(true)]
public class RoomReplicationData 
{
	public RoomReplicationData() {}
	public string localPlayerName;
	public string opponentName;
	public string code;
}

[MessagePackObject(true)]
public class RoomJoinAttempt 
{
	public RoomJoinAttempt() {}
	public bool RoomJoined;
	public RoomReplicationData? replicationData;
}

[MessagePackObject(true)]
public class RunConfigInfo 
{
	public RunConfigInfo() {}
	public string fileName;
	public string path;
	public string json;
}