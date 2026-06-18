using UnityEngine;

[SelectionBase]
public partial class Cargo : MonoBehaviour
{
    public ResourceNode.Kind content;
}

// inherent impl
public partial class Cargo
{
    public static bool Spawn(string name, Vector2Int pos, ResourceNode.Kind content,  out Cargo cargo)
    {
        bool obstructed = GameManager.SpatialDictionary.IsOccupied(pos);
        if (obstructed)
        {
            cargo = null;
            return false;
        }
        
        cargo = SpawnUnchecked(name, pos, content);
        
        return true;
    }

    public static Cargo SpawnUnchecked(string name, Vector2Int pos, ResourceNode.Kind content)
    {
        Cargo cargo = Instantiate(GameManager.Prefabs.cargo);
        cargo.name = name;
        cargo.transform.position = pos.X0Y();
        cargo.content = content;

        GameManager.ObjectPools.Add(cargo);
        GameManager.SpatialDictionary.Add(cargo);

        return cargo;
    }
}