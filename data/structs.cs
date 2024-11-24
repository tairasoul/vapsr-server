using System.Reflection;
using TcpSharp;
using MessagePack;

namespace VapSRServer;

public class RunFinishedArgs : EventArgs 
{
	public float time;
}

public class Player 
{
	public ConnectedClient client;
	public string UUID;
	public string name;
	public PrivateRoom room;
	public event EventHandler<RunFinishedArgs> RunFinished;
	public long lastResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	public bool inRoom;
	public bool isInGame;
	public bool runStarted;
	public bool runFinished;
	public float time;
	public bool isLoaded;
	public bool matchmaking;
	public Player upAgainst;
	public void ClearFinishedListeners() 
	{
		foreach (Delegate d in RunFinished.GetInvocationList()) {
			RunFinished -= (EventHandler<RunFinishedArgs>)d;
		}
	}
	public void RunCompleted() 
	{
		RunFinished.Invoke(this, new RunFinishedArgs() { time = time });
	}
	public void SendResponse(Response response) 
	{
		client.SendBytes(response.Bytes());
	}
	public void SendResponse(SendingMessageType messageType, object? data) 
	{
		client.SendBytes(new Response(messageType, data).Bytes());
	}
	public void SendResponse(SendingMessageType messageType) 
	{
		client.SendBytes(new Response(messageType).Bytes());
	}
}

public class PrivateRoom 
{
	public Player host;
	public Player[] connected;
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
	public MatchFoundResult() {}
	public string playerName;
}

[MessagePackObject(true)]
public class PlayerCompletedStage 
{
	public PlayerCompletedStage() {}
	public string playerName;
	public string stage;
}

[MessagePackObject(true)]
public class PlayerCompletedStageInfo 
{
	public PlayerCompletedStageInfo() {}
	public string stage;
}

[MessagePackObject(true)]
public class RunFinishedInfo 
{
	public RunFinishedInfo() {}
	public float time;
}

[MessagePackObject(true)]
public class RunFinishedRelayInfo 
{
	public RunFinishedRelayInfo() {}
	public string playerName;
	public float time;
	public bool youWon;
}

[MessagePackObject(true)]
public class UserInfo 
{
	public UserInfo() {}
	public string username;
	//public string id;
}

[MessagePackObject(true)]
public class RngData 
{
	public RngData() {}
	public int seed;
}

[MessagePackObject(true)]
public class Request 
{
	public Request() {}
	public string type;
	public object? data;
}

[MessagePackObject(true)]
public class Response 
{
	public Response() {}
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
		MessagePackSerializerOptions opts = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
		return MessagePackSerializer.Serialize(this, opts);
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
	public string host;
	public string[] opponents;
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
public class Run 
{
	public Run() {}
	public string name;
	public float time;
}

[MessagePackObject(true)]
public class BatchRoomRunsFinished 
{
	public BatchRoomRunsFinished() {}
	public Run[] times;
}

[MessagePackObject(true)]
public class RoomRunFinished 
{
	public RoomRunFinished() {}
	public string player;
	public float time;
}

[MessagePackObject(true)]
public class RunConfigInfo 
{
	public RunConfigInfo() {}
	public string fileName;
	public string path;
	public string json;
}