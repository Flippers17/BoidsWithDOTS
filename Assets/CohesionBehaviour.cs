using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CohesionBehaviour : SteeringBehaviour
{
    [SerializeField]
    private float _cohesionForce = 1f;

    public override Vector3 CalculateMovement(FlockAgent agentToMove, List<FlockAgent> context, float forceMultiplier)
    {
        int contextCount = context.Count;

        if(contextCount == 0)
            return Vector3.zero;

        Vector3 averagePosition = Vector3.zero;

        for(int i = 0; i < contextCount; i++)
        {
            averagePosition += context[i].position;
        }

        averagePosition /= contextCount;

        return (averagePosition - agentToMove.position) * (_cohesionForce * forceMultiplier);
    }
}
