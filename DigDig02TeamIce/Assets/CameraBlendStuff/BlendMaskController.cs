using UnityEngine;

public class CameraBlendMaskController : MonoBehaviour
{
    public Material blendMaterial;
    public Transform player;
    public Camera mainCamera;

    private void Awake()
    {
        player = GameObject.FindObjectOfType<Player>().transform;
        mainCamera = Camera.main;
    }
    void LateUpdate()
    {
        if (blendMaterial == null || player == null || mainCamera == null)
            return;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(player.position);
        blendMaterial.SetVector("_MaskCenter", new Vector4(screenPos.x, screenPos.y, 0, 0));
    }
}
