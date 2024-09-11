using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SteeringBehaviour : ScriptableObject
{
    public abstract Vector3 CalculateMovement(FlockAgent agentToMove, List<FlockAgent> context, float forceMultiplier);
}
