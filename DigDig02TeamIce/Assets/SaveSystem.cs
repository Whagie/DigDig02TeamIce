using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly string FileName = "save.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveData Data { get; private set; }

    // Load or create new
    public static void Load()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            Data = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            Data = new SaveData();
            Save(); // create file immediately
        }
    }

    public static void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(FilePath, json);
    }

    public static void Clear()
    {
        Data = new SaveData();

        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    public static bool HasSaveData()
    {
        if (File.Exists(FilePath))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}