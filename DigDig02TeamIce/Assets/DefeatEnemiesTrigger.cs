using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DefeatEnemiesTrigger : MonoBehaviour
{
    public List<Enemy> EnemiesToBeKilled = new();
    public OnEnemiesKilledEvent OnAllKilled;

    public bool SlowDownTime = true;

    public bool LockPlayerMovement = true;
    public float DurationToLockPlayerMovement = 2f;

    public bool SetRespawnsLeft = false;
    public int RespawnsToSet = 0;

    public bool Activated = false;
    public bool ShouldMarkSelfActivated = true;

    public bool ResetPlayerHealthOnDefeat = true;

    private Player player;

    [SerializeField] private MonoBehaviourID emptyID;

    private SingleBoolData activatedData;

    private void Start()
    {
        emptyID = GetComponentInChildren<MonoBehaviourID>();

        player = FindObjectOfType<Player>();

        if (emptyID != null)
        {
            if (SessionSaveData.Instance.TryGet(emptyID.ID, out activatedData))
            {
                Activated = activatedData.IsTrue;
            }
            else
            {
                SessionSaveData.Instance.AddOrUpdateData(emptyID.ID, Activated);
            }
        }
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
            {
                amountDead++;
                continue;
            }

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

        if (ShouldMarkSelfActivated)
        {
            if (emptyID != null)
            {
                SessionSaveData.Instance.AddOrUpdateData(emptyID.ID, Activated);
            }
        }

        StartCoroutine(OnEnemiesKilledRoutine());
    }

    private IEnumerator OnEnemiesKilledRoutine()
    {
        if (SlowDownTime)
        {
            Freezer.LerpTimeScale(0.1f, 0.25f, 1f, 0.5f);

            while (Freezer.IsTimeScaling)
            {
                yield return null;
            }
        }

        OnAllKilled?.Invoke();
        if (player != null)
        {
            if (ResetPlayerHealthOnDefeat)
            {
                player.Health = player.MaxHealth;
            }
            if (LockPlayerMovement)
            {
                player.LockMovement(DurationToLockPlayerMovement);
            }
        }

        if (SetRespawnsLeft)
        {
            if (DeathSceneManager.Instance != null)
            {
                DeathSceneManager.Instance.AlterRespawnsLeft(RespawnsToSet);
            }
        }
    }

    public void MarkActivated()
    {
        Activated = true;
        if (emptyID != null)
        {
            SessionSaveData.Instance.AddOrUpdateData(emptyID.ID, Activated);
        }
    }
}

[Serializable]
public class OnEnemiesKilledEvent : UnityEvent { }

