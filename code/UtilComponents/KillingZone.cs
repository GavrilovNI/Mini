using Sandbox;

namespace Mini.UtilComponents;

public sealed class KillingZone : Component, Component.ITriggerListener
{
    public void OnTriggerEnter(Collider other)
    {
        if(IsProxy)
            return;

        var damageable = other.Components.Get<IDamageable>();
        if(damageable is null)
            return;

        var damage = new DamageInfo(damageable is IHealthProvider healthProvider ? healthProvider.Health : float.MaxValue, GameObject, null);
        damageable.OnDamage(damage);
    }
}
