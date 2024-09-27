using Sandbox;

namespace Mini;

public static class Consts
{
    public const float CubeModelSize = 50f;

    public static int MinPlayersToPlay
    {
        get
        {
            if(GamesLauncher.Instance?.IsAllowedPlayAlone ?? false && GamesLauncher.Instance.CurrentGameInfo.CanPlayAlone)
                return 1;

            return Game.IsEditor ? 1 : 2;
        }
    }

}
