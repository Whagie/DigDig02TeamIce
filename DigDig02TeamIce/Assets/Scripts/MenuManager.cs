using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        if (UserInput.PausePressed)
        {
            if (!PauseManager.instance.IsPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
    }

    public void Pause()
    {
        PauseManager.instance.PauseGame();
    }
    public void Unpause()
    {
        PauseManager.instance.UnpauseGame();
    }
}
