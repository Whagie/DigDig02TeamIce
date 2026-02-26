using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WakeEnemies : MonoBehaviour
{
    public List<Enemy> EnemiesToWake = new List<Enemy>();

    public event System.Action WakeEnemiesEvent;

    private void Start()
    {
        foreach (var enemy in EnemiesToWake)
        {
            if (enemy == null) return;

            enemy.IsAwake = false;
        }
    }

    public void WakeUp()
    {
        WakeEnemiesEvent?.Invoke();
        foreach (var enemy in EnemiesToWake)
        {
            if (enemy == null) return;

            enemy.IsAwake = true;
        }
    }
}
