using Mini.Exceptions;
using Mini.Games;
using Sandbox;
using System;

namespace Mini;

public class GamesLauncher : Component
{
    public MiniGame? CurrentGame { get; private set; }

    [Property]
    public float TimeBeforeStart { get; set; } = 30f;

    [Property]
    public float TimeAfterEnd { get; set; } = 15f;

    [Property]
    private GameObject TestGamePrefab { get; set; } = null!;

    private const int MinPlayersToPlay = 1;
    private bool _hasEnoughPlayers = false;
    private TimeSince _timeSinceEnoughPlayersConnected;


    [Button("StartTestGame")]
    private void StartTestGame() => StartGame(TestGamePrefab);

    public void StartGame(GameObject gamePrefab)
    {
        if(CurrentGame.IsValid())
            throw new InvalidOperationException("Another game already exists.");

        var gameGameObject = gamePrefab.Clone(new CloneConfig(new global::Transform(), GameObject, false));
        var game = gameGameObject.Components.Get<MiniGame>(true);

        if(!game.IsValid())
            throw new ComponentNotFoundException(GameObject, typeof(MiniGame));

        gameGameObject.Enabled = true;
        CurrentGame = game;

        gameGameObject.Enabled = true;
        gameGameObject.NetworkMode = NetworkMode.Object;
        gameGameObject.NetworkSpawn();

        game.Setup();
    }

    protected override void OnUpdate()
    {
        if(!CurrentGame.IsValid())
            return;

        if(CurrentGame.Status == GameStatus.SetUp)
        {
            var hasEnoughPlayers = CurrentGame.PlayingPlayersCount >= MinPlayersToPlay;
            if(hasEnoughPlayers && !_hasEnoughPlayers)
                _timeSinceEnoughPlayersConnected = 0;
            _hasEnoughPlayers = hasEnoughPlayers;

            if(CurrentGame.TimeSinceStatusChanged >= TimeBeforeStart &&
                _hasEnoughPlayers &&
                _timeSinceEnoughPlayersConnected >= TimeBeforeStart
                )
            {
                CurrentGame.Start();
            }
        }
        else if(CurrentGame.Status == GameStatus.Stopped)
        {
            if(CurrentGame.TimeSinceStatusChanged >= TimeAfterEnd)
                ResetGame();
        }
    }

    private void ResetGame()
    {
        if(CurrentGame is null)
            throw new InvalidOperationException("No game was presented.");

        if(CurrentGame.Status == GameStatus.Started)
            CurrentGame.Stop();

        CurrentGame.GameObject.Destroy();
        CurrentGame = null;
    }
}
