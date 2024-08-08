using Sandbox;
using static Sandbox.Component;

namespace Mini.Networking;

public sealed class ProxyDisabled : Component, INetworkSpawn
{
    public void OnNetworkSpawn(Connection owner)
    {
        if(IsProxy)
            GameObject.Enabled = false;
    }
    protected override void OnEnabled()
    {
        if(IsProxy)
            GameObject.Enabled = false;
    }

    protected override void OnStart()
    {
        if(IsProxy)
            GameObject.Enabled = false;
    }
}
