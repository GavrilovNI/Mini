using Sandbox;

namespace Mini.Games;

public abstract class MiniGame : Component
{
    public GameStatus Status { get; private set; } = GameStatus.Created;
    public TimeSince TimeSinceStart { get; private set; }


    [Button("Start")]
    public void Start()
    {
        if(IsProxy)
            return;

        OnGameStart();
        TimeSinceStart = 0;
        Status = GameStatus.Started;
    }

    [Button("Stop")]
    public void Stop()
    {
        if(IsProxy)
            return;

        OnGameStop();
        Status = GameStatus.Stopped;
    }

    protected virtual void OnGameStart() { }
    protected virtual void OnGameStop() { }
}
