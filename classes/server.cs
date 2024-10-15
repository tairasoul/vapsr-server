using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProtoBuf;
using TcpSharp;

namespace VapSRServer;

public class ServerHandler 
{
	private TcpSharpSocketServer server;
	
	HandlerClassInfo[] info = [];
	
	public ServerHandler(int port) 
	{
		server = new(port);
		GrabHandlers();
		Task.Run(Matchmaker.MatchmakingLoop);
		Task.Run(Matchmaker.ReadyLoop);
		Task.Run(Matchmaker.MatchDoneLoop);
		Task.Run(Matchmaker.RoomReadyLoop);
	}
	
	private static async Task KeepAlive(ConnectedClient client) 
	{
		await Task.Delay(2000);
		client.SendString("k");
	}
	
	private async Task KeepAliveOperations() 
	{
		while (true) 
		{
			await Task.Delay(500);
			foreach (Player player in PlayerPool.players) 
				KeepAlive(player.client);
			await Task.Run(PruneDeadClients);
		}
	}
	
	private void Room_OpponentForfeit(PrivateRoom room, Player player) 
	{
		
	}
	
	private async Task PruneDeadClients() 
	{
		await Task.Delay(1700);
		foreach (Player player in PlayerPool.players) 
		{
			long lastResponseTime = player.lastResponseTime;
			if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastResponseTime > 10) 
			{
				Console.WriteLine($"Player {player.name} is likely disconnected (last response time is over 10 seconds ago). Disconnecting their socket connection.");
				if (player.upAgainst != null) 
				{
					Console.WriteLine($"Telling {player.name}'s opponent they have forfeited.");
					player.upAgainst.SendResponse(SendingMessageType.OtherPlayerForfeit, new MatchFoundResult() { playerName = player.name });
					server.Disconnect(player.UUID);
					return;
				}
				if (player.inRoom) 
				{
					Console.WriteLine($"Telling {player.name}'s opponents they have forfeited.");
					Room_OpponentForfeit(player.room, player);
					return;
				}
				server.Disconnect(player.UUID);
			}
		}
	}
	
	public async Task Start() 
	{
		Task.Run(KeepAliveOperations);
		server.OnConnected += (object sender, OnServerConnectedEventArgs args) =>
		{
			Player player = new() 
			{
				isInGame = false,
				isLoaded = false,
				runStarted = false,
				matchmaking = false,
				runFinished = false,
				inRoom = false,
				UUID = args.ConnectionId,
				client = server.GetClient(args.ConnectionId)
			};
			PlayerPool.players = [ .. PlayerPool.players, player ];
			Console.WriteLine($"Player connected, uuid {args.ConnectionId}");
		};
		
		server.OnDisconnected += (object sender, OnServerDisconnectedEventArgs args) =>
		{
			Console.WriteLine($"Player disconnected, uuid {args.ConnectionId}");
			PlayerPool.players = PlayerPool.players.Where(player => player.UUID != args.ConnectionId).ToArray();
		};
		
		server.OnDataReceived += (object sender, OnServerDataReceivedEventArgs args) =>
		{
			string uuid = args.ConnectionId;
			Player? player = PlayerPool.players.FirstOrDefault((v) => v.UUID == uuid);
			if (player != null)
				player.lastResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (Encoding.UTF8.GetString(args.Data) == "k")
				return;
			Console.WriteLine($"Player sent data, username {player?.name} uuid {uuid}");
			Request dataS = Serializer.Deserialize<Request>(args.Data.AsMemory());
			string data = JsonSerializer.Serialize(dataS);
			Console.WriteLine($"Data: {data}");
			HandleData(uuid, args.Data);
		};
		
		server.OnError += (object sender, OnServerErrorEventArgs args) => 
		{
			Console.Error.WriteLine(args.Exception.Source);
			Console.Error.WriteLine(args.Exception.Message);
			Console.Error.WriteLine(args.Exception.StackTrace);
			Console.Error.WriteLine(args.Exception.InnerException.Source);
			Console.Error.WriteLine(args.Exception.InnerException.Message);
			Console.Error.WriteLine(args.Exception.InnerException.StackTrace);
		};
		
		server.StartListening();
		await Task.Delay(Timeout.Infinite);
	}
	
	private void GrabHandlers() 
	{
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types) 
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			foreach (MethodInfo method in methods)
			{
				MessageHandler? attribute = method.GetCustomAttribute<MessageHandler>();
				if (attribute != null)
				{
					info = [ .. info, new HandlerClassInfo() 
					{
						handler = method,
						attribute = attribute
					}];
				}
			}
		}
	}
	
	private void HandleData(string uuid, byte[] data) 
	{
		/*MessagePackSerializerOptions opts = MessagePackSerializerOptions.Standard.WithResolver(
			MessagePack.Resolvers.CompositeResolver.Create(
				new IMessagePackFormatter[] { new RequestFormatter() },
				new[] { MessagePack.Resolvers.StandardResolver.Instance }
			)
		).WithCompression(MessagePackCompression.Lz4Block);*/
		//MessagePackSerializerOptions opts = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
		Request request = Serializer.Deserialize<Request>(data.AsMemory());
		Console.WriteLine($"Request type: {request.type}");
		if (request.type == "Disconnect") 
		{
			server.Disconnect(uuid);
			return;
		}
		foreach (HandlerClassInfo classInfo in info) 
		{
			if (classInfo.attribute.type.ToString() == request.type) 
			{
				Player player = PlayerPool.players.First((v) => v.UUID == uuid);
				classInfo.handler.Invoke(null, [player, request.data]);
				break;
			}
		}
	}
}
