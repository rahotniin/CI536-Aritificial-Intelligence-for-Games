using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps values sorted by their weights, in ascending order.
/// </summary>
public class MinHeap<T>
{
    List<Entry> entries = new();

    public int Count { get { return entries.Count; } }

    struct Entry
    {
        public int priority;
        public T value;

        public Entry(int priority, T value)
        {
            this.priority = priority;
            this.value = value;
        }
    }

    public void Add(int priority, T val)
    {
        entries.Add(new Entry(priority, val));

        if (entries.Count == 1) { return; }

        int current = entries.Count - 1;
        int parent = (current - 1) / 2;

        while (entries[current].priority < entries[parent].priority)
        {
            // swap current and parent
            var cached = entries[parent];
            entries[parent] = entries[current];
            entries[current] = cached;

            current = parent;
            parent = (current - 1) / 2;
        }
    }

    // todo: return bool, out T
    /// <summary>
    /// Removes and returns the value with the lowest weight;
    /// </summary>
    public T Pop()
    {
        // replace the root with the last element
        Entry root = entries[0];

        entries[0] = entries[entries.Count - 1];
        entries.RemoveAt(entries.Count - 1);
        
        // put remaining back into the right order
        int current = 0;
        int left = 1;
        int right = 2;

        while (current < entries.Count)
        {
            // check if left exists
            if (left >= entries.Count) { break; } 
            
            int child = left;

            // check if right exists
            if (right <= entries.Count - 1)
            {
                child = entries[left].priority < entries[right].priority ? left : right;
            }
            
            // check if current is larger than the child
            if (entries[current].priority > entries[child].priority)
            {
                Entry temp = entries[current];
                entries[current] = entries[child];
                entries[child] = temp;

                current = child;
                left = (current * 2) + 1;
                right = (current * 2) + 2;

                continue;
            }

            break;
        }

        return root.value;
    }
}