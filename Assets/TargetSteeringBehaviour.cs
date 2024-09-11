using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TargetSteeringBehaviour : SteeringBehaviour
{
    [NonSerialized]
    public static Transform Target;
    public float targetBias = 1f;

    public override Vector3 CalculateMovement(FlockAgent agentToMove, List<FlockAgent> context, float forceMultiplier)
    {
        if(!Target)
            return Vector3.zero;

        return (Target.position - agentToMove.position) * (targetBias * forceMultiplier);
    }
}
