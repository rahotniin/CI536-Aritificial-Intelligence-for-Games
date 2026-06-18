using System;
using System.Collections.Generic;
using UnityEngine;

public partial class MiningShip : Spaceship
{
    [Header("Mining Ship")]

    [SerializeField] [Min(0f)] float scanningRadius = 10f;
    [SerializeField] [Min(0f)] float miningSpeed = 1f;
    
    int capacity = 1;
    List<ResourceNode.Kind> cargo = new();

    // todo: non-instant cargo placing, `unloadCargoSpeed`?
}

// inherent impl
partial class MiningShip
{
    // todo:
    // -    `Despawn`
    // -    moveSpeed, turnSpeed, scanningRadius (currently set in prefab)
    public static bool Spawn(string name, Vector2Int pos, out MiningShip ship)
    {
        bool obstructed = GameManager.SpatialDictionary.IsOccupied(pos);
        if (obstructed)
        {
            ship = null;
            return false;
        }
        
        ship = SpawnUnchecked(name, pos);
        
        return true;
    }

    public static MiningShip SpawnUnchecked(string name, Vector2Int pos)
    {
        MiningShip ship = Instantiate(GameManager.Prefabs.miningShip);
        ship.name = name;
        ship.transform.position = pos.X0Y();
        ship.state = new Idle("Spawn");

        GameManager.ObjectPools.Add(ship);
        GameManager.SpatialDictionary.Add(ship);

        return ship;
    }

    public int CostToMine(Asteroid asteroid)
    {
        return Mathf.RoundToInt(asteroid.mass / miningSpeed);
    }

    //==========================//
    // `Spaceship` overrides

    protected override Action<Vector2Int> InitOnCommand()
    {
        Navigator navigator = new(this, "OnCommand navigator");
        MiningAsteroid mineAsteroid = new(this, "OnCommand mineAsteroid");

        mineAsteroid.Link(
            onComplete: null // set by the navigator during branch
        );

        navigator.Link(
            onArrived: () => state = new Idle("Arrived at command location"),
            onFailure: () => state = new Idle("Failed to navigate to command location"),
            branch: (occupants) =>
            {
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;

                    if (occupant is Asteroid asteroid)
                    {
                        mineAsteroid.target = asteroid;
                        mineAsteroid.onComplete = () => state = navigator.move;
                        return (true, mineAsteroid);
                    }

                    return (true, navigator.findPath);
                }
                return (false, null);
            }
        );
        
        return (destination) =>
        {
            navigator.heuristic = new(
                start: (Vector2Int  pos) => { return pos.TaxicabDistance(destination); },
                evaluate: (Vector2Int pos) =>
                {
                    int cost = pos.TaxicabDistance(destination);
                    
                    foreach (var occupant in GameManager.SpatialDictionary.Get(pos))
                    {
                        if (occupant is Cargo) continue;

                        if (occupant is Asteroid other && other.resources.Count <= 0f)
                        {
                            cost += CostToMine(other);
                            break;
                        }

                        return int.MaxValue;
                    }
                    
                    if (pos == destination) { return int.MinValue; }
                    
                    return cost;
                }
            );

            state = navigator.findPath;
        };
    }

    protected override Action InitOnIdle()
    {
        ScanningForResources scan = new(this, "OnIdle scan");
        
        Navigator navigateToResource = new(this, "OnIdle navigateToResource");
        MiningAsteroid mineAsteroid = new(this, "OnIdle mineAsteroid");
        MiningResources mineResource = new(this, "OnIdle mineResource");
        
        Navigator navigateToEmptySpace = new(this, "OnIdle navigateToEmptySpace");
        PlacingCargo placeCargo = new(this, "OnIdle placeCargo");

        scan.Link(
            onFound: asteroid => NavigateTo(asteroid),
            onNoneWithinRange: () => state = new Stuck("No resources within range")
        );

        navigateToResource.Link(
            // this should only happen if resource gets destroyed before we arrive
            onArrived: () => state = scan,
            onFailure: () => state = new Stuck("Failed to navigate to resource"),
            branch: ResourceNavBranch
        );

        mineAsteroid.Link(
            onComplete: null // set by the navigators during branch
        );

        mineResource.Link(
            onResourcesDepleted: () => { state = scan; },
            onCargoFull: () => NavigateToEmptySpace()
        );

        placeCargo.Link(
            onCargoEmpty: () => state = scan,
            onCargoRemaining: () => NavigateToEmptySpace()
        );

        navigateToEmptySpace.Link(
            onArrived: () => state = placeCargo,
            onFailure: () => state = new Stuck("Failed to place cargo"),
            branch: PlaceCargoNavBranch
        );

        return () => state = scan;

        // local helpers

        void NavigateTo(Asteroid asteroid)
        {
            Vector2Int destination = SpatialDictionary.PositionOf(asteroid);
            navigateToResource.heuristic = Toward(asteroid);
            state = navigateToResource.findPath;
            return;

            Heuristic Toward(Asteroid asteroid)
            {
                return new(Start, Evaluate);
                
                int Start(Vector2Int start)
                {
                    return start.TaxicabDistance(destination);
                }

                int Evaluate(Vector2Int pos)
                {
                    int cost = pos.TaxicabDistance(destination);
                    
                    var occupants = GameManager.SpatialDictionary.Get(pos);
                    foreach (var occupant in occupants)
                    {
                        if (occupant is Cargo) continue;
                        if (occupant is Asteroid other)
                        {
                            if (ReferenceEquals(other, asteroid)) return int.MinValue;
                            cost += CostToMine(other);
                            break;
                        }
                        // anything else is impassable
                        return int.MaxValue;
                    }
                    
                    return cost;
                }
            }
        }
        
        // todo: check if the resource has been destroyed before we arrive
        (bool, State) ResourceNavBranch(SpatialDictionary.Entry occupants)
        {
            foreach (var occupant in occupants)
            {
                if (occupant is Cargo) continue;
                if (occupant is Asteroid asteroid)
                {
                    if (asteroid.resources.Count > 0)
                    {
                        mineResource.target = asteroid;
                        return (true, mineResource);
                    } else
                    {
                        mineAsteroid.target = asteroid;
                        mineAsteroid.onComplete = () => state = navigateToResource.move;
                        return (true, mineAsteroid);
                    }
                }
                // anything else is impassible, new path needed
                return (true, navigateToResource.findPath);
            }
            return (false, null);
        }

        void NavigateToEmptySpace()
        {
            navigateToEmptySpace.heuristic = TowardEmptySpace();
            state = navigateToEmptySpace.findPath;
            return;
            
            Heuristic TowardEmptySpace()
            {   
                Vector2Int shipPos = SpatialDictionary.PositionOf(this);
                return new(Start, Evaluate);

                int Start(Vector2Int start)
                {
                    var occupants = GameManager.SpatialDictionary.Get(start);
                    foreach (var occupant in occupants)
                    {
                        if (occupant is Cargo) return int.MaxValue;
                        // nothing else should be able to be in the same place
                    }
                    
                    return int.MinValue;
                }

                int Evaluate(Vector2Int pos)
                {
                    int cost = int.MinValue;
                    
                    var occupants = GameManager.SpatialDictionary.Get(pos);
                    foreach (var occupant in occupants)
                    {
                        if (occupant is Asteroid asteroid)
                        {
                            cost = pos.TaxicabDistance(shipPos) + CostToMine(asteroid);
                            break;
                        }

                        return int.MaxValue;
                    }
                    
                    return cost;
                }
            }
        }

        (bool, State) PlaceCargoNavBranch(SpatialDictionary.Entry occupants)
        {
            foreach (var occupant in occupants)
            {
                if (occupant is Cargo) continue;
                
                // mine asteroids in the way
                if (occupant is Asteroid asteroid)
                {
                    mineAsteroid.target = asteroid;
                    mineAsteroid.onComplete = () => state = navigateToEmptySpace.move;
                    return (true, mineAsteroid);
                }

                return (true, navigateToEmptySpace.findPath);
            }
            
            return (false, null);
        }
    }

    //==========================//
    // states

    // todo: 
    // -    generic scanning state?
    // -    ignore asteroid that cannot be reached (e.g. surrounded by other ships)
    class ScanningForResources : State
    {
        MiningShip ship;
        ObjectPool resourcePool;
        Action<Asteroid> onFound;
        Action onNoneWithinRange;

        public ScanningForResources(MiningShip ship, string name)
        {
            this.name = name;
            this.ship = ship;
            resourcePool = GameManager.ObjectPools.Get<AsteroidWithResources>();
        }

        public void Link(Action<Asteroid> onFound, Action onNoneWithinRange)
        {
            this.onFound = onFound;
            this.onNoneWithinRange = onNoneWithinRange;
        }

        // todo: could this be spread out over several frames?
        public override Status Update()
        {
            float minableAmount = ship.miningSpeed * Time.deltaTime * GameManager.RateOfTime;

            Asteroid closest = null;
            float minDst = float.MaxValue;
            foreach (Asteroid asteroid in resourcePool.objects)
            {
                float sqrDst = (asteroid.transform.position - ship.transform.position).sqrMagnitude;
                if (sqrDst < ship.scanningRadius * ship.scanningRadius && sqrDst < minDst)
                {
                    closest = asteroid;
                    minDst = sqrDst;
                }
            }
            
            if (closest is not null)
            {
                onFound(closest);
            } else
            {
                onNoneWithinRange();
            }
            
            return Status.Interruptible;
        }
    }

    [Serializable]
    class MiningResources : State
    {
        MiningShip ship;
        [SerializeField] float progress;
        public Asteroid target;
        Action onResourcesDepleted;
        Action onCargoFull;

        public MiningResources(MiningShip ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Action onResourcesDepleted, Action onCargoFull)
        {
            this.onResourcesDepleted = onResourcesDepleted;
            this.onCargoFull = onCargoFull;
        }
        
        public override Status Update()
        {
            if (target.resources.Count == 0)
            {
                onResourcesDepleted();
                return Status.Interruptible;
            }
            
            progress += ship.miningSpeed * Time.deltaTime * GameManager.RateOfTime;

            if (progress >= 1f)
            {
                progress = 0f;
                
                target.resources.PopUnchecked(out ResourceNode node);
                ship.cargo.Add(node.kind);
                // safety: resource nodes are not pooled
                Destroy(node.gameObject);

                if (target.resources.Count == 0)
                {
                    GameManager.ObjectPools.Get<AsteroidWithResources>().Remove(target);
                }

                if (ship.cargo.Count >= ship.capacity)
                {
                    onCargoFull();
                    return Status.Interruptible;
                }
            }

            return Status.Interruptible;
        }
    }

    class MiningAsteroid : State
    {
        MiningShip ship;
        public Asteroid target;
        
        public Action onComplete;
        public Action onResourcesDepleted;
        public Action onCargoFull;

        public MiningAsteroid(MiningShip ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Action onComplete)
        {
            this.onComplete = onComplete;
        }

        public override Status Update()
        {
            float minableAmount = ship.miningSpeed * Time.deltaTime * GameManager.RateOfTime;
            
            if (target.mass > minableAmount)
            {
                target.RemoveMass(minableAmount);
                return Status.Interruptible;
            }
            
            target.Despawn();
            onComplete();
            
            return Status.Interruptible;
        }
    }

    class PlacingCargo : State
    {
        MiningShip ship;
        public Action onCargoRemaining;
        public Action onCargoEmpty;

        public PlacingCargo(MiningShip ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Action onCargoRemaining, Action onCargoEmpty)
        {
            this.onCargoRemaining = onCargoRemaining;
            this.onCargoEmpty = onCargoEmpty;
        }

        public override Status Update()
        {
            bool cargoEmpty = !ship.cargo.Pop(out var resource);
            if (cargoEmpty)
            {
                onCargoEmpty();
                return Status.Interruptible;
            }
            
            Vector2Int pos = SpatialDictionary.PositionOf(ship);
            
            Cargo.SpawnUnchecked("Cargo", pos, resource);

            if (ship.cargo.Count > 0)
            {
                onCargoRemaining();
            } else
            {
                onCargoEmpty();
            }
            
            return Status.Interruptible;
        }
    }
}