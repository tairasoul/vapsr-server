using VapSRServer.Data.Players;
using VapSRServer.Data.Rooms;
using VapSRServer.Networking.Common;
using VapSRServer.Networking.PacketTypes;
using VapSRServer.Networking.S2C;
using VapSRServer.Extensions;

namespace VapSRServer.Server;

class PlayerPool {
  public Player[] lobby = [];
  public Player[] matchmaking = [];
  public Player[] busy = [];
  public EventHandler<Player> newPlayer;
  public EventHandler<Player> playerStartedMatchmaking;
  public EventHandler<Player> playerStoppedMatchmaking;
  public EventHandler<(Player, float)> playerCompletedRun;
  public EventHandler<Player> playerLoaded;
  public EventHandler<Player> playerBusy;
  public EventHandler<Player> playerNotBusy;
  public EventHandler<Player> playerDisconnected;

  public PlayerPool()
  {
    PlayerPool s = this;
    newPlayer += (_, player) =>
    {
      if (s.lobby.FirstOrDefault((Player v) => v.UUID == player.UUID, null) == null)
        s.lobby = [..s.lobby, player];
    };
    playerStartedMatchmaking += (_, player) =>
    {
      player.matchmaking = true;
      s.lobby = [..s.lobby.Where((v) => v.UUID != player.UUID)];
      if (s.matchmaking.FirstOrDefault((Player v) => v.UUID == player.UUID, null) == null)
        s.matchmaking = [.. s.matchmaking, player];
    };
    playerStoppedMatchmaking += (_, player) =>
    {
      player.matchmaking = false;
      s.matchmaking = [..s.matchmaking.Where((v) => v.UUID != player.UUID)];
      if (s.lobby.FirstOrDefault((Player v) => v.UUID == player.UUID, null) == null)
        s.lobby = [.. s.lobby, player];
    };
    playerCompletedRun += (_, tuple) =>
    {
      Player player = tuple.Item1;
      float time = tuple.Item2;
      player.runFinished = true;
      player.time = time;
    };
    playerLoaded += (_, player) =>
    {
      player.isLoaded = true;
    };
    playerBusy += (_, player) =>
    {
      s.lobby = [..s.lobby.Where((v) => v.UUID != player.UUID)];
      if (s.busy.FirstOrDefault((Player v) => v.UUID == player.UUID, null) == null)
        s.busy = [.. s.busy, player];
    };
    playerNotBusy += (_, player) =>
    {
      s.busy = [..s.busy.Where((v) => v.UUID != player.UUID)];
      if (s.lobby.FirstOrDefault((Player v) => v.UUID == player.UUID, null) == null)
        s.lobby = [.. s.lobby, player];
    };
    playerDisconnected += (_, player) =>
    {
      s.busy = [.. s.busy.Where((v) => v.UUID != player.UUID)];
      s.lobby = [.. s.lobby.Where((v) => v.UUID != player.UUID)];
      s.matchmaking = [..s.matchmaking.Where((v) => v.UUID != player.UUID)];
    };
  }

  public Player GetPlayer(string uuid) {
    foreach (Player player in lobby) {
      if (player.UUID == uuid)
        return player;
    }
    foreach (Player player in matchmaking) {
      if (player.UUID == uuid)
        return player;
    }
    foreach (Player player in busy) {
      if (player.UUID == uuid)
        return player;
    }
    return null;
  }
}

class Matchmaker {
  public PlayerPool pool;

  public Matchmaker() {
    pool = new();
    Setup();
  }

  void VersusPass() {
    if (pool.matchmaking.Length > 1) {
      (Player, Player)[] pairs = [];
      (Player, Player) backlog = (null, null);
      Random.Shared.Shuffle(pool.matchmaking);
      foreach (Player player in pool.matchmaking) {
        if (backlog.Item1 == null) {
          backlog.Item1 = player;
        }
        else if (backlog.Item2 == null) {
          backlog.Item2 = player;
        }
        if (backlog.Item1 != null && backlog.Item2 != null) {
          pairs = [.. pairs.Append(backlog)];
          backlog.Item1 = null;
          backlog.Item2 = null;
        }
      }
      foreach ((Player, Player) pair in pairs) {
        Player p1 = pair.Item1;
        Player p2 = pair.Item2;
        pool.playerBusy.Invoke(this, p1);
        pool.playerBusy.Invoke(this, p2);
        p1.matchmaking = false;
        p2.matchmaking = false;
        p1.isInGame = true;
        p2.isInGame = true;
        PlayerResultCommon r1 = new() { playerName = p2.name };
        PlayerResultCommon r2 = new() { playerName = p1.name };
        p1.upAgainst = p2;
        p2.upAgainst = p1;
        p1.SendResponse(S2CTypes.MatchFound, r1);
        p1.SendResponse(S2CTypes.RequestSeed);
        p2.SendResponse(S2CTypes.MatchFound, r2);
      }
    }
  }

  void RunFinishCheck(Player player) {
    if (player.upAgainst.runFinished) {
      Player p1 = player;
      Player p2 = player.upAgainst;
      RunFinishedS2C i1 = new() {
        playerName = p2.name,
        time = p2.time,
        youWon = p1.time < p2.time
      };
      RunFinishedS2C i2 = new()
      {
        playerName = p1.name,
        time = p1.time,
        youWon = p2.time < p1.time
      };
      p1.upAgainst = null;
      p2.upAgainst = null;
      p1.isInGame = false;
      p2.isInGame = false;
      p1.isLoaded = false;
      p2.isLoaded = false;
      p1.runFinished = false;
      p2.runFinished = false;
      p1.runStarted = false;
      p2.runFinished = false;
      p1.time = 0f;
      p2.time = 0f;
      p1.SendResponse(S2CTypes.RunStopped, i1);
      p2.SendResponse(S2CTypes.RunStopped, i2);
      pool.playerNotBusy.Invoke(this, p1);
      pool.playerNotBusy.Invoke(this, p2);
    }
  }

  void RoomRunFinishedPass(PrivateRoom room) {
    Player[] players = [room.host, .. room.connected];
    bool allFinished = players.All((v) => v.runFinished);
    if (allFinished)
      foreach (Player plr in players)
        plr.SendResponse(S2CTypes.PrivateRoomEveryoneCompleted);
  }

  void RoomLoadedPass(PrivateRoom room) {
    Player[] players = [room.host, .. room.connected];
    bool allReady = players.All((v) => v.isLoaded);
    if (allReady) {
      foreach (Player player in players) {
        if (player.runStarted)
          continue;
        player.runStarted = true;
        player.SendResponse(S2CTypes.StartRun);
        player.RunFinished += (_, time) =>
        {
          pool.playerCompletedRun.Invoke(this, (player, time));
        };
      }
    }
  }

  void LoadedCheck(Player loaded) {
    if (loaded.upAgainst.isLoaded) {
      loaded.runStarted = true;
      loaded.upAgainst.runStarted = true;
      loaded.SendResponse(S2CTypes.StartRun);
      loaded.upAgainst.SendResponse(S2CTypes.StartRun);
    }
  }

  void PlayerLoaded(Player player) {
    if (player.inRoom) {
      RoomLoadedPass(player.room);
    }
    else {
      LoadedCheck(player);
    }
  }

  void RunCompleted(Player player) {
    if (player.inRoom) {
      RoomRunFinishedPass(player.room);
    }
    else {
      RunFinishCheck(player);
    }
  }

  void Setup() {
    pool.playerStartedMatchmaking += (_, __) => VersusPass();
    pool.playerLoaded += (_, player) => PlayerLoaded(player);
    pool.playerCompletedRun += (_, tuple) => RunCompleted(tuple.Item1);
  }
}