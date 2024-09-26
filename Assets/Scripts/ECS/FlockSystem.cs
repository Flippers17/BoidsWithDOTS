using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public partial struct FlockSystem : ISystem
{
    //private FlockAgentOcttree _octree;

    private ObstacleAvoidanceRays OARays;

    private EntityQuery query;
    private NativeArray<Entity> entities;
    private NativeArray<RefRO<LocalTransform>> transforms;
    private NativeArray<RefRO<AgentMovement>> movementComponents;
    private NativeArray<RefRO<AgentSight>> sightComponents;
    private NativeArray<bool> contextMask;

    ComponentLookup<LocalTransform> transformLookup;
    ComponentLookup<AgentMovement> movementLookup;
    ComponentLookup<AgentSight> sightLookup;


    private bool firstUpdateDone;

    public void OnCreate(ref SystemState state)
    {
        //return;
        //state.RequireForUpdate<AgentMovement>();
        OARays = new ObstacleAvoidanceRays(45);
        //query = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>() ,ComponentType.ReadWrite<AgentMovement>(), ComponentType.ReadOnly<AgentSight>());
        
        
        firstUpdateDone = false;
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state) 
    {
        //return;
        if (!firstUpdateDone)
        {
            query = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadWrite<AgentMovement>(), ComponentType.ReadOnly<AgentSight>());
            entities = query.ToEntityArray(Allocator.Persistent);

            transforms = new NativeArray<RefRO<LocalTransform>>(entities.Length, Allocator.Persistent);
            movementComponents = new NativeArray<RefRO<AgentMovement>>(entities.Length, Allocator.Persistent);
            sightComponents = new NativeArray<RefRO<AgentSight>>(entities.Length, Allocator.Persistent);

            contextMask = new NativeArray<bool>(entities.Length, Allocator.Persistent);
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

        for (int i = 0; i < entities.Length; i++)
        {
            for (int j = 0; j < entities.Length; j++)
            {
                if (i == j)
                {
                    contextMask[j] = false;
                    continue;
                }

                if (GetSquareMagnitude(transforms[j].ValueRO.Position - transforms[i].ValueRO.Position) < sightComponents[i].ValueRO.sightRadius * sightComponents[i].ValueRO.sightRadius)
                    contextMask[j] = true;
                else
                    contextMask[j] = false;
            }

            CalculateVelocity(i, ref state);

            LocalTransform newTransform = new LocalTransform() { Rotation = Quaternion.LookRotation(movementComponents[i].ValueRO.velocity), Position = transforms[i].ValueRO.Position, Scale = transforms[i].ValueRO.Scale };
            state.EntityManager.SetComponentData<LocalTransform>(entities[i], newTransform.Translate(movementComponents[i].ValueRO.velocity * SystemAPI.Time.DeltaTime));
            
        }

        //entities.Dispose();
        //contextMask.Dispose();
        //
        //transforms.Dispose();
        //movementComponents.Dispose();
        //sightComponents.Dispose();

    }

    public void CalculateVelocity(int index, ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float3 force = float3.zero;


        //for (int i = 0; i < behaviourCount; i++)
        //{
        //    force += behaviours[i].behaviour.CalculateMovement(this, context, behaviours[i].forceMultiplier) * (behaviours[i].weight * weightMultiplier);
        //}

        LocalTransform currentTransform = transforms[index].ValueRO;
        AgentMovement currentMovement = movementComponents[index].ValueRO;


        force += CohesionBehaviour.CalculateEntityMovement(currentTransform.Position, transforms, contextMask, 5);
        //force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(transforms[index].ValueRO, sightComponents[index].ValueRO, 1000, OARays);
        force += AlignmentBehaviour.CalculateEntityMovement(currentMovement, movementComponents, contextMask, 10);
        force += SeparationBehaviour.CalculateEntityMovement(currentTransform.Position, transforms, contextMask, 100);
        force += TargetSteeringBehaviour.CalculateEntityMovement(float3.zero, currentTransform.Position, 1f);


        force = force * deltaTime;
        float3 newVelocity = float3.zero;
        newVelocity = currentMovement.velocity + force;


        float squaredMaxSpeed = currentMovement.maxSpeed * currentMovement.maxSpeed;
        float squareMagnitudeNewVel = GetSquareMagnitude(newVelocity);

        //newVelocity = NormalizedFloat3(newVelocity) * movementComponents[index].ValueRO.maxSpeed;

        if (squareMagnitudeNewVel > squaredMaxSpeed && squareMagnitudeNewVel > GetSquareMagnitude(currentMovement.velocity))
            newVelocity = NormalizedFloat3(newVelocity) * (GetMagnitude(currentMovement.velocity) - (currentMovement.deceleration * deltaTime));

        //acceleration
        if (GetSquareMagnitude(newVelocity) < squaredMaxSpeed)
            newVelocity += NormalizedFloat3(newVelocity) * (currentMovement.acceleration * deltaTime);


        state.EntityManager.SetComponentData<AgentMovement>(entities[index], currentMovement.SetVelocity(newVelocity));
        //Debug.Log(movementComponents[index].ValueRO.velocity);


    }


    public void OnDestroy(ref SystemState state) 
    {
        entities.Dispose();
        contextMask.Dispose();

        transforms.Dispose();
        movementComponents.Dispose();
        sightComponents.Dispose();

        OARays.Dispose();
    }


    public void ResetSystem()
    {
        firstUpdateDone = false;
    }


    public static float GetSquareMagnitude(float3 v)
    {
        return (v.x * v.x) + (v.y * v.y) + (v.z * v.z);
    }

    public static float GetMagnitude(float3 v)
    {
        return Mathf.Sqrt((v.x * v.x) + (v.y * v.y) + (v.z * v.z));
    }

    public static float3 NormalizedFloat3(float3 v)
    {
        return v/GetMagnitude(v);
    }
}
