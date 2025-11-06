using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerInteractionBase : MonoBehaviour, IInteractable
{
    public GameObject Player { get; set; }
    public bool CanInteract { get; set; }

    private void Start()
    {
        Player = GameObject.FindObjectOfType<Player>().MainCollider.gameObject;
    }

    private void Update()
    {
        if (CanInteract)
        {
            if (UserInput.InteractPressed)
            {
                Interact();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == Player)
        {
            CanInteract = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == Player)
        {
            CanInteract = false;
        }
    }

    public virtual void Interact() { }
}
