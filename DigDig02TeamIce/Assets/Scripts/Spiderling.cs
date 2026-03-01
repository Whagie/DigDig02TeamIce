using FIMSpace.FProceduralAnimation;
using Game.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class Spiderling : Enemy
{
    public GameObject Bite;
    private MeleeAttack lunge;
    private MeleeAttack bite;

    public Collider MainCollider;
    public Collider BiteCollider;

    [SerializeField] private LegsAnimator legsAnimator;
    private Coroutine legBlendRoutine;

    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
    }
    protected override void InitializeActions()
    {
        Actions = new[]
        {
            new EnemyAction
            {
                TriggerName = "Bite",
                Weight = 0.6f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 2f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(WanderSpeed / 2f)
            },
            new EnemyAction
            {
                TriggerName = "Lunge",
                Weight = 0.8f,
                CanUse = () => SeeingPlayer && DistanceToPlayer >= 3.5f && DistanceToPlayer <= 7f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(0f)
            }
        };
    }

    protected override void Start()
    {
        base.Start();

        Collider = MainCollider;

        Bite.AddComponent<MeleeAttack>();

        bite = Bite.GetComponent<MeleeAttack>();
        bite.hitCollider = BiteCollider;
        bite.EnemyOwner = this;
        bite.LayerMask = LayerMask.GetMask("Player");

        lunge = Bite.GetComponent<MeleeAttack>();
        lunge.hitCollider = BiteCollider;
        lunge.EnemyOwner = this;
        lunge.LayerMask = LayerMask.GetMask("Player");

        BiteCollider.enabled = false;
    }

    protected override void Update()
    {
        base.Update();
        
        //for (int i = 0; i < Actions.Length; i++)
        //{
        //    Color textColor2;
        //    if (Actions[i].CanUse == null || Actions[i].CanUse())
        //    {
        //        textColor2 = Color.white;
        //    }
        //    else
        //    {
        //        textColor2 = Color.gray;
        //    }
        //    DrawUI.Draw(Actions[i].TriggerName, new Vector2(Screen.width * 0.9f, Screen.height * (0.05f + (0.05f * i))), textColor2, 10);
        //}

        //DrawUI.Draw($"Can rotate: {NavAgent.updateRotation}", new Vector2(Screen.width * 0.8f, Screen.height * 0.3f), Color.white, 8);
    }

    public override void HandleParried(IHurtbox by)
    {
        base.HandleParried(by);

        AlterBite(0);

        Debug.Log("Parried!");
    }

    public void AlterBite(int activate = 1)
    {
        if (bite == null || lunge == null)
            return; 

        if (activate == 1)
        {
            bite.Activate();
            lunge.Activate();
        }
        else
        {
            bite.Deactivate();
            lunge.Deactivate();
        }
    }

    public void SetBiteDamage(int newDamage)
    {
        bite.Damage = newDamage;
        lunge.Damage = newDamage;
    }
    public void ResetDamage()
    {
        bite.Damage = 1;
        lunge.Damage = 1;
    }

    public void ToggleRotation(int toggle)
    {
        if (toggle == 1)
        {
            NavAgent.updateRotation = false;
        }
        else
        {
            NavAgent.updateRotation = true;
        }
    }

    public void LungeDistanceDuration(string parameters)
    {
        var parts = parameters.Split(';').Select(float.Parse).ToArray();
        float distance = parts[0], duration = parts[1];

        if (DistanceToPlayer + 1 < distance) // Uhhhh help
        {
            distance -= (distance -  DistanceToPlayer);
            distance += 1f;
        }

        Lunge(distance, duration);
    }

    public override void OnActionStart(EnemyAction action)
    {
        if (legBlendRoutine != null)
            StopCoroutine(legBlendRoutine);

        legBlendRoutine = StartCoroutine(LegAnimatorBlendFade(0.01f, 0.2f));

        base.OnActionStart(action);
    }
    public override void OnActionEnd()
    {
        if (legBlendRoutine != null)
            StopCoroutine(legBlendRoutine);

        legBlendRoutine = StartCoroutine(LegAnimatorBlendFade(1f, 0.2f));

        base.OnActionEnd();
    }

    protected override void Die()
    {
        _animator.SetBool("Died", true);
        legsAnimator.LegsAnimatorBlend = 0f;

        // If dead is set to true before base.Die(), it means the enemy was dead in save data,
        // hence player has already killed them and no death animation
        if (Dead)
        {
            _animator.Play("KnockedOver", -1, 1f);
            _animator.Update(0f);
        }

        if (bite != null)
        {
            bite.Deactivate();
        }
        if (lunge != null)
        {
            lunge.Deactivate();
        }
        MainCollider.enabled = false;
        BiteCollider.enabled = false;
        base.Die();
        this.enabled = false;
    }

    private IEnumerator LegAnimatorBlendFade(float targetValue, float duration)
    {
        float startValue = legsAnimator.LegsAnimatorBlend;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);

            legsAnimator.LegsAnimatorBlend = Mathf.Lerp(startValue, targetValue, alpha);

            yield return null;
        }

        legsAnimator.LegsAnimatorBlend = targetValue;
        legBlendRoutine = null;
    }
}
