using Sandbox;
using System.Linq;

namespace Mini.Games.FallingBlocks;

public sealed class FallingBlock : Component
{
    [Property]
    [RequireComponent]
    public BoxCollider Collider { get; set; } = null!;

    [Property]
    [RequireComponent]
    public ModelRenderer Model { get; set; } = null!;

    [Property]
    public float Speed { get; set; } = 10f;

    public Color Color
    {
        get => Model.Tint;
        set => Model.Tint = value;
    }

    public bool Grounded { get; private set; } = false;

    protected override void OnFixedUpdate()
    {
        if(IsProxy)
            return;

        if(Grounded)
            return;

        var travelDistance = Time.Delta * Speed;

        var traceResults = Scene.Trace.Box(Collider.Scale * Transform.Scale, Transform.Position, Transform.Position + Vector3.Down * travelDistance)
            .IgnoreGameObjectHierarchy(GameObject)
            .WithCollisionRules(string.Empty)
            .RunAll();

        var others = traceResults.Where(t => t.GameObject.Components.Get<IDamageable>() is null);

        if(others.Any())
        {
            travelDistance = others.Max(t => t.Distance);
            Grounded = true;
        }

        Transform.Position += Vector3.Down * travelDistance;

        var damageables = traceResults.Select(t => t.GameObject.Components.Get<IDamageable>()).Where(d => d is not null);
        foreach(var damageable in damageables)
        {
            var damage = damageable is IHealthProvider healthProvider ? healthProvider.Health : float.MaxValue;
            damageable.OnDamage(new DamageInfo(damage, GameObject, GameObject));
        }
    }
}
