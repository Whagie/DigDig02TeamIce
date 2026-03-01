using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    public bool IsPaused { get; private set; } = false;

    private float? prevTimeScale;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Freezer.Freeze(99999f);
    }

    public void UnpauseGame()
    {
        IsPaused = false;
        Freezer.Cancel();
    }
}
