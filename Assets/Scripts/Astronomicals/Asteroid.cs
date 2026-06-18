using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public partial class Asteroid : MonoBehaviour
{
    [field: SerializeField]
    public int size { get; private set; }
    
    [field: SerializeField]
    public float mass { get; private set; }
    float initMass;
    Vector3 initScale;
    
    public List<ResourceNode> resources = new();
    
    [Header("Required Prefab Fields")]
    [SerializeField] GameObject model;
    [SerializeField] float maxYOffset;
}

public class AsteroidWithResources {}

// inherent impl
partial class Asteroid
{
    public static bool Spawn(string name, Vector2Int pos, int size, out Asteroid asteroid)
    {
        // check if we can spawn here
        var positions = Vector2IntExt.Iter.Square(pos, size);
        
        bool obstructed = !GameManager.SpatialDictionary.TryForEach(positions, (occupants, subPos) =>
        {
            return occupants.Count == 0 && NotWithinRocheLimit(subPos);
        });

        if (obstructed)
        {
            asteroid = null;
            return false;
        }

        List<Asteroid> prefabPool = GameManager.Prefabs.asteroids[size];
        Asteroid prefab = prefabPool[Random.Range(0, prefabPool.Count)];
        
        asteroid = Instantiate(prefab);
        asteroid.name = name;
        asteroid.size = size;
        asteroid.transform.position = pos.X0Y();
        asteroid.mass = size * size * size;
        asteroid.initMass = asteroid.mass;
        asteroid.initScale = asteroid.model.transform.localScale;
        
        asteroid.AddJitter();

        positions.Reset();
        GameManager.SpatialDictionary.Add(asteroid, positions);

        GameManager.ObjectPools.Add(asteroid);
        return true;

    }

    public void Despawn()
    {
        Vector2Int pos = SpatialDictionary.PositionOf(this);
        var positions = Vector2IntExt.Iter.Square(pos, size);
        GameManager.SpatialDictionary.Remove(this, positions);

        GameManager.ObjectPools.Destroy(this);
    }

    void AddJitter()
    {
        Vector3 offset = (Random.insideUnitCircle * 0.25f).X0Y()
            .WithY(Random.Range(maxYOffset, maxYOffset));
        model.transform.position += offset;

        float maxSideAngle = size switch
        {
            1 => 30f,
            2 => 20f,
            3 => 10f,
            _ => float.NaN,
        };
        model.transform.Rotate(Random.insideUnitCircle.X0Y(), Random.Range(0f, maxSideAngle));
        model.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
        transform.localScale *= Random.Range(0.9f, 1.1f);
    }

    public void AddResources(ResourceNode.Kind kind)
    {
        ResourceNode prefab = kind switch
        {
            ResourceNode.Kind.Green => GameManager.Prefabs.resourceNodeGreen,
            ResourceNode.Kind.Red => GameManager.Prefabs.resourceNodeRed,
            _ => throw new System.NotImplementedException(),
        };
        
        Vector3[] spawnPositions = model.GetComponent<MeshFilter>().mesh.vertices;

        (int minNodes, int maxNodes, float minNodeSize, float maxNodeSize) = size switch
        {
            1 => (1, 2, 0.1f, 0.2f),
            2 => (2, 4, 0.1f, 0.3f),
            3 => (4, 8, 0.1f, 0.4f),
            _ => (0, 0, float.NaN, float.NaN),
        };

        int numNodes = Random.Range(minNodes, maxNodes);

        for (int i = 0; i < numNodes; i++)
        {
            float sizeRng = Random.Range(0f, 1f);
            float size = Mathf.Lerp(minNodeSize, maxNodeSize, sizeRng * sizeRng);
            
            ResourceNode node = Instantiate(prefab);
            node.transform.localScale = Vector3.one * size;
            node.transform.parent = model.transform;
            node.transform.localPosition = spawnPositions[Random.Range(0, spawnPositions.Length)];
            node.transform.forward = Random.onUnitSphere;

            resources.Add(node);
        }
    }

    //==========================//
    
    public void RemoveResourceNodes()
    {
        foreach (var node in resources)
        {
            node.gameObject.SetActive(false);
        }

        // todo: cache these
        GameManager.ObjectPools.Get<AsteroidWithResources>().Remove(this, true);
        GameManager.ObjectPools.Get<Asteroid>().AddToHeaderUnchecked(this);
    }

    static bool NotWithinRocheLimit(Vector2Int pos)
    {
        foreach (Planet planet in GameManager.ObjectPools.Get<Planet>().objects)
        {
            float sqrDst = (planet.transform.position.XZ() - pos).sqrMagnitude;
            if (sqrDst < planet.sqrRocheLimit) { return false; }
        }
        return true;
    }

    public bool IsAdjacentTo(Vector2Int sample)
    {
        Vector2Int pos = SpatialDictionary.PositionOf(this);
        return IsAdjacentTo(sample, pos, size);
    }

    public static bool IsAdjacentTo(Vector2Int sample, Vector2Int pos, int size)
    {        
        if (sample.x == pos.x - 1 || sample.x == pos.x + size)
        {                
            if (sample.y >= pos.y && sample.y < pos.y + size) return true;

            return false;
        }

        if (sample.y == pos.y - 1 || sample.y == pos.y + size)
        {
            if (sample.x >= pos.x && sample.x < pos.x + size) return true;
        }

        return false;
    }

    public void RemoveMass(float amount)
    {
        mass -= amount;

        float scale = mass / initMass;
        model.transform.localScale = initScale * scale;
    }

}
