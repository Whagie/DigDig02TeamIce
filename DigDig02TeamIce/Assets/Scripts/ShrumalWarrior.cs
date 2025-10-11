using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrumalWarrior : Enemy
{
    public GameObject Sword;
    public GameObject Head;
    private MeleeAttack swordSwing;
    private MeleeAttack headBash;

    public Collider MainCollider;
    public Collider SwordCollider;
    public Collider HeadCollider;

    [SerializeField] private LayerMask layers;

    [SerializeField] private int health = 5;
    public float alertRadius = 5f;
    public float visionLength = 5f;
    public float visionAngle = 90f;
    public float rotationSpeed = 75f;
    public Vector3 visionRotation = Vector3.zero;

    protected override void OnEntityEnable()
    {
        HitboxManager.Register(this);
        base.OnEntityEnable();
    }
    protected override void OnEntityDisable()
    {
        HitboxManager.Unregister(this);
        base.OnEntityDisable();
    }
    protected override void InitializeActions()
    {
        Actions = new[]
        {
            new EnemyAction
            {
                TriggerName = "SwordSwing",
                Weight = 0.7f,
                CanUse = () => SeeingPlayer && FacingPlayer
            },
            new EnemyAction
            {
                TriggerName = "Headbash",
                Weight = 0.4f,
                CanUse = () => SeeingPlayer && FacingPlayer
            }
        };
    }

    protected override void OnStart()
    {
        Collider = MainCollider;
        LayerMask = layers;
        VisionCones.Add(new VisionCone(Vector3.zero, Vector3.zero, visionAngle, visionLength));
        AlertRadius = alertRadius;
        MarginDegrees = 2f;
        ActionInterval = 1f;

        Health = health;

        swordSwing = Sword.AddComponent<MeleeAttack>();
        swordSwing.hitCollider = SwordCollider;
        swordSwing.EnemyOwner = this;

        headBash = Head.AddComponent<MeleeAttack>();
        headBash.hitCollider = HeadCollider;
        headBash.EnemyOwner = this;

        SwordCollider.enabled = false;
        HeadCollider.enabled = false;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        VisionCones[0].angle = visionAngle;
        VisionCones[0].length = visionLength;
        VisionCones[0].rotation = visionRotation;

        Player player = TrackerHost.Current.Get<Player>();

        float rotSpeed;
        if (!SeeingPlayer)
        {
            rotSpeed = rotationSpeed * 2f;
        }
        else
        {
            rotSpeed = rotationSpeed;
        }

        if (DetectedPlayer)
        {
            RotateTowardsY(transform, player.transform.position, rotSpeed);
        }
        if (SeeingPlayer && FacingPlayer)
        {
            //_animator.SetTrigger("SwordSwing");
        }
    }

    public override void HandleParried(IHurtbox by)
    {
        base.HandleParried(by);

        AlterSword(0);
        AlterHead(0);

        Debug.Log("Parried!");

        TakeDamage(1);
    }

    public void AlterSword(int activate = 1)
    {
        if (activate == 1)
        {
            swordSwing.Activate();
            swordSwing.gizmoColor = Color.red;
        }
        else
        {
            swordSwing.Deactivate();
            swordSwing.gizmoColor = Color.blue;
        }
    }
    public void AlterHead(int activate = 1)
    {
        if (activate == 1)
        {
            headBash.Activate();
            headBash.gizmoColor = Color.red;
        }
        else
        {
            headBash.Deactivate();
            headBash.gizmoColor = Color.blue;
        }
    }
    public void TryFollowUp()
    {
        const float followUpChance = 0.3f;

        if (Random.value < followUpChance)
            _animator.SetBool("FollowUp", true);
        else
            _animator.SetBool("FollowUp", false);
    }
    public void ResetFollowUp()
    {
        _animator.SetBool("FollowUp", false);
    }

    public void SetSwordDamage(int newDamage)
    {
        swordSwing.Damage = newDamage;
    }
    public void SetHeadDamage(int newDamage)
    {
        headBash.Damage = newDamage;
    }

    public void ResetDamage()
    {
        swordSwing.Damage = 1;
        headBash.Damage = 1;
    }

    protected override void Die()
    {
        _animator.SetBool("Died", true);
        MainCollider.enabled = false;
        SwordCollider.enabled = false;
        HeadCollider.enabled = false;
        base.Die();
        this.enabled = false;
    }
}
