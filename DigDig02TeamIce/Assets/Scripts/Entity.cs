using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public int ID { get; set; }
    public string EntityName { get; set; }
    public bool Active { get; set; } = true;

    private float intervalTimer = 0f;

    protected virtual void Awake() => OnAwake();
    protected virtual void Start() => OnStart();

    // Update hooks, runs if Entity is Active
    protected virtual void Update() { if (Active) OnUpdate(); }
    protected virtual void FixedUpdate() { if (Active) OnFixedUpdate(); }
    protected virtual void OnEnable() => OnEntityEnable();
    protected virtual void OnDisable() => OnEntityDisable();
    protected virtual void OnDestroy() => OnEntityDestroy();

    // Rendering hooks
    protected virtual void OnPreRender() => PreRender();
    protected virtual void OnPostRender() => PostRender();
    protected virtual void OnRenderObject() => RenderObject();

    // Overridable hooks
    protected virtual void OnAwake() { }
    protected virtual void OnStart() { }
    protected virtual void OnUpdate() { }
    protected virtual void OnFixedUpdate() { }
    protected virtual void OnEntityEnable()
    {
        TrackerHost.Current.Register(this);
    }
    protected virtual void OnEntityDisable()
    {
        TrackerHost.Current.Unregister(this);
    }
    protected virtual void OnEntityDestroy() { }
    protected virtual void PreRender() { }
    protected virtual void PostRender() { }
    protected virtual void RenderObject() { }

    public static void RotateTowardsY(Transform obj, Vector3 targetPosition, float rotationSpeed)
    {
        // direction to target, flattened
        Vector3 direction = targetPosition - obj.position;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f) return; // avoid zero-length

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        obj.rotation = Quaternion.RotateTowards(obj.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    protected void OnInterval(float interval, Action action)
    {
        intervalTimer += Time.deltaTime;

        if (intervalTimer >= interval)
        {
            intervalTimer -= interval; // keep leftover time
            action?.Invoke();
        }
    }

    public GameObject GetVFXPrefab(string resourcePath)
    {
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
            Debug.LogWarning($"Failed to load VFX prefab at Resources/{resourcePath}");

        return prefab;
    }
}
