using Mini.Games;
using Mini.Players;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Mini.UtilComponents;

public class Finish : Component, Component.ITriggerListener
{
    [Property]
    public MiniGame MiniGame { get; set; } = null!;
    

    private readonly HashSet<Player> _players = new();
    public List<Player> FinishedPlayers => _players.ToList();

    public void OnTriggerEnter(Collider other)
    {
        var player = other.Components.Get<Player>();
        if(player.IsValid() && MiniGame.Status == GameStatus.Started)
            _players.Add(player);
    }
}
