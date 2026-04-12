using Game.Core;
using System;
using UnityEngine;

[Serializable]
public struct EnemyDeathData : ISaveDataWithID
{
    public string ID;
    public bool Dead;
    public Vector3 Position;
    public Quaternion Rotation;

    string ISaveDataWithID.ID => ID;
}

[Serializable]
public struct EnergyRechargeData : ISaveDataWithID
{
    public string ID;
    public int HitsLeft;
    public int OriginalEnergyAmount;

    string ISaveDataWithID.ID => ID;
}

[Serializable]
public struct SingleBoolData : ISaveDataWithID
{
    public string ID;
    public bool IsTrue;

    string ISaveDataWithID.ID => ID;
}

[Serializable]
public struct LightReflectorData : ISaveDataWithID
{
    public string ID;
    public Quaternion ReflectorRotation;
    public bool Glowing;
    public bool Solved;

    string ISaveDataWithID.ID => ID;
}

[Serializable]
public struct LightPuzzleGeneralData : ISaveDataWithID
{
    public string ID;
    public bool Glowing;
    public bool ReceiverActivated;

    string ISaveDataWithID.ID => ID;
}

[Serializable]
public struct PushableObjectData : ISaveDataWithID
{
    public string ID;
    public Vector3 Position;
    public Vector2Int OriginCoord;
    public bool Solved;

    string ISaveDataWithID.ID => ID;
}
