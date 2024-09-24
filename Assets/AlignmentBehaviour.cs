using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

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

    public static float3 CalculateEntityMovement(AgentMovement agentMovement, NativeArray<RefRO<AgentMovement>> context, NativeArray<bool> contextMask, float forceMultiplier, ref SystemState state)
    {
        int contextCount = context.Length;
        if (contextCount == 0)
            return float3.zero;

        int checkedContextCount = 0;
        float3 averageVelocity = float3.zero;

        for (int i = 0; i < contextCount; i++)
        {
            if (contextMask[i])
            {
                averageVelocity += context[i].ValueRO.velocity;
                checkedContextCount++;
            }
        }

        if (checkedContextCount == 0)
            return float3.zero;

        averageVelocity /= Mathf.Max(checkedContextCount, 1);
        averageVelocity -= agentMovement.velocity;
        averageVelocity *= forceMultiplier;

        //context.Dispose();
        //contextMask.Dispose();

        return averageVelocity;
    }


    public static float3 CalculateEntityMovement(AgentMovement agentMovement, NativeList<Entity> context, float forceMultiplier, ref SystemState state)
    {
        int contextCount = context.Length;
        if (contextCount == 0)
            return float3.zero;

        float3 averageVelocity = float3.zero;

        for (int i = 0; i < contextCount; i++)
        {
            averageVelocity += state.EntityManager.GetComponentData<AgentMovement>(context[i]).velocity;
        }


        averageVelocity /= contextCount;
        averageVelocity -= agentMovement.velocity;
        averageVelocity *= forceMultiplier;

        //context.Dispose();
        //contextMask.Dispose();

        return averageVelocity;
    }
}
