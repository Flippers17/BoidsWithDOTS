using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AlignmentBehaviour : SteeringBehaviour
{
    [SerializeField]
    private float _alignmentForce = 1f;
    public override Vector3 CalculateMovement(FlockAgent agentToMove, List<FlockAgent> context, float forceMultiplier)
    {
        int contextCount = context.Count;
        if(contextCount == 0)
            return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;

        for(int i = 0; i < contextCount; i++)
        {
            averageVelocity += context[i].velocity;
        }

        averageVelocity /= contextCount;
        averageVelocity -= agentToMove.velocity;
        averageVelocity *= _alignmentForce * forceMultiplier;

        return averageVelocity;
    }
}
