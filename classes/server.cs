using System.Reflection;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using TcpSharp;

namespace VapSRServer;

public class ServerHandler 
{
	private TcpSharpSocketServer server;
	
	HandlerClassInfo[] info = [];
	
	public ServerHandler(int port) 
	{
		server = new(port) 
		{
			KeepAlive = true,
			KeepAliveInterval = 1,
			KeepAliveRetryCount = 5
		};
		GrabHandlers();
		Task.Run(Matchmaker.MatchmakingLoop);
		Task.Run(Matchmaker.ReadyLoop);
		Task.Run(Matchmaker.MatchDoneLoop);
	}
	
	public async Task Start() 
	{
		server.OnConnected += (object sender, OnServerConnectedEventArgs args) =>
		{
			Player player = new() 
			{
				isInGame = false,
				isLoaded = false,
				runStarted = false,
				matchmaking = false,
				runFinished = false,
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
			Console.WriteLine($"Player sent data, uuid {args.ConnectionId}");
			string data = Encoding.UTF8.GetString(args.Data);
			Console.WriteLine($"Data: {data}");
			string uuid = args.ConnectionId;
			HandleData(uuid, data);
		};
		
		server.OnError += (object sender, OnServerErrorEventArgs args) => 
		{
			Console.Error.WriteLine(args.Exception);
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
	
	private void HandleData(string uuid, string data) 
	{
		Request request = JsonConvert.DeserializeObject<Request>(data);
		Console.WriteLine($"Request type: {request.type}");
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