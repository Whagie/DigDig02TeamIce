using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DefeatEnemiesTrigger : MonoBehaviour
{
    public List<Enemy> EnemiesToBeKilled = new();
    public OnEnemiesKilledEvent OnAllKilled;

    public bool LockPlayerMovement = true;
    public float DurationToLockPlayerMovement = 2f;

    [HideInInspector] public bool Activated = false;
    private Player player;

    private void Start()
    {
        player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (Activated)
            return;

        if (EnemiesToBeKilled.Count <= 0)
            return;

        int amountDead = 0;
        foreach (var enemy in EnemiesToBeKilled)
        {
            if (enemy == null)
                continue;

            if (enemy.Dead)
            {
                amountDead++;
            }
        }

        if (amountDead >= EnemiesToBeKilled.Count)
        {
            OnEnemiesKilled();
        }
    }

    public void OnEnemiesKilled()
    {
        if (Activated)
            return;

        Activated = true;
        StartCoroutine(OnEnemiesKilledRoutine());
    }

    private IEnumerator OnEnemiesKilledRoutine()
    {
        Freezer.LerpTimeScale(0.1f, 0.25f, 1f, 0.5f);

        while (Freezer.IsTimeScaling)
        {
            yield return null;
        }

        OnAllKilled?.Invoke();
        if (player != null && LockPlayerMovement)
        {
            player.LockMovement(DurationToLockPlayerMovement);
        }
    }
}

[Serializable]
public class OnEnemiesKilledEvent : UnityEvent { }

