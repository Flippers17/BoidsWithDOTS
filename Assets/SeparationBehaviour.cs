using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class SeparationBehaviour : SteeringBehaviour
{
    [SerializeField]
    private float separationStrength = 100f;

    [SerializeField]
    private float _protectedRange = 3f;

    public override Vector3 CalculateMovement(FlockAgent agentToMove, List<FlockAgent> context, float forceMultiplier)
    {
        int contextCount = context.Count;

        if(contextCount == 0)
            return Vector3.zero;

        Vector3 separationVector = Vector3.zero;

        Vector3 currentVector;

        float squaredProtectedRange = _protectedRange * _protectedRange;

        for(int i = 0; i < contextCount; i++)
        {
            currentVector = (context[i].position - agentToMove.position);

            float currentSquareMagnitude = currentVector.sqrMagnitude;
            if (currentSquareMagnitude < squaredProtectedRange)
                separationVector -= currentVector * ((separationStrength * forceMultiplier) / currentSquareMagnitude);
        }

        separationVector/= contextCount;
        return separationVector;
    }
}
