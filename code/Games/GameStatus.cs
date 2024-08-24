using System;

namespace Mini.Games;

public enum GameStatus
{
    None,
    Created,
    SettingUp,
    SetUp,
    Starting,
    Started,
    Stopping,
    Stopped
}

public static class GameStatusExtensions
{
    public static bool IsBefore(this GameStatus currentGamesStatus, GameStatus gameStatus) =>
        (int)currentGamesStatus < (int)gameStatus;
    public static bool IsAfter(this GameStatus currentGamesStatus, GameStatus gameStatus) =>
        (int)currentGamesStatus > (int)gameStatus;

    public static bool IsBetween(this GameStatus currentGamesStatus, GameStatus startStatus, GameStatus endStatus)
    {
        if(startStatus.IsAfter(endStatus))
            throw new ArgumentException($"Given {nameof(startStatus)} goes after given {nameof(endStatus)}");

        return currentGamesStatus.IsAfter(startStatus) && currentGamesStatus.IsBefore(endStatus);
    }

    public static bool IsBetweenIncluding(this GameStatus currentGamesStatus, GameStatus startStatus, GameStatus endStatus)
    {
        if(startStatus.IsAfter(endStatus))
            throw new ArgumentException($"Given {nameof(startStatus)} goes after given {nameof(endStatus)}");

        return currentGamesStatus == startStatus || currentGamesStatus == endStatus || currentGamesStatus.IsBetween(startStatus, endStatus);
    }
}
