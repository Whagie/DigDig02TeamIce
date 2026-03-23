using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SpikeGate : MonoBehaviourID
{
    public List<GameObject> Stakes = new();
    public List<Collider> StakeColliders = new();

    private SessionSaveData.SingleBoolData spikeGateStateData;

    public float RaiseHeight = 4f;

    public float RaiseDuration = 0.4f;
    public float DropDuration = 0.75f;

    public bool Raised = true;

    public bool MoveCameraOnDrop = false;

    private Coroutine raiseRoutine;
    private Coroutine dropRoutine;

    private CameraMovement cameraMovement;

    private void Start()
    {
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

        if (SessionSaveData.Instance.TryGet(ID, out spikeGateStateData))
        {
            Raised = spikeGateStateData.IsTrue;
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, Raised);
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

    public void RaiseGates()
    {
        if (raiseRoutine != null)
            StopCoroutine(raiseRoutine);

        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        raiseRoutine = StartCoroutine(RaiseGatesRoutine());
    }

    public void DropGates(float waitPeriod = 0f)
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        if (raiseRoutine != null)
            StopCoroutine(raiseRoutine);

        dropRoutine = StartCoroutine(DropGatesRoutine(waitPeriod));

        if (MoveCameraOnDrop)
        {
            if (cameraMovement == null)
                return;

            cameraMovement.SetOverrideTarget(this.transform, 1f);
        }
    }

    private IEnumerator RaiseGatesRoutine()
    {
        Raised = true;
        SessionSaveData.Instance.AddOrUpdateData(ID, Raised);

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
        SessionSaveData.Instance.AddOrUpdateData(ID, Raised);

        yield return new WaitForSeconds(waitPeriod);

        float startHeight = Stakes[0].transform.localPosition.y;
        float targetHeight = 0f;

        float time = 0f;
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
            yield return new WaitForSeconds(waitPeriod * 0.5f);
            cameraMovement.ClearOverrideTarget();
        }

        dropRoutine = null;
    }
}
