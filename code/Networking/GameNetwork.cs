using Sandbox;
using Sandbox.Network;
using System;
using System.Threading.Tasks;

namespace Mini.Networking;

public sealed class GameNetwork : Component
{
    [Property]
    public bool StartServer { get; set; } = true;
    [Property]
    public bool DisconnectOnDestroy { get; set; } = true;
    [Property]
    public bool DisconnectIfHostLeft { get; set; } = true;
    [Property]
    public string MainMenuScene { get; set; } = "scenes/mainmenu.scene";

    private Guid _hostId;
    private bool _wasConnected;

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

        _hostId = Connection.Host.Id;
    }

    protected override void OnUpdate()
    {
        if(DisconnectIfHostLeft && GameNetworkSystem.IsActive)
        {
            if(Connection.Host.Id != _hostId)
                GameNetworkSystem.Disconnect();
        }

        if(GameNetworkSystem.IsActive)
        {
            _wasConnected = true;
        }
        else if(_wasConnected)
        {
            var loadedMainMenu = Game.ActiveScene.LoadFromFile(MainMenuScene);
            if(!loadedMainMenu)
                Log.Error("Couldn't load main menu.");
        }
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
