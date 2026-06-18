using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public partial class LevelGenerator : ISerializationCallbackReceiver
{
    [Header("Jovian Planet")]
    [SerializeField] float gasGiantRadius = 5.1f;
    
    [Header("Broken Moon")]
    [SerializeField] float moonRadius = 1.8f;
    [SerializeField] int moonOrbitalRadius = 30;

    [Header("Ring System")]
    [SerializeField] float ringMajorRadius = 20f;
    [SerializeField] float ringMinorRadius = 10f;
    float ringOuterRadius;
    // magic number, tuned in the editor
    [SerializeField] [Range(0f, 1f)] float numAsteroidsMul = 0.5f;

    [Header("Asteroids")]
    [SerializeField] AsteroidSpawner asteroidSpawner;
}

// ISerializationCallbackReceiver
partial class LevelGenerator
{
    public void OnBeforeSerialize() {}
    public void OnAfterDeserialize()
    {
        ringOuterRadius = ringMajorRadius + ringMinorRadius;
    }
}

// inherent impl
partial class LevelGenerator
{
    public void SpawnLevel()
    {
        Planet.Spawn(GameManager.Prefabs.jovianPlanet, "Jovian Planet", new(0, 0), gasGiantRadius, 1.5f, out Planet jovian);
        jovian.transform.Rotate(Random.insideUnitCircle.X0Y(), Random.Range(20f, 30f));
        jovian.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
        
        Planet.Spawn(GameManager.Prefabs.brokenPlanet, "Broken Moon", new(0, moonOrbitalRadius), moonRadius, 2f, out Planet moon);
        moon.transform.Rotate(Random.insideUnitCircle.X0Y(), Random.Range(20f, 40f));
        moon.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

        //SpawnEnemyTeam();
        SpawnPlayerTeam();
        
        SpawnRingSystem();
    }

    void SpawnEnemyTeam()
    {
        Shipyard.Spawn("Enemy Shipyard", new(Mathf.CeilToInt(-ringOuterRadius), 0), new(0, 1), out Shipyard shipyard);        
    }

    void SpawnPlayerTeam()
    {
        Shipyard.Spawn("Player Shipyard", new(Mathf.CeilToInt(ringOuterRadius), 0), new(0, 1), out Shipyard shipyard);
        GameManager.PlayerObject.shipyards.Add(shipyard);
        
        // move player over their shipyard
        
        Vector3 focus = shipyard.transform.position + shipyard.dir.X0Y();// * 3;
        
        Transform playerTransform = GameManager.PlayerObject.transform;
        Vector3 localForward = playerTransform.worldToLocalMatrix * Camera.main.transform.forward;
        float cameraLocalY = Camera.main.transform.localPosition.y;
        float offset = cameraLocalY / localForward.y * localForward.z;
        
        playerTransform.position = focus + new Vector3(0f, 0f, -offset);
        playerTransform.forward = new(0f, 0f, -1f);

        // spawn initial fleet
        
        Vector2Int minerPos = shipyard.transform.position.XZ().RoundToInt() + 2 * shipyard.dir + new Vector2Int(1, 0);
        MiningShip.Spawn("Mining Ship 0", minerPos, out MiningShip miner);
        GameManager.PlayerObject.team.Add(miner);

        Vector2Int cargoPos = shipyard.transform.position.XZ().RoundToInt() + 2 * shipyard.dir + new Vector2Int(0, 0);
        CargoShip cargo = CargoShip.SpawnUnchecked("Cargo Ship 0", cargoPos);
        GameManager.PlayerObject.team.Add(cargo);

        Vector2Int combatPos = shipyard.transform.position.XZ().RoundToInt() + 2 * shipyard.dir + new Vector2Int(-1, 0);
        CombatShip.Spawn("Combat Ship 0", combatPos, out CombatShip combat);
        GameManager.PlayerObject.team.Add(combat);
    }

    void SpawnRingSystem()
    {
        float innerRadius = ringMajorRadius - ringMinorRadius;
        float innerArea = Mathf.PI * innerRadius * innerRadius;
        float outerArea = Mathf.PI * ringOuterRadius * ringOuterRadius;
        float ringArea = outerArea - innerArea;
        
        int targetCount = asteroidSpawner.EstimateHowManyCanSpawn(ringArea * numAsteroidsMul);

        int count = 0;
        int iter = 0;
        int maxIter = targetCount * 3;
        string name = "Asteroid 0";
       
        while (count < targetCount)
        {
            if (asteroidSpawner.Try(name, RandomPosition()))
            {
                count++;
                name = "Asteroid " + count;
            }
            
            if (++iter > maxIter)
            {
                Debug.LogWarning("Failed to spawn " + (targetCount - count) + " out of " + targetCount + " asteroids");
                break;
            }
        }

        Vector2Int RandomPosition()
        {
            float rng = Random.Range(-1f, 1f);
            float offset = ringMinorRadius * rng * rng * rng;
            float orbitalDst = ringMajorRadius + offset;
            float angle = Random.Range(0f, 2f) * Mathf.PI;
            Vector2Int pos = Vector2Int.RoundToInt(orbitalDst * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            return pos;
        }
    }
}
