using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class EnergyParticleManager : MonoBehaviour
{
    [SerializeField] private Transform Pos1;
    [SerializeField] private Transform Pos2;
    [SerializeField] private Transform Pos3;

    public Transform StartPos { get; set; }
    public Transform MiddlePos { get; set; }
    public Transform EndPos { get; set; }

    public VisualEffect vfx; // Drag your VFX component here in the Inspector

    void Start()
    {

    }

    void Update()
    {
        if (StartPos != null)
        {
            Pos1.position = StartPos.position;
        }
        if (MiddlePos != null)
        {
            Pos2.position = MiddlePos.position;
        }
        if (EndPos != null)
        {
            Pos3.position = EndPos.position;
        }
    }
}
