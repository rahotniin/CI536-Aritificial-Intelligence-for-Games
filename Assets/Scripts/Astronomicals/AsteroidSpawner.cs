using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AsteroidSpawner : ISerializationCallbackReceiver
{
    
    [SerializeField] [Range(0f, 1f)]
    float chanceOfResources = 0.05f;
    
    [Header("Resource Type Weights")]
    [SerializeField] int weightOfRed = 10;
    [SerializeField] int weightOfGreen = 3;
    int totalResourceWeight;
    [Header("Resource Type Probabilties (readonly)")]
    [SerializeField] float chanceOfRed = 10;
    [SerializeField] float chanceOfGreen = 3;

    [Header("Size Weights")]
    [SerializeField] int weightOf1x1s = 10;
    [SerializeField] int weightOf2x2s = 3;
    [SerializeField] int weightOf3x3s = 1;
    int totalAsteroidWeight;
    [Header("Size Probabilities (readonly)")]
    [SerializeField] float chanceOf3x3;
    [SerializeField] float chanceOf2x2;
    [SerializeField] float chanceOf1x1;

    public void OnBeforeSerialize() {}
    public void OnAfterDeserialize()
    {
        totalAsteroidWeight = weightOf1x1s + weightOf2x2s + weightOf3x3s;
        
        chanceOf3x3 = weightOf3x3s / (float)totalAsteroidWeight;
        chanceOf2x2 = weightOf2x2s / (float)totalAsteroidWeight;
        chanceOf1x1 = weightOf1x1s / (float)totalAsteroidWeight;

        totalResourceWeight = weightOfGreen + weightOfRed;

        chanceOfGreen = weightOfGreen / (float)totalResourceWeight;
        chanceOfRed   = weightOfRed   / (float)totalResourceWeight;
    }

    public int EstimateHowManyCanSpawn(float availableArea)
    {
        int estimate = 0;
        estimate += (int)(chanceOf3x3 * availableArea / 9f);
        estimate += (int)(chanceOf2x2 * availableArea / 4f);
        estimate += (int)(chanceOf1x1 * availableArea     );
        return estimate;
    }

    public bool Try(string name, Vector2Int pos)
    {
        int size = Random.Range(0, totalAsteroidWeight + 1) switch
        {
            var n when n > weightOf1x1s + weightOf2x2s => 3,
            var n when n > weightOf1x1s                => 2,
            _                                          => 1,
        };

        bool failed = !Asteroid.Spawn(name, pos, size, out Asteroid asteroid);
        if (failed) return false;

        bool dontAddResources = Random.Range(0f, 1f) > chanceOfResources;
        if (dontAddResources) return true;

        int randomWeight = Random.Range(0, totalResourceWeight + 1);
        var kind = randomWeight > weightOfGreen ? ResourceNode.Kind.Red : ResourceNode.Kind.Green;
        
        asteroid.AddResources(kind);

        GameManager.ObjectPools.Get<AsteroidWithResources>().Add(asteroid, true);
        
        return true;
    }
}
