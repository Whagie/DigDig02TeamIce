using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class WakeEnemies : MonoBehaviour
{
    public List<Enemy> EnemiesToWake = new List<Enemy>();

    public event System.Action WakeEnemiesEvent;

    public WakeEnemiesEvent OnWakeEnemies;

    public bool ForceTargetPlayerOnWake = false;

    public bool LockPlayerMovement = false;
    public float LockMovementDuration = 2f;

    public bool ActivateOnTriggerEnter = false;
    private Player player;

    public bool Activated = false;

    [SerializeField] private MonoBehaviourID emptyID;
    private SingleBoolData activatedData;

    private void Start()
    {
        foreach (var enemy in EnemiesToWake)
        {
            if (enemy == null) return;

            enemy.IsAwake = false;
        }

        emptyID = GetComponentInChildren<MonoBehaviourID>();

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

    private void OnTriggerEnter(Collider other)
    {
        if (player != null && !ActivateOnTriggerEnter || Activated)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;

            WakeUp();
        }
    }

    public void WakeUp()
    {
        if (Activated)
            return;

        if (player == null)
        {
            player = GameObject.FindObjectOfType<Player>();

            if (player == null)
                return;
        }

        int nullOrDeadCount = 0;

        foreach (var enemy in EnemiesToWake)
        {
            if (enemy == null)
            {
                nullOrDeadCount++;
                continue;
            }
            if (enemy.Dead)
            {
                nullOrDeadCount++;
                continue;
            }

            enemy.IsAwake = true;

            if (ForceTargetPlayerOnWake && enemy.NavAgent != null && player != null)
            {
                enemy.ShouldWander = false;
                enemy.NavAgent.SetDestination(player.transform.position);
            }
        }

        Activated = true;

        if (nullOrDeadCount == EnemiesToWake.Count && EnemiesToWake.Count != 0)
            return;

        WakeEnemiesEvent?.Invoke();
        OnWakeEnemies?.Invoke();

        if (LockPlayerMovement && player != null)
        {
            player.LockMovement(LockMovementDuration);
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
public class WakeEnemiesEvent : UnityEvent { }
