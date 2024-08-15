using Sandbox;
using System;

namespace Mini.Games;

public abstract class MiniGame : Component, Component.INetworkListener
{
    public GameStatus Status { get; private set; } = GameStatus.Created;
    public TimeSince TimeSinceStatusChanged { get; private set; }

    [Property]
    public float MaxGameTime { get; set; } = 120f;

    [Button("Setup")]
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

    [Button("Start")]
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

    [Button("Stop")]
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

    public virtual void OnConnected(Connection connection) { }
    public virtual void OnDisconnected(Connection connection) { }

    protected virtual void OnGameSetup() { }
    protected virtual void OnGameStart() { }
    protected virtual void OnGameStop() { }
}
