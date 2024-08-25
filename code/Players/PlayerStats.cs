using Sandbox;
using Sandbox.Services;
using Sandbox.Utility;
using System;

namespace Mini.Players;

public class PlayerStats
{
    private static PlayerStats? _local;
    public static PlayerStats Local
    {
        get
        {
            if(_local == null)
            {
                _local = new(Steam.SteamId);
                _local.UpdateLocalStats();
            }
            return _local;
        }
    }

    public readonly ulong SteamId;
    private Stats.PlayerStats Stats => Sandbox.Services.Stats.GetPlayerStats(Game.Ident, (long)SteamId);


    private int _gamesPlayed = -1;
    public int GamesPlayed
    {
        get
        {
            if(_gamesPlayed < 0)
                _gamesPlayed = (int)Stats.Get("games_played").Value;
            return _gamesPlayed;
        }
        private set => _gamesPlayed = value;
    }

    private int _wins = -1;
    public int Wins
    {
        get
        {
            if(_wins < 0)
                _wins = (int)Stats.Get("wins").Value;
            return _wins;
        }
        private set => _wins = value;
    }

    private float _winRate = -1;
    public float WinRate
    {
        get
        {
            if(_winRate < 0)
            {
                if(Stats.TryGet("win_rate", out var stat))
                    _winRate = (float)stat.Value;
                else
                    _winRate = 1f * Wins / Math.Max(1, GamesPlayed);
            }
            return _winRate;
        }
        private set => _winRate = value;
    }



    public PlayerStats(ulong steamId)
    {
        SteamId = steamId;
    }

    public void ResetLocal()
    {
        GamesPlayed = -1;
        Wins = -1;
        WinRate = -1;
    }

    public void UpdateLocalStats()
    {
        _gamesPlayed = GamesPlayed;
        _wins = Wins;
        _winRate = WinRate;
    }

    public void RegisterGame()
    {
        if(Steam.SteamId != SteamId)
            throw new InvalidOperationException("Can't set stats of non-local player");

        var oldGamesPlayed = GamesPlayed;
        Sandbox.Services.Stats.Increment("games_played", 1);
        GamesPlayed = oldGamesPlayed + 1;
    }

    public void RegisterWin()
    {
        if(Steam.SteamId != SteamId)
            throw new InvalidOperationException("Can't set stats of non-local player");

        var oldWins = Wins;
        Sandbox.Services.Stats.Increment("wins", 1);
        Wins = oldWins + 1;
    }

    public void UpdateWinRate()
    {
        if(Steam.SteamId != SteamId)
            throw new InvalidOperationException("Can't set stats of non-local player");

        WinRate = 1f * Wins / Math.Max(1, GamesPlayed);
        Sandbox.Services.Stats.SetValue("win_rate", WinRate);
    }
}
