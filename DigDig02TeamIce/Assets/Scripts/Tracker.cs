using System;
using System.Collections.Generic;
using UnityEngine;

public class Tracker
{
    private Dictionary<Type, List<Entity>> registry = new();

    public void Register(Entity entity)
    {
        var type = entity.GetType();
        if (!registry.TryGetValue(type, out var list))
        {
            list = new List<Entity>();
            registry[type] = list;
        }
        list.Add(entity);
    }

    public void Unregister(Entity entity)
    {
        var type = entity.GetType();
        if (registry.TryGetValue(type, out var list))
        {
            list.Remove(entity);
        }
    }

    public T Get<T>() where T : Entity
    {
        if (registry.TryGetValue(typeof(T), out var list) && list.Count > 0)
            return list[0] as T;
        return null;
    }

    public List<T> GetAll<T>() where T : Entity
    {
        if (registry.TryGetValue(typeof(T), out var list))
        {
            var result = new List<T>(list.Count);
            foreach (var e in list)
                result.Add((T)e);
            return result;
        }
        return new List<T>();
    }
}