using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this is to allow `{ get; init; }` properties
// see: https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

public static class Util
{
    // todo: better names
    
    public static Vector2[] DIRS = {
        new Vector2( 1,  0),
        new Vector2( 0,  1),
        new Vector2(-1,  0),
        new Vector2( 0, -1),
    };

    public static Vector2 RandomDir()
    {
        return DIRS[Random.Range(0, 4)];
    }

    public static Vector2Int[] DIRS_INT = {
        new Vector2Int( 1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int(-1,  0),
        new Vector2Int( 0, -1),
    };

    public static Vector2Int RandomDirInt()
    {
        return DIRS_INT[Random.Range(0, 4)];
    }
}

public static class Vector2Ext
{
    // swizzles

    public static Vector2 XZ(this Vector3 self)
    {
        return new(self.x, self.z);
    }

    public static Vector3 X0Y(this Vector2 self)
    {
        return new(self.x, 0f, self.y);
    }

    // float to int conversions

    public static Vector2Int RoundToInt(this Vector2 self)
    {
        return Vector2Int.RoundToInt(self);
    }

    // chaining component assignments

    public static Vector2 WithY(this Vector2 self, float y)
    {
        self.y = y;
        return self;
    }

    // misc

    public static int TaxicabDistance(this Vector2Int self, Vector2Int other)
    {
        return Mathf.Abs(self.x - other.x) + Mathf.Abs(self.y - other.y);
    }
}

public static class Vector2IntExt
{
    // swizzles

    public static Vector2Int XZ(this Vector3Int self)
    {
        return new(self.x, self.z);
    }

    public static Vector3Int X0Y(this Vector2Int self)
    {
        return new(self.x, 0, self.y);
    }

    // chaining component assignments

    public static Vector2Int WithY(this Vector2Int self, int y)
    {
        self.y = y;
        return self;
    }

    // iter

    public struct Iter : IEnumerator<Vector2Int>, IEnumerable<Vector2Int>
    {
        int xMin, yMin, xMax, yMax;
        Vector2Int index;

        public Vector2Int Current => index;
        object IEnumerator.Current => index;

        public Iter(int xMin, int yMin, int xMax, int yMax)
        {
            this.xMin = xMin;
            this.yMin = yMin;
            this.xMax = xMax;
            this.yMax = yMax;
            index = new(xMin - 1, yMin);
        }

        public void Dispose() {}

        public bool MoveNext()
        {
            index.x += 1;
            
            if (index.x > xMax)
            {
                index.y += 1;
                index.x = xMin;
            }

            if (index.y > yMax) { return false; }

            return true;
        }

        public void Reset()
        {
            index.x = xMin - 1;
            index.y = yMin;
        }

        public Iter GetEnumerator() { return this; }

        IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Iter Around(Vector2Int pos, int radius)
        {
            int xMin = pos.x - radius;
            int yMin = pos.y - radius;
            int xMax = pos.x + radius;
            int yMax = pos.y + radius;

            return new(xMin, yMin, xMax, yMax);
        }

        // todo: better name
        public static Iter Square(Vector2Int pos, int sideLength)
        {
            sideLength -= 1;
            int xMin = pos.x;
            int yMin = pos.y;
            int xMax = pos.x + sideLength;
            int yMax = pos.y + sideLength;

            return new(xMin, yMin, xMax, yMax);
        }
    }
}

public static class Vector3Ext {
    // chaining component assignments
    
    public static Vector3 WithY(this Vector3 self, float y)
    {
        self.y = y;
        return self;
    }
}

public static class Vector3IntExt {
    // chaining component assignments
    
    public static Vector3Int WithY(this Vector3Int self, int y)
    {
        self.y = y;
        return self;
    }
}

public static class ListExt
{
    public static bool Pop<T>(this List<T> self, out T val)
    {
        if (self.Count == 0)
        {
            val = default;
            return false;
        }
        val = self[self.Count - 1];
        self.RemoveAt(self.Count - 1);
        return true;
    }

    public static bool Pop<T>(this List<T> self)
    {
        if (self.Count == 0)
        {
            return false;
        }
        self.RemoveAt(self.Count - 1);
        return true;
    }

    public static void PopUnchecked<T>(this List<T> self, out T val)
    {
        val = self[self.Count - 1];
        self.RemoveAt(self.Count - 1);
    }

    public static void PopUnchecked<T>(this List<T> self)
    {
        self.RemoveAt(self.Count - 1);
    }

    public static bool SwapRemove<T>(this List<T> self, int index, out T val)
    {
        if (index >= self.Count)
        {
            val = default;
            return false;
        }

        val = self[index];
        self[index] = self[self.Count - 1];

        self.PopUnchecked();
        return true;
    }

    public static bool SwapRemove<T>(this List<T> self, int index)
    {
        if (index >= self.Count || index < 0)
        {
            return false;
        }

        self[index] = self[self.Count - 1];

        self.PopUnchecked();
        return true;
    }
}

