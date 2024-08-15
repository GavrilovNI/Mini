using Sandbox;
using System.Collections.Generic;

namespace Mini.Networking;

public sealed class ProxyDisabler : Component, Component.INetworkSpawn
{
    [Property]
    public List<Component> ComponentsToDisable { get; set; } = new();

    public void OnNetworkSpawn(Connection owner)
    {
        UpdateComponents();
    }

    protected override void OnStart()
    {
        UpdateComponents();
    }

    protected override void OnEnabled()
    {
        UpdateComponents();
    }

    private void UpdateComponents()
    {
        foreach(var component in ComponentsToDisable)
            component.Enabled = !IsProxy;
    }
}
