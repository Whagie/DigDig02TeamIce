using Game.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private float alertRadius = 5f;
    [SerializeField] private float visionLength = 5f;
    [SerializeField] private float chaseVisionLength = 16f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private float marginDegrees = 4f;
    [SerializeField] private Vector3 visionRotation = Vector3.zero;

    [SerializeField] private float wanderSpeed;
    [SerializeField] private float chaseSpeed;
    [SerializeField] private float wanderRadius;
    [SerializeField] private float waitTime;

    [SerializeField] private float actionInterval;

    protected override void OnEntityEnable()
    {
        base.OnEntityEnable();
    }
    protected override void OnEntityDisable()
    {
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
                CanUse = () => SeeingPlayer && FacingPlayer,
                MinDistance = 4.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(WanderSpeed / 2)
            },
            new EnemyAction
            {
                TriggerName = "Headbash",
                Weight = 0.4f,
                CanUse = () => SeeingPlayer && FacingPlayer,
                MinDistance = 4.5f,
                Modifier = new ActionModifier()
                    .StopAgent()
            }
        };
    }

    protected override void OnStart()
    {
        Collider = MainCollider;
        LayerMask = layers;
        VisionCones.Add(new VisionCone(Vector3.zero, Vector3.zero, visionAngle, visionLength));
        AlertRadius = alertRadius;
        MarginDegrees = marginDegrees;
        ActionInterval = 1f;
        Health = health;

        VisionCones[0].angle = visionAngle;
        VisionCones[0].length = visionLength;
        VisionCones[0].rotation = visionRotation;

        WanderSpeed = wanderSpeed;
        ChaseSpeed = chaseSpeed;
        WanderRadius = wanderRadius;
        WaitTime = waitTime;

        ActionInterval = actionInterval;

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
        VisionCones[0].rotation = visionRotation;

        if (SeeingPlayer)
        {
            VisionCones[0].length = chaseVisionLength;
        }
        else
        {
            VisionCones[0].length = visionLength;
        }

        if (Attacking)
        {
            //NavAgent.speed = WanderSpeed / 2;
            NavAgent.updateRotation = true;
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

    public void LungeDistanceDuration(string parameters)
    {
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist > Actions[1].MinDistance)
        {
            var parts = parameters.Split(';').Select(float.Parse).ToArray();
            float distance = parts[0], duration = parts[1];

            Lunge(distance, duration);
        }
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
