using Mini.Players;
using Mini.UtilComponents;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mini.Games.Parkour;

public class ParkourGame : MiniGame
{
    [Property]
    public GameObject SpawnBarrier { get; set; } = null!;
    [Property]
    public Finish Finish { get; set; } = null!;
    [Property]
    public float WinnersPercentage { get; set; } = 0.3f;

    public int StartingPlayersCount { get; protected set; }
    public int MaxPlayersToFinish { get; protected set; }


    [Broadcast(NetPermission.OwnerOnly)]
    protected virtual void EnableBarrier(bool enabled)
    {
        SpawnBarrier.Enabled = enabled;
    }

    protected override async Task OnGameSetup()
    {
        await base.OnGameSetup();
        Finish.PlayerFinished += OnPlayerFinished;
    }

    protected override async Task OnGameStart()
    {
        await base.OnGameStart();
        StartingPlayersCount = PlayingPlayersCount;
        MaxPlayersToFinish = Math.Max(1, (StartingPlayersCount * WinnersPercentage).FloorToInt());
        EnableBarrier(false);
    }

    protected override async Task OnGameStop()
    {
        await base.OnGameStop();
        Finish.PlayerFinished -= OnPlayerFinished;
    }

    protected virtual void OnPlayerFinished(Player player)
    {
        if(ShouldStopGameByPlayersCount())
            Stop();
    }

    protected override bool ShouldStopGameByPlayersCount()
    {
        if(base.ShouldStopGameByPlayersCount())
            return true;

        return Finish.FinishedPlayers.Count >= MaxPlayersToFinish;
    }

    protected override ISet<ulong> ChooseWinners() =>
        Finish.FinishedPlayers.Select(p => p.Network.OwnerConnection.SteamId).ToHashSet();
}
