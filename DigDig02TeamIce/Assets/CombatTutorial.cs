using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTutorial : MonoBehaviour
{
    private static GameObject playerAndSuch;
    private Player player;
    private CameraMovement _camera;
    private Companion _companion;

    public Enemy MeleeTutorialEnemy;
    public Enemy SpearTutorialEnemy;

    public Canvas Canvas;
    public CanvasGroup ParryInputGroup;
    public CanvasGroup AttackInputGroup;
    public CanvasGroup LockOnInputGroup;
    public CanvasGroup SpearAtackInputGroup;

    private void Start()
    {
        playerAndSuch = GameObject.Find("PERSISTOBJECTS");
        if (playerAndSuch == null)
        {
            playerAndSuch = GameObject.Find("PERSISTOBJECTS(Clone)");
        }

        if (playerAndSuch != null)
        {
            player = GameObject.FindObjectOfType<Player>();
            _camera = GameObject.FindObjectOfType<CameraMovement>();
            _companion = GameObject.FindObjectOfType<Companion>();
        }
        else
        {
            Debug.LogError($"Could not find PERSISTOBJECTS at {name}!");
            return;
        }

        player.Energy = 0;
        SpearTutorialEnemy.CanAttack = false;

        FadeGroup(ParryInputGroup, 0f, Color.black, 0f);
        FadeGroup(LockOnInputGroup, 0f, Color.black, 0f);
        FadeGroup(SpearAtackInputGroup, 0f, Color.black, 0f);

        StartCoroutine(CombatTutorialSequence());
    }

    private IEnumerator CombatTutorialSequence()
    {
        while (!MeleeTutorialEnemy.InCombat)
        {
            yield return null;
        }

        FadeGroup(AttackInputGroup, 1f, Color.white, 0.75f);

        while (!MeleeTutorialEnemy.Dead)
        {
            yield return null;
        }

        FadeGroup(AttackInputGroup, 0f, Color.black);

        while (!SpearTutorialEnemy.InCombat)
        {
            yield return null;
        }

        FadeGroup(LockOnInputGroup, 1f, Color.white);

        while (player.currentTarget == null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.25f);

        FadeGroup(LockOnInputGroup, 0f, Color.black, 0.75f);

        yield return new WaitForSeconds(0.25f);

        FadeGroup(ParryInputGroup, 1f, Color.white, 0.75f);
        SpearTutorialEnemy.CanAttack = true;

        while (player.Energy < 4)
        {
            yield return null;
        }

        FadeGroup(ParryInputGroup, 0f, Color.black, 0.75f);

        yield return new WaitForSeconds(0.25f);

        FadeGroup(SpearAtackInputGroup, 1f, Color.white, 0.75f);
        _companion.lockSpearAttack = false;

        int spearsUsed = 0;
        bool fadedGroup = false;
        while (!SpearTutorialEnemy.Dead)
        {
            if (UserInput.SpearAttackPressed)
            {
                spearsUsed++;
            }

            if (spearsUsed > 2 && !fadedGroup)
            {
                FadeGroup(SpearAtackInputGroup, 0f, Color.black, 0.75f);
                fadedGroup = true;
            }

            yield return null;
        }
    }

    public void FadeGroup(CanvasGroup group, float toAlpha, Color toColor, float duration = 1.25f)
    {
        StartCoroutine(FadeRoutine(group, toAlpha, toColor, duration));
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float toAlpha, Color toColor, float duration)
    {
        float time = 0f;

        float startAlpha = group.alpha;

        CanvasColorGroup colorGroup = group.gameObject.GetComponent<CanvasColorGroup>();

        // Tint starts as "no tint"
        Color startTint = Color.white;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            group.alpha = Mathf.Lerp(startAlpha, toAlpha, t);

            if (colorGroup != null)
            {
                Color currentTint = Color.Lerp(startTint, toColor, t);

                foreach (var kvp in colorGroup.OriginalColors)
                {
                    var g = kvp.Key;
                    if (g == null) continue;

                    Color baseColor = kvp.Value;

                    g.color = new Color(
                        baseColor.r * currentTint.r,
                        baseColor.g * currentTint.g,
                        baseColor.b * currentTint.b,
                        baseColor.a // preserve alpha
                    );
                }
            }

            yield return null;
        }

        group.alpha = toAlpha;

        if (colorGroup != null)
        {
            foreach (var kvp in colorGroup.OriginalColors)
            {
                var g = kvp.Key;
                if (g == null) continue;

                Color baseColor = kvp.Value;

                g.color = new Color(
                    baseColor.r * toColor.r,
                    baseColor.g * toColor.g,
                    baseColor.b * toColor.b,
                    baseColor.a
                );
            }
        }
    }
}
