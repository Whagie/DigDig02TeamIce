using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PushableObject : MonoBehaviourID
{
    public PushableGrid Grid;

    public int LengthOnGridX = 1;
    public int LengthOnGridZ = 1;

    public bool CanPushX = true;
    public bool CanPushZ = true;

    public int StepsToMove = 0;
    public float MoveDurationPerStep = 0.5f;

    public bool HasOrigin;
    public Vector2Int OriginCoord;

    public PushableGridPoint OriginPoint =>
    HasOrigin && Grid != null
        ? Grid.Get(OriginCoord)
        : null;

    public bool Moving { get; private set; } = false;

    public bool MovesUntilStop;

    public OnPushEvent OnPush;

    [SerializeField] private float audioFadeOutDuration = 0.5f;

    [HideInInspector] public bool Solved = false;

    private SessionSaveData.PushableObjectData pushableData;

    private void Start()
    {
        if (!MovesUntilStop)
        {
            MovesUntilStop = this.CompareTag("MoveUntilStop");
        }

        if (SessionSaveData.Instance.TryGet(ID, out pushableData))
        {
            if (pushableData.Solved)
            {
                transform.position = pushableData.Position;
                OriginCoord = pushableData.OriginCoord;
                Solved = pushableData.Solved;

                if (Grid == null)
                    return;

                Grid.SetOccupiedArea(
                    OriginCoord,
                    LengthOnGridX,
                    LengthOnGridZ,
                    this
                );
            }
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, transform.position, OriginCoord, Solved);
        }
    }

    new void OnValidate()
    {
        if (Grid != null && HasOrigin)
            OnGetOrigin();
    }

    public Coroutine MoveSteps(Vector2Int gridDirection, int steps, float duration, System.Action<Vector3> onDelta)
    {
        return StartCoroutine(
            MoveStepsRoutine(gridDirection, steps, duration, onDelta)
        );
    }

    IEnumerator MoveStepsRoutine(Vector2Int gridDirection, int steps, float duration, System.Action<Vector3> onDelta)
    {
        if (Grid == null || !HasOrigin)
            yield break;

        int allowedSteps = ComputeMaxSteps(gridDirection, steps);
        if (allowedSteps == 0)
            yield break;

        Moving = true;
        OnPush?.Invoke();

        AudioSource audioSource = null;
        if (!MovesUntilStop)
        {
            SoundFXManager.instance.PlaySoundFXClip(FX.FX_light_puzzle_push_reflector, transform, 1f, 1.6f, 0.75f);
        }
        else
        {
            SoundFXManager.instance.PlaySoundFXClipLooping(FX.FX_bookshelf_rolling, transform, out audioSource, 1f);
        }

        Vector2Int startCoord = OriginCoord;
        Vector2Int targetCoord = startCoord + gridDirection * allowedSteps;

        // --- Convert to world positions ---
        Vector3 startPos = transform.position;
        Vector3 targetPos =
            Grid.transform.position +
            new Vector3(targetCoord.x * Grid.GridMargin, 0f, targetCoord.y * Grid.GridMargin) - CellExtents();

        Vector3 prevPos = startPos;
        float t = 0f;

        while (t < 1f)
        {
            t = Mathf.Min(t + Time.deltaTime / duration, 1f);

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            Vector3 delta = newPos - prevPos;

            transform.position = newPos;
            onDelta?.Invoke(delta);

            prevPos = newPos;

            yield return null;
        }

        Vector3 finalDelta = targetPos - prevPos;
        transform.position = targetPos;
        onDelta?.Invoke(finalDelta);

        // --- Update origin after move ---
        Grid.ClearOccupiedArea(this);

        OriginCoord = targetCoord;

        Grid.SetOccupiedArea(
            OriginCoord,
            LengthOnGridX,
            LengthOnGridZ,
            this
        );

        if (audioSource != null)
        {
            StartCoroutine(FadeOutSoundFX(audioSource, audioFadeOutDuration));
        }

        Moving = false;
    }

    int ComputeMaxSteps(Vector2Int dir, int requestedSteps)
    {
        int maxSteps = 0;

        for (int step = 1; step <= requestedSteps; step++)
        {
            if (IsStepBlocked(dir, step))
                break;

            maxSteps = step;
        }

        return maxSteps;
    }

    public int GetMaxSteps(Vector2Int gridDirection)
    {
        if (Grid == null || !HasOrigin)
            return 0;

        return ComputeMaxSteps(gridDirection, int.MaxValue);
    }

    bool IsStepBlocked(Vector2Int dir, int step)
    {
        // Moving along X axis
        if (dir.x != 0)
        {
            int edgeX = dir.x > 0
                ? OriginCoord.x + LengthOnGridX - 1 + step
                : OriginCoord.x - step;

            for (int z = 0; z < LengthOnGridZ; z++)
            {
                Vector2Int cell =
                    new Vector2Int(edgeX, OriginCoord.y + z);

                if (Grid.IsCellBlocked(cell, this))
                    return true;
            }
        }
        // Moving along Z axis
        else if (dir.y != 0)
        {
            int edgeZ = dir.y > 0
                ? OriginCoord.y + LengthOnGridZ - 1 + step
                : OriginCoord.y - step;

            for (int x = 0; x < LengthOnGridX; x++)
            {
                Vector2Int cell =
                    new Vector2Int(OriginCoord.x + x, edgeZ);

                if (Grid.IsCellBlocked(cell, this))
                    return true;
            }
        }

        return false;
    }

    public void OnGetOrigin()
    {
        if (!HasOrigin || Grid == null)
            return;

        Grid.SetOccupiedArea(
            OriginCoord,
            LengthOnGridX,
            LengthOnGridZ,
            this
        );
    }

    public void OnClearOrigin()
    {
        Grid.ClearOccupiedArea(this);
        HasOrigin = false;
        OriginCoord = Vector2Int.zero;
    }

    public static Vector2Int PushDirToGrid(Vector3 dir)
    {
        return Mathf.Abs(dir.x) > Mathf.Abs(dir.z)
            ? new Vector2Int((int)Mathf.Sign(dir.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(dir.z));
    }

    public Vector3 CellExtents()
    {
        return new Vector3(Grid.GridMargin * 0.5f, 0f, Grid.GridMargin * 0.5f);
    }

    public void SaveData()
    {
        if (Moving || Grid == null)
            return;

        SessionSaveData.Instance.AddOrUpdateData(ID, transform.position, OriginCoord, Solved);
    }

    public void MarkSolved()
    {
        Solved = true;
        SaveData();
    }

    private IEnumerator FadeOutSoundFX(AudioSource source, float duration)
    {
        float startVolume = source.volume;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            source.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        Destroy(source.gameObject);
    }
}

[Serializable]
public class OnPushEvent : UnityEvent { }
