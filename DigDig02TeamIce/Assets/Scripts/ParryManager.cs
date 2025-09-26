using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryManager : MonoBehaviour
{
    public GameObject ParryAnimation;

    public void Parry(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Parried!");
            Instantiate(ParryAnimation, transform.position, Quaternion.identity);


            CameraActions.Main.Punch(-0.8f, 0.15f);

            //CameraActions.Main.Shake(
            //    duration: 0.5f,
            //    fovIntensity: 5f,
            //    tiltIntensity: 3f,
            //    curve: AnimationCurve.EaseInOut(0, 1, 1, 0)
            //);
        }
    }
}
