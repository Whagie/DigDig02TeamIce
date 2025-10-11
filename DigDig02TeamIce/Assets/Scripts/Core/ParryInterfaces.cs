using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public interface IHitbox
    {
        GameObject Owner { get; }
        bool CanBeParried { get; }
        Collider Collider { get; }
        LayerMask LayerMask { get; }
        int Damage { get; }
        void OnHit(IHurtbox target);
        void OnParried(IHurtbox by); // called when a parry happens
    }

    public interface IHurtbox
    {
        GameObject Owner { get; }
        Collider Collider { get; }
        LayerMask LayerMask { get; }
        void OnHit(IHitbox source);
        void TakeDamage(int damage);
    }
}