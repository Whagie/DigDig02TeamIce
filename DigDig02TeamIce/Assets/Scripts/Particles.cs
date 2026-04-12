using System.Collections.Generic;
using UnityEngine;

public static class Particles
{
    private static Dictionary<string, GameObject> lookup;

    public static GameObject P_spark { get; private set; }
    public static GameObject P_SpearExplosion { get; private set; }
    public static GameObject P_SlamAttack { get; private set; }
    public static GameObject P_BreakableWall { get; private set; }
    public static GameObject P_ShamefulBallCharge { get; private set; }
    public static GameObject P_EvilBallCharge { get; private set; }
    public static GameObject P_PinkMagicProjectile { get; private set; }
    public static GameObject P_PinkMagicHit { get; private set; }
    public static GameObject P_LightBeamHit { get; private set; }
    public static GameObject P_LightBeamReflectorHit { get; private set; }
    public static GameObject P_GuardSlam { get; private set; }
    public static GameObject P_PlayerSlash { get; private set; }

    static Particles()
    {
        var prefabs = Resources.LoadAll<GameObject>("Particles");
        lookup = new Dictionary<string, GameObject>();

        foreach (var prefab in prefabs)
        {
            lookup[prefab.name] = prefab;

            // Auto-map by name
            switch (prefab.name)
            {
                case nameof(P_spark): P_spark = prefab; break;
                case nameof(P_SpearExplosion): P_SpearExplosion = prefab; break;
                case nameof(P_SlamAttack): P_SlamAttack = prefab; break;
                case nameof(P_BreakableWall): P_BreakableWall = prefab; break;
                case nameof(P_ShamefulBallCharge): P_ShamefulBallCharge = prefab; break;
                case nameof(P_EvilBallCharge): P_EvilBallCharge = prefab; break;
                case nameof(P_PinkMagicProjectile): P_PinkMagicProjectile = prefab; break;
                case nameof(P_PinkMagicHit): P_PinkMagicHit = prefab; break;
                case nameof(P_LightBeamHit): P_LightBeamHit = prefab; break;
                case nameof(P_LightBeamReflectorHit): P_LightBeamReflectorHit = prefab; break;
                case nameof(P_GuardSlam): P_GuardSlam = prefab; break;
                case nameof(P_PlayerSlash): P_PlayerSlash = prefab; break;
            }
        }
    }

    // Currently unused
    public static GameObject Get(string name) =>
        lookup.TryGetValue(name, out var prefab) ? prefab : null;
}
