using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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


    public static float3 CalculateEntityMovement(float3 agentToMove, NativeArray<RefRO<LocalTransform>> context, NativeArray<bool> contextMask, float forceMultiplier, ref SystemState state)
    {
        int contextCount = context.Length;

        if (contextCount == 0)
            return float3.zero;

        float3 averagePosition = float3.zero;
        int checkedCount = 0;

        for (int i = 0; i < contextCount; i++)
        {
            if (contextMask[i])
            {
                averagePosition += context[i].ValueRO.Position;
                checkedCount++;
            }
        }

        if (checkedCount == 0)
            return float3.zero;

        averagePosition /= checkedCount;

        //context.Dispose();
        //contextMask.Dispose();

        return (averagePosition - agentToMove) * (forceMultiplier);
    }


    public static float3 CalculateEntityMovement(float3 agentToMove, NativeList<Entity> context, float forceMultiplier, ref SystemState state)
    {
        int contextCount = context.Length;

        if (contextCount == 0)
            return float3.zero;

        float3 averagePosition = float3.zero;

        for (int i = 0; i < contextCount; i++)
        {
            averagePosition += state.EntityManager.GetComponentData<LocalTransform>(context[i]).Position;
        }

        averagePosition /= contextCount;

        //context.Dispose();
        //contextMask.Dispose();

        return (averagePosition - agentToMove) * (forceMultiplier);
    }
}
