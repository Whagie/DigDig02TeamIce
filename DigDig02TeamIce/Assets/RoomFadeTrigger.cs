using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomFadeTrigger : MonoBehaviour
{
    [SerializeField] private Room ThisRoom;
    [SerializeField] private Room FromRoom;
    private GameObject Player;

    private void Start()
    {
        Player = GameObject.FindObjectOfType<Player>().MainCollider.gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == Player)
        {
            if (ThisRoom != null)
            {
                if (!ThisRoom.FadedIn)
                {
                    ThisRoom.StartFadeIn();
                }
            }
            if (FromRoom != null)
            {
                if (!FromRoom.FadedOut)
                {
                    FromRoom.StartFadeOut();
                }
            }
        }
    }
}
