using Mini.Games;
using Mini.Players;
using Sandbox;
using System.Collections.Generic;

namespace Mini.UtilComponents;

public class Finish : Component, Component.ITriggerListener
{
    [Property]
    public MiniGame MiniGame { get; set; } = null!;
    

    private readonly HashSet<Player> _players = new();
    public IEnumerable<Player> FinishedPlayers => _players;

    public void OnTriggerEnter(Collider other)
    {
        var player = other.Components.Get<Player>();
        if(player.IsValid() && MiniGame.Status == GameStatus.Started)
            _players.Add(player);
    }
}
