using MessagePack;

namespace VapSRServer.Networking.S2C;

[MessagePackObject(true)]
public struct PlayerCompletedStageS2C {
  public string playerName;
  public string stage;
}

[MessagePackObject(true)]
public struct RunS2C {
  public string name;
  public float time;
}

[MessagePackObject(true)]
public struct BatchRoomRunsFinishedS2C {
  public RunS2C[] times;
}

[MessagePackObject(true)]
public struct PrivateRoomNewHostS2C {
  public bool youAreNewHost;
}

[MessagePackObject(true)]
public struct RoomReplicationDataS2C {
  public string host;
  public string[] opponents;
  public string code;
}

[MessagePackObject(true)]
public struct RoomJoinAttemptS2C {
  public bool RoomJoined;
  public RoomReplicationDataS2C? replicationData;
}

[MessagePackObject(true)]
public struct RunFinishedS2C {
  public string playerName;
  public float time;
  public bool youWon;
}