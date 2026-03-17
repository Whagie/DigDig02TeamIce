using System;
using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    public bool IsColliding { get; private set; } = false;

    [HideInInspector] public float SphereColliderRadius = 1f;

    public event Action<Collider> OnEnter;
    public event Action<Collider> OnExit;
    public event Action<Collider> OnStay;

    private void Awake()
    {
        if (TryGetComponent<SphereCollider>(out SphereCollider sphere))
        {
            SphereColliderRadius = sphere.radius;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IsColliding = true;
        OnEnter?.Invoke(other);
    }
    private void OnTriggerExit(Collider other)
    {
        IsColliding = false;
        OnExit?.Invoke(other);
    }
    private void OnTriggerStay(Collider other)
    {
        IsColliding = true;
        OnStay?.Invoke(other);
    }
}
