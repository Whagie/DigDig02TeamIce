using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string currentRoom = "MainMenu";
    public int lastExitedDoor = 1; // store enum as int
    public float cameraRotationY = -45f;
    public Vector3 cameraChildLocalPosition = new Vector3(0f, 0f, -85f);
    public int energy = 0;
    public string constructCarriedItem = string.Empty;
    public int runesFound = 0;

    public List<EnemyDeathData> deadEnemies = new();
    public List<EnergyRechargeData> energyRechargeCrystals = new();
    public List<SingleBoolData> singleBoolDatas = new();
    public List<LightReflectorData> lightReflectors = new();
    public List<LightPuzzleGeneralData> lightPuzzleObjects = new();
    public List<PushableObjectData> pushableObjects = new();
}