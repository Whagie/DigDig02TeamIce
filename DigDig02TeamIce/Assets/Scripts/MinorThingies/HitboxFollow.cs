using UnityEngine;

public class HitboxFollow : MonoBehaviour
{
    [SerializeField] private Transform follow;
    [SerializeField] private float scaleMultiplier = 1f;
    [SerializeField] private bool useOffset = false;

    private Vector3 offset;
    private Quaternion rotOffset;
    void Start()
    {
        if (follow == null)
        {
            follow = gameObject.transform.parent.transform;
        }
        if (follow != null)
        {
            if (useOffset)
            {
                offset = transform.position - follow.position;
                rotOffset = transform.rotation * Quaternion.Inverse(follow.rotation);
                transform.SetPositionAndRotation(follow.position + offset, follow.rotation * rotOffset);
                transform.localScale = follow.localScale * scaleMultiplier;
            }
            else
            {
                transform.SetPositionAndRotation(follow.position, follow.rotation);
                transform.localScale = follow.localScale * scaleMultiplier;
            }
        }
    }

    void LateUpdate()
    {
        if (follow != null)
        {
            if (useOffset)
            {
                transform.SetPositionAndRotation(follow.position + offset, follow.rotation * rotOffset);
                transform.localScale = follow.localScale * scaleMultiplier;
            }
            else
            {
                transform.SetPositionAndRotation(follow.position, follow.rotation);
                transform.localScale = follow.localScale * scaleMultiplier;
            }
        }
    }
}
