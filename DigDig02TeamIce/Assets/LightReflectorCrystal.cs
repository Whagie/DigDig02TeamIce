using UnityEngine;

public class LightReflectorCrystal : MonoBehaviour
{
    public LightPuzzleReflector ParentReflector;
    public bool IsPos1;

    private LightBeam occupyingBeam;
    public LightBeam OccupyingBeam
    {
        get => occupyingBeam;
        set
        {
            if (occupyingBeam == value)
                return;

            if (occupyingBeam != null)
                ParentReflector.UnregisterCrystalHit();

            occupyingBeam = value;

            if (occupyingBeam != null)
                ParentReflector.RegisterCrystalHit();
        }
    }
}