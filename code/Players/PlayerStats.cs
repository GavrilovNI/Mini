using System;

namespace Mini.Players;

public static class PlayerStats
{
    private static int _gamesPlayed = -1;
    public static int GamesPlayed
    {
        get
        {
            if(_gamesPlayed < 0)
                _gamesPlayed = (int)Sandbox.Services.Stats.LocalPlayer.Get("games_played").Value;
            return _gamesPlayed;
        }
        private set => _gamesPlayed = value;
    }

    private static int _wins = -1;
    public static int Wins
    {
        get
        {
            if(_wins < 0)
                _wins = (int)Sandbox.Services.Stats.LocalPlayer.Get("wins").Value;
            return _wins;
        }
        private set => _wins = value;
    }

    private static float _winRate = -1;
    public static float WinRate
    {
        get
        {
            if(_winRate < 0)
            {
                if(Sandbox.Services.Stats.LocalPlayer.TryGet("win_rate", out var stat))
                    _winRate = (float)stat.Value;
                else
                    _winRate = 1f * Wins / Math.Max(1, GamesPlayed);
            }
            return _winRate;
        }
        private set => _winRate = value;
    }



    public static void ResetLocal()
    {
        GamesPlayed = -1;
        Wins = -1;
        WinRate = -1;
    }

    public static void UpdateLocalStats()
    {
        GamesPlayed = GamesPlayed;
        Wins = Wins;
        WinRate = WinRate;
    }

    public static void RegisterGame()
    {
        var oldGamesPlayed = GamesPlayed;
        Sandbox.Services.Stats.Increment("games_played", 1);
        GamesPlayed = oldGamesPlayed + 1;
    }

    public static void RegisterWin()
    {
        var oldWins = Wins;
        Sandbox.Services.Stats.Increment("wins", 1);
        Wins = oldWins + 1;
    }

    public static void UpdateWinRate()
    {
        WinRate = 1f * Wins / Math.Max(1, GamesPlayed);
        Sandbox.Services.Stats.SetValue("win_rate", WinRate);
    }
}
