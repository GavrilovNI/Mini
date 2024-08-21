using Sandbox;

namespace Mini;

public static class Consts
{
    public const float CubeModelSize = 50f;

    public static int MinPlayersToPlay => Game.IsEditor ? 1 : 2;

}
