using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public partial class Shipyard : MonoBehaviour
{
    // direction the shipyard is facing.
    [field: SerializeField]
    public Vector2Int dir { get; private set; }
    public Vector2Int constructionPos;

    public Resources resources = new() { green = 0, red = 0 };
}

// monobehaviour
public partial class Shipyard : MonoBehaviour
{
    public void Start()
    {
        
    }
}

// inherent impl
partial class Shipyard
{
    // todo: `SpawnUnchecked`
    public static bool Spawn(string name, Vector2Int pos, Vector2Int dir, out Shipyard shipyard)
    {
        List<Vector2Int> positions = new() { pos, pos + dir, pos - dir };
        bool obstructed = GameManager.SpatialDictionary.AreOccupied(positions);
        if (obstructed)
        {
            shipyard = null;
            return false;
        }
        
        shipyard = Instantiate(GameManager.Prefabs.shipyard).GetComponent<Shipyard>();
        shipyard.name = name;
        shipyard.transform.position = pos.X0Y();
        shipyard.transform.forward = new(dir.x, 0f, dir.y);
        shipyard.dir = dir;
        shipyard.constructionPos = pos + dir;

        GameManager.SpatialDictionary.Add(shipyard, positions);

        GameManager.ObjectPools.Add(shipyard);

        return true;
    }

    public void Add(Cargo cargo)
    {
        ResourceNode.Kind resource = cargo.content;
        GameManager.ObjectPools.Destroy(cargo);

        switch (resource)
        {
            case ResourceNode.Kind.Green :
                resources.green++; break;
            case ResourceNode.Kind.Red :
                resources.red++; break;
        }

        // todo: move to player object?
        // this *would* get called for the enemy shipyard as well
        GameManager.UIManager.UpdatePlayerResources(resources);
    }

    public bool ConstructionAreaBlocked()
    {
        var occupants = GameManager.SpatialDictionary.Get(constructionPos);
        foreach (var occupant in occupants)
        {
            if (occupant is Spaceship) return true;
        }
        
        return false;
    }
}