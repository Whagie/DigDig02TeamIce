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

    private void Start()
    {
        if (energyBar != null)
        {
            Width = energyBar.rect.width;
            Height = energyBar.rect.height;
        }
        Player player = TrackerHost.Current.Get<Player>();
        if (player != null)
        {
            player.OnGetEnergy += SetEnergy;
            SetMaxEnergy(player.MaxEnergy);
            Energy = player.Energy;
            SetEnergy(Energy);
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
