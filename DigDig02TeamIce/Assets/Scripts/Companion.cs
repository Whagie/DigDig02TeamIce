using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Companion : Entity
{
    public Player player;
    public Vector3 Offset;
    public Vector3 SpearOffset;

    [SerializeField] private GameObject Spear;

    public List<SpearAttackScript> previousSpears;
    private SpearAttackScript.SpearSpawnState lastState;

    public float spearAttackCooldown = 0.6f;

    public float slamCooldown = 1.1f;
    public float slamShockwaveRadius = 6f;

    private bool canAttack = true;

    protected override void OnEntityEnable()
    {
        Companion existing = GameObject.FindObjectOfType<Companion>();
        if (existing != null && existing != this)
        {
            Debug.Log("Companion already exists, cancelling spawn.");
            Destroy(gameObject);
            return;
        }

        base.OnEntityEnable();
    }
    protected override void OnStart()
    {
        base.OnStart();
        Enemy.OnSendEnergy += CollectEnergy;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        Enemy.OnSendEnergy -= CollectEnergy;
    }

    protected override void OnUpdate()
    {
        SpearAttack();
        SlamAttack();

        if (player != null)
        {
            transform.SetPositionAndRotation(player.transform.position + Offset, new Quaternion(transform.rotation.x, player.transform.rotation.y, transform.rotation.z, transform.rotation.w));
        }
    }

    public void SpearAttack()
    {
        if (UserInput.SpearAttackPressed && canAttack)
        {
            if (TryAttack(2))
            {
                SpearOffset = GetRandomSpawnPosition(transform, out var spawnState);

                GameObject instance = Instantiate(Spear, SpearOffset, Quaternion.identity);
                var spearAttack = instance.GetComponent<SpearAttackScript>();
                spearAttack.State = spawnState;

                previousSpears.Add(spearAttack);

                StartCoroutine(AttackCooldown(spearAttackCooldown));
            }
        }
    }

    public void SlamAttack()
    {
        if (UserInput.SlamAttackPressed && canAttack && !player.Parrying)
        {
            if (TryAttack(2))
            {
                StartCoroutine(SlamAttackRoutine());

                StartCoroutine(AttackCooldown(slamCooldown));
            }
        }
    }

    public bool TryAttack(int energyCost)
    {
        if (player.Energy >= energyCost)
        {
            player.ConsumeEnergy(energyCost);
            return true;
        }
        return false;
    }

    private void CollectEnergy(Vector3 senderPos)
    {

        GameObject prefab = VFX.Construct_GainEnergy;

        Vector3 dir = senderPos - transform.position;
        Quaternion rotation = Quaternion.LookRotation(dir);
        rotation *= Quaternion.Euler(0f, -90f, 0f);
        StartCoroutine(EnergyCollectEffectTimer(0.5f, prefab, transform, rotation, 1f));
    }

    private IEnumerator EnergyCollectEffectTimer(float time, GameObject instance, Transform transform, Quaternion dir, float lifetime)
    {
        yield return new WaitForSeconds(time);

        var instance2 = Instantiate(instance, transform.position, dir, transform);
        Destroy(instance2, lifetime);
    }

    private IEnumerator SlamAttackRoutine()
    {
        player.animator.SetBool("SlamAttacking", true);
        yield return new WaitForSeconds(0.2f);

        ParticleSpawner.Spawn(Particles.P_SlamAttack, player.transform.position);
        CameraActions.Main.Shake(0.3f, 0.15f, 0.1f);

        Collider[] enemyColliders = Physics.OverlapSphere(
            player.transform.position,
            slamShockwaveRadius,
            LayerMask.GetMask("Enemy")
        );

        HashSet<Enemy> affectedEnemies = new HashSet<Enemy>();

        foreach (var col in enemyColliders)
        {
            if (col == null)
                continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null)
            {
                enemy = GetComponent<Enemy>();
                if (enemy == null)
                    continue;
            }

            // Skip if we've already handled this enemy
            if (!affectedEnemies.Add(enemy))
                continue;

            Vector3 pushDir = player.transform.position - enemy.transform.position;
            Vector3 final = new Vector3(-pushDir.x, 0, -pushDir.z);

            enemy.ApplyPushback(final, 12f, 0.2f);
            enemy.Stun(4f);
        }

        yield return new WaitForSeconds(0.5f);
        player.animator.SetBool("SlamAttacking", false);
    }

    Vector3 GetRandomSpawnPosition(Transform origin, out SpearAttackScript.SpearSpawnState spawnState)
    {
        bool hasLeft = previousSpears.Exists(s => s.State == SpearAttackScript.SpearSpawnState.Left);
        bool hasRight = previousSpears.Exists(s => s.State == SpearAttackScript.SpearSpawnState.Right);
        bool hasTop = previousSpears.Exists(s => s.State == SpearAttackScript.SpearSpawnState.Top);

        SpearAttackScript.SpearSpawnState chosenState;

        if (!hasLeft && !hasRight)
        {
            chosenState = UnityEngine.Random.value < 0.5f ? SpearAttackScript.SpearSpawnState.Left : SpearAttackScript.SpearSpawnState.Right;
        }
        else if (hasLeft && !hasRight)
        {
            chosenState = SpearAttackScript.SpearSpawnState.Right;
        }
        else if (!hasLeft && hasRight)
        {
            chosenState = SpearAttackScript.SpearSpawnState.Left;
        }
        else if (!hasTop)
        {
            chosenState = SpearAttackScript.SpearSpawnState.Top;
        }
        else
        {
            float val = UnityEngine.Random.Range(0f, 9f);
            if (val <= 3f)
            {
                if (lastState != SpearAttackScript.SpearSpawnState.Left)
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Left;
                }
                else
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Right;
                }
            }
            else if (val <= 6f && val > 3f)
            {
                if (lastState != SpearAttackScript.SpearSpawnState.Right)
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Right;
                }
                else
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Left;
                }
            }
            else
            {
                if (lastState != SpearAttackScript.SpearSpawnState.Top)
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Top;
                }
                else
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Left;
                }
            }
        }

            // Define cube dimensions
        float halfWidth = 4f / 2f;
        float halfDepth = 2f / 2f;
        float halfHeight = 4f / 2f;
        float distance = 3f;

        // Random local offset inside cube
        float offsetX = UnityEngine.Random.Range(-halfWidth, halfWidth);
        float offsetY = UnityEngine.Random.Range(-halfHeight, halfHeight);
        float offsetZ = UnityEngine.Random.Range(-halfDepth, halfDepth);

        // Shift the cube depending on chosen side
        switch (chosenState)
        {
            case SpearAttackScript.SpearSpawnState.Left:
                offsetX -= (halfWidth + distance);
                break;
            case SpearAttackScript.SpearSpawnState.Right:
                offsetX += (halfWidth + distance);
                break;
            case SpearAttackScript.SpearSpawnState.Top:
                offsetY += (halfHeight + distance);
                break;
        }

        // Convert local offset to world space
        Vector3 localOffset = new Vector3(offsetX, offsetY, offsetZ);
        spawnState = chosenState;
        lastState = chosenState;
        return origin.TransformPoint(localOffset);
    }

    private IEnumerator AttackCooldown(float amount)
    {
        canAttack = false;
        yield return new WaitForSeconds(amount);
        canAttack = true;
    }
}
