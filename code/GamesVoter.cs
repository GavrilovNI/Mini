using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mini;

public class GamesVoter : Component, Component.INetworkListener
{
    [Property]
    public GamesLoader GamesLoader { get; set; } = null!;

    [Sync]
    private NetList<string> NetGames { get; set; } = new();

    [Sync]
    private NetDictionary<ulong, string> NetVotes { get; set; } = new();


    public bool IsVoting => GamesCount > 0;
    public IEnumerable<string> Games => NetGames;
    public int GamesCount => NetGames.Count;
    public IEnumerable<KeyValuePair<ulong, string>> Votes => NetVotes;


    public void ChooseGames(int count)
    {
        if(count <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(count), "Count is negative.");

        if(count > GamesLoader.GamesCount)
            throw new System.ArgumentOutOfRangeException(nameof(count), "There are no that much games.");

        var games = GamesLoader.Games.OrderBy(g => Guid.NewGuid()).Take(count);

        NetGames.Clear();
        foreach(var game in games)
            NetGames.Add(game.GameId);
    }

    public void Clear()
    {
        NetGames.Clear();
        NetVotes.Clear();
    }

    public void OnDisconnected(Connection channel)
    {
        if(!IsProxy)
            NetVotes.Remove(channel.SteamId);
    }

    [Broadcast]
    public void Vote(string gameId)
    {
        if(IsProxy || !IsVoting)
            return;

        var steamId = Rpc.Caller.SteamId;

        if(!NetGames.Contains(gameId))
        {
            Log.Error($"{steamId} voted for unknown game {gameId}");
            return;
        }

        NetVotes[steamId] = gameId;

        Log.Info($"{steamId} voted for {gameId}.");
    }

    [Broadcast]
    public void RemoveVote()
    {
        if(IsProxy || !IsVoting)
            return;

        var steamId = Rpc.Caller.SteamId;
        NetVotes.Remove(steamId);

        Log.Info($"{steamId} cleared vote.");
    }

    public string GetMostWantedGame()
    {
        if(NetVotes.Count == 0)
            return NetGames.Skip(Game.Random.Next(NetGames.Count)).First();

        var groups = NetVotes.GroupBy(x => x.Value);
        groups = groups.OrderBy(x => x.Count());
        var maxVotes = groups.First().Count();
        groups = groups.TakeWhile(x => x.Count() == maxVotes);

        return groups.Skip(Game.Random.Next(groups.Count())).First().Key;
    }

    public IEnumerable<ulong> GetGameVotes(string gameId) => NetVotes.Where(v => v.Value == gameId).Select(x => x.Key);
}
