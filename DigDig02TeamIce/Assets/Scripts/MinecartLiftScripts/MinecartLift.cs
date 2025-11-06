using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class MinecartLift : MonoBehaviour
{
    [SerializeField] private Transform[] bonesToRotate;
    [SerializeField] private Transform[] bonesToLift;
    [SerializeField] private List<Vector3> startPositions;

    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float liftSpeed = 1f;

    private bool gateActive = false;
    private bool lifted = false;
    private bool startedLift = false;
    private bool rotated = false;
    private bool startedRotation = false;

    public bool idle = true;

    [SerializeField] private GameObject[] railPieces;
    [SerializeField] private Material originalRailMaterial;

    private Material sharedRailMaterial;
    private Color origEmissionColor;
    private Coroutine glowRoutine;

    [SerializeField] private Animator _animator;
    [SerializeField] private Spline Spline;
    public Minecart cart;
    public List<Minecart> cartQueue;
    public SplineFollowerPointLock pointLock;

    [SerializeField] private TriggerRelay gateCollider;
    private Vector3 gateColliderOrigPos;
    private Quaternion gateColliderOrigRot;

    private void Awake()
    {
        gateCollider.OnEnter += HandleGateTriggerEnter;
        gateCollider.OnExit += HandleTriggerExit;
        gateCollider.OnStay += GateCollider_OnStay;
    }

    private void Start()
    {
        // Create one shared copy up front
        sharedRailMaterial = new Material(originalRailMaterial);

        foreach (var rail in railPieces)
        {
            if (!rail.TryGetComponent(out SkinnedMeshRenderer meshRenderer))
                continue;

            var sharedMats = meshRenderer.sharedMaterials;
            var mats = meshRenderer.materials; // copy
            for (int i = 0; i < mats.Length; i++)
            {
                if (sharedMats[i] == originalRailMaterial)
                {
                    mats[i] = sharedRailMaterial;
                    origEmissionColor = mats[i].GetColor("_EmissionColor");
                }
            }
            meshRenderer.materials = mats; // assign back
        }

        foreach (var obj in bonesToLift)
        {
            startPositions.Add(obj.position);
        }

        gateColliderOrigPos = gateCollider.transform.position;
        gateColliderOrigRot = gateCollider.transform.rotation;
    }

    private void Update()
    {
        if (idle && cartQueue.Count > 0)
        {
            Minecart nextCart = cartQueue[0];

            // Only move if the cart is ready (not blocked and not already traveling)
            if (!nextCart.travelingToLift && !IsBlocked(nextCart))
            {
                cartQueue.RemoveAt(0);
                cart = nextCart;
                idle = false;
                cart.travelingToLift = true;

                // Smoothly accelerate toward lift
                ReleaseCart(cart.splineFollower, 5f, 1f, false);
            }
        }
    }

    private bool IsBlocked(Minecart cartToCheck)
    {
        // If the cart has a front collider touching something, it is blocked
        if (cartToCheck.frontCollider.IsColliding) return true;

        // Optional: you could also check the previous cart in queue
        if (cartQueue.Count > 1)
        {
            Minecart previous = cartQueue[1];
            if (previous.transform.position.z > cartToCheck.transform.position.z - 0.1f)
                return true;
        }

        return false;
    }


    public void ChangeRailColor()
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        glowRoutine = StartCoroutine(StopGlowRoutine());
    }
    private IEnumerator StopGlowRoutine()
    {
        float time = 0f;
        const float duration = 1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color newColor = Color.Lerp(origEmissionColor, Color.black, t);
            sharedRailMaterial.SetColor("_EmissionColor", newColor);

            yield return null;
        }

        sharedRailMaterial.SetColor("_EmissionColor", Color.black); // ensure final color
    }
    public void RevertRailColor()
    {
        if (glowRoutine != null) StopCoroutine(glowRoutine);
        glowRoutine = StartCoroutine(RevertGlowRoutine());
    }
    private IEnumerator RevertGlowRoutine()
    {
        float time = 0f;
        const float duration = 1f;

        // Start from whatever the material currently has
        Color startColor = sharedRailMaterial.GetColor("_EmissionColor");

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color newColor = Color.Lerp(startColor, origEmissionColor, t);
            sharedRailMaterial.SetColor("_EmissionColor", newColor);

            yield return null;
        }

        sharedRailMaterial.SetColor("_EmissionColor", origEmissionColor);
    }

    public void GateRaised()
    {
        if (!gateActive)
        {
            Debug.Log("Gate Raised!");
            gateActive = true;
            StopCoroutine(nameof(LiftRoutine));
            StartCoroutine(LiftRoutine());
        }
    }
    public IEnumerator LiftRoutine()
    {
        if (!rotated && !startedRotation)
        {
            Spin(90f);
        }
        while (!rotated)
        {
            yield return null;
        }

        if (!lifted && !startedLift)
        {
            Lift((9.2f / 100f));
        }
        while (!lifted)
        {
            yield return null;
        }

        RevertRailColor(); // Release potential cart
        if (cart != null)
        {
            cart.subscribedLift = null;
            cart.travelingToLift = false;
            cart.nextUp = false;
            ReleaseCart(cart.splineFollower, 10f, 1f, true);
        }
        yield return new WaitForSeconds(2f);
        ChangeRailColor();
        yield return new WaitForSeconds(1f);

        Debug.Log("Move down");
        Lift(-(9.2f / 100f));
        while (!lifted)
        {
            yield return null;
        }

        Spin(-90f);
        while (!rotated)
        {
            yield return null;
        }

        rotated = false;
        startedRotation = false;
        lifted = false;
        startedLift = false;

        _animator.SetBool("Descended", true);
        yield return new WaitForSeconds(0.05f);
        float length = _animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        idle = true;
        gateActive = false;
    }
    public void Spin(float degrees)
    {
        StopCoroutine(nameof(RotateBones));
        StartCoroutine(RotateBones(degrees));
        rotated = false;
        startedRotation = true;
    }

    private IEnumerator RotateBones(float degrees)
    {
        var startRotations = new Quaternion[bonesToRotate.Length];
        var targetRotations = new Quaternion[bonesToRotate.Length];

        for (int i = 0; i < bonesToRotate.Length; i++)
        {
            startRotations[i] = bonesToRotate[i].localRotation;
            targetRotations[i] = bonesToRotate[i].localRotation * Quaternion.Euler(0f, degrees, 0f);
        }

        float totalAngle = Mathf.Abs(degrees);
        float rotatedAngle = 0f;

        while (rotatedAngle < totalAngle)
        {
            // Rotate at constant rate, scaled by rotationSpeed
            float step = rotationSpeed * Time.deltaTime;
            rotatedAngle += step;
            float t = Mathf.Clamp01(rotatedAngle / totalAngle);

            // Apply smooth easing but still move at constant rate
            float smoothT = t * t * (3f - 2f * t); // SmoothStep manually

            for (int i = 0; i < bonesToRotate.Length; i++)
                bonesToRotate[i].localRotation = Quaternion.Slerp(startRotations[i], targetRotations[i], smoothT);

            yield return null;
        }

        for (int i = 0; i < bonesToRotate.Length; i++)
            bonesToRotate[i].localRotation = targetRotations[i];

        rotated = true;
        startedRotation = false;
    }

    public void Lift(float height)
    {
        StopCoroutine(nameof(LiftBones));
        StartCoroutine(LiftBones(height));
        lifted = false;
        startedLift = true;
    }

    private IEnumerator LiftBones(float height)
    {
        var startPositions = new Vector3[bonesToLift.Length];
        var targetPositions = new Vector3[bonesToLift.Length];

        for (int i = 0; i < bonesToLift.Length; i++)
        {
            startPositions[i] = bonesToLift[i].localPosition;
            targetPositions[i] = bonesToLift[i].localPosition + Vector3.up * height;
        }

        float totalDistance = Mathf.Abs(height);
        float movedDistance = 0f;

        while (movedDistance < totalDistance)
        {
            // Move at constant rate
            float step = liftSpeed * Time.deltaTime;
            movedDistance += step;
            float t = Mathf.Clamp01(movedDistance / totalDistance);

            // Smooth interpolation
            float smoothT = t * t * (3f - 2f * t);

            for (int i = 0; i < bonesToLift.Length; i++)
                bonesToLift[i].localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], smoothT);

            yield return null;
        }

        for (int i = 0; i < bonesToLift.Length; i++)
            bonesToLift[i].localPosition = targetPositions[i];

        lifted = true;
        startedLift = false;
    }

    private void HandleGateTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger enter from relay: {other.name}");

        if (other.CompareTag("Minecart"))
        {
            Minecart minecart = other.GetComponent<Minecart>();
            if (minecart == null) return;

            minecart.subscribedLift = this;
            minecart.nextUp = true;
            if (idle)
            {
                idle = false;
                minecart.splineFollower.followSpeed = 5f;
                Debug.Log("IS IDLE AND CARTED");
            }
            else
            {
                if (!minecart.travelingToLift && cartQueue.Count >= 1)
                {
                    if (!minecart.nextUp)
                    {
                        Debug.Log("IS NOT IDLE AND STOP CART");
                        ReceiveCart(minecart.splineFollower, 0.3f, false);
                    }
                    if (!cartQueue.Contains(minecart))
                    {
                        cartQueue.Add(minecart);
                    }
                }
                else
                {
                    minecart.splineFollower.followSpeed = 5f;
                }
            }
        }
    }
    private void GateCollider_OnStay(Collider other)
    {

    }
    private void HandleTriggerExit(Collider other)
    {
        Debug.Log($"Trigger exit from relay: {other.name}");
    }

    public void ReceiveCart(SplineFollower follower, float duration, bool activatePointLock, bool doNotStop = false)
    {
        if (!doNotStop)
        {
            StartCoroutine(MoveToZero(follower.followSpeed, duration, v => follower.followSpeed = v, activatePointLock));
        }
    }
    public void ReleaseCart(SplineFollower follower, float targetSpeed, float duration, bool nullifyCart)
    {
        StartCoroutine(MoveUpTo(targetSpeed, duration, v => follower.followSpeed = v));
        if (nullifyCart)
        {
            pointLock.Deactivate();
            cart = null;
            pointLock = null;
        }
    }
    IEnumerator MoveToZero(float startValue, float duration, Action<float> onValueChanged, bool activatePointLock = false)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t); // smooth easing
            float value = Mathf.Lerp(startValue, 0f, smoothT);
            onValueChanged(value);
            yield return null;
        }
        onValueChanged(0f);
        if (activatePointLock)
        {
            pointLock.Activate();
            LoadedCart();
        }
    }
    IEnumerator MoveUpTo(float targetValue, float duration, Action<float> onValueChanged)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t); // smooth easing in/out
            float value = Mathf.Lerp(0f, targetValue, smoothT);
            onValueChanged(value);
            yield return null;
        }
        onValueChanged(targetValue); // ensure exact final value
    }

    private void LoadedCart()
    {
        idle = false;
        _animator.SetTrigger("StartLift");
        _animator.SetBool("Descended", false);
    }
}
