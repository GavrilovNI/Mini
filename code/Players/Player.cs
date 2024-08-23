using Mini.Interfaces;
using Mini.Networking.Exceptions;
using Mini.UI;
using Sandbox;
using System;
using static Sandbox.Component;

namespace Mini.Players;

public sealed class Player : Component, IDamageable, IHealthProvider, Component.INetworkSpawn, IEyeProvider
{
    public event Action<Player>? Died;
    public event Action<Player>? Destroyed;

    [Property]
    public GameObject Eye { get; private set; } = null!;
    [Property]
    public GameObject Camera { get; private set; } = null!;
    [Property]
    public PlayerVisibilityController VisibilityController { get; private set; } = null!;

    [HostSync]
    [Property]
    public float Health { get; private set; } = 100f;

    [HostSync]
    [Property]
    public float MaxHealth { get; private set; } = 100f;

    public bool IsDead => Health <= 0;


    public void OnNetworkSpawn(Connection owner)
    {
        if(IsProxy)
            return;

        if(SpectatingCamera.Instance.IsValid())
            SpectatingCamera.Instance.GameObject.Enabled = false;
        VisibilityController.CameraControllerOverride = null;
    }

    public void OnDamage(in DamageInfo damageInfo)
    {
        if(damageInfo.Damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damageInfo), damageInfo, "Damage is negative.");

        Damage(damageInfo.Damage);
    }

    public void Damage(float damage)
    {
        NotEnoughNetworkAuthorityException.ThrowIfLocalIsNotHost();
        if(damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage is negative.");
        if(IsDead)
            throw new InvalidOperationException("Can't damage dead player.");

        Health = Math.Max(0, Health - damage);

        if(Health <= 0)
            OnDied();
    }

    public void Heal(float health)
    {
        NotEnoughNetworkAuthorityException.ThrowIfLocalIsNotHost();
        if(health < 0)
            throw new ArgumentOutOfRangeException(nameof(health), health, "Healing health is negative.");
        if(IsDead)
            throw new InvalidOperationException("Can't heal dead player.");

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

        if(Connection.Local?.IsHost ?? false)
            Chat.Instance?.AddSystemText($"Player {Network.OwnerConnection.DisplayName} died!");

        if(!IsProxy)
        {
            MainHUD.Instance?.ShowDeathMessage();
            EnableSpectatingCamera();
        }
    }

    protected override void OnDestroy()
    {
        Destroyed?.Invoke(this);
        if(!IsProxy)
            EnableSpectatingCamera();
    }

    private void EnableSpectatingCamera()
    {
        var spectatingCamera = SpectatingCamera.Instance;
        if(spectatingCamera.IsValid())
        {
            spectatingCamera.SetTarget(null);
            spectatingCamera.Transform.World = Camera.Transform.World;
            spectatingCamera.GameObject.Enabled = true;
        }
    }

    [Button("Kill")]
    public void Kill()
    {
        NotEnoughNetworkAuthorityException.ThrowIfLocalIsNotHost();
        if(IsDead)
            throw new InvalidOperationException("Can't kill dead player.");

        Health = 0;
        OnDied();
    }
}
