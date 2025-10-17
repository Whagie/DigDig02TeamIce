using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public int Health;
    public int MaxHealth;
    public float Width;
    public float Height;

    [SerializeField] private RectTransform healthBar;

    private Player player;

    private void Start()
    {
        if (healthBar != null)
        {
            Width = healthBar.rect.width;
            Height = healthBar.rect.height;
        }
        player = TrackerHost.Current.Get<Player>();
        if (player != null)
        {
            player.OnPlayerTakeDamage += SetHealth;
            SetMaxHealth(player.MaxHealth);
            Health = player.Health;
            SetHealth(Health);
        }
    }
    private void Update()
    {
        if (player != null)
        {
            SetMaxHealth(player.MaxHealth);
            SetHealth(player.Health);
        }
    }
    public void SetMaxHealth(int maxHealth)
    {
        MaxHealth = maxHealth;
    }

    public void SetHealth(int health)
    {
        Health = health;
        float newWidth = ((float)Health / MaxHealth) * Width;
        healthBar.sizeDelta = new Vector2(newWidth, Height);
    }
}
