namespace VapSRServer;

public enum ReceivingMessageType 
{
	UserInfo,
	StartMatchmaking,
	LoadingFinished,
	RouteStageFinished,
	RunFinished,
	LeftToMenu,
	RngSeed,
	Disconnect,
	CreatePrivateRoom,
	JoinPrivateRoom,
	PrivateRoomStart,
	CancelMatchmaking,
	LeavePrivateRoom
}

public enum SendingMessageType 
{
	MatchmakingStarted,
	MatchFound,
	StartRun,
	PlayerFinishedStage,
	RunStopped,
	OtherPlayerForfeit,
	RequestSeed,
	RngSeedSet,
	PrivateRoomCreated,
	PrivateRoomStarted,
	PrivateRoomJoinAttempt,
	ReplicateRoomData
}