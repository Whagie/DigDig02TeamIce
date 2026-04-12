using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Credits : MonoBehaviour
{
    public GameObject CreditsObject;
    public Vector3 TargetPosition;
    public float MoveDuration;

    public CanvasGroup CreditsGroup;
    public CanvasGroup EscapeGroup;

    private bool startedEscapeRoutine = false;

    [SerializeField] private SceneField mainMenuScene;

    private Player player;
    private Companion companion;

    private void Awake()
    {
        MusicManager.instance.StopAllCoroutines();
        MusicManager.instance.Play(FX.Music_Credits, true);
        MusicManager.instance.AudioSourceA.volume = 0.35f;

        CreditsGroup.alpha = 0f;

        player = GameObject.FindObjectOfType<Player>();
        companion = GameObject.FindObjectOfType<Companion>();

        player.MovementOverride = true;
        player.lockMeleeAttack = true;
        companion.lockSlamAttack = true;
        companion.lockSpearAttack = true;

        MenuManager.instance.CanPause = false;

        StartCoroutine(CreditsRoutine());
    }

    private void Update()
    {
        if (UserInput.EscapePressed && !startedEscapeRoutine)
        {
            startedEscapeRoutine = true;
            StartCoroutine(EscapeCreditsRoutine());
        }
    }

    private IEnumerator CreditsRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        FadeGroup(CreditsGroup, 1f, Color.white, 1.5f);

        yield return new WaitForSeconds(3f);

        StartCoroutine(ScrollRoutine());

        yield return new WaitForSeconds(MoveDuration);

        yield return new WaitForSeconds(2f);

        FadeGroup(CreditsGroup, 0f, Color.black, 3f);
        FadeGroup(EscapeGroup, 0f, Color.black, 1.25f);

        yield return new WaitForSeconds(4f);

        MusicManager.instance.FadeOutPrimary(1f, 0f);

        yield return new WaitForSeconds(1.5f);

        MusicManager.instance.Play(FX.Music_IntroCutscene, true);
        MusicManager.instance.AudioSourceA.volume = 1f;

        yield return null;

        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    private IEnumerator ScrollRoutine()
    {
        Vector3 startPos = CreditsObject.transform.localPosition;
        float time = 0f;
        while (time < MoveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / MoveDuration);

            Vector3 pos = Vector3.Lerp(startPos, TargetPosition, t);
            CreditsObject.transform.localPosition = pos;

            yield return null;
        }
    }

    private IEnumerator EscapeCreditsRoutine()
    {
        FadeGroup(EscapeGroup, 1f, Color.white, 0.75f);

        yield return new WaitForSeconds(0.75f);

        float time = 0f;
        float duration = 5f;

        bool shouldEscape = false;

        while (time < duration)
        {
            time += Time.deltaTime;

            if (UserInput.EscapePressed)
            {
                time = duration + 1f;
                shouldEscape = true;
                yield return null;
            }

            yield return null;
        }

        if (shouldEscape)
        {
            FadeGroup(CreditsGroup, 0f, Color.black, 1.5f);
            FadeGroup(EscapeGroup, 0f, Color.black, 1.5f);

            MusicManager.instance.FadeOutPrimary(1f, 0f);

            yield return new WaitForSeconds(2f);

            startedEscapeRoutine = false;

            SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
        }
        else
        {
            FadeGroup(EscapeGroup, 0f, Color.black, 0.5f);

            yield return new WaitForSeconds(0.5f);

            startedEscapeRoutine = false;
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
