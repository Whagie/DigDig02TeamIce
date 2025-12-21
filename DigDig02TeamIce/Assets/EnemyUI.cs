using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public Enemy EnemyOwner;
    public Slider healthBar;
    public CanvasGroup group;
    public Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
        if (EnemyOwner != null)
        {
            healthBar.maxValue = EnemyOwner.MaxHealth;
            healthBar.value = EnemyOwner.Health;
        }
        group.alpha = 0f;
    }

    private void Update()
    {
        if (EnemyOwner != null)
        {
            healthBar.maxValue = EnemyOwner.MaxHealth;
            healthBar.value = EnemyOwner.Health;
        }
    }

    private void LateUpdate()
    {
        if (cam != null)
        {
            transform.LookAt(cam);
        }
    }
}
