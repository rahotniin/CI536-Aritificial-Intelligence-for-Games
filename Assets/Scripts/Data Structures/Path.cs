using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public partial struct Path
{
    [SerializeField] List<Vector2Int> inner;
}

// Positions with a score of `int.MinValue` are destinations
// Positions with a score of `int.MaxValue` are impassable
public struct Heuristic
{
    // used to evaluate the starting position
    public Func<Vector2Int, int> start { get; }
    // used to evaluate non-start positions
    public Func<Vector2Int, int> evaluate { get; }

    // todo: explain why the start position is evaluated separately from others

    public Heuristic(Func<Vector2Int, int> start, Func<Vector2Int, int> evaluate)
    {
        this.start = start;
        this.evaluate = evaluate;
    }
}

struct Node
{
    public int gScore;
    public int hScore;
    public Vector2Int pos;

    public int FScore { get { return gScore + hScore; } }

    public Node(int gScore, int hScore, Vector2Int pos)
    {
        this.gScore = gScore;
        this.hScore = hScore;
        this.pos = pos;
    }
}

// inherent impl
partial struct Path
{
    public int Count { get { return inner.Count; } }

    public bool Next(out Vector2Int pos)
    {
        return inner.Pop(out pos);
    }

    public void Draw()
    {
        Color prev = Gizmos.color;
        Gizmos.color = Color.red;
        foreach (var pos in inner)
        {
            Gizmos.DrawCube(pos.X0Y(), new(0.5f, 0.1f, 0.5f));
        }
        Gizmos.color = prev;
    }

    // todo: doc proper comments
    // Attemps to find a path to the lowest scoring position within `maxSteps` steps.
    //
    // Returns true (and `path` is initialised) if a path was found, 
    // otherwise false (and `path` is uninitialised).
    //
    // Additionally:
    // -    `path.Count` is `<= maxSteps`.
    // -    if `start` is a valid desination, `path.Count == 0`.
    public static bool Find(Vector2Int start, Heuristic heuristic, int maxSteps, out Path path)
    {
        int startHScore = heuristic.start(start);
        
        // check if start is a valid destination
        if (startHScore == int.MinValue) {
            path = new() { inner = new() };
            return true;
        }

        Node startNode = new(0, startHScore, start);
        
        MinHeap<Node> unexplored = new();
        Dictionary<Vector2Int, Node> parentage = new();
        
        unexplored.Add(startHScore, startNode);
        parentage.Add(start, startNode);

        int lowest = startHScore;
        Vector2Int end = start;
        
        Explore();
        
        // local helper for breaking outer loop from inner loop
        void Explore()
        {
            for (int i = 0; i <= maxSteps; i++)
            {
                if (unexplored.Count == 0) { break; }
                
                Node parent = unexplored.Pop();
                
                // todo: factor in the cost of turning / changing direction
                foreach (Vector2Int dir in Util.DIRS_INT)
                {
                    Vector2Int pos = parent.pos + dir;
                    
                    int hScore = heuristic.evaluate(pos);
                    if (hScore == int.MaxValue) { continue; }

                    // check if this node has been explored before
                    if (parentage.TryGetValue(pos, out Node prevParent))
                    {
                        // this wont work until we use a sorted set
                        // check if theres already has a shorter path to it
                        //if (prevParent.gScore < parent.gScore) { continue; }
                        continue;
                    }
                    
                    parentage[pos] = parent;
                    
                    if (hScore < lowest)
                    {
                        lowest = hScore;
                        end = pos;
                    }

                    // check if this is a valid destination
                    if (hScore == int.MinValue) { return; }

                    int gScore = parent.gScore + 1;

                    Node child = new(gScore, hScore, pos);
                    
                    // todo: 'sorted set' data structure to replace minHeap,
                    // in order to update the priorities of nodes already added to `unexplored`
                    unexplored.Add(child.FScore, child);
                }
            }
        }

        if (end == start)
        {
            path = new() { inner = null };
            return false;
        }

        var inner = new List<Vector2Int>() { end };

        for (int i = 0; i < maxSteps; i++)
        {
            Vector2Int last = inner[inner.Count - 1];
            Node parent = parentage[last];
            if (parent.pos == start) { break; }
            inner.Add(parent.pos);
        }

        path = new() { inner = inner };

        return true;
    }
}
