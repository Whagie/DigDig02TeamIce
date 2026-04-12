using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    public CanvasGroup MainMenuGroup;
    public CanvasGroup ContactsGroup;
    public CanvasGroup SaveDataNoteGroup;
    public CanvasGroup ExitGameGroup;
    public CanvasGroup OverlayImageGroup;
    public GameObject SaveDataExistsGroup;
    public GameObject StartButton;

    public CanvasGroup IntroCutsceneGroup;
    public VideoPlayer IntroCutscene;

    [SerializeField] private SceneField introPrisonScene;
    [SerializeField] private SceneField creditsScene;

    public Transform UIPos;

    public float bobSpeed = 1f;
    public float bobHeight = 0.5f;

    private bool switchingScenes = false;

    private Player player;
    private Companion companion;

    private bool openedNewGameMenu = false;
    private bool startNewGame = false;

    private void Awake()
    {
        MainMenuGroup.alpha = 0f;
        OverlayImageGroup.alpha = 1f;
        SaveDataNoteGroup.alpha = 0f;
        StartCoroutine(OpenMenuRoutine());

        ContactsGroup.alpha = 0f;
        ContactsGroup.blocksRaycasts = false;
        ContactsGroup.interactable = false;

        IntroCutsceneGroup.alpha = 0f;

        if (SaveSystem.HasSaveData())
        {
            SaveDataExistsGroup.SetActive(true);
            StartButton.SetActive(false);
        }
        else
        {
            SaveDataExistsGroup.SetActive(false);
            StartButton.SetActive(true);
        }

        player = GameObject.FindObjectOfType<Player>();
        companion = GameObject.FindObjectOfType<Companion>();

        player.MovementOverride = true;
        player.lockMeleeAttack = true;
        companion.lockSlamAttack = true;
        companion.lockSpearAttack = true;

        MenuManager.instance.CanPause = false;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        UIPos.localPosition = startPos + new Vector3(0f, yOffset, 0f);
    }

    public void CopyText(string text)
    {
        text.CopyToClipboard();
    }

    public void CloseGame()
    {
        if (SaveSystem.HasSaveData())
        {
            SaveSystem.Save();
        }
        Application.Quit();
    }

    public void OpenExitGameMenu()
    {
        TurnOnGroup(ExitGameGroup);
    }
    public void CloseExitGameMenu()
    {
        TurnOffGroup(ExitGameGroup);
    }

    public void OpenContactsMenu()
    {
        ContactsGroup.alpha = 1f;
        ContactsGroup.blocksRaycasts = true;
        ContactsGroup.interactable = true;
    }
    public void CloseContactsMenu()
    {
        ContactsGroup.alpha = 0f;
        ContactsGroup.blocksRaycasts = false;
        ContactsGroup.interactable = false;
    }
    public void CloseNewGameMenu()
    {
        ContactsGroup.alpha = 0f;
        ContactsGroup.blocksRaycasts = false;
        ContactsGroup.interactable = false;

        startNewGame = false;
        openedNewGameMenu = false;
    }
    public void StartNewGamePressed()
    {
        startNewGame = true;
        openedNewGameMenu = false;
    }

    public void StartGame(bool withData = false)
    {
        if (switchingScenes)
            return;

        SaveSystem.Load();
        StartCoroutine(StartGameRoutine(withData));
    }
    public void ContinueGame()
    {
        StartGame(true);
    }
    public void NewGame()
    {
        if (switchingScenes)
            return;

        startNewGame = false;
        openedNewGameMenu = true;

        StartCoroutine(NewGameRoutine());
    }

    public void LoadCredits()
    {
        if (switchingScenes)
            return;

        StartCoroutine(LoadCreditsRoutine());
    }

    private IEnumerator OpenMenuRoutine()
    {
        switchingScenes = true;

        FadeGroup(MainMenuGroup, 1f, Color.white, 1.5f);
        FadeGroup(OverlayImageGroup, 0f, Color.black, 1.5f);

        MusicManager.instance.Play(FX.Music_MainTheme, true);
        MusicManager.instance.FadeOutPrimary(1f, 1f);

        yield return new WaitForSeconds(1.1f);

        switchingScenes = false;
    }

    private IEnumerator LoadCreditsRoutine()
    {
        switchingScenes = true;

        FadeGroup(MainMenuGroup, 0f, Color.black, 1.5f);
        FadeGroup(OverlayImageGroup, 1f, Color.white, 1.5f);

        MusicManager.instance.FadeOutPrimary(1f, 0f);

        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene(creditsScene, LoadSceneMode.Single);
    }

    private IEnumerator NewGameRoutine()
    {
        switchingScenes = true;

        MarkInteractable(MainMenuGroup, false, 0f);
        FadeGroup(SaveDataNoteGroup, 1f, Color.white, 0.5f);
        MarkInteractable(SaveDataNoteGroup, true, 0.5f);

        yield return new WaitForSeconds(0.75f);

        while (openedNewGameMenu)
        {
            yield return null;
        }

        if (!startNewGame)
        {
            FadeGroup(SaveDataNoteGroup, 0f, Color.black, 0.5f);
            MarkInteractable(MainMenuGroup, true, 0.5f);
            MarkInteractable(SaveDataNoteGroup, false, 0f);
            switchingScenes = false;
        }
        else
        {
            switchingScenes = false;

            SaveSystem.Clear();
            FadeGroup(SaveDataNoteGroup, 0f, Color.black, 1f);
            MarkInteractable(SaveDataNoteGroup, false, 0f);
            MarkInteractable(MainMenuGroup, false, 0f);

            FadeGroup(MainMenuGroup, 0f, Color.black, 1.5f);
            FadeGroup(OverlayImageGroup, 1f, Color.white, 1.5f);

            StartGame();
        }
    }

    private IEnumerator StartGameRoutine(bool withData = false)
    {
        switchingScenes = true;

        FadeGroup(MainMenuGroup, 0f, Color.black, 1.5f);
        FadeGroup(OverlayImageGroup, 1f, Color.white, 1.5f);

        MusicManager.instance.FadeOutPrimary(1f, 0f);

        yield return new WaitForSeconds(2f);

        if (withData)
        {
            player.MovementOverride = false;
            player.lockMeleeAttack = false;
            companion.lockSlamAttack = false;
            companion.lockSpearAttack = false;
            SceneSwapManager.EnterSceneThroughSaveData();
        }
        else
        {
            yield return IntroCutsceneRoutine();

            player.MovementOverride = false;
            player.lockMeleeAttack = false;
            companion.lockSlamAttack = false;
            companion.lockSpearAttack = false;

            SceneSwapManager.LoadFromDeathScene = true;
            SceneSwapManager.SwapSceneFromDoorUse(introPrisonScene, DoorTriggerInteraction.DoorToSpawnAt.None, DoorTriggerInteraction.DoorToSpawnAt.None);
        }
    }

    private IEnumerator IntroCutsceneRoutine()
    {
        MusicManager.instance.Play(FX.Music_IntroCutscene, true);
        MusicManager.instance.FadeOutPrimary(1f, 1f);

        yield return new WaitForSeconds(1f);

        MusicManager.instance.AudioSourceA.loop = false;

        FadeGroup(IntroCutsceneGroup, 1f, Color.white, 1.5f);

        yield return new WaitForSeconds(1.5f);

        IntroCutscene.Play();

        while (!(IntroCutscene.frame >= (long)(IntroCutscene.frameCount - 60)))
        {
            yield return null;
        }

        IntroCutscene.playbackSpeed = 0.1f;

        yield return new WaitForSeconds(6f);

        FadeGroup(IntroCutsceneGroup, 0f, Color.black, 0.5f);

        yield return new WaitForSeconds(1.5f);

        MusicManager.instance.FadeOutPrimary(0.25f, 0f);

        IntroCutscene.Stop();

        yield return new WaitForSeconds(1);

        MusicManager.instance.AudioSourceA.loop = true;

        yield return new WaitForSeconds(1f);
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

    public void MarkInteractable(CanvasGroup group, bool interactable, float delay = 0f)
    {
        StartCoroutine(MarkInteractableRoutine(group, interactable, delay));
    }

    private IEnumerator MarkInteractableRoutine(CanvasGroup group, bool interactable, float delay)
    {
        yield return new WaitForSeconds(delay);

        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    public UIMenuHierarchy MainPauseMenu;
    public UIMenuHierarchy MainSettingsMenu;
    public UIMenuHierarchy AudioMenu;

    private UIMenuHierarchy currentMenu;

    private float prevMusicVolume = 1f;

    private Vector3 startPos;

    [Serializable]
    public class UIMenuHierarchy
    {
        public CanvasGroup Group;
        public int Order = 0;
    }

    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();
        startPos = UIPos.localPosition;

        currentMenu = MainPauseMenu;

        TurnOffGroup(MainSettingsMenu.Group);
        TurnOffGroup(AudioMenu.Group);
        TurnOnGroup(MainPauseMenu.Group);
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
}

public static class Utility
{
    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }
}
