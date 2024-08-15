using Sandbox;
using System;
using static Sandbox.Component;

namespace Mini.Players;

public sealed class Player : Component, IDamageable, IHealthProvider
{
    public event Action<Player>? Died;

    [Sync, HostSync]
    [Property]
    public float Health { get; private set; } = 100f;

    public void OnDamage(in DamageInfo damageInfo)
    {
        if(damageInfo.Damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damageInfo), damageInfo, "Damage is negative.");

        Damage(damageInfo.Damage);
    }

    [Broadcast(NetPermission.HostOnly)]
    public void Damage(float damage)
    {
        if(damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage is negative.");

        Health -= damage;

        if(Health <= 0)
            OnDied();
    }

    [Broadcast(NetPermission.HostOnly)]
    public void Heal(float health)
    {
        if(health < 0)
            throw new ArgumentOutOfRangeException(nameof(health), health, "Healing health is negative.");

        if(IsProxy)
            return;

        Health += health;
    }

    [Broadcast(NetPermission.HostOnly)]
    private void OnDied()
    {
        if(Health > 0)
            throw new InvalidOperationException("Health os positive.");

        Died?.Invoke(this);

        if(!IsProxy)
            GameObject.Destroy();
    }
}
