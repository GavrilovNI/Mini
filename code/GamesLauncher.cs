using Mini.Exceptions;
using Mini.Games;
using Sandbox;
using System;
using System.Linq;

namespace Mini;

public class GamesLauncher : Component
{
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

    [Property]
    public float TimeBeforeStart { get; set; } = 30f;

    [Property]
    public float TimeAfterEnd { get; set; } = 15f;

    [Property]
    private GameObject TestGamePrefab { get; set; } = null!;

    private const int MinPlayersToPlay = 1;

    [Sync]
    public GameStatus GameStatus { get; private set; }

    [Sync]
    public TimeUntil TimeUntilGameStatusEnd { get; private set; }

    [Sync]
    public bool HasEnoughPlayers { get; private set; } = false;

    private TimeSince _timeSinceVotingStarted;
    private TimeSince _timeSinceEnoughPlayersConnected;



    [Button("StartTestGame")]
    private void StartTestGame() => StartGame(TestGamePrefab, new GameInfo());

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

        var gameGameObject = GamesLoader.CloneGamePrefab(gameId, new CloneConfig(new global::Transform(), GameObject, false));
        var gameInfo = GamesLoader.GetGameInfo(gameId);
        StartGameByGameObject(gameGameObject, gameInfo);
    }

    public void StartGame(GameObject gamePrefab, GameInfo gameInfo)
    {
        if(IsProxy)
            return;

        if(CurrentGame.IsValid())
            throw new InvalidOperationException("Another game already exists.");

        var gameGameObject = gamePrefab.Clone(new CloneConfig(new global::Transform(), GameObject, false));
        StartGameByGameObject(gameGameObject, gameInfo);
    }

    private void StartGameByGameObject(GameObject gameGameObject, GameInfo gameInfo)
    {
        if(CurrentGame.IsValid())
            throw new InvalidOperationException("Another game already exists.");

        var game = gameGameObject.Components.Get<MiniGame>(true);

        if(!game.IsValid())
            throw new ComponentNotFoundException(GameObject, typeof(MiniGame));

        gameGameObject.Enabled = true;
        CurrentGame = game;
        CurrentGameInfo = gameInfo;

        gameGameObject.Enabled = true;
        gameGameObject.NetworkMode = NetworkMode.Object;
        gameGameObject.NetworkSpawn();

        game.Setup();
    }

    protected override void OnUpdate()
    {
        UpdateGameStartRequirements();

        if(!CurrentGame.IsValid())
        {
            GameStatus = GameStatus.None;
            HandleVoting();
            return;
        }

        GameStatus = CurrentGame.Status;

        if(Connection.All.Count < MinPlayersToPlay && GameStatus == GameStatus.SetUp)
        {
            if(CurrentGame.Status == GameStatus.Started)
                CurrentGame.Stop();
            return;
        }

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
        bool hasEnoughPlayers;
        if(CurrentGame.IsValid())
            hasEnoughPlayers = CurrentGame.PlayingPlayersCount >= MinPlayersToPlay;
        else
            hasEnoughPlayers = Connection.All.Count >= MinPlayersToPlay;

        if(hasEnoughPlayers && !HasEnoughPlayers)
            _timeSinceEnoughPlayersConnected = 0;
        HasEnoughPlayers = hasEnoughPlayers;
    }

    private void UpdateGameLifetime()
    {
        if(CurrentGame!.Status == GameStatus.SetUp && HasEnoughPlayers)
        {
            var timeSinceStartRequirementsMet = Math.Min(CurrentGame.TimeSinceStatusChanged, _timeSinceEnoughPlayersConnected);
            TimeUntilGameStatusEnd = TimeBeforeStart - timeSinceStartRequirementsMet;

            if(TimeUntilGameStatusEnd <= 0)
                CurrentGame.Start();
        }
        else if(CurrentGame.Status == GameStatus.Started)
        {
            TimeUntilGameStatusEnd = CurrentGame.MaxGameTime - CurrentGame.TimeSinceStatusChanged;
        }
        else if(CurrentGame.Status == GameStatus.Stopped)
        {
            TimeUntilGameStatusEnd = TimeAfterEnd - CurrentGame.TimeSinceStatusChanged;

            if(CurrentGame.TimeSinceStatusChanged >= TimeAfterEnd)
                ResetGame();
        }
    }

    private void HandleVoting()
    {
        if(!GamesVoter.IsVoting)
        {
            _timeSinceVotingStarted = 0;
            GamesVoter.ChooseGames(Math.Min(GamesCountToVote, GamesLoader.GamesCount));
        }

        TimeUntilGameStatusEnd = VotingTime - Math.Min(_timeSinceVotingStarted, _timeSinceEnoughPlayersConnected);

        if(TimeUntilGameStatusEnd <= 0)
        {
            var choosedGame = GamesVoter.GetMostWantedGame();
            GamesVoter.ClearGames();
            GamesVoter.ClearVotes();
            StartGame(choosedGame);
        }
    }
}
