namespace Dreamteck.Splines.Examples
{
    using UnityEngine;

    public class AddForceAlongPath : MonoBehaviour
    {
        public float force = 10f;
        Rigidbody rb;
        SplineProjector projector;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            projector = GetComponent<SplineProjector>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                rb.AddForce(projector.result.forward * force, ForceMode.Impulse);
            }
        }
    }
}
