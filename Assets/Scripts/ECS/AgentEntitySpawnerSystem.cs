using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using Unity.Collections;

[UpdateBefore(typeof(FlockSystem))]
public partial struct AgentEntitySpawnerSystem : ISystem
{
    private NativeList<Entity> spawnedEntities;

    public int spawnCount;
    public bool doingExperiment;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AgentEntitySpawner>();
        spawnedEntities = new NativeList<Entity>(Allocator.Persistent);
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        for(int i = spawnedEntities.Length - 1; i >= 0; i--)
        {
            state.EntityManager.DestroyEntity(spawnedEntities[i]);
            spawnedEntities.RemoveAt(i);
        }

        //spawnedEntities.Clear();

        AgentEntitySpawner spawner = SystemAPI.GetSingleton<AgentEntitySpawner>();
        int amountToSpawn = doingExperiment ? spawnCount : spawner.agentCount;

        for (int i = 0; i < spawner.agentCount; i++)
        {
            Entity e = state.EntityManager.Instantiate(spawner.entityPrefab);
            spawnedEntities.Add(e);
            state.EntityManager.SetComponentData<LocalTransform>(e, state.EntityManager.GetComponentData<LocalTransform>(e).Translate(Random.insideUnitSphere * spawner.radius));
            state.EntityManager.SetComponentData<AgentMovement>(e, state.EntityManager.GetComponentData<AgentMovement>(e).SetID(i));
        }

    }
}
