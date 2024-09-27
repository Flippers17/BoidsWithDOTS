using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile, UpdateAfter(typeof(AgentEntitySpawnerSystem))]
public partial struct FlockSystemOctreeJobsBurst : ISystem
{
    //private FlockAgentOcttree _octree;
    public EntityOctreeJobsBurst octree;

    public ObstacleAvoidanceRays OARays;

    private EntityQuery query;
    private NativeArray<Entity> entities;

    //These should be NativeHashMaps
    public NativeArray<RefRO<LocalTransform>> transforms;
    public NativeArray<RefRO<AgentMovement>> movementComponents;
    //public NativeArray<RefRO<AgentSight>> sightComponents;

    ComponentLookup<LocalTransform> transformLookup;
    ComponentLookup<AgentMovement> movementLookup;
    ComponentLookup<AgentSight> sightLookup;


    private bool firstUpdateDone;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        OARays = new ObstacleAvoidanceRays(45);
        octree = new EntityOctreeJobsBurst(6, 4, new Bounds(Vector3.zero, new Vector3(120, 120, 120)));

        firstUpdateDone = false;
        state.Enabled = false;
    }

    
    public void OnUpdate(ref SystemState state)
    {
        RecordingManager.StartSample();

        if (!firstUpdateDone)
        {
            query = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadWrite<AgentMovement>(), ComponentType.ReadOnly<AgentSight>());
            entities = query.ToEntityArray(Allocator.Persistent);

            transforms = new NativeArray<RefRO<LocalTransform>>(entities.Length, Allocator.Persistent);
            movementComponents = new NativeArray<RefRO<AgentMovement>>(entities.Length, Allocator.Persistent);
            //sightComponents = new NativeArray<RefRO<AgentSight>>(entities.Length, Allocator.Persistent);

            firstUpdateDone = true;
        }

        transformLookup = state.GetComponentLookup<LocalTransform>();
        movementLookup = state.GetComponentLookup<AgentMovement>();
        sightLookup = state.GetComponentLookup<AgentSight>();

        for (int i = 0; i < entities.Length; i++)
        {
            transforms[i] = transformLookup.GetRefRO(entities[i]);
            movementComponents[i] = movementLookup.GetRefRO(entities[i]);
            //sightComponents[i] = sightLookup.GetRefRO(entities[i]);
        }


        octree.ClearTree();
        //Insertion seems to work, based on drawing the nodes as gizmos in the scene view
        for (int i = 0; i < entities.Length; ++i)
        {
            octree.InsertPointToTree((i, transforms[i].ValueRO, movementComponents[i].ValueRO), transforms[i].ValueRO.Position);
        }


        var entityJob = new CalculateBoidsJobBurst { octree = this.octree, deltaTime = SystemAPI.Time.DeltaTime };
        var handle = entityJob.ScheduleParallel(query, state.Dependency);
        handle.Complete();


        for(int i = 0; i < entities.Length; i++)
        {
            LocalTransform newTransform = new LocalTransform() { Rotation = Quaternion.LookRotation(movementComponents[i].ValueRO.velocity), Position = transforms[i].ValueRO.Position, Scale = transforms[i].ValueRO.Scale };
            state.EntityManager.SetComponentData<LocalTransform>(entities[i], newTransform.Translate(movementComponents[i].ValueRO.velocity * SystemAPI.Time.DeltaTime));
        }


        RecordingManager.EndSample();
    }


    public void ResetSystem()
    {
        firstUpdateDone = false;
    }

    

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        entities.Dispose();

        transforms.Dispose();
        movementComponents.Dispose();
        //sightComponents.Dispose();

        OARays.Dispose();
        octree.Dispose();
    }

}


[BurstCompile]
public partial struct CalculateBoidsJobBurst : IJobEntity
{
    [ReadOnly]
    public float deltaTime;

    [ReadOnly]
    public EntityOctreeJobsBurst octree;

    [BurstCompile]
    public void Execute(in LocalTransform transform, ref AgentMovement movement, in AgentSight sight)
    {
        NativeList<LocalTransform> contextTransforms = new NativeList<LocalTransform>(Allocator.Temp);
        NativeList<AgentMovement> contextMovement = new NativeList<AgentMovement>(Allocator.Temp);
        octree.FindNeighbouringAgents(-1, sight.sightRadius, transform.Position, ref contextTransforms, ref contextMovement);

        float3 force = float3.zero;


        force += CohesionBehaviour.CalculateEntityMovement(transform.Position, contextTransforms, 5);
        //Can not do physics checks in jobs. I have therefore opted to not use it for this experiment
        //force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(transforms[index].ValueRO, sightComponents[index].ValueRO, 1000, OARays);
        force += AlignmentBehaviour.CalculateEntityMovement(movement, contextMovement, 10);
        force += SeparationBehaviour.CalculateEntityMovement(transform.Position, contextTransforms, 1000);
        force += TargetSteeringBehaviour.CalculateEntityMovement(float3.zero, transform.Position, 1f);

        
        force = force * deltaTime;
        float3 newVelocity = float3.zero;
        newVelocity = movement.velocity + force;


        float squaredMaxSpeed = movement.maxSpeed * movement.maxSpeed;
        float squareMagnitudeNewVel = FlockSystem.GetSquareMagnitude(newVelocity);

        //Deceleration
        if (squareMagnitudeNewVel > squaredMaxSpeed && squareMagnitudeNewVel > FlockSystem.GetSquareMagnitude(movement.velocity))
            newVelocity = FlockSystem.NormalizedFloat3(newVelocity) * (FlockSystem.GetMagnitude(movement.velocity) - (movement.deceleration * deltaTime));

        //acceleration
        if (FlockSystem.GetSquareMagnitude(newVelocity) < squaredMaxSpeed)
            newVelocity += FlockSystem.NormalizedFloat3(newVelocity) * (movement.acceleration * deltaTime);


        movement.velocity = newVelocity;
    }
}