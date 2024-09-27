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
	// afraid to implement right now
	// tired lol
	//CancelMatchmaking
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
	RngSeedSet
}