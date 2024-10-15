using System.Reflection;
using TcpSharp;
using ProtoBuf;

namespace VapSRServer;

public class Player 
{
	public ConnectedClient client;
	public string UUID;
	public string name;
	public PrivateRoom room;
	public event EventHandler RunFinished;
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
			RunFinished -= (EventHandler)d;
		}
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

[ProtoContract]
public class MatchFoundResult 
{
	[ProtoMember(1)]
	public string playerName {get; set;}
}

[ProtoContract]
public class PlayerCompletedStage 
{
	[ProtoMember(1)]
	public string playerName {get; set;}
	[ProtoMember(2)]	
	public string stage {get; set;}
}

[ProtoContract]
public class PlayerCompletedStageInfo 
{
	[ProtoMember(1)]
	public string stage {get; set;}
}

[ProtoContract]
public class RunFinishedInfo 
{
	[ProtoMember(1)]
	public float time {get; set;}
}

[ProtoContract]
public class RunFinishedRelayInfo 
{
	[ProtoMember(1)]
	public string playerName {get; set;}
	[ProtoMember(2)]
	public float time {get; set;}
	[ProtoMember(3)]
	public bool youWon {get; set;}
}

[ProtoContract]
public class UserInfo 
{
	[ProtoMember(1)]
	public string username {get; set;}
	//public string id;
}

[ProtoContract]
public class RngData 
{
	[ProtoMember(1)]
	public int seed {get; set;}
}

[ProtoContract]
public class Request 
{
	[ProtoMember(1)]
	public string type {get; set;}
	[ProtoMember(2)]
	private byte[]? _data {get; set;}
	[ProtoIgnore]
	public object? data
	{ 
		get => DeserializeData(); 
		set => _data = value != null ? SerializeData(value) : null; 
	}
	private byte[]? SerializeData(object data)
	{
		using MemoryStream stream = new();
		Serializer.Serialize(stream, data);
		return stream.ToArray();
	}
	private object? DeserializeData()
	{
		if (_data == null)
			return null;
		using var stream = new MemoryStream(_data);
		return type switch
		{
			"UserInfo" => Serializer.Deserialize<UserInfo>(stream),
			"RouteStageFinished" => Serializer.Deserialize<PlayerCompletedStageInfo>(stream),
			"RngSeed" => Serializer.Deserialize<RngData>(stream),
			"RunFinished" => Serializer.Deserialize<RunFinishedInfo>(stream),
			"JoinPrivateRoom" => Serializer.Deserialize<RoomData>(stream),
			_ => null,
		};
	}
}

[ProtoContract]
public class Response 
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
	[ProtoMember(1)]
	public string type {get; set;}
	[ProtoMember(2)]
	private byte[]? _data {get; set;}
	[ProtoIgnore]
	public object? data
	{ 
		set => _data = value != null ? SerializeData(value) : null; 
	}
	private byte[]? SerializeData(object data)
	{
		using MemoryStream stream = new();
		Serializer.Serialize(stream, data);
		return stream.ToArray();
	}
	
	public byte[] Bytes() 
	{
		using MemoryStream stream = new();
		Serializer.Serialize(stream, this);
		return stream.ToArray();
	}
}

[ProtoContract]
public class RoomData 
{
	[ProtoMember(1)]
	public string code {get; set;}
}

[ProtoContract]
public class RoomReplicationData 
{
	[ProtoMember(1)]
	public string host {get; set;}
	[ProtoMember(2)]
	public string[] opponents {get; set;}
	[ProtoMember(3)]
	public string code {get; set;}
}

[ProtoContract]
public class RoomJoinAttempt 
{
	[ProtoMember(1)]
	public bool RoomJoined {get; set;}
	[ProtoMember(2)]
	public RoomReplicationData? replicationData {get; set;}
}

[ProtoContract]
public class Run 
{
	[ProtoMember(1)]
	public string name {get; set;}
	[ProtoMember(2)]
	public float time {get; set;}
}

[ProtoContract]
public class BatchRoomRunsFinished 
{
	[ProtoMember(1)]
	public Run[] times {get; set;}
}

[ProtoContract]
public class RoomRunFinished 
{
	[ProtoMember(1)]
	public string player {get; set;}
	[ProtoMember(2)]
	public float time {get; set;}
}

[ProtoContract]
public class RunConfigInfo 
{
	[ProtoMember(1)]
	public string fileName {get; set;}
	[ProtoMember(2)]
	public string path {get; set;}
	[ProtoMember(3)]
	public string json {get; set;}
}