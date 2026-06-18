using System;
using System.Collections.Generic;
using UnityEngine;

/*
    # SAFETY
    objects must be manullay removed from the spatial dictionary when they are destroyed
*/

// todo: better name
// maybe `Space`, though that conflicts with `UnityEngine.Space`
public struct SpatialDictionary
{
    private Dictionary<Vector2Int, Entry> inner;

    // 'parameterless struct constructors' is not available in C# 9.0.
    public SpatialDictionary Init()
    {
        inner = new();
        return this;
    }

    static internal SpatialDictionary From(Dictionary<Vector2Int, Entry> inner)
    {
        return new() { inner = inner };
    }

    public struct Entry
    {
        private List<MonoBehaviour> inner;

        // 'parameterless struct constructors' is not available in C# 9.0.
        public Entry Init()
        {
            inner = new();
            return this;
        }

        public int Count { get { return inner.Count; } }

        int FindIndex(MonoBehaviour obj)
        {
            return inner.FindIndex(other => { return ReferenceEquals(other, obj); });
        }
        
        public bool Contains(MonoBehaviour obj)
        {
            return FindIndex(obj) != -1;
        }
        
        public void Add(MonoBehaviour obj)
        {
            if (Contains(obj)) { return; }
            inner.Add(obj);
        }
        
        public bool Remove(MonoBehaviour obj)
        {
            return inner.SwapRemove(FindIndex(obj));
        }

        public List<MonoBehaviour>.Enumerator GetEnumerator()
        {
            return inner.GetEnumerator();
        }
    }
    
    public Entry Get(Vector2Int pos)
    {
        if(inner.TryGetValue(pos, out Entry entry))
        {
            return entry;
        }

        entry = new Entry().Init();
        inner[pos] = entry;
        
        return entry;
    }

    public static Vector2Int PositionOf(MonoBehaviour obj)
    {
        return obj.transform.position.XZ().RoundToInt();
    }
    
    public void Add(MonoBehaviour obj)
    {
        Get(PositionOf(obj)).Add(obj);
    }

    public void Add(MonoBehaviour obj, Vector2Int pos)
    {
        Get(pos).Add(obj);
    }

    public void Add<T>(MonoBehaviour obj, T positions) where T: IEnumerable<Vector2Int>
    {
        ForEach(positions, entry => entry.Add(obj));
    }

    public bool Remove(MonoBehaviour obj)
    {
        return Get(PositionOf(obj)).Remove(obj);
    }

    public bool Remove(MonoBehaviour obj, Vector2Int pos)
    {
        return Get(pos).Remove(obj);
    }

    public bool Remove<T>(MonoBehaviour obj, T positions) where T: IEnumerable<Vector2Int>
    {
        bool removed = true;
        ForEach(positions, (entry) => { removed &= entry.Remove(obj); });
        return removed;
    }

    public void ForEach<T>(T positions, Action<Entry, Vector2Int> func) where T: IEnumerable<Vector2Int>
    {
        foreach (Vector2Int pos in positions) { func(Get(pos), pos); }
    }

    public void ForEach<T>(T positions, Action<Entry> func) where T: IEnumerable<Vector2Int>
    {
        foreach (Vector2Int pos in positions) { func(Get(pos)); }
    }

    public bool TryForEach<T>(T positions, Func<Entry, Vector2Int, bool> func) where T: IEnumerable<Vector2Int>
    {
        foreach (Vector2Int pos in positions)
        {
            if (!func(Get(pos), pos)) { return false; }
        }
        return true;
    }

    public bool TryForEach<T>(T positions, Func<Entry, bool> func) where T: IEnumerable<Vector2Int>
    {
        foreach (Vector2Int pos in positions)
        {
            if (!func(Get(pos))) { return false; }
        }
        return true;
    }

    public bool AreOccupied<T>(T positions) where T: IEnumerable<Vector2Int>
    {
        return TryForEach(positions, (occupants, pos) => occupants.Count != 0);
    }

    public bool IsOccupied(Vector2Int position)
    {
        return Get(position).Count > 0;
    }

    public void DrawOccupied(Color color)
    {
        Color cached = Gizmos.color;
        Gizmos.color = color;
        foreach ((Vector2Int pos, Entry occupants) in inner)
        {
            if (occupants.Count == 0) { continue; }
            Gizmos.DrawCube(new Vector3(pos.x, -0.5f, pos.y), new Vector3(1f, 0.01f, 1f));
        }
        Gizmos.color = cached;
    }
}