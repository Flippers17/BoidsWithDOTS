using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public partial class SystemHandlerSystem : SystemBase
{


    public void DisableSystem(int i)
    {
        switch (i)
        {
            case 0:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystem>()).Enabled = false;
                break;

            case 1:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystemWithOctree>()).Enabled = false;
                break;
            
            case 2:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystemOctreeJobs>()).Enabled = false;
                break;

            case 3:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystemOctreeJobsBurst>()).Enabled = false;
                break;
        }
    }


    public void EnableSystem(int i)
    {
        switch (i)
        {
            case 0:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystem>()).Enabled = true;
                break;

            case 1:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystemWithOctree>()).Enabled = true;
                break;

            case 2:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystemOctreeJobs>()).Enabled = true;
                break;

            case 3:
                World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<FlockSystemOctreeJobsBurst>()).Enabled = true;
                break;
        }
    }

    public void ResetSystem(int i)
    {
        switch (i)
        {
            case 0:
                World.Unmanaged.GetUnsafeSystemRef<FlockSystem>(CheckedStateRef.World.GetExistingSystem<FlockSystem>()).ResetSystem();
                break;

            case 1:
                World.Unmanaged.GetUnsafeSystemRef<FlockSystemWithOctree>(CheckedStateRef.World.GetExistingSystem<FlockSystemWithOctree>()).ResetSystem();
                break;

            case 2:
                World.Unmanaged.GetUnsafeSystemRef<FlockSystemOctreeJobs>(CheckedStateRef.World.GetExistingSystem<FlockSystemOctreeJobs>()).ResetSystem();
                break;

            case 3:
                World.Unmanaged.GetUnsafeSystemRef<FlockSystemOctreeJobsBurst>(CheckedStateRef.World.GetExistingSystem<FlockSystemOctreeJobsBurst>()).ResetSystem();
                break;
        }
    }

    public void SpawnEntities(int amount)
    {
        World.Unmanaged.GetUnsafeSystemRef<AgentEntitySpawnerSystem>(CheckedStateRef.World.GetExistingSystem<AgentEntitySpawnerSystem>()).doingExperiment = true;
        World.Unmanaged.GetUnsafeSystemRef<AgentEntitySpawnerSystem>(CheckedStateRef.World.GetExistingSystem<AgentEntitySpawnerSystem>()).spawnCount = amount;
        World.Unmanaged.ResolveSystemStateRef(CheckedStateRef.World.GetExistingSystem<AgentEntitySpawnerSystem>()).Enabled = true;
    }

    public void DestroyEntities()
    {
        World.Unmanaged.GetUnsafeSystemRef<AgentEntitySpawnerSystem>(CheckedStateRef.World.GetExistingSystem<AgentEntitySpawnerSystem>()).DestroyEntities(CheckedStateRef.EntityManager);
    }


    protected override void OnUpdate()
    {

    }
}
