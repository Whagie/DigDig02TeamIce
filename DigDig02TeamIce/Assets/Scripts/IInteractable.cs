using UnityEngine;

public interface IInteractable
{
    GameObject Player { get; set; }

    bool CanInteract { get; set; }

    void Interact();

    void OnEnter();

    void OnExit();
}
