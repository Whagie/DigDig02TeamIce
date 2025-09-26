using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public string EntityName { get; set; }
    public bool Active { get; set; } = true;

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
}
