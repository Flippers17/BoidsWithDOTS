using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

public partial struct FlockSystem : ISystem
{
    //private FlockAgentOcttree _octree;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AgentMovement>();
    }

    public void OnUpdate(ref SystemState state) 
    {
        //FlockAgentOcttree.instance.CreateNewTree();
        //FlockAgentOcttree.
        //EntityQuery query = state.GetEntityQuery(typeof(AgentMovement));


        //NativeArray<(RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>)> context = new NativeArray<(RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>)> { [0] =  }
        foreach((RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>) entity in SystemAPI.Query<RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>>())
        {

            foreach((RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>) other in SystemAPI.Query<RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>>())
            {
                if(other.Item2.ValueRO.id == entity.Item2.ValueRO.id)
                    continue;


            }

            entity.Item1.ValueRW = entity.Item1.ValueRO.Translate(entity.Item2.ValueRO.velocity * SystemAPI.Time.DeltaTime);
        }
    }

    public void CalculateVelocity((RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>) entity, NativeArray<(RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>)> context, ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 force = new float3();

        //if (_debugAgent)
        //{
        //    neighbours.Clear();
        //    for (int i = 0; i < context.Count; i++)
        //    {
        //        neighbours.Add(context[i]);
        //    }
        //}


        //for (int i = 0; i < behaviourCount; i++)
        //{
        //    force += behaviours[i].behaviour.CalculateMovement(this, context, behaviours[i].forceMultiplier) * (behaviours[i].weight * weightMultiplier);
        //}

        force += CohesionBehaviour.CalculateEntityMovement(entity.Item1.ValueRO.Position, context, 5);


        force = force * deltaTime;
        float3 newVelocity = new float3();

        //if (_debugAgent)
        //{
        //    //Debug.DrawRay(position, force, Color.cyan, .02f);
        //    //Debug.DrawRay(position, newVelocity, Color.blue, .02f);
        //}

        float squaredMaxSpeed = entity.Item2.ValueRO.maxSpeed * entity.Item2.ValueRO.maxSpeed;
        
        if (GetSquareMagnitude(newVelocity) > squaredMaxSpeed && GetSquareMagnitude(newVelocity) > GetSquareMagnitude(entity.Item2.ValueRO.velocity))
            newVelocity = NormalizedFloat3(newVelocity) * (GetMagnitude(entity.Item2.ValueRO.velocity) - (entity.Item2.ValueRO.deceleration * deltaTime));

        entity.Item2.ValueRW.velocity = newVelocity;

        //acceleration
        if (GetSquareMagnitude(entity.Item2.ValueRO.velocity) < squaredMaxSpeed)
            entity.Item2.ValueRW.velocity += NormalizedFloat3(entity.Item2.ValueRO.velocity) * (entity.Item2.ValueRO.acceleration * deltaTime);
    }


    public void OnDestroy(ref SystemState state) 
    {
    
    }



    public float GetSquareMagnitude(float3 v)
    {
        return (v.x * v.x) + (v.y * v.y) + (v.z * v.z);
    }

    public float GetMagnitude(float3 v)
    {
        return Mathf.Sqrt((v.x * v.x) + (v.y * v.y) + (v.z * v.z));
    }

    public float3 NormalizedFloat3(float3 v)
    {
        return v/GetMagnitude(v);
    }
}
