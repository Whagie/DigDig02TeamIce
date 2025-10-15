using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionModifier
{
    private readonly List<Action<Enemy>> _onApply = new();
    private readonly List<Action<Enemy>> _onRevert = new();

    public void Evaluate(Enemy enemy)
    {
        foreach (var apply in _onApply)
            apply(enemy);
    }

    public void Revert(Enemy enemy)
    {
        foreach (var revert in _onRevert)
            revert(enemy);
    }

    // --- Examples ---
    public ActionModifier StopAgent()
    {
        _onApply.Add(e =>
        {
            if (e.NavAgent != null)
            {
                e.NavAgent.isStopped = true;
                e.NavAgent.velocity = Vector3.zero;
            }
        });
        _onRevert.Add(e =>
        {
            if (e.NavAgent != null)
            {
                e.NavAgent.isStopped = false;

                // If destination still valid, reapply it to ensure resuming works:
                if (e.NavAgent.hasPath)
                    e.NavAgent.SetDestination(e.NavAgent.destination);
            }
        });
        return this;
    }

    public ActionModifier ChangeSpeed(float speed)
    {
        _onApply.Add(e =>
        {
            if (e.NavAgent != null)
            {
                e.speedOverride = true;
                e.tempSpeed = e.NavAgent.speed;
                e.SetSpeed(speed, true);
            }
        });
        _onRevert.Add(e =>
        {
            if (e.NavAgent != null)
                e.speedOverride = false;
                e.SetSpeed(e.tempSpeed);
        });
        return this;
    }
}

