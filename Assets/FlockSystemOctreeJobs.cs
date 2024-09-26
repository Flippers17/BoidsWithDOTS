using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct FlockSystemOctreeJobs : ISystem
{
    //private FlockAgentOcttree _octree;
    public EntityOctreeJobs octree;

    public ObstacleAvoidanceRays OARays;

    private EntityQuery query;
    private NativeArray<Entity> entities;

    //These should be NativeHashMaps
    public NativeArray<RefRO<LocalTransform>> transforms;
    public NativeArray<RefRO<AgentMovement>> movementComponents;
    public NativeArray<RefRO<AgentSight>> sightComponents;

    ComponentLookup<LocalTransform> transformLookup;
    ComponentLookup<AgentMovement> movementLookup;
    ComponentLookup<AgentSight> sightLookup;


    private bool firstUpdateDone;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        OARays = new ObstacleAvoidanceRays(45);
        octree = new EntityOctreeJobs(6, 4, new Bounds(Vector3.zero, new Vector3(120, 120, 120)));

        firstUpdateDone = false;
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!firstUpdateDone)
        {
            query = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadWrite<AgentMovement>(), ComponentType.ReadOnly<AgentSight>());
            entities = query.ToEntityArray(Allocator.Persistent);
            //Debug.Log(entities.Length);
            transforms = new NativeArray<RefRO<LocalTransform>>(entities.Length, Allocator.Persistent);
            movementComponents = new NativeArray<RefRO<AgentMovement>>(entities.Length, Allocator.Persistent);
            sightComponents = new NativeArray<RefRO<AgentSight>>(entities.Length, Allocator.Persistent);

            //Debug.Log(contextMask.Length);
            firstUpdateDone = true;
        }

        transformLookup = state.GetComponentLookup<LocalTransform>();
        movementLookup = state.GetComponentLookup<AgentMovement>();
        sightLookup = state.GetComponentLookup<AgentSight>();

        for (int i = 0; i < entities.Length; i++)
        {
            transforms[i] = transformLookup.GetRefRO(entities[i]);
            movementComponents[i] = movementLookup.GetRefRO(entities[i]);
            sightComponents[i] = sightLookup.GetRefRO(entities[i]);
        }


        //_octree = new EntityOctree(6, 4, new Bounds(Vector3.zero, new Vector3(120, 120, 120)));
        octree.ClearTree();
        //Insertion seems to work, based on drawing the nodes as gizmos in the scene view
        for (int i = 0; i < entities.Length; ++i)
        {
            octree.InsertPointToTree((i, transforms[i].ValueRO, movementComponents[i].ValueRO), transforms[i].ValueRO.Position);
        }


        //for (int i = 0; i < entities.Length; i++)
        //{

        //    NativeList<int> context = new NativeList<int>(16, Allocator.TempJob);
        //    octree.FindNeighbouringAgents(entities[i].Index, sightComponents[i].ValueRO.sightRadius, transforms[i].ValueRO.Position, ref context);
        //    //var searchJob = new FindNeighboursJob { octree = octree, entityIndex = entities[i].Index, entityPos = transforms[i].ValueRO.Position, sightRadius = sightComponents[i].ValueRO.sightRadius };
        //    //searchJob.Schedule(state.Dependency);

        //    CalculateVelocity(i, ref state, context);

        //    LocalTransform newTransform = new LocalTransform() { Rotation = Quaternion.LookRotation(movementComponents[i].ValueRO.velocity), Position = transforms[i].ValueRO.Position, Scale = transforms[i].ValueRO.Scale };
        //    state.EntityManager.SetComponentData<LocalTransform>(entities[i], newTransform.Translate(movementComponents[i].ValueRO.velocity * SystemAPI.Time.DeltaTime));
        //}

        var entityJob = new CalculateBoidsJob { octree = this.octree, deltaTime = SystemAPI.Time.DeltaTime };
        var handle = entityJob.ScheduleParallel(query, state.Dependency);
        handle.Complete();


        for(int i = 0; i < entities.Length; i++)
        {
            LocalTransform newTransform = new LocalTransform() { Rotation = Quaternion.LookRotation(movementComponents[i].ValueRO.velocity), Position = transforms[i].ValueRO.Position, Scale = transforms[i].ValueRO.Scale };
            state.EntityManager.SetComponentData<LocalTransform>(entities[i], newTransform.Translate(movementComponents[i].ValueRO.velocity * SystemAPI.Time.DeltaTime));
        }

        //for (int i = 0; i < entities.Length; i++)
        //{
        //    NativeList<int> newContext = new NativeList<int>(Allocator.Temp);
        //    int stopValue = i < entityStartValue.Length ? entityStartValue[i + 1] : entityStartValue.Length;
        //    for (int j = entityStartValue[i]; j < stopValue; j++)
        //    {
        //        newContext.Add(context[j]);
        //    }

        //    CalculateVelocity(i, ref state, newContext);
        //}

        //EntityOctreeGizmos.nodes = _octree.GetNodes().ToList();

        //_octree.Dispose();
        //entities.Dispose();
        //contextMask.Dispose();
        //
        //transforms.Dispose();
        //movementComponents.Dispose();
        //sightComponents.Dispose();

    }

    

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        Debug.Log("DESTROYEEDE");
        entities.Dispose();

        transforms.Dispose();
        movementComponents.Dispose();
        sightComponents.Dispose();

        OARays.Dispose();
        octree.Dispose();
    }


    //public static float GetSquareMagnitude(float3 v)
    //{
    //    return (v.x * v.x) + (v.y * v.y) + (v.z * v.z);
    //}

    //public static float GetMagnitude(float3 v)
    //{
    //    return Mathf.Sqrt((v.x * v.x) + (v.y * v.y) + (v.z * v.z));
    //}

    //public static float3 NormalizedFloat3(float3 v)
    //{
    //    return v / GetMagnitude(v);
    //}
}


[BurstCompile]
public partial struct CalculateBoidsJob : IJobEntity
{
    [ReadOnly]
    public float deltaTime;

    [ReadOnly]
    public EntityOctreeJobs octree;


    public void Execute(in LocalTransform transform, ref AgentMovement movement, in AgentSight sight)
    {
        NativeList<LocalTransform> contextTransforms = new NativeList<LocalTransform>(Allocator.Temp);
        NativeList<AgentMovement> contextMovement = new NativeList<AgentMovement>(Allocator.Temp);
        octree.FindNeighbouringAgents(-1, sight.sightRadius, transform.Position, ref contextTransforms, ref contextMovement);

        float3 force = float3.zero;


        force += CohesionBehaviour.CalculateEntityMovement(transform.Position, contextTransforms, 5);
        //force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(transforms[index].ValueRO, sightComponents[index].ValueRO, 1000, OARays);
        force += AlignmentBehaviour.CalculateEntityMovement(movement, contextMovement, 10);
        force += SeparationBehaviour.CalculateEntityMovement(transform.Position, contextTransforms, 1000);
        force += TargetSteeringBehaviour.CalculateEntityMovement(float3.zero, transform.Position, 1f);

        
        force = force * deltaTime;
        float3 newVelocity = float3.zero;
        newVelocity = movement.velocity + force;


        float squaredMaxSpeed = movement.maxSpeed * movement.maxSpeed;
        float squareMagnitudeNewVel = FlockSystem.GetSquareMagnitude(newVelocity);

        //newVelocity = NormalizedFloat3(newVelocity) * movementComponents[index].ValueRO.maxSpeed;

        if (squareMagnitudeNewVel > squaredMaxSpeed && squareMagnitudeNewVel > FlockSystem.GetSquareMagnitude(movement.velocity))
            newVelocity = FlockSystem.NormalizedFloat3(newVelocity) * (FlockSystem.GetMagnitude(movement.velocity) - (movement.deceleration * deltaTime));

        //acceleration
        if (FlockSystem.GetSquareMagnitude(newVelocity) < squaredMaxSpeed)
            newVelocity += FlockSystem.NormalizedFloat3(newVelocity) * (movement.acceleration * deltaTime);


        //state.EntityManager.SetComponentData<AgentMovement>(entities[index], movementComponents[index].ValueRO.SetVelocity(newVelocity));
        movement.velocity = newVelocity;
        //Debug.Log(movementComponents[index].ValueRO.velocity);

        //if (index == 0)
        //    Debug.Log(context.Length);

        //contextTransforms.Dispose();
        //contextMovement.Dispose();
    }
}