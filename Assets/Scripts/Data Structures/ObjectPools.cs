using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/*
    # SAFETY
        once an object is added to a pool, it must only be destroyed 
        using `ObjectPools.Destroy`.

    todo: 
    -   methods for deactivating and reusing objects
        rather than just destroying them
    -   sub-headers?
*/

// todo: (optimisation) struct?
public partial class ObjectPools
{
    // todo: `PoolID` key type
    private Dictionary<int, ObjectPool> pools = new();
    private Dictionary<MonoBehaviour, List<ObjectPool>> poolsContaining = new(ReferenceEqualityComparer.Instance);
}

partial class ObjectPools
{
    public void Add<T>(T obj) where T: MonoBehaviour
    {
        Get<T>().Add(obj, true);
    }

    public void Add<T>(T obj, bool addToHeader) where T: MonoBehaviour
    {
        Get<T>().Add(obj, addToHeader);
    }

    public ObjectPool Get<T>()
    {
        Type type = typeof(T);
        int key = type.GetHashCode();

        bool poolNotFound = !pools.TryGetValue(key, out ObjectPool pool);
        if (poolNotFound) {
            pool = new(type.Name.ToString() + " Pool", poolsContaining);
            pools[key] = pool;
        }

        return pool;
    }

    public void Destroy(MonoBehaviour obj)
    {
        bool notPooled = !poolsContaining.Remove(obj, out var pools);
        if (notPooled) return;

        foreach (ObjectPool pool in pools)
        {
            pool.objects.Remove(obj);
        }

        UnityEngine.Object.Destroy(obj.gameObject);
    }
}

// todo: struct?
public partial class ObjectPool
{
    readonly string name;
    
    private Transform header;
    // todo: make private and implement `GetEnumerator`
    public readonly HashSet<MonoBehaviour> objects;
    readonly Dictionary<MonoBehaviour, List<ObjectPool>> poolsContaining;
}

partial class ObjectPool
{
    public ObjectPool(string name, Dictionary<MonoBehaviour, List<ObjectPool>> poolsContaining)
    {
        this.name = name;
        objects = new(ReferenceEqualityComparer.Instance);
        this.poolsContaining = poolsContaining;
    }

    public void Add(MonoBehaviour obj, bool addToHeader = false)
    {
        // check if `obj` has been pooled before
        if (poolsContaining.TryGetValue(obj, out List<ObjectPool> poolsContainingObj))
        {
            // check if `poolsContainingObj` already contains `pool`
            if (poolsContainingObj.Exists((p) => ReferenceEquals(p, this))) { return; }
            poolsContainingObj.Add(this);
        } else
        {
            poolsContaining.Add(obj, new() { this });
        }
        
        objects.Add(obj);

        if (!addToHeader) { return; }
        
        if (header == null)
        {
            header = new GameObject(name).transform;
        }
        
        obj.transform.parent = header;
    }
    
    public void Remove(MonoBehaviour obj, bool removeFromHeader = false)
    {
        // check if `pool` contains `obj`, and remove it
        bool wasNotInPool = !objects.Remove(obj);
        if (wasNotInPool) { return; }
        
        // Safety: 
        // if we reach here, `pool` did contain `obj`.
        // so, `poolsContaining` is guaranteed to have an entry for `obj`
        // and that entry is guaranteed to contain `pool`
        
        var poolsContainingObj = poolsContaining[obj];
        int index = poolsContainingObj.FindIndex(other => ReferenceEquals(other, this));
        poolsContainingObj.SwapRemove(index);

        if (removeFromHeader)
        {
            obj.transform.parent = null;
        }
    }

    public void Clear()
    {
        foreach (var obj in objects)
        {
            var poolsContainingObj = poolsContaining[obj];
            int index = poolsContainingObj.FindIndex((p) => ReferenceEquals(p, this));
            poolsContainingObj.SwapRemove(index);
        }
        objects.Clear();
    }

    public void AddToHeaderUnchecked(MonoBehaviour obj)
    {
        obj.transform.parent = header;
    }
}