using Sandbox;

namespace Mini.Games.FindTheWay;

public sealed class FakeBlock : FindTheWayBlock, Component.ITriggerListener
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

        GameObject.Destroy();
    }
}
