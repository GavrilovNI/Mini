using Mini.Networking.Exceptions;
using Mini.Players;
using Sandbox;

namespace Mini.Games.BlockRun;

public class BlockRunBlock : Component, Component.ITriggerListener
{
    [Property, RequireComponent]
    public ModelRenderer Model { get; set; } = null!;
    [Property]
    public float BreakingTime { get; set; } = 1f;
    [Property]
    public Color Color { get; set; } = Color.White;

    public bool IsBreaking { get; private set; }
    public TimeSince TimeSinceStartedToBreak { get; private set; }

    [Button("Break")]
    public void Break()
    {
        if(IsProxy)
            throw new NotEnoughNetworkAuthorityException("Only an owner can break it.");

        if(IsBreaking)
            return;

        IsBreaking = true;
        TimeSinceStartedToBreak = 0;
    }

    public void OnTriggerEnter(Collider collider)
    {
        if(IsProxy || IsBreaking)
            return;

        if(!collider.GameObject.Components.Get<Player>().IsValid())
            return;

        Break();
    }

    protected override void OnAwake()
    {
        UpdateColor(Color);
    }

    protected override void OnUpdate()
    {
        if(IsProxy)
            return;

        if(IsBreaking)
        {
            UpdateColor(Color.Lerp(Color, Color.WithAlpha(0f), TimeSinceStartedToBreak / BreakingTime));
            if(TimeSinceStartedToBreak > BreakingTime)
                GameObject.Destroy();
        }
    }

    [Broadcast(NetPermission.OwnerOnly)]
    private void UpdateColor(Color color)
    {
        Model.Tint = color;
    }
}
