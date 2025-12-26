using Game.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Spiderling : Enemy
{
    public GameObject Bite;
    private MeleeAttack lunge;
    private MeleeAttack bite;

    public Collider MainCollider;
    public Collider BiteCollider;

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
                TriggerName = "Bite",
                Weight = 0.6f,
                CanUse = () => SeeingPlayer && FacingPlayer,
                MinDistance = 3f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(WanderSpeed / 2f)
            },
            new EnemyAction
            {
                TriggerName = "Lunge",
                Weight = 0.8f,
                CanUse = () => SeeingPlayer,
                MinDistance = 6f,
                Modifier = new ActionModifier()
                    .StopAgent()
            }
        };
    }

    protected override void OnStart()
    {
        base.OnStart();

        Collider = MainCollider;

        Bite.AddComponent<MeleeAttack>();

        bite = Bite.GetComponent<MeleeAttack>();
        bite.hitCollider = BiteCollider;
        bite.EnemyOwner = this;
        bite.LayerMask = LayerMask.GetMask("Player");

        BiteCollider.enabled = false;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (Attacking)
        {
            NavAgent.updateRotation = false;
        }
    }

    public override void HandleParried(IHurtbox by)
    {
        base.HandleParried(by);

        AlterBite(0);

        Debug.Log("Parried!");
    }

    public void AlterBite(int activate = 1)
    {
        if (activate == 1)
        {
            bite.Activate();
        }
        else
        {
            bite.Deactivate();
        }
    }

    public void SetBiteDamage(int newDamage)
    {
        bite.Damage = newDamage;
    }
    public void ResetDamage()
    {
        bite.Damage = 1;
    }

    public void LungeDistanceDuration(string parameters)
    {
        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist > Actions[1].MinDistance)
        {
            var parts = parameters.Split(';').Select(float.Parse).ToArray();
            float distance = parts[0], duration = parts[1];

            float finalDistance;
            if (dist >= distance)
            {
                finalDistance = distance;
            }
            else
            {
                finalDistance = dist - 1.5f;
            }

            Lunge(distance, duration);
        }
    }

    public override void OnActionEnd()
    {
        base.OnActionEnd();
    }

    protected override void Die()
    {
        _animator.SetBool("Died", true);
        MainCollider.enabled = false;
        BiteCollider.enabled = false;
        base.Die();
        this.enabled = false;
    }
}
