using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WakeEnemies : MonoBehaviour
{
    public List<Enemy> EnemiesToWake = new List<Enemy>();

    public event System.Action WakeEnemiesEvent;

    public WakeEnemiesEvent OnWakeEnemies;

    public bool ActivateOnTriggerEnter = false;
    private Player player;

    private void Start()
    {
        foreach (var enemy in EnemiesToWake)
        {
            if (enemy == null) return;

            enemy.IsAwake = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (player != null && !ActivateOnTriggerEnter)
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
        WakeEnemiesEvent?.Invoke();
        OnWakeEnemies?.Invoke();

        foreach (var enemy in EnemiesToWake)
        {
            if (enemy == null) return;

            enemy.IsAwake = true;
        }
    }
}

[Serializable]
public class WakeEnemiesEvent : UnityEvent { }
