using Sandbox;
using Sandbox.Network;
using System.Threading.Tasks;

namespace Mini.Networking;

public sealed class GameNetwork : Component, Component.INetworkListener
{
    [Property]
    public bool StartServer { get; set; } = true;
    [Property]
    public bool DisconnectOnDestroy { get; set; } = true;
    [Property]
    public bool DisconnectIfHostLeft { get; set; } = true;
    [Property]
    public string MainMenuScene { get; set; } = "scenes/mainmenu.scene";

    private ulong _hostId;

    protected override async Task OnLoad()
    {
        if(Scene.IsEditor)
            return;

        if(StartServer && !GameNetworkSystem.IsActive)
        {
            LoadingScreen.Title = "Creating Lobby";
            await Task.DelayRealtimeSeconds(0.1f);
            GameNetworkSystem.CreateLobby();
        }

        _hostId = Connection.Host.SteamId;
    }

    protected override void OnUpdate()
    {
        if(DisconnectIfHostLeft && GameNetworkSystem.IsActive && !Connection.Local.IsHost)
        {
            if(Connection.Host.SteamId != _hostId)
            {
                GameNetworkSystem.Disconnect();
            }
        }    
    }

    public void OnConnected(Connection channel)
    {
        _hostId = Connection.Host.SteamId;
    }

    public void OnDisconnected(Connection channel)
    {
        var loadedMainMenu = Game.ActiveScene.LoadFromFile(MainMenuScene);
        if(!loadedMainMenu)
            Log.Error("Couldn't load main menu.");
    }

    protected override void OnDestroy()
    {
        if(DisconnectOnDestroy && GameNetworkSystem.IsActive)
        {
            if(Connection.Local.IsHost)
                DisconnectEveryone();
            else
                GameNetworkSystem.Disconnect();
        }
    }

    [Broadcast(NetPermission.HostOnly)]
    private void DisconnectEveryone()
    {
        GameNetworkSystem.Disconnect();
    }
}
