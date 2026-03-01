using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public Dictionary<string, EnemyDeathData> DeadEnemies 
        = new Dictionary<string, EnemyDeathData>();

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

    public bool TryGet(string id, out EnemyDeathData data)
    {
        return DeadEnemies.TryGetValue(id, out data);
    }
}
