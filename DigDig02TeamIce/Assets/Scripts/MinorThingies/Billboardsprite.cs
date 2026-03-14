using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    private Player player;
    private Transform cam;
    public Vector3 target;

    [SerializeField] private bool LockOnTarget;
    void Start()
    {
        cam = Camera.main.transform;
        player = FindObjectOfType<Player>();
    }

    void LateUpdate()
    {
        transform.LookAt(cam);

        if (LockOnTarget && player.currentTarget != null)
        {
            if (player.currentTarget.Dead)
                return;

            transform.localScale = Vector3.one;
            transform.position = player.currentTarget.Center.position;
            //transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180f, transform.rotation.w);
        }
        else
        {
            transform.position = target;
            transform.localScale = Vector3.zero;
        }
    }
}
