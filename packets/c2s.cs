using MessagePack;

namespace VapSRServer.Networking.C2S;

[MessagePackObject(true)]
public struct PlayerCompletedStageC2S {
  public string stage;
}

[MessagePackObject(true)]
public struct RunFinishedC2S {
  public float time;
}

[MessagePackObject(true)]
public struct UserInfoC2S {
  public string username;
}