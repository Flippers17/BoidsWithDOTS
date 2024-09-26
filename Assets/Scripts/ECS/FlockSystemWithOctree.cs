using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;


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

    public void OnCreate(ref SystemState state)
    {
        OARays = new ObstacleAvoidanceRays(45);
        octree = new EntityOctree(6, 4, new Bounds(Vector3.zero, new Vector3(120, 120, 120)));

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
            sightComponents = new NativeArray<RefRO<AgentSight>>(entities.Length, Allocator.Persistent);

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

        RecordingManager.EndSample();

    }

    public void CalculateVelocity(int index, ref SystemState state, NativeList<int> context)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 force = float3.zero;


        LocalTransform currentTransform = transforms[index].ValueRO;
        AgentMovement currentMovement = movementComponents[index].ValueRO;

        //Force from different steering behaviours
        force += CohesionBehaviour.CalculateEntityMovement(currentTransform.Position, transforms, context, 5);
        //force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(currentTransform, sightComponents[index].ValueRO, 1000, OARays);
        force += AlignmentBehaviour.CalculateEntityMovement(currentMovement, movementComponents, context, 10);
        force += SeparationBehaviour.CalculateEntityMovement(currentTransform.Position, transforms, context, 1000);
        force += TargetSteeringBehaviour.CalculateEntityMovement(float3.zero, currentTransform.Position, 1f);

       

        force = force * deltaTime;
        float3 newVelocity = float3.zero;
        newVelocity = currentMovement.velocity + force;


        float squaredMaxSpeed = currentMovement.maxSpeed * currentMovement.maxSpeed;
        float squareMagnitudeNewVel = FlockSystem.GetSquareMagnitude(newVelocity);

        //newVelocity = NormalizedFloat3(newVelocity) * movementComponents[index].ValueRO.maxSpeed;

        if (squareMagnitudeNewVel > squaredMaxSpeed && squareMagnitudeNewVel > FlockSystem.GetSquareMagnitude(currentMovement.velocity))
            newVelocity = FlockSystem.NormalizedFloat3(newVelocity) * (FlockSystem.GetMagnitude(currentMovement.velocity) - (currentMovement.deceleration * deltaTime));

        //acceleration
        if (FlockSystem.GetSquareMagnitude(newVelocity) < squaredMaxSpeed)
            newVelocity += FlockSystem.NormalizedFloat3(newVelocity) * (currentMovement.acceleration * deltaTime);


        state.EntityManager.SetComponentData<AgentMovement>(entities[index], currentMovement.SetVelocity(newVelocity));
       

        context.Dispose(); 

    }

    public void OnDestroy(ref SystemState state)
    {
        entities.Dispose();

        transforms.Dispose();
        movementComponents.Dispose();
        sightComponents.Dispose();

        OARays.Dispose();
        octree.Dispose();
    }


    public void ResetSystem()
    {
        firstUpdateDone = false;
    }

}



