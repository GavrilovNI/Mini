using Sandbox;
using System;
using static Sandbox.Component;

namespace Mini.Players;

public sealed class Player : Component, IDamageable, IHealthProvider
{
    public event Action<Player>? Died;

    [HostSync]
    [Property]
    public float Health { get; private set; } = 100f;

    [HostSync]
    [Property]
    public float MaxHealth { get; private set; } = 100f;

    public bool IsDead => Health <= 0;



    public void OnDamage(in DamageInfo damageInfo)
    {
        if(damageInfo.Damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damageInfo), damageInfo, "Damage is negative.");

        Damage(damageInfo.Damage);
    }

    public void Damage(float damage)
    {
        if(damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage is negative.");

        if(IsDead)
            throw new InvalidOperationException("Can't damage dead player.");

        if(!Connection.Local.IsHost)
            throw new InvalidOperationException("Tried damage player by non-host.");

        Health = Math.Max(0, Health - damage);

        if(Health <= 0)
            OnDied();
    }

    public void Heal(float health)
    {
        if(health < 0)
            throw new ArgumentOutOfRangeException(nameof(health), health, "Healing health is negative.");

        if(IsDead)
            throw new InvalidOperationException("Can't heal dead player.");

        if(!Connection.Local.IsHost)
            throw new InvalidOperationException("Tried heal player by non-host.");

        Health = Math.Min(MaxHealth, Health + health);
    }

    [Broadcast(NetPermission.HostOnly)]
    private void OnDied()
    {
        if(!IsDead)
            throw new InvalidOperationException("Health is positive.");

        Died?.Invoke(this);

        if(GameObject.IsValid() && (!IsProxy || Connection.Local.IsHost))
            GameObject.Destroy();
    }

    [Button("Kill")]
    public void Kill()
    {
        Damage(Health);
    }
}
