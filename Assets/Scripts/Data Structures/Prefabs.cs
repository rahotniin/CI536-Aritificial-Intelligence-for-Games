using System;
using System.Collections.Generic;
using UnityEngine;

// todo: make the prefabs readonly
[Serializable]
public class Prefabs : ISerializationCallbackReceiver
{
    [field: Header("Planets")]
    [field: SerializeField] public Planet jovianPlanet { get; private set; }
    [field: SerializeField] public Planet brokenPlanet { get; private set; }
    
    [field: Header("Asteroids")]
    [field: SerializeField] public List<Asteroid> size1x1Asteroids { get; private set; }
    [field: SerializeField] public List<Asteroid> size2x2Asteroids { get; private set; }
    [field: SerializeField] public List<Asteroid> size3x3Asteroids { get; private set; }
    public List<List<Asteroid>> asteroids { get; private set; } // indexed by asteroid size

    [Header("Resource Nodes")]
    [field: SerializeField] public ResourceNode resourceNodeGreen { get; private set; }
    [field: SerializeField] public ResourceNode resourceNodeRed   { get; private set; }
    
    [field: Header("Spaceships")]
    [field: SerializeField] public MiningShip miningShip { get; private set; }
    [field: SerializeField] public CargoShip cargoShip   { get; private set; }
    [field: SerializeField] public CombatShip combatShip { get; private set; }
    
    [field: Header("Miscellaneous")]
    [field: SerializeField] public Shipyard shipyard { get; private set; }
    [field: SerializeField] public Cargo cargo    { get; private set; }

    public void OnBeforeSerialize() {}
    public void OnAfterDeserialize()
    {
        asteroids = new List<List<Asteroid>> { null, size1x1Asteroids, size2x2Asteroids, size3x3Asteroids };
    }
}

