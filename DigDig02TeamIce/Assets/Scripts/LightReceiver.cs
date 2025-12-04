using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightReceiver : MonoBehaviour
{
    public event System.Action OnReceivedLight;
    public bool ReceivingLight = false;

    private float receiveLightTimer = 2f;
    public float lightTimerLength = 2f;

    public bool Activated = false;

    private MeshRenderer meshRenderer;

    [SerializeField] private List<GameObject> destroyOnRecieve;

    [SerializeField] private WakeEnemies wakeEnemies;

    private void Start()
    {
        if (wakeEnemies == null)
        {
            wakeEnemies = GetComponent<WakeEnemies>();
        }

        receiveLightTimer = lightTimerLength;
        meshRenderer = GetComponent<MeshRenderer>();
    }
    private void Update()
    {
        if (Activated)
            return;

        if (ReceivingLight)
        {
            receiveLightTimer -= Time.deltaTime;
        }
        else if (receiveLightTimer < lightTimerLength)
        {
            receiveLightTimer += Time.deltaTime * 2f;
        }

        meshRenderer.material.color = Color.Lerp(Color.green, Color.white, receiveLightTimer / lightTimerLength);

        if (receiveLightTimer <= 0f)
        {
            ReceivedLight();
        }
    }
    public void ReceivedLight()
    {
        OnReceivedLight?.Invoke();
        Activated = true;
        meshRenderer.material.color = Color.cyan;
        foreach (var obj in destroyOnRecieve)
        {
            Destroy(obj);
        }

        if (wakeEnemies != null)
        {
            wakeEnemies.WakeUp();
        }
    }
}
