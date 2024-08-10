using Sandbox;
using System;
using static Sandbox.Component;

namespace Mini.Players;

public sealed class Player : Component, IDamageable, IHealthProvider
{
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

        if(IsProxy)
            return;

        Health -= damage;

        if(Health <= 0)
            OnKilled();
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

    private void OnKilled()
    {
        if(Health > 0)
            throw new InvalidOperationException("Health os positive.");

        if(IsProxy)
            return;

        GameObject.Destroy();
    }
}
