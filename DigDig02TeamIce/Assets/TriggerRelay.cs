using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    public bool IsColliding { get; private set; } = false;

    public event Action<Collider> OnEnter;
    public event Action<Collider> OnExit;
    public event Action<Collider> OnStay;

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
