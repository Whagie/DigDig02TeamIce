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

    [SerializeField] private string gainEnergyResourcePath = "Construct_GainEnergy";
    private static GameObject gainEnergyVFXAsset;

    public List<SpearAttackScript> previousSpears;
    private SpearAttackScript.SpearSpawnState lastState;

    protected override void OnEntityEnable()
    {
        Companion existing = TrackerHost.Current.Get<Companion>();
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
        Enemy.OnSendEnergy += CollectEnergy;
    }

    protected override void OnUpdate()
    {
        if (player != null)
        {
            transform.SetPositionAndRotation(player.transform.position + Offset, new Quaternion(transform.rotation.x, player.transform.rotation.y, transform.rotation.z, transform.rotation.w));
        }
    }

    public void SpearAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (TryAttack(1))
            {
                SpearOffset = GetRandomSpawnPosition(transform, out var spawnState);

                GameObject instance = Instantiate(Spear, SpearOffset, Quaternion.identity);
                var spearAttack = instance.GetComponent<SpearAttackScript>();
                spearAttack.State = spawnState;

                previousSpears.Add(spearAttack);
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
        if (gainEnergyVFXAsset == null)
        {
            gainEnergyVFXAsset = GetVFXPrefab(gainEnergyResourcePath);
            if (gainEnergyVFXAsset == null) return;
        }

        GameObject prefab = gainEnergyVFXAsset;

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
}
