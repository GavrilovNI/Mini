using Mini.Players;
using Sandbox;
using System.Collections.Generic;

namespace Mini.UtilComponents;

public class PlayersTriggerContainer : Component, Component.ITriggerListener
{
    private readonly HashSet<Player> _players = new();
    public IEnumerable<Player> Players => _players;

    public void OnTriggerEnter(Collider other)
    {
        var player = other.Components.Get<Player>();
        if(player.IsValid())
            _players.Add(player);
    }

    public void OnTriggerExit(Collider other)
    {
        var player = other.Components.Get<Player>();
        if(player.IsValid())
            _players.Remove(player);
    }
}
