using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyBar : MonoBehaviour
{
    public int Energy;
    public int MaxEnergy;
    public float Width;
    public float Height;

    [SerializeField] private RectTransform energyBar;

    private Player player;

    private void Start()
    {
        if (energyBar != null)
        {
            Width = energyBar.rect.width;
            Height = energyBar.rect.height;
        }
        player = GameObject.FindObjectOfType<Player>();
        if (player != null)
        {
            player.OnChangeEnergy += SetEnergy;
            SetMaxEnergy(player.MaxEnergy);
            Energy = player.Energy;
            SetEnergy(Energy);
        }
    }
    private void Update()
    {
        if (player != null)
        {
            SetEnergy(player.Energy);
        }
    }
    public void SetMaxEnergy(int maxHealth)
    {
        MaxEnergy = maxHealth;
    }

    public void SetEnergy(int energy)
    {
        Energy = energy;
        float newWidth = ((float)Energy / MaxEnergy) * Width;
        energyBar.sizeDelta = new Vector2(newWidth, Height);
    }
}
