using Game.Core;
using UnityEngine;

public class WrenchAttack : MeleeAttack
{
    [SerializeField] private Player player;
    [SerializeField] private LayerMask layers;
    [SerializeField] private Transform endPoint;

    void Start()
    {
        PlayerAttack = true;
        DestroyOnHit = false;
        LayerMask = layers;
        Deactivate();
    }

    public override void OnHit(IHurtbox target)
    {
        target.OnHit(this);
        ParticleSpawner.Spawn(Particles.P_spark, endPoint.position);

        if (target.Owner.TryGetComponent<EnergyRecharge>(out EnergyRecharge recharge))
        {
            Deactivate();
            CameraActions.Main.Punch(-0.08f, 0.035f);
            Freezer.Freeze(0.015f);
        }

        if (target.Owner.layer == LayerMask.NameToLayer("Enemy"))
        {
            Deactivate();

            Vector3 pushDir = target.Owner.transform.position - transform.position;
            Vector3 final = new Vector3(-pushDir.x, 0, -pushDir.z);
            player.ApplyPushback(final, 1f, 0.1f);

            if (target.Owner.TryGetComponent<IPushbackReceiver>(out var receiver))
            {
                receiver.ApplyPushback(-final, 2f, 0.1f);
            }

            CameraActions.Main.Punch(-0.08f, 0.035f);
            Freezer.Freeze(0.015f);
        }
    }
}
