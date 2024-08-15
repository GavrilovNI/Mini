using Mini.Exceptions;
using Mini.Players;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mini.Games;

public abstract class MiniGame : Component, Component.INetworkListener
{
    public GameStatus Status { get; private set; } = GameStatus.Created;
    public TimeSince TimeSinceStatusChanged { get; private set; }

    [Property, Group("Main Settings")]
    public float MaxGameTime { get; set; } = 120f;


    [Property, Group("Players")]
    public GameObject PlayerPrefab { get; set; } = null!;
    [Property, Group("Players")]
    public GameObject PlayersParent { get; set; } = null!;


    private readonly List<Player> _players = new();
    public IEnumerable<Player> PlayingPlayers => _players;
    public int PlayingPlayersCount => _players.Count;


    private List<SpawnPoint> _spawnPoints = null!;
    private int _nextSpawnPointIndex = 0;




    [Button("Setup"), Group("Debug")]
    public void Setup()
    {
        if(IsProxy)
            return;

        if(Status != GameStatus.Created)
            throw new InvalidOperationException("Incorrect game status.");

        OnGameSetup();
        Status = GameStatus.SetUp;
        TimeSinceStatusChanged = 0;
    }

    [Button("Start"), Group("Debug")]
    public void Start()
    {
        if(IsProxy)
            return;

        if(Status != GameStatus.SetUp)
            throw new InvalidOperationException("Incorrect game status.");

        OnGameStart();
        Status = GameStatus.Started;
        TimeSinceStatusChanged = 0;
    }

    [Button("Stop"), Group("Debug")]
    public void Stop()
    {
        if(IsProxy)
            return;

        if(Status != GameStatus.Started)
            throw new InvalidOperationException("Incorrect game status.");

        OnGameStop();
        Status = GameStatus.Stopped;
        TimeSinceStatusChanged = 0;
    }



    protected override void OnAwake()
    {
        _spawnPoints = GameObject.Components.GetAll<SpawnPoint>(FindMode.EverythingInSelfAndDescendants)
            .OrderBy(x => Guid.NewGuid()).ToList();
    }

    protected override void OnUpdate()
    {
        if(IsProxy || Status != GameStatus.Started)
            return;

        if(TimeSinceStatusChanged >= MaxGameTime)
        {
            Stop();
            return;
        }
    }


    public Player? GetPlayer(Connection connection) => _players.FirstOrDefault(p => p.Network.OwnerConnection == connection, null);
    protected void SpawnPlayer(Connection connection)
    {
        var existingPlayer = GetPlayer(connection);
        if(existingPlayer.IsValid())
            throw new InvalidOperationException("Player already spawned");

        var spawnPoint = _spawnPoints[_nextSpawnPointIndex];
        _nextSpawnPointIndex = (_nextSpawnPointIndex + 1) % _spawnPoints.Count;

        var startLocation = spawnPoint.Transform.World.WithScale(1f);
        var playerGameObject = PlayerPrefab.Clone(startLocation, null, false, $"Player - {connection.DisplayName}");
        playerGameObject.SetParent(PlayersParent);

        var player = playerGameObject.Components.Get<Player>(true);
        if(!player.IsValid())
            throw new ComponentNotFoundException(playerGameObject, typeof(Player));

        _players.Add(player);
        player.Died += OnPlayerDied;

        playerGameObject.Enabled = true;

        playerGameObject.NetworkMode = NetworkMode.Object;
        playerGameObject.NetworkSpawn(connection);

        OnPlayerSpawned(player);
    }

    protected virtual void OnPlayerSpawned(Player player)
    {

    }
    protected virtual void OnPlayerDied(Player player)
    {
        if(IsProxy)
            return;

        player.Died -= OnPlayerDied;
        player.GameObject.Destroy();
        _players.Remove(player);

        if(Status == GameStatus.Started && _players.Count <= 1)
            Stop();
    }



    public virtual void OnConnected(Connection connection)
    {
        if(IsProxy)
            return;

        if(Status == GameStatus.SetUp)
            SpawnPlayer(connection);
    }
    public virtual void OnDisconnected(Connection connection)
    {
        var player = PlayingPlayers.FirstOrDefault(p => p.Network.OwnerConnection == connection, null);
        if(player is not null)
            OnPlayerDied(player);
    }



    protected virtual void OnGameSetup()
    {
        foreach(var connection in Connection.All)
            SpawnPlayer(connection);
    }
    protected virtual void OnGameStart() { }
    protected virtual void OnGameStop() { }
}
