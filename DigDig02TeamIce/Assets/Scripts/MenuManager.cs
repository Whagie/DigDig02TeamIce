using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    private Player player;
    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();
    }

    void Update()
    {
        if (UserInput.PausePressed && !player.Dead && !SceneFadeManager.instance.IsFadingIn && !SceneFadeManager.instance.IsFadingOut)
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
