using Sandbox;
using Sandbox.Network;
using Sandbox.Utility;
using System.Threading.Tasks;

namespace Mini.Networking;

public static class NetworkUtils
{
    public static async Task<bool> TryConnectToLocal(TaskSource taskSource)
    {
        GameNetworkSystem.Connect("local");
        while(GameNetworkSystem.IsConnecting)
            await taskSource.Yield();

        bool connected = GameNetworkSystem.IsActive;

        if(connected && Connection.Host.SteamId != Steam.SteamId)
        {
            GameNetworkSystem.Disconnect();
            connected = false;
        }

        return connected;
    }

    public static Task<bool> TryConnect(TaskSource taskSource, LobbyInformation lobby, bool allowLocalConnection = false)
    {
        if(lobby.OwnerId == Steam.SteamId && allowLocalConnection)
            return TryConnectToLocal(taskSource);
        return GameNetworkSystem.TryConnectSteamId(lobby.LobbyId);
    }
}
