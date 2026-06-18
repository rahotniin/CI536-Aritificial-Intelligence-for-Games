using System;
using System.Collections.Generic;
using UnityEngine;

/*
    todo: 
    -   When a ships path is blocked by another, it should check if
        the other will soon move out the way.

    -   Icons above ships showing what they are doing,
        should work similar to the selection marker.

    -   More ergonmic (opt-in) way for states to be uninterruptible,
        rather than returning a `Status`. The vast majority of
        are interruptible. 
            field: `bool interruptable = true` on the `State` base class?
        Or, for if it becomes an interface, a method?
*/

[SelectionBase]
public abstract partial class Spaceship : MonoBehaviour
{
    [Header("Spaceship")]
    
    [SerializeField]
    GameObject selectionMarker;
    
    public float moveSpeed;
    public float turnSpeed;

    [SerializeReference]
    protected State state = new Idle("Default");
    
    public Vector2Int? nextCommand = null;
    // todo: make private, implement some `QueueCommand` method
    public Queue<Vector2Int> deferredCommands = new();

    private Action<Vector2Int> onCommand;
    private Action onIdle;
}

// todo: make states into structs and this into an interface
[Serializable]
public abstract class State
{
    [SerializeField] protected string name; // must not be edited in the inspector
    // bool interruptible?
    public abstract Status Update();
}

public enum Status
{
    InProgress,
    Interruptible,
}

// MonoBehaviour
partial class Spaceship
{
    protected void Start()
    {
        onCommand = InitOnCommand();
        onIdle = InitOnIdle();
    }
    
    protected void Update()
    {
        Status status = state.Update();

        if (status is not Status.Interruptible) return;

        if (nextCommand is Vector2Int next)
        {
            onCommand(next);
            nextCommand = null;
            return;
        }

        if (deferredCommands.TryDequeue(out Vector2Int deferred))
        {
            onCommand(deferred);
            return;
        }

        if (state is not (Idle or Stuck)) return;

        onIdle();
    }
}

// inherent impl
partial class Spaceship
{
    public void Select() { selectionMarker.SetActive(true); }
    public void Deselect() { selectionMarker.SetActive(false); }

    protected virtual Action<Vector2Int> InitOnCommand()
    {
        Navigator navigator = new(this, "OnCommand navigator");

        navigator.Link(
            onArrived: () => state = new Idle("Arrived at command location"),
            onFailure: () => state = new Stuck("Failed to reach command location"),
            branch: (occupants) =>
            {
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;
                    return (true, navigator.findPath);
                }
                return (false, null);
            }
        );
        
        return (destination) =>
        {
            navigator.heuristic = new(
                start: (Vector2Int  pos) =>
                {
                    if (pos == destination) { return int.MinValue; }
                    return pos.TaxicabDistance(destination);
                },
                evaluate: (Vector2Int pos) =>
                {
                    foreach (var occupant in GameManager.SpatialDictionary.Get(pos))
                    {
                        if (occupant is Cargo) continue;
                        return int.MaxValue;
                    }
                    
                    if (pos == destination) { return int.MinValue; }
                    
                    int cost = pos.TaxicabDistance(destination);
                    return cost;
                }
            );

            state = navigator.findPath;
        };
    }

    protected virtual Action InitOnIdle()
    {
        return () => {};
    }
    
    //==========================//
    // finite state graphs

    public void RandomWalk()
    {        
        PickRandomStep pick = new(this, "RandomWalk Pick");
        RotateTowards rotate = new(this, "RandomWalk Rotate");
        Branch branch = new(this, "RandomWalk Branch");
        MoveTo move = new(this, "RandomWalk Move");

        pick.Link(
            onComplete: (destination) =>
            {
                rotate.target = destination;
                state = rotate;
            }
        );

        rotate.Link(
            onComplete: (destination) =>
            {
                branch.pos = destination;
                state = branch;
            }
        );

        branch.Link(
            onContinue: (target) => {
                move.target = target;
                state = move;
            },
            condition: (occupants) =>
            {
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;
                    return (true, pick);
                }
                return (false, null);
            }
        );

        move.Link(
            onComplete: () => { state = pick; }
        );

        state = pick;
    }

    //==========================//
    // sub-graphs

    // todo:
    // -    different (and optional) branches, onBeforeStep, onAfterStep
    protected class Navigator
    {
        public FindPath findPath { get; init; }
        FollowPath follow;
        RotateTowards rotate;
        Branch branch;
        public MoveTo move { get; init; }

        public Heuristic heuristic { get { return findPath.heuristic; } set { findPath.heuristic = value; } }

        public Navigator(Spaceship ship, string name)
        {
            findPath = new(ship, name + " findPath");
            follow = new(name + " follow");
            rotate = new(ship, name + " rotate");
            branch = new(ship, name + " branch");
            move = new(ship, name + " move");

            findPath.heuristic = new(null, null); // must be set externally

            findPath.Link(
                onFound: (path) =>
                {
                    follow.path = path;
                    ship.state = follow;
                },
                onArrived: null, // external link
                onFailure: null  // external link
            );

            follow.Link(
                onNextStep: (step) =>
                {
                    rotate.target = step;
                    ship.state = rotate;
                },
                onPathExhausted: () => { ship.state = findPath; }
            );

            rotate.Link(
                onComplete: (step) =>
                {
                    branch.pos = step;
                    ship.state = branch;
                    
                    // necessary to ensure `move` is in the right state
                    // if we return to it after a branch
                    move.target = step;
                }
            );

            branch.Link(
                onContinue: (pos) =>
                {
                    ship.state = move;
                },
                condition: null // external link
            );

            move.Link(
                onComplete: () => { ship.state = follow; }
            );
        }

        public void Link(Action onArrived, Action onFailure, Branch.Condition branch)
        {
            findPath.onArrived = onArrived;
            findPath.onFailure = onFailure;
            this.branch.condition = branch;
        }
    }

    //==========================//
    // states

    protected class Idle : State
    {
        public Idle(string name)
        {
            this.name = name;
        }

        public override Status Update()
        {
            return Status.Interruptible;
        }
    }

    protected class Stuck : State
    {
        public Stuck(string name)
        {
            this.name = name;
        }

        public override Status Update()
        {
            return Status.Interruptible;
        }
    }
    
    protected class PickRandomStep : State
    {
        Spaceship ship;
        Action<Vector2Int> onComplete;

        public PickRandomStep(Spaceship ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Action<Vector2Int> onComplete)
        {
            this.onComplete = onComplete;
        }

        public override Status Update()
        {
            Vector2Int pos = SpatialDictionary.PositionOf(ship);
            Vector2Int dir = Util.RandomDirInt();
            Vector2Int destination = pos + dir;
            
            var occupants = GameManager.SpatialDictionary.Get(destination);
            foreach (var occupant in occupants)
            {
                // todo: check for cargo
                return Status.Interruptible;
            }
        
            onComplete(destination);
            return Status.Interruptible;
        }
    }

    protected class RotateTowards : State
    {
        Spaceship ship;
        public Vector2Int target;
        public Action<Vector2Int> onComplete;

        public RotateTowards(Spaceship ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Action<Vector2Int> onComplete)
        {
            this.onComplete = onComplete;
        }

        public override Status Update()
        {
            Vector2 dS = target - ship.transform.position.XZ();
            float angle = Vector2.SignedAngle(ship.transform.forward.XZ(), dS);
            
            float min = ship.turnSpeed * Time.deltaTime * GameManager.RateOfTime;
            
            if (Mathf.Abs(angle) < min)
            {
                ship.transform.forward = new(dS.x, 0f, dS.y);
                onComplete(target);
                return Status.Interruptible;
            }
            
            float dr = Mathf.Sign(-angle) * min;
            ship.transform.Rotate(Vector3.up, dr);
            return Status.Interruptible;
        }
    }

    // provides an opportunity for the state to branch
    // depending on the occupants of a position in space
    // todo:
    // -    make more generic, not just based on occupants?
    protected class Branch : State
    {
        Spaceship ship;
        public Vector2Int pos;
        public Condition condition;
        public Action<Vector2Int> onContinue;

        // on true, the ships state is changed to the returned state
        // on false, the returned state is discarded
        public delegate (bool, State) Condition(SpatialDictionary.Entry occupants);

        public Branch(Spaceship ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Condition condition, Action<Vector2Int> onContinue)
        {
            this.condition = condition;
            this.onContinue = onContinue;
        }
        
        public override Status Update()
        {
            var occupants = GameManager.SpatialDictionary.Get(pos);
            (bool branch, State state) = condition(occupants);
            if (branch)
            {
                ship.state = state;
            } else
            {
                onContinue(pos);
            }
           
           return Status.Interruptible;
        }
    }

    protected class MoveTo : State
    {
        Spaceship ship;
        bool start;
        public Vector2Int target;

        Action onComplete;

        public MoveTo(Spaceship ship, string name)
        {
            this.name = name;
            this.ship = ship;
            start = true;
        }

        public void Link(Action onComplete)
        {
            this.onComplete = onComplete;
        }

        public override Status Update()
        {
            if (start)
            {
                Vector2Int pos = SpatialDictionary.PositionOf(ship);
                var prev = GameManager.SpatialDictionary.Get(pos);
                prev.Remove(ship);
                
                var next = GameManager.SpatialDictionary.Get(target);
                next.Add(ship);
                
                start = false;
                return Status.InProgress;
            }

            Vector2 dS = target - ship.transform.position.XZ();
            Vector2 dir = dS.normalized;

            float min = ship.moveSpeed * Time.deltaTime * GameManager.RateOfTime;

            if (dS.sqrMagnitude < min * min)
            {
                ship.transform.position = new(target.x, 0f, target.y);
                onComplete();
                start = true;
                return Status.Interruptible;
            }
            
            Vector2 ds = dir * min;
            ship.transform.position += new Vector3(ds.x, 0f, ds.y);
            return Status.InProgress;
        }
    }

    protected class FindPath : State
    {
        Spaceship ship;
        
        public Heuristic heuristic;
        
        public Action<Path> onFound;
        public Action onArrived;
        public Action onFailure;

        public FindPath(Spaceship ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }
        
        public void Link(Action<Path> onFound, Action onArrived, Action onFailure)
        {
            this.onFound = onFound;
            this.onArrived = onArrived;
            this.onFailure = onFailure;
        }

        public override Status Update()
        {
            Vector2Int start = SpatialDictionary.PositionOf(ship);

            bool failed = !Path.Find(start, heuristic, 20, out Path path);

            if (failed)
            {
                onFailure();
            }
            else if (path.Count == 0)
            {
                onArrived();
            }
            else
            {
                onFound(path);
            }
            
            return Status.Interruptible;
        }
    }

    protected class FollowPath : State
    {
        public Path path;
        
        Action<Vector2Int> onNextStep;
        Action onPathExhausted;

        public FollowPath(string name)
        {
            this.name = name;
        }

        public void Link(Action<Vector2Int> onNextStep, Action onPathExhausted)
        {
            this.onNextStep = onNextStep;
            this.onPathExhausted = onPathExhausted;
        }

        public override Status Update()
        {
            if (path.Next(out Vector2Int step)) 
            { 
                onNextStep(step); 
                return Status.Interruptible; 
            }

            onPathExhausted();
            return Status.Interruptible;
        }
    }
}