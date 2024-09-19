using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

[UpdateBefore(typeof(FlockSystem))]
public partial struct AgentEntitySpawnerSystem : ISystem
{

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AgentEntitySpawner>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        AgentEntitySpawner spawner = SystemAPI.GetSingleton<AgentEntitySpawner>();

        for (int i = 0; i < spawner.agentCount; i++)
        {
            Entity e = state.EntityManager.Instantiate(spawner.entityPrefab);
            state.EntityManager.SetComponentData<LocalTransform>(e, state.EntityManager.GetComponentData<LocalTransform>(e).Translate(Random.insideUnitSphere * spawner.radius));
            state.EntityManager.SetComponentData<AgentMovement>(e, state.EntityManager.GetComponentData<AgentMovement>(e).SetID(i));
        }
    }
}
