using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinecartCrystalRemover : MonoBehaviour
{
    [SerializeField] private bool restore = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Minecart"))
        {
            if (!restore)
            {
                Minecart minecart = other.GetComponent<Minecart>();
                if (minecart != null)
                {
                    minecart.RemoveCrystals();
                }
            }
            else
            {
                Minecart minecart = other.GetComponent<Minecart>();
                if (minecart != null)
                {
                    minecart.RestoreCrystals();
                }
            }
        }
    }
}
