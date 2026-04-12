using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class RunePuzzle : MonoBehaviourID
{
    [HideInInspector] public bool RunePuzzling = false;

    public bool Solved = false;

    public event System.Action OnSolve;

    public Animator DoorAnimator;

    public GameObject Door;
    public GameObject DoorSideStuff;
    public GameObject DiskCenter;
    public GameObject DiskPlatform;
    public GameObject InnerDisk;
    public GameObject MiddleDisk;
    public GameObject OuterDisk;

    private Material doorMaterial;
    private Material doorSideStuffMaterial;
    private Material diskCenterMaterial;
    private Material diskPlatformMaterial;
    private Material innerDiskMaterial;
    private Material middleDiskMaterial;
    private Material outerDiskMaterial;

    public int InnerDiskRuneCount = 6;
    public int MiddleDiskRuneCount = 10;
    public int OuterDiskRuneCount = 15;

    public int CorrectInnerRuneIndex = 4;
    public int CorrectMiddleRuneIndex = 6;
    public int CorrectOuterRuneIndex = 8;

    private float innerRuneCorrectRotation = 240f;
    private float middleRuneCorrectRotation = 144f;
    private float outerRuneCorrectRotation = 96f;

    public int innerRuneIndex = 0;
    public int middleRuneIndex = 0;
    public int outerRuneIndex = 0;

    public float RotationDuration = 0.5f;

    public Transform ConstructTargetPos;
    public Transform PlayerTargetPos;

    public CameraZoomTrigger cameraZoom;
    private CameraMovement _camera;

    private Player player;

    public Color DefaultColor = Color.black;
    public Color GlowColor = new Color(0.314f, 0.753f, 2.996f, 1.000f);
    static readonly int EmID = Shader.PropertyToID("_EmissionColor");

    public bool ShowInteractBubble = true;

    public Transform UIPos;
    public CanvasGroup InteractBubble;
    public CanvasGroup InputBubble;
    public Transform cam;

    public float IdleBobSpeed = 1f;
    public float IdleBobHeight = 0.5f;
    public float TalkingBobSpeed = 2f;
    public float TalkingBobHeight = 0.15f;
    private float bobSpeed;
    private float bobHeight;

    private Vector3 startPos;

    private Coroutine currentFade;

    private enum ActiveRuneDiskStates
    {
        None,
        Inner,
        Middle,
        Outer,
        All
    }
    private ActiveRuneDiskStates activeDisk = ActiveRuneDiskStates.None;

    private bool startupGlow = true;
    private bool allowForInputs = false;
    private bool firstColorChange = true;

    private Color prevInnerDiskColor;
    private Color prevMiddleDiskColor;
    private Color prevOuterDiskColor;
    private Color innerDiskTargetColor;
    private Color middleDiskTargetColor;
    private Color outerDiskTargetColor;
    private Color doorTargetColor;
    private Color doorSideStuffTargetColor;
    private float diskGlowTime = 0f;
    private float diskGlowDuration = 0.35f;
    private Coroutine startupGlowRoutine;
    private Coroutine diskGlowRoutine;
    private Coroutine stopAllGlowRoutine;
    private Coroutine inputCooldownRoutine;

    private SingleBoolData solvedPuzzleData;

    private void Start()
    {
        Renderer renderer1 = DiskCenter.GetComponent<Renderer>();
        Material[] mats1 = renderer1.materials;
        int matIndex1 = Array.FindIndex(mats1, m => m.name.Contains("RuneGlow"));
        diskCenterMaterial = mats1[matIndex1];
        diskCenterMaterial.EnableKeyword("_EMISSION");

        Renderer renderer2 = DiskPlatform.GetComponent<Renderer>();
        Material[] mats2 = renderer2.materials;
        int matIndex2 = Array.FindIndex(renderer2.sharedMaterials, m => m.name.Contains("RuneGlow"));
        diskPlatformMaterial = mats2[matIndex2];
        diskPlatformMaterial.EnableKeyword("_EMISSION");

        Renderer renderer3 = InnerDisk.GetComponent<Renderer>();
        Material[] mats3 = renderer3.materials;
        int matIndex3 = Array.FindIndex(renderer3.sharedMaterials, m => m.name.Contains("RuneGlow"));
        innerDiskMaterial = mats3[matIndex3];
        innerDiskMaterial.EnableKeyword("_EMISSION");

        Renderer renderer4 = MiddleDisk.GetComponent<Renderer>();
        Material[] mats4 = renderer4.materials;
        int matIndex4 = Array.FindIndex(renderer4.sharedMaterials, m => m.name.Contains("RuneGlow"));
        middleDiskMaterial = mats4[matIndex4];
        middleDiskMaterial.EnableKeyword("_EMISSION");

        Renderer renderer5 = OuterDisk.GetComponent<Renderer>();
        Material[] mats5 = renderer5.materials;
        int matIndex5 = Array.FindIndex(renderer5.sharedMaterials, m => m.name.Contains("RuneGlow"));
        outerDiskMaterial = mats5[matIndex5];
        outerDiskMaterial.EnableKeyword("_EMISSION");

        Renderer renderer6 = Door.GetComponent<Renderer>();
        Material[] mats6 = renderer6.materials;
        int matIndex6 = Array.FindIndex(renderer6.sharedMaterials, m => m.name.Contains("RuneGlow"));
        doorMaterial = mats6[matIndex6];
        doorMaterial.EnableKeyword("_EMISSION");

        Renderer renderer7 = DoorSideStuff.GetComponent<Renderer>();
        Material[] mats7 = renderer7.materials;
        int matIndex7 = Array.FindIndex(renderer7.sharedMaterials, m => m.name.Contains("RuneGlow"));
        doorSideStuffMaterial = mats7[matIndex7];
        doorSideStuffMaterial.EnableKeyword("_EMISSION");

        _camera = GameObject.FindObjectOfType<CameraMovement>();

        cam = Camera.main.transform;
        startPos = UIPos.localPosition;

        InteractBubble.alpha = 1f;
        InputBubble.alpha = 0f;

        if (!ShowInteractBubble)
        {
            InteractBubble.alpha = 0f;
        }

        bobHeight = IdleBobHeight;
        bobSpeed = IdleBobSpeed;

        if (SessionSaveData.Instance.TryGet(ID, out solvedPuzzleData))
        {
            Solved = solvedPuzzleData.IsTrue;
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, Solved);
        }

        if (Solved)
        {
            GlowColor *= 0.0125f;
            innerDiskMaterial.SetColor(EmID, GlowColor);
            middleDiskMaterial.SetColor(EmID, GlowColor);
            outerDiskMaterial.SetColor(EmID, GlowColor);
            diskCenterMaterial.SetColor(EmID, GlowColor);
            diskPlatformMaterial.SetColor(EmID, GlowColor);
            doorSideStuffMaterial.SetColor(EmID, GlowColor * 0.75f);
            doorMaterial.SetColor(EmID, GlowColor * 0.75f);

            InnerDisk.transform.localRotation = Quaternion.AngleAxis(innerRuneCorrectRotation, Vector3.up);
            MiddleDisk.transform.localRotation = Quaternion.AngleAxis(middleRuneCorrectRotation, Vector3.up);
            OuterDisk.transform.localRotation = Quaternion.AngleAxis(outerRuneCorrectRotation, Vector3.up);

            Destroy(UIPos.gameObject);

            DoorAnimator.Play("OpenDoor", -1, 1f);
        }
    }

    private void Update()
    {
        if (Solved)
            return;

        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        UIPos.localPosition = startPos + new Vector3(0f, yOffset, 0f);

        if (player == null)
            return;

        if (UserInput.InteractPressed && startupGlow && !RunePuzzling && !player.Companion.IsDoingRunePuzzle)
        {
            player.Companion.StartCarry(ConstructTargetPos, true);
            StartCoroutine(MovePlayer());
            RunePuzzling = true;
            player.Companion.IsDoingRunePuzzle = true;

            FadeTo(InputBubble, InteractBubble, 0.25f, true);

            _camera.SetOverrideTarget(new Vector3(0.5f, -2, 6f));
        }

        if (!RunePuzzling)
            return;

        if (player.Companion.CanExitRunePuzzleState && startupGlow)
        {
            if (firstColorChange)
            {
                GlowColor *= 0.0125f;
                firstColorChange = false;
            }

            if (startupGlowRoutine != null)
                StopCoroutine(startupGlowRoutine);
            startupGlowRoutine = StartCoroutine(StartupGlowRoutine());

            allowForInputs = false;
            startupGlow = false;
        }

        if (!allowForInputs)
            return;

        if (UserInput.EscapePressed)
        {
            allowForInputs = false;
            RunePuzzling = false;
            player.Companion.IsDoingRunePuzzle = false;
            player.Companion.CanExitRunePuzzleState = false;
            activeDisk = ActiveRuneDiskStates.None;

            StopCoroutine(diskGlowRoutine);
            diskGlowRoutine = null;

            if (stopAllGlowRoutine != null)
                StopCoroutine(stopAllGlowRoutine);
            stopAllGlowRoutine = StartCoroutine(StopAllGlowRoutine(false));

            _camera.ClearOverrideTarget();
            _camera.ClearOverrideTargetOffset();

            FadeTo(InteractBubble, InputBubble, 0.25f);
            MenuManager.instance.FadeGroup(MenuManager.instance.RunePuzzleGroup, 0f, 0.4f);

            player.MovementOverride = false;
            return;
        }

        if (UserInput.RunePuzzleLeftPressed)
        {
            RotateDisk(-1);
        }
        else if (UserInput.RunePuzzleRightPressed)
        {
            RotateDisk(1);
        }

        if (!allowForInputs)
            return;

        if (UserInput.RunePuzzleNextDiskPressed)
        {
            IterateThroughDisks(1);
        }
        else if (UserInput.RunePuzzlePreviousDiskPressed)
        {
            IterateThroughDisks(-1);
        }

        if (UserInput.InteractPressed)
        {
            if (CheckForCorrectRunes())
            {
                allowForInputs = false;
                RunePuzzling = false;
                player.Companion.IsDoingRunePuzzle = false;
                player.Companion.CanExitRunePuzzleState = false;
                activeDisk = ActiveRuneDiskStates.All;
                diskGlowDuration = 1f;
                UpdateDiskState();
                StartCoroutine(DoorOpeningRoutine());
                Destroy(UIPos.gameObject);
                MenuManager.instance.FadeGroup(MenuManager.instance.RunePuzzleGroup, 0f, 0.4f);
                OnSolve?.Invoke();
                SessionSaveData.Instance.AddOrUpdateData(ID, Solved);
            }
            else
            {
                allowForInputs = false;
                activeDisk = ActiveRuneDiskStates.None;
                diskGlowDuration = 1f;
                UpdateDiskState();
                StartCoroutine(WrongCodeRoutine());
            }
        }
    }

    private void LateUpdate()
    {
        if (Solved)
            return;

        if (cam != null && UIPos != null)
        {
            UIPos.LookAt(cam);
        }
    }

    private IEnumerator MovePlayer()
    {
        player.MovementOverride = true;
        player.animator.SetLayerWeight(0, 0.5f);
        Vector3 startPos = player.transform.position;
        Vector3 targetPos = PlayerTargetPos.position;
        Vector3 dir = startPos - PlayerTargetPos.position;
        Vector3 dir2 = startPos - ConstructTargetPos.position;
        dir.Normalize();
        dir2.Normalize();
        dir.y = 0f;
        dir2.y = 0f;
        Quaternion startRot = player.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir2, Vector3.up) * Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

        float time = 0f;
        float duration = 0.75f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            player.transform.position = Vector3.Lerp(startPos, targetPos, t);
            player.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            player.animator.SetFloat("Move", 1f);
            player.animator.SetFloat("MoveX", dir.x);
            player.animator.SetFloat("MoveZ", dir.y);

            yield return null;
        }

        player.animator.SetLayerWeight(0, 1f);
        player.transform.position = targetPos;
        player.animator.SetFloat("Move", 0f);
        player.animator.SetFloat("MoveX", 0f);
        player.animator.SetFloat("MoveZ", 0f);
    }

    private IEnumerator StartupGlowRoutine()
    {
        float time = 0f;
        float duration = 0.75f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color newColor = Color.Lerp(DefaultColor, GlowColor, t);

            diskCenterMaterial.SetColor(EmID, newColor);
            diskPlatformMaterial.SetColor(EmID, newColor);

            yield return null;
        }
        diskCenterMaterial.SetColor(EmID, GlowColor);
        diskPlatformMaterial.SetColor(EmID, GlowColor);

        MenuManager.instance.FadeGroup(MenuManager.instance.RunePuzzleGroup, 1f, 2f);

        float time2 = 0f;
        float duration2 = 0.75f;

        while (time2 < duration2)
        {
            time2 += Time.deltaTime;
            float t = time2 / duration2;

            Color newColor = Color.Lerp(DefaultColor, GlowColor, t);

            outerDiskMaterial.SetColor(EmID, newColor);

            yield return null;
        }
        outerDiskMaterial.SetColor(EmID, GlowColor);

        activeDisk = ActiveRuneDiskStates.Outer;
        UpdateDiskState();

        if (diskGlowRoutine != null)
            StopCoroutine(diskGlowRoutine);
        diskGlowRoutine = StartCoroutine(DiskGlowRoutine());

        startupGlowRoutine = null;
    }
    private IEnumerator StopAllGlowRoutine(bool allowInput = true)
    {
        float time = 0f;
        float duration = 1.5f;

        Color prevColor1 = innerDiskMaterial.GetColor(EmID);
        Color prevColor2 = middleDiskMaterial.GetColor(EmID);
        Color prevColor3 = outerDiskMaterial.GetColor(EmID);
        Color prevColor4 = GlowColor; // DiskCenter and DiskPlatform is guaranteed to be GlowColor at this point

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color newColor1 = Color.Lerp(prevColor1, DefaultColor, t);
            Color newColor2 = Color.Lerp(prevColor2, DefaultColor, t);
            Color newColor3 = Color.Lerp(prevColor3, DefaultColor, t);
            Color newColor4 = Color.Lerp(prevColor4, DefaultColor, t);

            innerDiskMaterial.SetColor(EmID, newColor1);
            middleDiskMaterial.SetColor(EmID, newColor2);
            outerDiskMaterial.SetColor(EmID, newColor3);
            diskCenterMaterial.SetColor(EmID, newColor4);
            diskPlatformMaterial.SetColor(EmID, newColor4);

            yield return null;
        }

        innerDiskMaterial.SetColor(EmID, DefaultColor);
        middleDiskMaterial.SetColor(EmID, DefaultColor);
        outerDiskMaterial.SetColor(EmID, DefaultColor);
        diskCenterMaterial.SetColor(EmID, DefaultColor);
        diskPlatformMaterial.SetColor(EmID, DefaultColor);

        FadeTo(InputBubble, InteractBubble, 0.25f);

        if (allowInput)
        {
            allowForInputs = true;
        }

        startupGlow = true;

        stopAllGlowRoutine = null;
    }

    private IEnumerator DiskGlowRoutine()
    {
        while (true)
        {
            while (diskGlowTime >= 5f)
                yield return null;

            while (diskGlowTime < diskGlowDuration)
            {
                diskGlowTime += Time.deltaTime;
                float t = diskGlowTime / diskGlowDuration;

                Color inner = Color.Lerp(prevInnerDiskColor, innerDiskTargetColor, t);
                Color middle = Color.Lerp(prevMiddleDiskColor, middleDiskTargetColor, t);
                Color outer = Color.Lerp(prevOuterDiskColor, outerDiskTargetColor, t);

                innerDiskMaterial.SetColor(EmID, inner);
                middleDiskMaterial.SetColor(EmID, middle);
                outerDiskMaterial.SetColor(EmID, outer);

                yield return null;
            }

            innerDiskMaterial.SetColor(EmID, innerDiskTargetColor);
            middleDiskMaterial.SetColor(EmID, middleDiskTargetColor);
            outerDiskMaterial.SetColor(EmID, outerDiskTargetColor);
            diskGlowTime = 10f;

            if (Solved)
            {
                StopCoroutine(diskGlowRoutine);
            }
        }
    }

    private void RotateDisk(int direction = 1)
    {
        if (direction != 1 && direction != -1)
            return;

        allowForInputs = false;
        StartCoroutine(RotateDiskRoutine(direction));
    }

    private IEnumerator RotateDiskRoutine(int direction)
    {
        GameObject disk;
        float degrees;

        switch (activeDisk)
        {
            case ActiveRuneDiskStates.Inner:
                disk = InnerDisk;
                degrees = 60f;
                break;
            case ActiveRuneDiskStates.Middle:
                disk = MiddleDisk;
                degrees = 36f;
                break;
            case ActiveRuneDiskStates.Outer:
                disk = OuterDisk;
                degrees = 24f;
                break;
            default:
                Debug.LogWarning("Tried to rotate rune disk, but no single disk was active!");
                yield break;
        }

        degrees *= direction;

        Quaternion startRot = disk.transform.localRotation;
        Quaternion targetRot = Quaternion.AngleAxis(disk.transform.localEulerAngles.y + degrees, Vector3.up);

        SoundFXManager.instance.PlaySoundFXClip(FX.FX_rotate_stone, transform, 0.9f, 1.35f, 0.75f);

        float time = 0f;

        while (time < RotationDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / RotationDuration);

            Quaternion rotation = Quaternion.Lerp(startRot, targetRot, t);
            disk.transform.localRotation = rotation;

            yield return null;
        }

        disk.transform.localRotation = targetRot;

        StepCurrentRuneIndex(activeDisk, direction * -1);

        if (inputCooldownRoutine != null)
            StopCoroutine(inputCooldownRoutine);
        inputCooldownRoutine = StartCoroutine(InputCooldown(0.05f));
    }

    private void StepCurrentRuneIndex(ActiveRuneDiskStates diskState, int direction, int count = 1)
    {
        int step = direction * count;

        switch (diskState)
        {
            case ActiveRuneDiskStates.Inner:
                innerRuneIndex = WrapIndex(innerRuneIndex + step, InnerDiskRuneCount);
                break;

            case ActiveRuneDiskStates.Middle:
                middleRuneIndex = WrapIndex(middleRuneIndex + step, MiddleDiskRuneCount);
                break;

            case ActiveRuneDiskStates.Outer:
                outerRuneIndex = WrapIndex(outerRuneIndex + step, OuterDiskRuneCount);
                break;

            case ActiveRuneDiskStates.All:
                innerRuneIndex = WrapIndex(innerRuneIndex + step, InnerDiskRuneCount);
                middleRuneIndex = WrapIndex(middleRuneIndex + step, MiddleDiskRuneCount);
                outerRuneIndex = WrapIndex(outerRuneIndex + step, OuterDiskRuneCount);
                break;
        }
    }

    private int WrapIndex(int index, int count)
    {
        return (index % count + count) % count;
    }

    private bool CheckForCorrectRunes()
    {
        if (innerRuneIndex == CorrectInnerRuneIndex &&
            middleRuneIndex == CorrectMiddleRuneIndex &&
            outerRuneIndex == CorrectOuterRuneIndex)
        {
            Solved = true;
            return true;
        }
        else
        {
            Solved = false;
            return false;
        }
    }

    private void IterateThroughDisks(int direction = 1)
    {
        if (direction != 1 && direction != -1)
            return;

        if (direction == 1)
        {
            switch (activeDisk)
            {
                case ActiveRuneDiskStates.Inner:
                    break;
                case ActiveRuneDiskStates.Middle:
                    activeDisk = ActiveRuneDiskStates.Inner;
                    break;
                case ActiveRuneDiskStates.Outer:
                    activeDisk = ActiveRuneDiskStates.Middle;
                    break;
                default:
                    break;
            }
        }
        else if (direction == -1)
        {
            switch (activeDisk)
            {
                case ActiveRuneDiskStates.Inner:
                    activeDisk = ActiveRuneDiskStates.Middle;
                    break;
                case ActiveRuneDiskStates.Middle:
                    activeDisk = ActiveRuneDiskStates.Outer;
                    break;
                case ActiveRuneDiskStates.Outer:
                    break;
                default:
                    break;
            }
        }

        UpdateDiskState();
    }

    private void UpdateDiskState()
    {
        PrepareDiskColors();
        diskGlowTime = 0f;

        if (inputCooldownRoutine != null)
            StopCoroutine(inputCooldownRoutine);
        inputCooldownRoutine = StartCoroutine(InputCooldown(0.25f));
    }

    private void PrepareDiskColors()
    {
        prevInnerDiskColor = innerDiskMaterial.GetColor(EmID);
        prevMiddleDiskColor = middleDiskMaterial.GetColor(EmID);
        prevOuterDiskColor = outerDiskMaterial.GetColor(EmID);

        switch (activeDisk)
        {
            case ActiveRuneDiskStates.None:
                innerDiskTargetColor = DefaultColor;
                middleDiskTargetColor = DefaultColor;
                outerDiskTargetColor = DefaultColor;
                break;

            case ActiveRuneDiskStates.Inner:
                innerDiskTargetColor = GlowColor;
                middleDiskTargetColor = DefaultColor;
                outerDiskTargetColor = DefaultColor;
                break;

            case ActiveRuneDiskStates.Middle:
                innerDiskTargetColor = DefaultColor;
                middleDiskTargetColor = GlowColor;
                outerDiskTargetColor = DefaultColor;
                break;

            case ActiveRuneDiskStates.Outer:
                innerDiskTargetColor = DefaultColor;
                middleDiskTargetColor = DefaultColor;
                outerDiskTargetColor = GlowColor;
                break;

            case ActiveRuneDiskStates.All:
                innerDiskTargetColor = GlowColor;
                middleDiskTargetColor = GlowColor;
                outerDiskTargetColor = GlowColor;
                break;

            default:
                break;
        }
    }

    private IEnumerator DoorOpeningRoutine()
    {
        if (cameraZoom != null)
        {
            if (_camera != null)
            {
                _camera.SetOverrideTarget(Door.transform, 0.75f);
                _camera.ZoomIn(-110f, 1f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        doorSideStuffTargetColor = GlowColor * 0.75f;

        float time = 0f;
        float duration = 0.6f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color doorSideStuff = Color.Lerp(DefaultColor, doorSideStuffTargetColor, t);

            doorSideStuffMaterial.SetColor(EmID, doorSideStuff);

            yield return null;
        }

        doorSideStuffMaterial.SetColor(EmID, doorSideStuffTargetColor);

        yield return new WaitForSeconds(0.5f);

        diskGlowTime = 0f;
        doorTargetColor = GlowColor * 0.75f;

        float time2 = 0f;
        float duration2 = 0.6f;

        while (time2 < duration2)
        {
            time2 += Time.deltaTime;
            float t = time2 / duration2;

            Color door = Color.Lerp(DefaultColor, doorTargetColor, t);

            doorMaterial.SetColor(EmID, door);

            yield return null;
        }

        doorMaterial.SetColor(EmID, doorTargetColor);

        yield return new WaitForSeconds(0.5f);

        DoorAnimator.SetTrigger("Open");

        float totalTime = DoorAnimator.runtimeAnimatorController.animationClips.Where(c => c.name == "OpenDoor").FirstOrDefault().length;

        // Plays for too long! Use PlayLooping and cancel when it's time 
        SoundFXManager.instance.PlaySoundFXClipLooping(FX.FX_Gears, Door.transform, out AudioSource sourceA, 0.3f, 0.8f);
        SoundFXManager.instance.PlaySoundFXClipLooping(FX.FX_StoneSlide, Door.transform, out AudioSource sourceB, 0.8f, 0.65f);

        yield return new WaitForSeconds(totalTime - 1.5f);

        SoundFXManager.instance.PlaySoundFXClip(FX.FX_DoorDrop, Door.transform, 2.5f);

        float time3 = 0f;
        float duration3 = 1f;

        float startVolumeA = sourceA.volume;
        float startVolumeB = sourceB.volume;

        while (time3 < duration3)
        {
            time3 += Time.deltaTime;
            float t = Mathf.Clamp01(time3 / duration3);

            sourceA.volume = Mathf.Lerp(startVolumeA, 0f, t);
            sourceB.volume = Mathf.Lerp(startVolumeB, 0f, t);

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        if (cameraZoom != null)
        {
            cameraZoom.ZoomBack();
            cameraZoom.Activated = false;
            cameraZoom.enabled = false;

            if (_camera != null)
            {
                _camera.ClearOverrideTarget();
                _camera.ClearOverrideTargetOffset();
            }
        }

        yield return new WaitForSeconds(0.35f);

        player.MovementOverride = false;
    }

    private IEnumerator WrongCodeRoutine()
    {
        if (inputCooldownRoutine != null)
            StopCoroutine(inputCooldownRoutine);

        SoundFXManager.instance.PlaySoundFXClip(FX.FX_construct_no_energy, transform, 1.25f);

        while (diskGlowTime < (diskGlowDuration - 0.05f))
        {
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        allowForInputs = false;

        activeDisk = ActiveRuneDiskStates.Outer;
        UpdateDiskState();
        allowForInputs = true;
    }

    private IEnumerator InputCooldown(float duration)
    {
        allowForInputs = false;
        yield return new WaitForSeconds(duration);
        allowForInputs = true;
        inputCooldownRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (player != null || Solved)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;

            bobHeight = TalkingBobHeight;
            bobSpeed = TalkingBobSpeed;

            FadeTo(InteractBubble, InputBubble, 0.25f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (player == null || Solved)
            return;

        if (other.GetComponentInParent<Player>() == player)
        {
            RunePuzzling = false;
            player.Companion.IsDoingRunePuzzle = false;

            bobSpeed = IdleBobSpeed;
            bobHeight = IdleBobHeight;

            FadeTo(InputBubble, InteractBubble, 0.25f);

            player = null;
        }
    }

    public void FadeTo(CanvasGroup from, CanvasGroup to, float duration, bool bothToZero = false)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeRoutine(from, to, duration, bothToZero));
    }

    IEnumerator FadeRoutine(CanvasGroup from, CanvasGroup to, float duration, bool bothToZero = false)
    {
        float t = 0f;

        float fromStart = from.alpha;
        float toStart = to.alpha;

        while (t < duration)
        {
            float a = t / duration;

            if (!bothToZero)
            {
                to.alpha = Mathf.Lerp(toStart, 1f, a);
            }
            from.alpha = Mathf.Lerp(fromStart, 0f, a);

            t += Time.deltaTime;
            yield return null;
        }

        // Final values
        from.alpha = 0f;
        if (!bothToZero)
        {
            to.alpha = 1f;
        }
        else
        {
            to.alpha = 0f;
        }
    }

    public void FadeInInteractBubble()
    {
        FadeTo(InputBubble, InteractBubble, 0.25f);
    }
}
