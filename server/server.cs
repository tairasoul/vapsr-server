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
  private static byte KEEPALIVE = Convert.ToByte(0x91);
  internal static bool debug;
  Dictionary<C2STypes, MethodInfo> info = [];
  public Server(int port, bool debug) {
    server = new(port)
    {
      KeepAlive = true,
      KeepAliveTime = 10,
      KeepAliveInterval = 1,
      KeepAliveRetryCount = 5
    };
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

  private void GrabHandlers() {
    Handlers.RoomInstance = rooms;
    Handlers.MatchmakingInstance = matchmaker;
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
      matchmaker.pool.newPlayer.Invoke(this, player);
      if (debug)
				Console.WriteLine($"Player connected, uuid {args.ConnectionId}");
    };
		
		server.OnDisconnected += (_, args) =>
		{
      Player player = matchmaker.pool.GetPlayer(args.ConnectionId);
			if (debug)
				Console.WriteLine($"Player {player.name ?? player.UUID} disconnected");
      if (player != null)
        matchmaker.pool.playerDisconnected.Invoke(this, player);
    };

    server.OnDataReceived += (_, args) =>
    {
      string uuid = args.ConnectionId;
      Player player = matchmaker.pool.GetPlayer(uuid);
      if (player == null) return;
			if (debug)
				Console.WriteLine($"Player {player.name ?? uuid} sent data");
      MessagePackSerializerOptions opts = MessagePackSerializerOptions.Standard.WithResolver(
        MessagePack.Resolvers.CompositeResolver.Create(
          [new ClientRequestFormatter(), new ServerResponseFormatter()],
          [MessagePack.Resolvers.StandardResolver.Instance]
        )
      ).WithCompression(MessagePackCompression.Lz4Block);
      ClientRequest dataS = MessagePackSerializer.Deserialize<ClientRequest>(args.Data, opts);
      if (debug)
        Console.WriteLine($"Received C2S packet of type {dataS.type}");
      HandleData(player, dataS);
    };

    server.OnError += (_, args) =>
    {
			Console.Error.WriteLine(args.Exception.Source);
			Console.Error.WriteLine(args.Exception.Message);
			Console.Error.WriteLine(args.Exception.StackTrace);
      if (args.Exception.InnerException != null) {
        Console.Error.WriteLine(args.Exception.InnerException.Source);
        Console.Error.WriteLine(args.Exception.InnerException.Message);
        Console.Error.WriteLine(args.Exception.InnerException.StackTrace);
      }
    };

    server.OnDisconnected += (_, args) =>
    {
      string uuid = args.ConnectionId;
      Player player = matchmaker.pool.GetPlayer(uuid);
      if (player != null) {
        matchmaker.pool.playerDisconnected.Invoke(this, player);
        if (player.upAgainst != null) {
          Player u = player.upAgainst;
          u.SendResponse(S2CTypes.OtherPlayerForfeit, new PlayerResultCommon() { playerName = player.name });
        }
        else if (player.inRoom) {
          PrivateRoom room = player.room;
          if (room.host == player) {
            Handlers.HostLeft(room);
          }
          else {
            room.connected = [.. room.connected.Where((v) => v.UUID != player.UUID)];
            Handlers.UpdateRoomData(room);
          }
          Player[] players = [room.host, .. room.connected];
          foreach (Player plr in players)
            plr.SendResponse(S2CTypes.OpponentForfeit, new PlayerResultCommon() { playerName = player.name });
        }
      }
    };

    server.StartListening();
    await Task.Delay(Timeout.Infinite);
  }
}