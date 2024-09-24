using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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

    public static float3 CalculateEntityMovement(float3 agentToMove, NativeArray<RefRO<LocalTransform>> context, NativeArray<bool> contextMask, float forceMultiplier, ref SystemState state)
    {
        int contextCount = context.Length;

        if (contextCount == 0)
            return float3.zero;

        float3 separationVector = float3.zero;

        float3 currentVector;

        float squaredProtectedRange = 9;

        int checkedCount = 0;

        for (int i = 0; i < contextCount; i++)
        {
            if (!contextMask[i])
                continue;

            
            currentVector = (context[i].ValueRO.Position - agentToMove);

            float currentSquareMagnitude = FlockSystem.GetSquareMagnitude(currentVector);
            if (currentSquareMagnitude < squaredProtectedRange)
            {
                separationVector -= currentVector * (forceMultiplier / Mathf.Max(currentSquareMagnitude, .1f));
                checkedCount++;
            }
        }

        //context.Dispose();
        //contextMask.Dispose();

        separationVector /= Mathf.Max(checkedCount, 1);
        return separationVector;
    }



    public static float3 CalculateEntityMovement(float3 agentToMove, NativeList<Entity> context, float forceMultiplier, ref SystemState state)
    {
        int contextCount = context.Length;

        if (contextCount == 0)
            return float3.zero;

        float3 separationVector = float3.zero;

        float3 currentVector;

        float squaredProtectedRange = 9;

        for (int i = 0; i < contextCount; i++)
        {
            currentVector = (state.EntityManager.GetComponentData<LocalTransform>(context[i]).Position - agentToMove);

            float currentSquareMagnitude = FlockSystem.GetSquareMagnitude(currentVector);
            if (currentSquareMagnitude < squaredProtectedRange)
            {
                separationVector -= currentVector * (forceMultiplier / Mathf.Max(currentSquareMagnitude, .1f));
            }
        }

        //context.Dispose();
        //contextMask.Dispose();

        separationVector /= contextCount;
        return separationVector;
    }
}
