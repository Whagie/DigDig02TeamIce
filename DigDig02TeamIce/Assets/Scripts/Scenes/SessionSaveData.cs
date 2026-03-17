using System.Collections.Generic;
using UnityEngine;

public class SessionSaveData : MonoBehaviour
{
    public static SessionSaveData Instance;

    public struct EnemyDeathData
    {
        public string ID;
        public bool Dead;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public struct EnergyRechargeData
    {
        public string ID;
        public int HitsLeft;
        public int OriginalEnergyAmount;
    }

    public struct SpikeGateStateData
    {
        public string ID;
        public bool Raised;
    }

    public struct LightReflectorData
    {
        public string ID;
        public Vector3 Position;
        public Quaternion ReflectorRotation;
        public bool Glowing;
        public Vector2Int? OriginCoord;
    }

    public struct LightPuzzleGeneralData
    {
        public string ID;
        public bool Glowing;
    }

    public Dictionary<string, EnemyDeathData> DeadEnemies = new();

    public Dictionary<string, EnergyRechargeData> EnergyRechargeCrystals = new();

    public Dictionary<string, SpikeGateStateData> SpikeGateStates = new();

    public Dictionary<string, LightReflectorData> LightReflectors = new();

    public Dictionary<string, LightPuzzleGeneralData> LightPuzzleObjects = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddOrUpdateData(string id, bool dead, Vector3 position, Quaternion rotation)
    {
        DeadEnemies[id] = new EnemyDeathData
        {
            Dead = dead,
            Position = position,
            Rotation = rotation
        };
    }
    public void AddOrUpdateData(string id, int hitsLeft, int origEnergyAmount)
    {
        EnergyRechargeCrystals[id] = new EnergyRechargeData
        {
            HitsLeft = hitsLeft,
            OriginalEnergyAmount = origEnergyAmount
        };
    }
    public void AddOrUpdateData(string id, bool raised)
    {
        SpikeGateStates[id] = new SpikeGateStateData
        {
            Raised = raised
        };
    }

    public void AddOrUpdateData(string id, Vector3 position, Quaternion reflectorRotation, bool glowing, Vector2Int? originCoord)
    {
        LightReflectors[id] = new LightReflectorData
        {
            Position = position,
            ReflectorRotation = reflectorRotation,
            Glowing = glowing,
            OriginCoord = originCoord
        };
    }

    public void AddOrUpdateData(string id, bool glowing, bool isLightPuzzle)
    {
        LightPuzzleObjects[id] = new LightPuzzleGeneralData
        {
            Glowing = glowing
        };
    }

    public bool TryGet(string id, out EnemyDeathData data)
    {
        return DeadEnemies.TryGetValue(id, out data);
    }
    public bool TryGet(string id, out EnergyRechargeData data)
    {
        return EnergyRechargeCrystals.TryGetValue(id, out data);
    }
    public bool TryGet(string id, out SpikeGateStateData data)
    {
        return SpikeGateStates.TryGetValue(id, out data);
    }
    public bool TryGet(string id, out LightReflectorData data)
    {
        return LightReflectors.TryGetValue(id, out data);
    }
    public bool TryGet(string id, out LightPuzzleGeneralData data)
    {
        return LightPuzzleObjects.TryGetValue(id, out data);
    }

    public void ClearAllData()
    {
        DeadEnemies.Clear();
        EnergyRechargeCrystals.Clear();
        SpikeGateStates.Clear();
        LightReflectors.Clear();
        LightPuzzleObjects.Clear();
    }
}
