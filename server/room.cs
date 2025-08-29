using VapSRServer.Data.Players;

namespace VapSRServer.Data.Rooms;

public class PrivateRoom {
  public string code;
  public Player[] connected;
  public Player host;
}