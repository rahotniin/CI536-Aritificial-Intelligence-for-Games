using System;
using System.Collections.Generic;
using UnityEngine;

public partial class CargoShip : Spaceship
{
    [Header("Cargo Ship")]
    [SerializeField] [Min(0f)] float scanningRadius = 10f;
    [SerializeField] int capacity = 3;
    [SerializeField] List<Cargo> cargo = new();

    [Header("Prefab Fields")]
    [SerializeField] float cargoYStart;
    [SerializeField] float cargoYOffset;
    // todo: non-instant cargo transfer, `transferSpeed`?
}

// inherent impl
partial class CargoShip
{
    // todo: 
    // -    despawn
    // -    moveSpeed, turnSpeed, capacity, scanningRadius (currently set in prefab)
    public static bool Spawn(string name, Vector2Int pos, out CargoShip ship)
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

    public static CargoShip SpawnUnchecked(string name, Vector2Int pos)
    {
        CargoShip ship = Instantiate(GameManager.Prefabs.cargoShip);
        ship.name = name;
        ship.transform.position = pos.X0Y();
        ship.state = new Idle("Spawn");

        GameManager.ObjectPools.Add(ship);
        GameManager.SpatialDictionary.Add(ship);

        return ship;
    }

    //==========================//
    // `Spaceship` overrides
    
    protected override Action InitOnIdle()
    {
        ScanningForCargo scan = new(this, "OnIdle scan");

        Navigator navigateToCargo = new(this, "OnIdle navigateToCargo");
        PickingUpCargo pickup = new(this, "OnIdle Scan");

        Navigator navigateToShipyard = new(this, "OnIdle navigateToShipyard");
        UnloadingCargo unloadCargo = new(this, "OnIdle unloadCargo");

        scan.Link(
            onFound: (cargo) => NavigateTo(cargo),
            onNoneWithinRange: () => {
                if (cargo.Count > 0)
                {
                    NavigateToShipyard();
                } else
                {
                    state = new Idle("No cargo within range");
                }
            }
        );

        // todo: check if the cargo has been picked up before we arrive
        navigateToCargo.Link(
            onArrived: () => state = pickup,
            onFailure: () => state = new Stuck("Failed to navigate to cargo"),
            branch: (occupants) =>
            {
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;
                    return (true, navigateToCargo.findPath);
                }
                return (false, null);
            }
        );

        pickup.Link(
            onRoomForMore: () => state = scan,
            onCargoFull: () => NavigateToShipyard(),
            onCargoNotFound: () => state = scan
        );

        navigateToShipyard.Link(
            onArrived: () => state = unloadCargo,
            onFailure: () => { state = new Stuck("Failed to navigate to shipyard"); },
            branch: (occupants) =>
            {
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;
                    return (true, navigateToShipyard.findPath);
                }
                return (false, null);
            }
        );

        unloadCargo.Link(
            onComplete: () => state = scan
        );
        
        return () => state = scan;

        // local helpers

        void NavigateTo(Cargo cargo)
        {
            Vector2Int destination = SpatialDictionary.PositionOf(cargo);
            navigateToCargo.heuristic = Toward(cargo);
            state = navigateToCargo.findPath;
            return;

            Heuristic Toward(Cargo cargo)
            {
                return new(Start, Evaluate);
                
                int Start(Vector2Int start)
                {
                    int cost = start.TaxicabDistance(destination);
                    
                    var occupants = GameManager.SpatialDictionary.Get(start);
                    foreach (var occupant in occupants)
                    {
                        if (occupant is Cargo) {
                            cost = int.MinValue;
                            continue;
                        }
                        if (occupant is Shipyard) continue;
                        if(ReferenceEquals(this, occupant)) continue;
                        
                        // unreachable?
                        // A cargo ship should never end up in the 
                        // same position as anything but cargo or the shipyard
                        Debug.LogError("Cargo ship in invalid position");
                        return int.MaxValue;
                    }
                    
                    return cost;
                }

                int Evaluate(Vector2Int pos)
                {
                    int cost = pos.TaxicabDistance(destination);
                    
                    var occupants = GameManager.SpatialDictionary.Get(pos);
                    foreach (var occupant in occupants)
                    {
                        if (occupant is Cargo) {
                            cost = int.MinValue;
                            continue;
                        }
                        // anything else is impassable
                        return int.MaxValue;
                    }
                    
                    return cost;
                }
            }
        }
    
        void NavigateToShipyard()
        {
            bool shipyardNotFound = !GameManager.PlayerObject.Shipyard(out var shipyard);
            if (shipyardNotFound)
            {
                state = new Stuck("Shipard not found");
                return;
            }

            unloadCargo.shipyard = shipyard;

            // todo: (optimisation)
            // this only really needs to be set once, as the player only has 1 shipyard
            navigateToShipyard.heuristic = Toward(shipyard);
            state = navigateToShipyard.findPath;
        }

        Heuristic Toward(Shipyard shipyard)
        {
            Vector2Int destination = SpatialDictionary.PositionOf(shipyard);

            return new(Start, Evaluate);
                
            int Start(Vector2Int start)
            {
                int dst = start.TaxicabDistance(destination);
                if (dst == 1) return int.MinValue;
                return dst;
            }

            int Evaluate(Vector2Int pos)
            {
                int cost = pos.TaxicabDistance(destination);
                
                var occupants = GameManager.SpatialDictionary.Get(pos);
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;
                    // anything else is impassable
                    return int.MaxValue;
                }
                
                int dst = pos.TaxicabDistance(destination);
                if (dst == 1) return int.MinValue;
                return dst;
            }
        }
    }

    //==========================//
    // state

    /*
        states:
        -   scan for cargo
        -   navigate to cargo (navigator)
        -   pick up cargo
        -   navigate to shipyard (navigator)
        -   transfer cargo to shipyard

        heuristcs:
        -   toward cargo
        -   toward shipyard
    */
    
    // todo: generic scanning state?
    class ScanningForCargo : State
    {
        CargoShip ship;
        ObjectPool cargoPool;
        Action<Cargo> onFound;
        Action onNoneWithinRange;

        public ScanningForCargo(CargoShip ship, string name)
        {
            this.name = name;
            this.ship = ship;
            cargoPool = GameManager.ObjectPools.Get<Cargo>();
        }


        public void Link(Action<Cargo> onFound, Action onNoneWithinRange)
        {
            this.onFound = onFound;
            this.onNoneWithinRange = onNoneWithinRange;
        }

        // todo: could this be spread out over several frames?
        public override Status Update()
        {
            Cargo closest = null;
            float minDst = float.MaxValue;

            foreach (Cargo cargo in cargoPool.objects)
            {
                // check if its out of range
                float sqrDst = (cargo.transform.position - ship.transform.position).sqrMagnitude;
                if (sqrDst > ship.scanningRadius * ship.scanningRadius || sqrDst > minDst) continue;

                // check if there's something in the way on top if it
                Vector2Int pos = SpatialDictionary.PositionOf(cargo);
                var occupants = GameManager.SpatialDictionary.Get(pos);
                
                bool unobstructed = true;
                foreach (var occupant in occupants)
                {
                    if (occupant is Cargo) continue;
                    if (ReferenceEquals(occupant, ship)) break;
                    unobstructed = false;
                    break;
                }

                if (unobstructed)
                {
                    closest = cargo;
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

    class PickingUpCargo : State
    {
        CargoShip ship;

        Action onRoomForMore;
        Action onCargoFull;
        Action onCargoNotFound;

        public PickingUpCargo(CargoShip ship, string name)
        {
            this.name = name;
            this.ship = ship;
        }

        public void Link(Action onRoomForMore, Action onCargoFull, Action onCargoNotFound)
        {
            this.onRoomForMore = onRoomForMore;
            this.onCargoFull = onCargoFull;
            this.onCargoNotFound = onCargoNotFound;
        }
        
        public override Status Update()
        {
            if (ship.cargo.Count >= ship.capacity)
            {
                onCargoFull();
                return Status.Interruptible;
            }
            
            Cargo target = null;
            
            Vector2Int pos = SpatialDictionary.PositionOf(ship);
            var occupants = GameManager.SpatialDictionary.Get(pos);
            foreach (var occupant in occupants)
            {
                if (occupant is Cargo cargo)
                {
                    target = cargo;
                    break;
                }
            }

            if (target is null)
            {
                onCargoNotFound();
                return Status.Interruptible;
            }

            target.transform.parent = ship.transform;
            float cargoY = ship.cargoYStart + ship.cargo.Count * ship.cargoYOffset;
            target.transform.localPosition = new(0f, cargoY, 0f);
            ship.cargo.Add(target);
            
            occupants.Remove(target);
            GameManager.ObjectPools.Get<Cargo>().Remove(target);

            if (ship.cargo.Count < ship.capacity)
            {
                onRoomForMore();
            }
            
            return Status.Interruptible;
        }
    }

    class UnloadingCargo : State
    {
        CargoShip ship;
        public Shipyard shipyard;

        Action onComplete;

        public UnloadingCargo(CargoShip ship, string name)
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
            while (ship.cargo.Pop(out Cargo cargo))
            {
                shipyard.Add(cargo);
            }
            
            onComplete();
            
            return Status.Interruptible;
        }
    }
}
