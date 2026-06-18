using UnityEngine;

[SelectionBase]
public class Planet : MonoBehaviour
{
    public float rotationSpeed;
    
    public float radius;
    public float sqrRadius;
    //public float rocheLimit;
    public float sqrRocheLimit;

    [SerializeField] GameObject model;

    public enum Type
    {
        Broken,
        Jovian,
    }

    void Update()
    {
        model.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    public static bool Spawn(Planet prefab, string name, Vector2Int pos, float radius, float rocheFactor, out Planet planet)
    {
        var positions = Vector2IntExt.Iter.Around(Vector2Int.zero, Mathf.CeilToInt(radius));
        float sqrRadius = radius * radius;
        foreach (Vector2Int offset in positions)
        {
            if (offset.sqrMagnitude > sqrRadius) { continue; }
            Vector2Int subPos = pos + offset;
            if (GameManager.SpatialDictionary.IsOccupied(subPos))
            {
                planet = null;
                return false;
            }
        }

        planet = SpawnUnchecked(prefab, name, pos, radius, rocheFactor);

        return true;
    }

    public static Planet SpawnUnchecked(Planet prefab, string name, Vector2Int pos, float radius, float rocheFactor)
    {
        Planet planet = Instantiate(prefab);
        planet.name = name;
        planet.transform.position = pos.X0Y();
        planet.transform.localScale = new(radius, radius, radius);
        planet.radius = radius;
        planet.sqrRadius = radius * radius;
        planet.sqrRocheLimit = planet.sqrRadius * rocheFactor * rocheFactor;

        GameManager.ObjectPools.Add(planet);

        var positions = Vector2IntExt.Iter.Around(Vector2Int.zero, Mathf.CeilToInt(radius));
        foreach (Vector2Int offset in positions)
        {
            if (offset.sqrMagnitude > planet.sqrRadius) { continue; }
            Vector2Int subPos = pos + offset;
            GameManager.SpatialDictionary.Add(planet, subPos);
        }

        return planet;
    }
}