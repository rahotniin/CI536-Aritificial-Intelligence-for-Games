using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager
{
    private ObjectPools objectPools = new();
    private SpatialDictionary spatialDictionary = new SpatialDictionary().Init();
    
    [SerializeField]
    private Prefabs prefabs;

    [SerializeField]
    private LevelGenerator levelGenerator;
    
    [SerializeField]
    private PlayerObject playerObject;

    [SerializeField]
    private UIManager uiManager;

    [Header("Miscellaneous")]
    [Min(float.Epsilon)]
    public float rateOfTime = 1.0f;

    [Header("Debug (Spatial Dictionary)")]
    [SerializeField] bool drawOccupied = false;
}

// Singleton
partial class GameManager
{
    private static GameManager Instance = null;

    public static ObjectPools       ObjectPools       { get { return Instance.objectPools;       } }
    public static SpatialDictionary SpatialDictionary { get { return Instance.spatialDictionary; } }
    public static Prefabs           Prefabs           { get { return Instance.prefabs;           } }
    public static LevelGenerator    Level             { get { return Instance.levelGenerator;    } }
    public static PlayerObject      PlayerObject      { get { return Instance.playerObject;      } }
    public static float             RateOfTime        { get { return Instance.rateOfTime;        } set { Instance.rateOfTime = value; } }
    public static UIManager         UIManager         { get { return Instance.uiManager;         } }
}

// i.e. `impl Monobehaviour for GameManager`
partial class GameManager : MonoBehaviour
{
    void OnValidate()
    {
        if (Instance == null)
        {
            Instance = this;
        } else if (Instance != this)
        {
            Debug.LogError("Non-singleton instance of 'GameManager' created: " + name);
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        Debug.developerConsoleEnabled = true;
        Debug.developerConsoleVisible = true;
        levelGenerator.SpawnLevel();
    }

    void OnDrawGizmos()
    {
        if (drawOccupied) { spatialDictionary.DrawOccupied(Color.blue); }
    }
}