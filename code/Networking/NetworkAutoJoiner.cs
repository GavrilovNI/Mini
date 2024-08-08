using Sandbox;
using Sandbox.Network;
using System.Linq;
using System.Threading.Tasks;
using static Sandbox.Component;

namespace Mini.Networking;

public class NetworkAutoJoiner : Component, INetworkListener
{
    protected override async Task OnLoad()
    {
        if(Scene.IsEditor || !Active || GameNetworkSystem.IsConnecting)
            return;

        var lobbies = await GameNetworkSystem.QueryLobbies();
        var validLobbies = lobbies.Where(l => l.Members > 0 && !l.IsFull && l.OwnerId != Connection.Local.SteamId);
        bool connected = false;

        if(validLobbies.Any())
        {
            foreach(var lobby in validLobbies)
            {
                Log.Info($"Connecting to {lobby.OwnerId}...");
                connected = await GameNetworkSystem.TryConnectSteamId(lobby.OwnerId);
                Log.Info($"Connected to {lobby.OwnerId}: {connected}.");
                
                if(connected)
                {
                    if(Connection.All.Any())
                    {
                        break;
                    }
                    else
                    {
                        Log.Info($"Connected to empty lobby.");
                        connected = false;
                        GameNetworkSystem.Disconnect();
                    }
                }
            }

            if(!connected)
                Log.Info($"Couldn't connect to any lobby.");
        }

        if(!connected)
        {
            Log.Info($"Creating lobby...");
            GameNetworkSystem.CreateLobby();
        }
    }
}
