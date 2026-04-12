using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NoteBook;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [HideInInspector] public bool CanPause = true;

    public UIMenuHierarchy ParentPauseMenu;
    public UIMenuHierarchy MainPauseMenu;
    public UIMenuHierarchy MainSettingsMenu;
    public UIMenuHierarchy AudioMenu;

    private UIMenuHierarchy currentMenu;

    public int RunesAquired = 0;
    public GameObject OuterRune;
    public GameObject MiddleRune;
    public GameObject InnerRune;

    public CanvasGroup RunePuzzleGroup;
    public CanvasGroup LightPuzzleGroup;

    public CanvasGroup NoteGroup;
    public TextMeshProUGUI NoteText;

    public CanvasGroup LockOnTutorialGroup;

    public SceneField MainMenu;

    private Player player;

    private float prevMusicVolume = 1f;

    [Serializable]
    public class UIMenuHierarchy
    {
        public CanvasGroup Group;
        public int Order = 0;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();

        currentMenu = MainPauseMenu;

        TurnOffGroup(ParentPauseMenu.Group);
        TurnOffGroup(MainSettingsMenu.Group);
        TurnOffGroup(AudioMenu.Group);
        TurnOnGroup(MainPauseMenu.Group);
    }

    void Update()
    {
        if (UserInput.PausePressed)
        {
            if (CanPause && !player.Dead && !player.Companion.IsDoingRunePuzzle && !SceneFadeManager.instance.IsFadingIn && !SceneFadeManager.instance.IsFadingOut)
            {
                if (!PauseManager.instance.IsPaused)
                {
                    Pause();
                }
                else
                {
                    Unpause();
                }
            }
        }
    }

    public void Pause()
    {
        PauseManager.instance.PauseGame();

        TurnOnGroup(ParentPauseMenu.Group);
        TurnOnGroup(MainPauseMenu.Group);

        TurnOffGroup(MainSettingsMenu.Group);
        TurnOffGroup(AudioMenu.Group);

        SoundFXManager.instance.PauseSoundEffects();
        SoundFXManager.instance.PlayUISoundFX(FX.FX_UI_Pause, true, 1f, 1.5f);

        prevMusicVolume = MusicManager.instance.AudioSourceA.volume;
        MusicManager.instance.AudioSourceA.volume = prevMusicVolume * 0.6f;

        currentMenu = MainPauseMenu;
    }
    public void Unpause()
    {
        PauseManager.instance.UnpauseGame();

        TurnOffGroup(ParentPauseMenu.Group);
        TurnOffGroup(AudioMenu.Group);
        TurnOffGroup(MainSettingsMenu.Group);

        TurnOnGroup(MainPauseMenu.Group);

        SoundFXManager.instance.PlayUISoundFX(FX.FX_UI_Unpause, true, 1f, 1.5f);
        SoundFXManager.instance.UnpauseSoundEffects();

        MusicManager.instance.AudioSourceA.volume = prevMusicVolume;

        currentMenu = MainPauseMenu;
    }

    public void OpenMainPauseMenu()
    {
        TurnOnGroup(MainPauseMenu.Group);

        TurnOffGroup(MainSettingsMenu.Group);
        TurnOffGroup(AudioMenu.Group);

        PickUISoundFX(MainPauseMenu);

        MusicManager.instance.AudioSourceA.volume = prevMusicVolume * 0.6f;

        currentMenu = MainPauseMenu;
    }

    public void OpenMainSettings()
    {
        TurnOnGroup(MainSettingsMenu.Group);

        TurnOffGroup(MainPauseMenu.Group);
        TurnOffGroup(AudioMenu.Group);

        PickUISoundFX(MainSettingsMenu);

        MusicManager.instance.AudioSourceA.volume = prevMusicVolume * 0.6f;

        currentMenu = MainSettingsMenu;
    }

    public void OpenAudioSettings()
    {
        TurnOnGroup(AudioMenu.Group);

        TurnOffGroup(MainPauseMenu.Group);
        TurnOffGroup(MainSettingsMenu.Group);

        PickUISoundFX(AudioMenu);

        MusicManager.instance.AudioSourceA.volume = prevMusicVolume;

        currentMenu = AudioMenu;

    }

    public void SaveAndQuit()
    {
        StartCoroutine(SaveAndQuitRoutine());
    }

    private IEnumerator SaveAndQuitRoutine()
    {
        SaveSystem.Save();
        CanPause = false;

        SceneFadeManager.instance.StartFadeOut();

        FadeGroup(ParentPauseMenu.Group, 0f, 1f, true);

        yield return new WaitForSecondsRealtime(2.5f);

        PauseManager.instance.UnpauseGame();

        TurnOffGroup(ParentPauseMenu.Group);
        TurnOffGroup(AudioMenu.Group);
        TurnOffGroup(MainSettingsMenu.Group);

        TurnOnGroup(MainPauseMenu.Group);

        SoundFXManager.instance.UnpauseSoundEffects();

        MusicManager.instance.AudioSourceA.volume = prevMusicVolume;

        currentMenu = MainPauseMenu;

        player.Companion.BlobShadow.maxAirHeight = 12f;

        yield return new WaitForSecondsRealtime(0.25f);

        SceneSwapManager.LoadFromDeathScene = true;
        SceneSwapManager.SwapSceneFromDoorUse(MainMenu, DoorTriggerInteraction.DoorToSpawnAt.None, DoorTriggerInteraction.DoorToSpawnAt.None);
    }

    private void TurnOffGroup(CanvasGroup group)
    {
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    private void TurnOnGroup(CanvasGroup group)
    {
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private void PickUISoundFX(UIMenuHierarchy targetMenu)
    {
        if (currentMenu.Order > targetMenu.Order)
        {
            SoundFXManager.instance.PlayUISoundFX(FX.FX_UI_Return);
        }
        else
        {
            SoundFXManager.instance.PlayUISoundFX(FX.FX_UI_Select);
        }
    }

    public void GetRune()
    {
        RunesAquired++;
        SaveSystem.Data.runesFound = RunesAquired;
        SaveSystem.Save();

        Vector3 runePos = new Vector3(-800, 325, 0);

        switch (RunesAquired)
        {
            case 1:
                StartCoroutine(RuneAnimationRoutine(InnerRune, runePos));
                break;

            case 2:
                StartCoroutine(RuneAnimationRoutine(MiddleRune, runePos + new Vector3(0, -100, 0)));
                break;

            case 3:
                StartCoroutine(RuneAnimationRoutine(OuterRune, runePos + new Vector3(0, -200, 0)));
                break;

            default:
                break;
        }
    }

    public void TurnOnRuneUI()
    {
        Vector3 runePos = new Vector3(-800, 325, 0);

        switch (RunesAquired)
        {
            case 1:
                StartCoroutine(RuneAnimationRoutine(InnerRune, runePos, true));
                break;

            case 2:
                StartCoroutine(RuneAnimationRoutine(MiddleRune, runePos + new Vector3(0, -100, 0), true));
                break;

            case 3:
                StartCoroutine(RuneAnimationRoutine(OuterRune, runePos + new Vector3(0, -200, 0), true));
                break;

            default:
                break;
        }
    }

    private IEnumerator RuneAnimationRoutine(GameObject rune, Vector3 uiPos, bool noAnimation = false)
    {
        CanvasGroup group = rune.GetComponent<CanvasGroup>();
        RectTransform rect = rune.GetComponent<RectTransform>();
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.zero;

        group.alpha = 0f;
        FadeGroup(group, 1f, 0.6f);

        float time = 0f;
        float duration = noAnimation ? 0f : 0.5f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            rect.localScale = Vector3.Slerp(Vector3.zero, Vector3.one * 30f, t);

            yield return null;
        }

        yield return new WaitForSeconds(noAnimation ? 0f : 0.75f);

        float time2 = 0f;
        float duration2 = noAnimation ? 0f : 1f;
        while (time2 < duration2)
        {
            time2 += Time.deltaTime;
            float t = Mathf.Clamp01(time2 / duration2);

            rect.localScale = Vector3.Slerp(Vector3.one * 30f, Vector3.one * 6f, t);
            rect.localPosition = Vector3.Slerp(Vector3.zero, uiPos, t);

            yield return null;
        }

        rect.localScale = Vector3.one * 6f;
        rect.localPosition = uiPos;
    }

    public void ApplyDialogue(DialogueData data)
    {
        NoteText.text = data.text;
        NoteText.fontSize = data.fontSize;
        NoteText.color = data.color;
        NoteText.alignment = data.alignment;
    }

    public void FadeGroup(CanvasGroup group, float toAlpha, float duration = 1.25f, bool unscaledTime = false)
    {
        StartCoroutine(FadeRoutine(group, toAlpha, duration, unscaledTime));
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float toAlpha, float duration, bool unscaledTime = false)
    {
        float time = 0f;

        float startAlpha = group.alpha;

        while (time < duration)
        {
            if (unscaledTime)
            {
                time += Time.unscaledDeltaTime;
            }
            else
            {
                time += Time.deltaTime;
            }

            float t = Mathf.Clamp01(time / duration);

            group.alpha = Mathf.Lerp(startAlpha, toAlpha, t);

            yield return null;
        }

        group.alpha = toAlpha;
    }
}
