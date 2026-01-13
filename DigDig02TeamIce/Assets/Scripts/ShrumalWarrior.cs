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
    private MeleeAttack stabSequence;

    public Collider MainCollider;
    public Collider SwordCollider;
    public Collider HeadCollider;

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
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 4.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(WanderSpeed / 1.5f)
            },
            new EnemyAction
            {
                TriggerName = "Headbash",
                Weight = 0.4f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 4.5f,
                Modifier = new ActionModifier()
                    .StopAgent()
            },
            new EnemyAction
            {
                TriggerName = "StabSequence",
                Weight = 0.3f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 4.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(WanderSpeed / 0.75f)
            }
        };
    }

    protected override void OnStart()
    {
        base.OnStart();

        Collider = MainCollider;

        Sword.AddComponent<MeleeAttack>();
        Head.AddComponent<MeleeAttack>();

        swordSwing = Sword.GetComponent<MeleeAttack>();
        swordSwing.hitCollider = SwordCollider;
        swordSwing.EnemyOwner = this;
        swordSwing.LayerMask = LayerMask.GetMask("Player");

        headBash = Head.GetComponent<MeleeAttack>();
        headBash.hitCollider = HeadCollider;
        headBash.EnemyOwner = this;
        headBash.LayerMask = LayerMask.GetMask("Player");

        stabSequence = Sword.GetComponent<MeleeAttack>();
        stabSequence.hitCollider = SwordCollider;
        stabSequence.EnemyOwner = this;
        stabSequence.LayerMask = LayerMask.GetMask("Player");

        SwordCollider.enabled = false;
        HeadCollider.enabled = false;
    }

    public override void HandleParried(IHurtbox by)
    {
        base.HandleParried(by);

        AlterSword(0);
        AlterHead(0);

        Debug.Log("Parried!");
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
            CameraActions.Main.Shake(0.3f, 0.15f, 0.1f);
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
        var parts = parameters.Split(';').Select(float.Parse).ToArray();
        float distance = parts[0], duration = parts[1];

        if (DistanceToPlayer < distance)
        {
            distance -= (distance - DistanceToPlayer);
        }

        Lunge(distance, duration);
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
