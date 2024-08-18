using Mini.Exceptions;
using Mini.Players;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mini.Games;

public abstract class MiniGame : Component, Component.INetworkListener
{
    [Property, Group("Debug")]
    public GameStatus Status { get; private set; } = GameStatus.Created;
    public TimeSince TimeSinceStatusChanged { get; private set; }

    [Property, Group("Main Settings")]
    public float MaxGameTime { get; set; } = 120f;
    [Property, Group("Main Settings")]
    public bool StopGameIfNotEnoughPlayer { get; set; } = true;


    [Property, Group("Players")]
    public GameObject PlayerPrefab { get; set; } = null!;
    [Property, Group("Players")]
    public GameObject PlayersParent { get; set; } = null!;


    protected readonly List<Player> Players = new();
    public IEnumerable<Player> PlayingPlayers => Players;
    public int PlayingPlayersCount => Players.Count;


    protected List<SpawnPoint> SpawnPoints = null!;
    private int _nextSpawnPointIndex = 0;

    [Sync]
    private NetList<ulong> NetWinners { get; set; } = new();



    [Button("Setup"), Group("Debug")]
    public void Setup()
    {
        if(IsProxy)
            return;

        if(Status != GameStatus.Created)
            throw new InvalidOperationException("Incorrect game status.");

        Status = GameStatus.SettingUp;
        TimeSinceStatusChanged = 0;

        _ = OnGameSetup().ContinueWith(t =>
        {
            Status = GameStatus.SetUp;
            TimeSinceStatusChanged = 0;
        });
    }

    [Button("Start"), Group("Debug")]
    public void Start()
    {
        if(IsProxy)
            return;

        if(Status != GameStatus.SetUp)
            throw new InvalidOperationException("Incorrect game status.");

        Status = GameStatus.Starting;
        TimeSinceStatusChanged = 0;

        NetWinners.Clear();

        _ = OnGameStart().ContinueWith(t =>
        {
            Status = GameStatus.Started;
            TimeSinceStatusChanged = 0;
        });
    }

    [Button("Stop"), Group("Debug")]
    public void Stop()
    {
        if(IsProxy)
            return;

        if(Status != GameStatus.Started)
            throw new InvalidOperationException("Incorrect game status.");

        Status = GameStatus.Stopping;
        TimeSinceStatusChanged = 0;

        NetWinners.Clear();
        foreach(var winner in ChooseWinners())
            NetWinners.Add(winner);

        _ = OnGameStop().ContinueWith(t =>
        {
            Status = GameStatus.Stopped;
            TimeSinceStatusChanged = 0;
        });
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


    public Player? GetPlayer(Connection connection) => Players.FirstOrDefault(p => p.Network.OwnerConnection == connection, null);
    protected void SpawnPlayer(Connection connection)
    {
        var existingPlayer = GetPlayer(connection);
        if(existingPlayer.IsValid())
            throw new InvalidOperationException("Player already spawned");

        var spawnPoint = SpawnPoints[_nextSpawnPointIndex];
        _nextSpawnPointIndex = (_nextSpawnPointIndex + 1) % SpawnPoints.Count;

        var startLocation = spawnPoint.Transform.World.WithScale(1f);
        var playerGameObject = PlayerPrefab.Clone(startLocation, null, false, $"Player - {connection.DisplayName}");
        playerGameObject.SetParent(PlayersParent);

        var player = playerGameObject.Components.Get<Player>(true);
        if(!player.IsValid())
            throw new ComponentNotFoundException(playerGameObject, typeof(Player));

        Players.Add(player);
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
        Players.Remove(player);

        if(StopGameIfNotEnoughPlayer && Status == GameStatus.Started && Players.Count <= 1)
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



    protected virtual Task OnGameSetup()
    {
        UpdateSpawnPoints();

        if(SpawnPoints.Count == 0)
            Log.Warning("No spawn points were found.");

        foreach(var connection in Connection.All)
            SpawnPlayer(connection);

        return Task.CompletedTask;
    }

    protected virtual void UpdateSpawnPoints()
    {
        SpawnPoints = GameObject.Components.GetAll<SpawnPoint>(FindMode.EverythingInSelfAndDescendants)
            .OrderBy(x => Guid.NewGuid()).ToList();
    }

    protected virtual Task OnGameStart() => Task.CompletedTask;
    protected virtual Task OnGameStop() => Task.CompletedTask;

    protected virtual ISet<ulong> ChooseWinners() => PlayingPlayers.Select(p => p.Network.OwnerConnection.SteamId).ToHashSet();
    public ISet<ulong> GetWinners() => NetWinners.ToHashSet();
}
