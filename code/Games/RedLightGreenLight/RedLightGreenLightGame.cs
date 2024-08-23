using Mini.Players;
using Mini.UtilComponents;
using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mini.Games.RedLightGreenLight;

public class RedLightGreenLightGame : MiniGame
{
    [Property]
    public PlayersTriggerContainer RunZone { get; set; } = null!;
    [Property]
    public Finish Finish { get; set; } = null!;
    [Property]
    public GameObject SpawnBarrier { get; set; } = null!;
    [Property]
    public IndicatingLight IndicatingLight { get; set; } = null!;
    [Property]
    public float MinTimeToChangeLight { get; set; } = 0.3f;
    [Property]
    public float MaxTimeToChangeLight { get; set; } = 3f;
    [Property]
    public float YellowColorTime { get; set; } = 0.7f;
    [Property]
    public float MaxMoveDistance { get; set; } = 3f;

    private readonly Dictionary<Player, Vector3> _lastPlayerPositions = new();


    protected override async Task OnGameSetup()
    {
        await base.OnGameSetup();
        IndicatingLight.Paused = true;
        IndicatingLight.SetColor(IndicatingLight.LightColor.Red);
        IndicatingLight.MinTimeToChangeLight = MinTimeToChangeLight;
        IndicatingLight.MaxTimeToChangeLight = MaxTimeToChangeLight;
        IndicatingLight.YellowColorTime = YellowColorTime;
        EnableBarrier(true);
    }

    protected override async Task OnGameStart()
    {
        await base.OnGameStart();
        IndicatingLight.Paused = false;
        IndicatingLight.SetColor(IndicatingLight.LightColor.Green);
        EnableBarrier(false);
    }

    [Broadcast(NetPermission.OwnerOnly)]
    private void EnableBarrier(bool enabled)
    {
        SpawnBarrier.Enabled = enabled;
    }

    protected override void OnPlayerSpawned(Player player)
    {
        base.OnPlayerSpawned(player);
        _lastPlayerPositions[player] = player.Transform.Position;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if(IsProxy)
            return;

        var runningPlayers = PlayingPlayers.ToList().Except(Finish.FinishedPlayers);
        foreach(var player in runningPlayers)
        {
            if(player.Transform.Position.AlmostEqual(_lastPlayerPositions[player], MaxMoveDistance))
                continue;

            if(IndicatingLight.CurrentColor == IndicatingLight.LightColor.Red)
            {
                if(RunZone.Players.Contains(player))
                    player.Kill();
            }

            _lastPlayerPositions[player] = player.Transform.Position;
        }

        if(!runningPlayers.Any())
            Stop();
    }

    protected override ISet<ulong> ChooseWinners() =>
        Finish.FinishedPlayers.Select(p => p.Network.OwnerConnection.SteamId).ToHashSet();
}
