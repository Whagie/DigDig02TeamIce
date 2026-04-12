using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatMusicController : MonoBehaviour
{
    public Enemy EnemyToDefeatBeforeMusicEnd;

    public bool RegisterEnemyWhenPlayerEnterCombat = false;
    private bool enteredCombat = false;

    private Player player;

    private void Start()
    {
        player = GameObject.FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (enteredCombat)
            return;

        if (RegisterEnemyWhenPlayerEnterCombat && player != null)
        {
            if (player.EnemiesInCombat.Count > 0)
            {
                enteredCombat = true;
                RegisterEnemy();
            }
        }
    }

    public void RegisterEnemy()
    {
        if (EnemyToDefeatBeforeMusicEnd == null) return;

        if (EnemyToDefeatBeforeMusicEnd.Dead) return;

        EnemyToDefeatBeforeMusicEnd.CanRegisterToCombatList = false;

        if (player != null)
        {
            if (player.EnemiesInCombat.Contains(EnemyToDefeatBeforeMusicEnd))
                return;

            player.EnemiesInCombat.Add(EnemyToDefeatBeforeMusicEnd);
        }
    }

    private void OnDisable()
    {
        KillEnemy();
    }

    public void KillEnemy()
    {
        if (EnemyToDefeatBeforeMusicEnd == null) return;

        if (EnemyToDefeatBeforeMusicEnd.Dead) return;

        if (player != null)
        {
            player.EnemiesInRoom.Remove(EnemyToDefeatBeforeMusicEnd);
            if (!player.EnemiesInCombat.Contains(EnemyToDefeatBeforeMusicEnd))
                return;

            player.EnemiesInCombat.Remove(EnemyToDefeatBeforeMusicEnd);
        }
    }
}
