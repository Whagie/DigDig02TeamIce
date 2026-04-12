using Game.Core;
using System.Collections.Generic;
using UnityEngine;

public class SessionSaveData : MonoBehaviour
{
    public static SessionSaveData Instance;

    private SaveData Data => SaveSystem.Data;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ---------- HELPERS ----------
    private int FindIndex<T>(List<T> list, string id) where T : struct, ISaveDataWithID
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].ID == id)
                return i;
        }
        return -1;
    }

    private void RemoveByID<T>(List<T> list, string id) where T : struct, ISaveDataWithID
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].ID == id)
            {
                list.RemoveAt(i);
                SaveSystem.Save();
                return;
            }
        }
    }

    // ---------- ADD / UPDATE ----------

    public void AddOrUpdateData(string id, bool dead, Vector3 position, Quaternion rotation)
    {
        var list = Data.deadEnemies;

        int index = FindIndex(list, id);

        var data = new EnemyDeathData
        {
            ID = id,
            Dead = dead,
            Position = position,
            Rotation = rotation
        };

        if (index >= 0) list[index] = data;
        else list.Add(data);

        SaveSystem.Save();
    }

    public void AddOrUpdateData(string id, int hitsLeft, int origEnergyAmount)
    {
        var list = Data.energyRechargeCrystals;

        int index = FindIndex(list, id);

        var data = new EnergyRechargeData
        {
            ID = id,
            HitsLeft = hitsLeft,
            OriginalEnergyAmount = origEnergyAmount
        };

        if (index >= 0) list[index] = data;
        else list.Add(data);

        SaveSystem.Save();
    }

    public void AddOrUpdateData(string id, bool raised)
    {
        var list = Data.singleBoolDatas;

        int index = FindIndex(list, id);

        var data = new SingleBoolData
        {
            ID = id,
            IsTrue = raised
        };

        if (index >= 0) list[index] = data;
        else list.Add(data);

        SaveSystem.Save();
    }

    public void AddOrUpdateData(string id, Quaternion reflectorRotation, bool glowing, bool solved)
    {
        var list = Data.lightReflectors;

        int index = FindIndex(list, id);

        var data = new LightReflectorData
        {
            ID = id,
            ReflectorRotation = reflectorRotation,
            Glowing = glowing,
            Solved = solved
        };

        if (index >= 0) list[index] = data;
        else list.Add(data);

        SaveSystem.Save();
    }

    public void AddOrUpdateData(string id, bool glowing, bool receiverActivated)
    {
        var list = Data.lightPuzzleObjects;

        int index = FindIndex(list, id);

        var data = new LightPuzzleGeneralData
        {
            ID = id,
            Glowing = glowing,
            ReceiverActivated = receiverActivated
        };

        if (index >= 0) list[index] = data;
        else list.Add(data);

        SaveSystem.Save();
    }

    public void AddOrUpdateData(string id, Vector3 position, Vector2Int originCoord, bool solved)
    {
        var list = Data.pushableObjects;

        int index = FindIndex(list, id);

        var data = new PushableObjectData
        {
            ID = id,
            Position = position,
            OriginCoord = originCoord,
            Solved = solved
        };

        if (index >= 0) list[index] = data;
        else list.Add(data);

        SaveSystem.Save();
    }

    // ---------- TRY GET ----------

    public bool TryGet(string id, out EnemyDeathData result)
    {
        foreach (var d in Data.deadEnemies)
        {
            if (d.ID == id)
            {
                result = d;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGet(string id, out EnergyRechargeData result)
    {
        foreach (var d in Data.energyRechargeCrystals)
        {
            if (d.ID == id)
            {
                result = d;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGet(string id, out SingleBoolData result)
    {
        foreach (var d in Data.singleBoolDatas)
        {
            if (d.ID == id)
            {
                result = d;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGet(string id, out LightReflectorData result)
    {
        foreach (var d in Data.lightReflectors)
        {
            if (d.ID == id)
            {
                result = d;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGet(string id, out LightPuzzleGeneralData result)
    {
        foreach (var d in Data.lightPuzzleObjects)
        {
            if (d.ID == id)
            {
                result = d;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGet(string id, out PushableObjectData result)
    {
        foreach (var d in Data.pushableObjects)
        {
            if (d.ID == id)
            {
                result = d;
                return true;
            }
        }

        result = default;
        return false;
    }

    // ---------- CLEAR ----------

    public void ClearAllData()
    {
        Data.deadEnemies.Clear();
        Data.energyRechargeCrystals.Clear();
        Data.singleBoolDatas.Clear();
        Data.lightReflectors.Clear();
        Data.lightPuzzleObjects.Clear();
        Data.pushableObjects.Clear();

        SaveSystem.Save();
    }

    public void RemoveEnemyDeathData(string id)
    {
        RemoveByID(SaveSystem.Data.deadEnemies, id);
    }

    public void RemoveSingleBoolData(string id)
    {
        RemoveByID(SaveSystem.Data.singleBoolDatas, id);
    }
}