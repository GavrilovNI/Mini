using Sandbox;
using Sandbox.Network;

namespace Mini.Networking;
public class OnDestroyDisconnector : Component
{
    protected override void OnDestroy()
    {
        GameNetworkSystem.Disconnect();
    }
}
