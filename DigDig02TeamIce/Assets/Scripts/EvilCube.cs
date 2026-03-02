using UnityEngine;

public class EvilCube : Enemy
{
    public Collider MainCollider;

    [SerializeField] private float shootInterval = 2f;

    protected override void Awake()
    {
        base.Awake();

        ShouldWander = false;
        ShouldMove = false;
        ProjectileDamage = 2;
    }

    protected override void Start()
    {
        base.Start();

        if (MainCollider != null)
        {
            Collider = MainCollider;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (DetectedPlayer)
        {
            RotateTowardsY(transform, player.transform.position, 90f);

            OnInterval(shootInterval, () =>
            {
                FireProjectile(player.Center.transform);
            });
        }
    }

    protected override void Die()
    {
        base.Die();

        Destroy(gameObject);
    }
}
