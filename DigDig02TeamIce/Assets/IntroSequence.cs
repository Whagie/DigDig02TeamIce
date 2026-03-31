using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    private static GameObject playerAndSuch;
    private Player player;
    private CameraMovement _camera;
    private Companion _companion;

    public Transform ConstructStartPos;
    public Transform ConstructTargetPos;
    private Vector3 direction;

    public GameObject BreakableWall;

    public Canvas Canvas;
    public CanvasGroup MoveInputGroup;
    public CanvasGroup AttackInputGroup;
    public CanvasGroup SlamAttackInputGroup;
    public CanvasGroup CrystalRechargeHelperNoteGroup;

    [Space]
    public TriggerRelay MovementTutorialTrigger;

    private bool touchedMoveTutorialTrigger = false;
    private bool playerRan = false;
    private float runTimer = 0f;

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

        player.lockMeleeAttack = true;
        player.Energy = 0;

        direction = ConstructTargetPos.position - ConstructStartPos.position;
        direction.Normalize();
        direction.y = 0f;

        _companion.StopMovement();
        _companion.movementOverride = true;
        _companion.lockSpearAttack = true;
        _companion.lockSlamAttack = true;

        _companion.transform.position = ConstructStartPos.position;
        _companion.transform.rotation = Quaternion.LookRotation(direction);
        _companion.transform.localScale = Vector3.one * 0.05f;

        MovementTutorialTrigger.OnEnter += MovementTutorialTrigger_OnEnter;

        FadeGroup(MoveInputGroup, 0f, Color.black, 0f);
        FadeGroup(AttackInputGroup, 0f, Color.black, 0f);
        FadeGroup(SlamAttackInputGroup, 0f, Color.black, 0f);
        FadeGroup(CrystalRechargeHelperNoteGroup, 0f, Color.black, 0f);

        MusicManager.instance.AudioSourceA.volume = 0f;

        StartCoroutine(IntroSequenceRoutine());
    }

    private void Update()
    {
        if (playerRan)
            return;

        bool isRunning = UserInput.SprintHeld && player.moveDir.magnitude > 0.05f;

        if (isRunning)
        {
            runTimer += Time.deltaTime;

            if (runTimer >= 0.5f)
                playerRan = true;
        }
        else
        {
            runTimer = 0f;
        }
    }

    private void MovementTutorialTrigger_OnEnter(Collider other)
    {
        if (touchedMoveTutorialTrigger)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            touchedMoveTutorialTrigger = true;
        }
    }

    private IEnumerator IntroSequenceRoutine()
    {
        yield return new WaitForSeconds(3f);

        FadeGroup(MoveInputGroup, 1f, Color.white);

        yield return new WaitForSeconds(1.5f);
        while (!touchedMoveTutorialTrigger || !playerRan)
        {
            yield return null;
        }

        FadeGroup(MoveInputGroup, 0f, Color.black);

        yield return new WaitForSeconds(2f);

        SoundFXManager.instance.PlaySoundFXClipLooping(FX.FX_intro_metal_hits, _companion.transform, out AudioSource audioSource, 1f);

        yield return new WaitForSeconds(9f);

        Destroy(audioSource.gameObject);

        StartCoroutine(_companion.DoorEntranceAnimation(ConstructStartPos.position, ConstructTargetPos.position, direction, true));

        Vector3 startSize = _companion.transform.localScale;
        float time = 0f;
        float duration = 0.25f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            _companion.transform.localScale = Vector3.Lerp(startSize, Vector3.one, t);
        }

        _companion.transform.localScale = Vector3.one;

        while (_companion.isPlayingEntranceAnim)
        {
            yield return null;
        }

        _companion.movementOverride = false;

        MusicManager.instance.AudioSourceA.volume = 1f;

        yield return new WaitForSeconds(1.5f);

        FadeGroup(AttackInputGroup, 1f, Color.white);
        player.lockMeleeAttack = false;

        float time2 = 0f;
        bool startedHelperNoteFade = false;

        while (player.Energy < 2)
        {
            time2 += Time.deltaTime;
            if (time2 >= 18f && !startedHelperNoteFade)
            {
                FadeGroup(CrystalRechargeHelperNoteGroup, 1f, Color.white, 2f);
                startedHelperNoteFade = true;
            }

            yield return null;
        }

        FadeGroup(AttackInputGroup, 0f, Color.black);
        FadeGroup(CrystalRechargeHelperNoteGroup, 0f, Color.black);

        yield return new WaitForSeconds(1f);

        FadeGroup(SlamAttackInputGroup, 1f, Color.white);
        _companion.lockSlamAttack = false;

        while (BreakableWall != null)
        {
            yield return null;
        }

        FadeGroup(SlamAttackInputGroup, 0f, Color.black);
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
