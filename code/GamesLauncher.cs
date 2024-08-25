using Mini.Exceptions;
using Mini.Games;
using Mini.Players;
using Sandbox;
using Sandbox.Internal;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mini;

public class GamesLauncher : Component, Component.INetworkListener
{
    public static GamesLauncher? Instance { get; private set; }

    public MiniGame? CurrentGame { get; private set; }
    public GameInfo CurrentGameInfo { get; private set; }

    [Property]
    public GamesLoader GamesLoader { get; set; } = null!;
    [Property]
    public GamesVoter GamesVoter { get; set; } = null!;

    [Property]
    public int GamesCountToVote { get; set; } = 15;
    [Property]
    public float VotingTime { get; set; } = 20f;
    [Property, Description("Time to vote after all players voted.")]
    public float VotingEndTime { get; set; } = 3f;

    [Property]
    public float TimeBeforeStart { get; set; } = 30f;

    [Property]
    public float TimeAfterEnd { get; set; } = 15f;

    [Sync]
    public GameStatus GameStatus { get; private set; }

    [Sync]
    public TimeUntil TimeUntilGameStatusEnd { get; private set; }

    [Sync]
    public bool HasEnoughPlayers { get; private set; } = false;

    [Sync]
    protected NetList<ulong> NetWinners { get; private set; } = new();
    [Sync]
    protected NetList<ulong> NetPlayers { get; private set; } = new();


    public bool IsAllowedPlayAlone { get; private set; } = false;

    public ISet<ulong> Winners => NetWinners.ToHashSet();
    public ISet<ulong> Players => NetPlayers.ToHashSet();

    private TimeSince _timeSinceVotingStarted;
    private TimeSince _timeSinceAllPlayersVoted;
    private TimeSince _timeSinceEnoughPlayersConnected;
    private GameStatus _oldGameStatus;
    private bool _allPlayersVoted;



    protected override Task OnLoad()
    {
        if(Scene.IsEditor)
            return Task.CompletedTask;

        if(Instance.IsValid())
        {
            GameObject.Destroy();
            return Task.CompletedTask;
        }
        Instance = this;
        return Task.CompletedTask;
    }

    public void OnConnected(Connection channel)
    {
        DisallowPlayAlone();
    }

    public void OnDisconnected(Connection channel)
    {
        DisallowPlayAlone();
    }

    [Button("Try Allow Play Alone")]
    private void TryAllowPlayAloneBtn() => TryAllowPlayAlone();
    public bool TryAllowPlayAlone()
    {
        if(IsAllowedPlayAlone)
            return true;

        if(Connection.All.Count > 1)
            return false;

        if(GameStatus != GameStatus.None)
            return false;

        IsAllowedPlayAlone = true;
        GamesVoter.Clear();
        return true;
    }

    public void DisallowPlayAlone()
    {
        if(!IsAllowedPlayAlone)
            return;

        IsAllowedPlayAlone = false;
        GamesVoter.Clear();
    }

    [Button("StartRandomGame")]
    private void StartRandomGame()
    {
        var gameIndex = Game.Random.Next(GamesLoader.GamesCount);
        var gameId = GamesLoader.Games.Skip(gameIndex).First().GameId;
        StartGame(gameId);
    }

    public void StartGame(string gameId)
    {
        if(IsProxy)
            return;

        if(CurrentGame.IsValid())
            throw new InvalidOperationException("Another game already exists.");


        GamesVoter.Clear();

        var gameGameObject = GamesLoader.CloneGamePrefab(gameId, new CloneConfig(new global::Transform(), null, false));
        var gameInfo = GamesLoader.GetGameInfo(gameId);
        StartGameByGameObject(gameGameObject, gameInfo);
    }

    public void StartGame(GameObject gamePrefab, GameInfo gameInfo)
    {
        if(IsProxy)
            return;

        if(CurrentGame.IsValid())
            throw new InvalidOperationException("Another game already exists.");

        var gameGameObject = gamePrefab.Clone(new CloneConfig(new global::Transform(), null, false));
        StartGameByGameObject(gameGameObject, gameInfo);
    }

    private void StartGameByGameObject(GameObject gameGameObject, GameInfo gameInfo)
    {
        if(CurrentGame.IsValid())
            throw new InvalidOperationException("Another game already exists.");

        var game = gameGameObject.Components.Get<MiniGame>(true);

        if(!game.IsValid())
            throw new ComponentNotFoundException(gameGameObject, typeof(MiniGame));

        CurrentGame = game;
        CurrentGameInfo = gameInfo;

        gameGameObject.Enabled = true;
        gameGameObject.NetworkMode = NetworkMode.Object;
        gameGameObject.NetworkSpawn();

        game.Setup();
    }

    protected override void OnUpdate()
    {
        if(IsProxy)
            return;

        UpdateGameStartRequirements();

        if(!CurrentGame.IsValid())
        {
            _oldGameStatus = GameStatus;
            GameStatus = GameStatus.None;
            HandleVoting();
            return;
        }

        _oldGameStatus = GameStatus;
        GameStatus = CurrentGame.Status;

        UpdateGameLifetime();
    }

    [Button("Reset")]
    private void ResetGame()
    {
        if(CurrentGame is null)
            throw new InvalidOperationException("No game was presented.");

        if(CurrentGame.Status == GameStatus.Started)
            CurrentGame.Stop();

        CurrentGame.GameObject.Destroy();
        CurrentGame = null;
    }

    private void UpdateGameStartRequirements()
    {
        bool hasEnoughPlayers = Connection.All.Count >= Consts.MinPlayersToPlay;
        if(hasEnoughPlayers && !HasEnoughPlayers)
            _timeSinceEnoughPlayersConnected = 0;
        HasEnoughPlayers = hasEnoughPlayers;
    }

    private void UpdateGameLifetime()
    {
        if(GameStatus != _oldGameStatus)
        {
            OnGameStatusChanged(_oldGameStatus, GameStatus);
            _oldGameStatus = GameStatus;
        }

        if(CurrentGame!.Status == GameStatus.SetUp)
        {
            if(HasEnoughPlayers)
            {
                if(TimeUntilGameStatusEnd <= 0)
                    CurrentGame.Start();
            }
            else
            {
                ResetGame();
            }
        }
        else if(CurrentGame.Status == GameStatus.Stopped)
        {
            if(CurrentGame.TimeSinceStatusChanged >= TimeAfterEnd)
                ResetGame();
        }
    }

    private void OnGameStatusChanged(GameStatus oldStatus, GameStatus newStatus)
    {
        if(newStatus == GameStatus.SetUp)
        {
            var timeSinceStartRequirementsMet = Math.Min(CurrentGame!.TimeSinceStatusChanged, _timeSinceEnoughPlayersConnected);
            TimeUntilGameStatusEnd = TimeBeforeStart - timeSinceStartRequirementsMet;
        }
        else if(newStatus == GameStatus.Started)
        {
            NetWinners.Clear();
            NetPlayers.Clear();
            foreach(var playerSteamId in CurrentGame!.PlayingPlayers.Select(p => p.Network.OwnerConnection.SteamId))
                NetPlayers.Add(playerSteamId);
            OnGameStarted();
            TimeUntilGameStatusEnd = CurrentGame.MaxGameTime - CurrentGame.TimeSinceStatusChanged;
        }
        else if(newStatus == GameStatus.Stopped)
        {
            NetWinners.Clear();
            foreach(var winner in CurrentGame!.GetWinners())
                NetWinners.Add(winner);
            OnWinnersChosen();
            TimeUntilGameStatusEnd = TimeAfterEnd - CurrentGame.TimeSinceStatusChanged;
        }
    }

    private void HandleVoting()
    {
        if(!GamesVoter.IsVoting)
        {
            _allPlayersVoted = false;
            _timeSinceVotingStarted = 0;
            GamesVoter.Clear();

            bool playingAlone = IsAllowedPlayAlone && Connection.All.Count == 1;
            GamesVoter.ChooseGames(Math.Min(GamesCountToVote, GamesLoader.GamesCount), i => !playingAlone || i.GameInfo.CanPlayAlone);
        }

        TimeUntilGameStatusEnd = VotingTime - Math.Min(_timeSinceVotingStarted, _timeSinceEnoughPlayersConnected);

        bool allPlayersVoted = GamesVoter.Votes.Any() && Connection.All.Select(c => c.SteamId).ToHashSet().IsSubsetOf(GamesVoter.Votes.Select(v => v.Key));
        if(allPlayersVoted)
        {
            if(!_allPlayersVoted)
                _timeSinceAllPlayersVoted = 0;

            TimeUntilGameStatusEnd = Math.Min(TimeUntilGameStatusEnd, VotingEndTime - _timeSinceAllPlayersVoted);
        }
        _allPlayersVoted = allPlayersVoted;

        if(TimeUntilGameStatusEnd <= 0)
        {
            var choosedGame = GamesVoter.GetMostWantedGame();
            GamesVoter.Clear();
            StartGame(choosedGame);
        }
    }

    [Broadcast(NetPermission.OwnerOnly)]
    private void OnWinnersChosen()
    {
        if(NetPlayers.Count <= 1)
            return;

        if(NetWinners.Contains(Steam.SteamId))
            PlayerStats.Local.RegisterWin();

        PlayerStats.Local.UpdateWinRate();
    }

    [Broadcast(NetPermission.OwnerOnly)]
    private void OnGameStarted()
    {
        if(NetPlayers.Count <= 1)
            return;

        if(NetPlayers.Contains(Steam.SteamId))
            PlayerStats.Local.RegisterGame();
    }
}
