using System.Reflection;
using System.Text;
using MessagePack;
using TcpSharp;
using VapSRServer.Attributes;
using VapSRServer.Data.Players;
using VapSRServer.Data.Rooms;
using VapSRServer.Networking.Base;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;

namespace VapSRServer.Server;

public class Server {
  private TcpSharpSocketServer server;
  private Matchmaker matchmaker;
  private Rooms rooms;
  private static byte KEEPALIVE = Convert.ToByte(0x91054);
  internal static bool debug;
  Dictionary<C2STypes, MethodInfo> info = [];
  public Server(int port, bool debug) {
    server = new(port);
    Server.debug = debug;
    matchmaker = new();
    rooms = new();
    GrabHandlers();
  }

  private void RoomOpponentForfeit(PrivateRoom room, Player player) {
    player.ClearFinishedListeners();
    server.Disconnect(player.UUID);
    matchmaker.pool.playerDisconnected.Invoke(null, player);
    if (room.host == player)
      Handlers.HostLeft(room);
    Player[] players = [room.host, .. room.connected];
    foreach (Player plr in players)
      plr.SendResponse(S2CTypes.OpponentForfeit, new PlayerResultCommon() { playerName = player.name });
  }

  private async Task KeepAliveOps() {
    while (true) {
      await Task.Delay(500);
      Player[] players = [.. matchmaker.pool.lobby, ..matchmaker.pool.matchmaking, ..matchmaker.pool.busy];
      foreach (Player player in players) {
        if (debug)
          Console.WriteLine($"Sending KeepAlive to {player.name ?? player.UUID}");
        player.client.SendBytes([Server.KEEPALIVE]);
      }
      await Task.Delay(500);
      foreach (Player player in players) {
        long lastResponseTime = player.lastResponseTime;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastResponseTime > 10)
        {
          Console.WriteLine($"Player {player.name} is likely disconnected (last response time is over 10 seconds ago). Disconnecting their socket connection.");
          if (player.upAgainst != null) {
            Console.WriteLine($"Telling {player.name}'s opponent they have forfeited.");
            player.upAgainst.SendResponse(S2CTypes.OtherPlayerForfeit, new PlayerResultCommon() { playerName = player.name });
            server.Disconnect(player.UUID);
            return;
          }
          if (player.inRoom) 
          {
            Console.WriteLine($"Telling {player.name}'s opponents they have forfeited.");
            RoomOpponentForfeit(player.room, player);
            return;
          }
          server.Disconnect(player.UUID);
          }
      }
    }
  }

  private void GrabHandlers() {
    Type[] types = Assembly.GetExecutingAssembly().GetTypes();
    foreach (Type type in types) {
			MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
      foreach (MethodInfo method in methods) {
        MessageHandler? attribute = method.GetCustomAttribute<MessageHandler>();
        if (attribute != null) {
          info.Add(attribute.type, method);
        }
      }
    }
  }

  private void HandleData(Player player, ClientRequest request) {
    if (request.type == C2STypes.Disconnect) {
      server.Disconnect(player.UUID);
      return;
    }
    if (info.TryGetValue(request.type, out MethodInfo handler)) {
      handler.Invoke(null, [player, request.data]);
    }
  }

  public async Task Start() {
    Task.Run(KeepAliveOps);
    server.OnConnected += (_, args) =>
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
      matchmaker.pool.newPlayer.Invoke(null, player);
			if (debug)
				Console.WriteLine($"Player connected, uuid {args.ConnectionId}");
    };
		
		server.OnDisconnected += (_, args) =>
		{
      Player player = matchmaker.pool.GetPlayer(args.ConnectionId);
			if (debug)
				Console.WriteLine($"Player {player.name ?? player.UUID} disconnected");
      if (player != null)
        matchmaker.pool.playerDisconnected.Invoke(null, player);
    };

    server.OnDataReceived += (_, args) =>
    {
      string uuid = args.ConnectionId;
      Player player = matchmaker.pool.GetPlayer(uuid);
      if (player == null) return;
      player.lastResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      if (args.Data.SequenceEqual([KEEPALIVE]))
        return;
			if (debug)
				Console.WriteLine($"Player {player.name ?? uuid} sent data");
      MessagePackSerializerOptions opts = MessagePackSerializerOptions.Standard.WithResolver(
        MessagePack.Resolvers.CompositeResolver.Create(
          [new ClientRequestFormatter(), new ServerResponseFormatter()],
          [MessagePack.Resolvers.StandardResolver.Instance]
        )
      );
      ClientRequest dataS = MessagePackSerializer.Deserialize<ClientRequest>(args.Data, opts);
      HandleData(player, dataS);
    };
  }
}