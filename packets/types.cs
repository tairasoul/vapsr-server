namespace VapSRServer.Networking.PacketTypes;

public enum C2STypes {
  RngSeed = -1,
  UserInfo = 0,
  StartMatchmaking = 1,
  LoadingFinished = 2,
  RouteStageFinished = 3,
  RunFinished = 4,
  LeftToMenu = 5,
  Disconnect = 6,
  CreatePrivateRoom = 7,
  JoinPrivateRoom = 8,
  PrivateRoomStart = 9,
  CancelMatchmaking = 10,
  LeavePrivateRoom = 11,
  RequestCurrentHost = 12
}

public enum S2CTypes {
  RngSeed = -1,
  MatchmakingStarted = 0,
  MatchFound = 1,
  StartRun = 2,
  PlayerFinishedStage = 3,
  RunStopped = 4,
  OtherPlayerForfeit = 5,
  RequestSeed = 6,
  PrivateRoomCreated = 7,
  PrivateRoomStarted = 8,
  PrivateRoomJoinAttempt = 9,
  ReplicateRoomData = 10,
  OpponentForfeit = 11,
  PrivateRoomRunFinished = 12,
  PrivateRoomBatchRunsFinished = 13,
  PrivateRoomEveryoneCompleted = 14,
  PrivateRoomNewHost = 15
}

