using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities.UniversalDelegates;
using System;
using UnityEngine.UIElements;

[BurstCompile]
public partial struct FlockSystemWithOctree : ISystem
{
    //private FlockAgentOcttree _octree;
    public EntityOctree octree;

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
        return;
        OARays = new ObstacleAvoidanceRays(45);
        octree = new EntityOctree(6, 4, new Bounds(Vector3.zero, new Vector3(120, 120, 120)));

        firstUpdateDone = false;
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        return;
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
            octree.InsertPointToTree(i, transforms[i].ValueRO.Position);
        }


        for (int i = 0; i < entities.Length; i++)
        {

            NativeList<int> context = new NativeList<int>(16, Allocator.TempJob);
            octree.FindNeighbouringAgents(entities[i].Index, sightComponents[i].ValueRO.sightRadius, transforms[i].ValueRO.Position, ref context);
            
            CalculateVelocity(i, ref state, context);

            LocalTransform newTransform = new LocalTransform() { Rotation = Quaternion.LookRotation(movementComponents[i].ValueRO.velocity), Position = transforms[i].ValueRO.Position, Scale = transforms[i].ValueRO.Scale };
            state.EntityManager.SetComponentData<LocalTransform>(entities[i], newTransform.Translate(movementComponents[i].ValueRO.velocity * SystemAPI.Time.DeltaTime));
        }


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
    public void CalculateVelocity(int index, ref SystemState state, NativeList<int> context)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 force = float3.zero;


        //for (int i = 0; i < behaviourCount; i++)
        //{
        //    force += behaviours[i].behaviour.CalculateMovement(this, context, behaviours[i].forceMultiplier) * (behaviours[i].weight * weightMultiplier);
        //}

        //When all are active, they seem to be drawn towards 0, 0, 0 .
        force += CohesionBehaviour.CalculateEntityMovement(transforms[index].ValueRO.Position, transforms, context, 5);
        force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(transforms[index].ValueRO, sightComponents[index].ValueRO, 1000, OARays);
        force += AlignmentBehaviour.CalculateEntityMovement(movementComponents[index].ValueRO, movementComponents, context, 10);
        force += SeparationBehaviour.CalculateEntityMovement(transforms[index].ValueRO.Position, transforms, context, 1000);
        force += TargetSteeringBehaviour.CalculateEntityMovement(float3.zero, transforms[index].ValueRO.Position, .1f);

        //NativeArray<bool> contextMask = new NativeArray<bool>(entities.Length, Allocator.Temp);
        //for (int i = 0;i < entities.Length;i++)
        //{
        //    for(int j = 0;j < context.Length; j++)
        //    {
        //        if (context[j] == i)
        //            contextMask[i] = true;
        //    }
        //}

        //force += CohesionBehaviour.CalculateEntityMovement(transforms[index].ValueRO.Position, transforms, contextMask, 5, ref state);
        //force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(transforms[index].ValueRO, sightComponents[index].ValueRO, 100, ref state, OARays);
        //force += AlignmentBehaviour.CalculateEntityMovement(movementComponents[index].ValueRO, movementComponents, contextMask, 10, ref state);
        //force += SeparationBehaviour.CalculateEntityMovement(transforms[index].ValueRO.Position, transforms, contextMask, 1000, ref state);


        //if(index == 0)
        //    Debug.Log(force);
        //Velocity is fucked up here somewhere...-
        force = force * deltaTime;
        float3 newVelocity = float3.zero;
        newVelocity = movementComponents[index].ValueRO.velocity + force;


        float squaredMaxSpeed = movementComponents[index].ValueRO.maxSpeed * movementComponents[index].ValueRO.maxSpeed;
        float squareMagnitudeNewVel = FlockSystem.GetSquareMagnitude(newVelocity);

        //newVelocity = NormalizedFloat3(newVelocity) * movementComponents[index].ValueRO.maxSpeed;

        if (squareMagnitudeNewVel > squaredMaxSpeed && squareMagnitudeNewVel > FlockSystem.GetSquareMagnitude(movementComponents[index].ValueRO.velocity))
            newVelocity = FlockSystem.NormalizedFloat3(newVelocity) * (FlockSystem.GetMagnitude(movementComponents[index].ValueRO.velocity) - (movementComponents[index].ValueRO.deceleration * deltaTime));

        //acceleration
        if (FlockSystem.GetSquareMagnitude(newVelocity) < squaredMaxSpeed)
            newVelocity += FlockSystem.NormalizedFloat3(newVelocity) * (movementComponents[index].ValueRO.acceleration * deltaTime);


        state.EntityManager.SetComponentData<AgentMovement>(entities[index], movementComponents[index].ValueRO.SetVelocity(newVelocity));
        //Debug.Log(movementComponents[index].ValueRO.velocity);

        //if (index == 0)
        //    Debug.Log(context.Length);

        context.Dispose();

        

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



