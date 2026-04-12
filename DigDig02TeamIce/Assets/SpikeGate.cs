using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SpikeGate : MonoBehaviour
{
    public List<GameObject> Stakes = new();
    public List<Collider> StakeColliders = new();

    private SingleBoolData spikeGateStateData;

    public float RaiseHeight = 4f;

    public float RaiseDuration = 0.4f;
    public float DropDuration = 0.75f;

    public bool Raised = true;

    public bool MoveCameraOnDrop = false;
    public float DurationBeforeCameraReset = 0.325f;

    public float DelayBeforeCameraMove = 0f;

    private Coroutine raiseRoutine;
    private Coroutine dropRoutine;

    private CameraMovement cameraMovement;

    [SerializeField] private MonoBehaviourID emptyID;

    private void Start()
    {
        emptyID = GetComponentInChildren<MonoBehaviourID>();

        if (Stakes.Count <= 0)
        {
            foreach (Transform t in transform.GetComponentsInChildren<Transform>())
            {
                if (t != transform)
                {
                    foreach (Transform t2 in t.GetComponentsInChildren<Transform>().Where(s => s.name == "stake"))
                    {
                        Stakes.Add(t2.gameObject);
                    }
                }
            }
        }

        if (emptyID != null)
        {
            if (SessionSaveData.Instance.TryGet(emptyID.ID, out spikeGateStateData))
            {
                Raised = spikeGateStateData.IsTrue;
            }
            else
            {
                SessionSaveData.Instance.AddOrUpdateData(emptyID.ID, Raised);
            }
        }

        foreach (GameObject stake in Stakes)
        {
            if (stake.TryGetComponent<Collider>(out var col))
            {
                StakeColliders.Add(col);
            }

            if (Raised)
            {
                Vector3 pos = stake.transform.localPosition;
                pos.y = 0f;

                stake.transform.localPosition = pos + Vector3.up * RaiseHeight;
            }
            else
            {
                Vector3 pos = stake.transform.localPosition;
                pos.y = 0f;

                stake.transform.localPosition = pos;
            }
        }

        cameraMovement = Camera.main.GetComponentInParent<CameraMovement>();
    }

    private void OnEnable()
    {
        SceneSwapManager.instance.OnStartSceneSwap += Instance_OnStartSceneSwap;
    }
    private void OnDisable()
    {
        SceneSwapManager.instance.OnStartSceneSwap -= Instance_OnStartSceneSwap;
    }

    private void Instance_OnStartSceneSwap()
    {
        if (SceneSwapManager.LoadFromDeathScene && emptyID != null)
        {
            SessionSaveData.Instance.RemoveSingleBoolData(emptyID.ID);
        }
    }

    public void RaiseGates()
    {
        if (raiseRoutine != null)
            StopCoroutine(raiseRoutine);

        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        raiseRoutine = StartCoroutine(RaiseGatesRoutine());

        SoundFXManager.instance.PlaySoundFXClip(FX.FX_gate_raise, transform, 1f);
    }

    public void DropGates(float waitPeriod = 0f)
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        if (raiseRoutine != null)
            StopCoroutine(raiseRoutine);

        dropRoutine = StartCoroutine(DropGatesRoutine(waitPeriod));
    }

    private IEnumerator RaiseGatesRoutine()
    {
        Raised = true;
        if (emptyID != null)
        {
            SessionSaveData.Instance.AddOrUpdateData(emptyID.ID, Raised);
        }

        foreach (Collider col in StakeColliders)
        {
            col.enabled = true;
        }

        float startHeight = Stakes[0].transform.localPosition.y;
        float targetHeight = RaiseHeight;

        float time = 0f;
        while (time < RaiseDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / RaiseDuration);
            float t2 = t * t * t;

            float lerpY = Mathf.Lerp(startHeight, targetHeight, t2);

            foreach (GameObject stake in Stakes)
            {
                Vector3 pos = stake.transform.localPosition;
                pos.y = lerpY;
                stake.transform.localPosition = pos;
            }
            yield return null;
        }

        foreach (GameObject stake in Stakes)
        {
            Vector3 pos = stake.transform.localPosition;
            pos.y = targetHeight;
            stake.transform.localPosition = pos;
        }

        raiseRoutine = null;
    }

    private IEnumerator DropGatesRoutine(float waitPeriod = 0f)
    {
        Raised = false;
        if (emptyID != null)
        {
            SessionSaveData.Instance.AddOrUpdateData(emptyID.ID, Raised);
        }

        if (MoveCameraOnDrop)
        {
            yield return new WaitForSeconds(DelayBeforeCameraMove);
            if (cameraMovement != null)
            {
                cameraMovement.SetOverrideTarget(this.transform, 1f);
            }
        }

        yield return new WaitForSeconds(waitPeriod);

        float startHeight = Stakes[0].transform.localPosition.y;
        float targetHeight = 0f;

        float time = 0f;
        bool playedAudio = false;
        while (time < DropDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / DropDuration);
            float t2 = t * t * t;

            float lerpY = Mathf.Lerp(startHeight, targetHeight, t2);

            foreach (GameObject stake in Stakes)
            {
                Vector3 pos = stake.transform.localPosition;
                pos.y = lerpY;
                stake.transform.localPosition = pos;
            }

            if (t >= 0.5f && !playedAudio)
            {
                SoundFXManager.instance.PlaySoundFXClip(FX.FX_gate_drop, transform, 1f);
                playedAudio = true;
            }

            yield return null;
        }

        foreach (GameObject stake in Stakes)
        {
            Vector3 pos = stake.transform.localPosition;
            pos.y = targetHeight;
            stake.transform.localPosition = pos;
        }

        foreach (Collider col in StakeColliders)
        {
            col.enabled = true;
        }

        if (MoveCameraOnDrop && cameraMovement != null)
        {
            yield return new WaitForSeconds(DurationBeforeCameraReset);
            cameraMovement.ClearOverrideTarget();
        }

        dropRoutine = null;
    }
}
