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
        // Make the quad face the cam
        transform.LookAt(cam);

        if (LockOnTarget && player.currentTarget != null)
        {
            transform.localScale = Vector3.one * 0.75f;
            transform.position = player.currentTarget.transform.position + new Vector3(0, player.currentTarget.GetComponent<Collider>().bounds.size.y * 2f, 0);
            //transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, 180f, transform.rotation.w);
        }
        else
        {
            transform.position = target;
            transform.localScale = Vector3.zero;
        }
    }
}
