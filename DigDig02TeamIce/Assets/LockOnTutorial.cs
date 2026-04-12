using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnTutorial : MonoBehaviour
{
    public Enemy combatEnemy;

    private Player player;

    private bool stopped = false;

    private void Start()
    {
        player = FindObjectOfType<Player>();
    }

    public void StartTutorial()
    {
        StartCoroutine(TutorialRoutine());
    }

    private void Update()
    {
        if (stopped)
            return;

        if (combatEnemy == null)
        {
            stopped = true;
            StopAllCoroutines();
            return;
        }

        if (combatEnemy.Dead)
        {
            stopped = true;
            StopAllCoroutines();
            return;
        }
    }

    private IEnumerator TutorialRoutine()
    {
        yield return new WaitForSeconds(8f);

        if (player != null)
        {
            if (player.currentTarget == null)
            {
                MenuManager.instance.FadeGroup(MenuManager.instance.LockOnTutorialGroup, 1f, 0.5f);
            }
        }

        float durationLockedOn = 0f;
        while (durationLockedOn < 3f)
        {
            if (player.currentTarget != null)
            {
                durationLockedOn += Time.deltaTime;
            }

            yield return null;
        }

        MenuManager.instance.FadeGroup(MenuManager.instance.LockOnTutorialGroup, 0f, 0.5f);
    }
}
