using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartLiftTrigger : MonoBehaviour
{
    [SerializeField] private MinecartLift minecartLift;
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Minecart"))
        {
            Minecart follower = other.GetComponent<Minecart>();
            if (follower == null)
            {
                Debug.Log("Is NULL");
            }
            minecartLift.cart = follower;
            minecartLift.pointLock = follower.GetComponent<SplineFollowerPointLock>();
            minecartLift.ReceiveCart(follower.splineFollower, 1f, true);
        }
    }
}
