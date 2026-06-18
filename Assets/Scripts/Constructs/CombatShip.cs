using System;
using UnityEngine;

public partial class CombatShip : Spaceship
{
    
}

// inherent impl
partial class CombatShip
{
    // todo:
    // -    `Despawn`
    // -    moveSpeed, turnSpeed (currently set in prefab)
    public static bool Spawn(string name, Vector2Int pos, out CombatShip ship)
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

    public static CombatShip SpawnUnchecked(string name, Vector2Int pos)
    {
        CombatShip ship = Instantiate(GameManager.Prefabs.combatShip);
        ship.name = name;
        ship.transform.position = pos.X0Y();
        ship.state = new Idle("Spawn");

        GameManager.ObjectPools.Add(ship);
        GameManager.SpatialDictionary.Add(ship);

        return ship;
    }
}