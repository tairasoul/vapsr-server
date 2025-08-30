using TcpSharp;
using VapSRServer.Data.Rooms;
using VapSRServer.Networking.Base;
using VapSRServer.Networking.PacketTypes;

namespace VapSRServer.Data.Players;

public class Player {
  public ConnectedClient client;
  public string UUID;
  public string name;
  public PrivateRoom? room;
  public event EventHandler<float> RunFinished;
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
			RunFinished -= (EventHandler<float>)d;
		}
  }

  public void SendResponse(S2CTypes messageType) 
  {
    SendResponse(messageType, null);
  }

  public void SendResponse(S2CTypes messageType, object? data) 
  {
    if (Server.Server.debug)
      Console.WriteLine($"Sending S2C packet of type {messageType}");
    client.SendBytes(new ServerResponse() { type = messageType, data = data }.Bytes());
  }
}