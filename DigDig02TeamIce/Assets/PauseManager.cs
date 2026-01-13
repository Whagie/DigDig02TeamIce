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
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public void UnpauseGame()
    {
        if (prevTimeScale.HasValue)
        {
            IsPaused = false;
            Time.timeScale = prevTimeScale.Value;
            prevTimeScale = null;
        }
        else
        {
            IsPaused = false;
            Time.timeScale = 1f;
            Debug.Log("Time scale was null!");
        }
    }
}
