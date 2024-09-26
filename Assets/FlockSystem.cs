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
        return;
        //state.RequireForUpdate<AgentMovement>();
        OARays = new ObstacleAvoidanceRays(45);
        //query = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>() ,ComponentType.ReadWrite<AgentMovement>(), ComponentType.ReadOnly<AgentSight>());
        
        
        firstUpdateDone = false;
    }

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

            
            ////movementComponents = query.ToComponentDataArray<AgentMovement>(Allocator.Persistent);
            //sightComponents = query.ToComponentDataArray<AgentSight>(Allocator.Temp);
            //movementComponents = query.ToComponentDataArray<AgentMovement>(Allocator.Temp);
            //transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            contextMask = new NativeArray<bool>(entities.Length, Allocator.Persistent);
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
            //state.EntityManager.GetComponentDataRW<LocalTransform>(state.SystemHandle);
        }

        //FlockAgentOcttree.instance.CreateNewTree();
        //FlockAgentOcttree.
        //EntityQuery query = state.GetEntityQuery(typeof(AgentMovement));


        //state.EntityManager.GetComponentData<AgentMovement>(array[0]);
        //Debug.Log("Array: " + array.Length);
        //Debug.Log("Context: " + contextMask.Length);

        //NativeArray<(RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>)> context = new NativeArray<(RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>)> { [0] =  }
        //foreach((RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>) entity in SystemAPI.Query<RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>>())
        for (int i = 0; i < entities.Length; i++)
        {
            ////foreach((RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>) other in SystemAPI.Query<RefRW<LocalTransform>, RefRW<AgentMovement>, RefRO<AgentSight>>())
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
        
        //When all are active, they seem to be drawn towards 0, 0, 0 .
        force += CohesionBehaviour.CalculateEntityMovement(transforms[index].ValueRO.Position, transforms, contextMask, 5);
        force += ObstacleAvoidanceBehaviour.CalculateEntityMovement(transforms[index].ValueRO, sightComponents[index].ValueRO, 1000, OARays);
        force += AlignmentBehaviour.CalculateEntityMovement(movementComponents[index].ValueRO, movementComponents, contextMask, 10);
        force += SeparationBehaviour.CalculateEntityMovement(transforms[index].ValueRO.Position, transforms, contextMask, 100);

        //Debug.Log(force);
        //Velocity is fucked up here somewhere...-
        force = force * deltaTime;
        float3 newVelocity = float3.zero;
        newVelocity = movementComponents[index].ValueRO.velocity + force;


        float squaredMaxSpeed = movementComponents[index].ValueRO.maxSpeed * movementComponents[index].ValueRO.maxSpeed;
        float squareMagnitudeNewVel = GetSquareMagnitude(newVelocity);

        //newVelocity = NormalizedFloat3(newVelocity) * movementComponents[index].ValueRO.maxSpeed;

        if (squareMagnitudeNewVel > squaredMaxSpeed && squareMagnitudeNewVel > GetSquareMagnitude(movementComponents[index].ValueRO.velocity))
            newVelocity = NormalizedFloat3(newVelocity) * (GetMagnitude(movementComponents[index].ValueRO.velocity) - (movementComponents[index].ValueRO.deceleration * deltaTime));

        //acceleration
        if (GetSquareMagnitude(newVelocity) < squaredMaxSpeed)
            newVelocity += NormalizedFloat3(newVelocity) * (movementComponents[index].ValueRO.acceleration * deltaTime);


        state.EntityManager.SetComponentData<AgentMovement>(entities[index], movementComponents[index].ValueRO.SetVelocity(newVelocity));
        //Debug.Log(movementComponents[index].ValueRO.velocity);


    }


    public void OnDestroy(ref SystemState state) 
    {
        return;
        Debug.Log("DESTROYEEDE");
        entities.Dispose();
        contextMask.Dispose();

        transforms.Dispose();
        movementComponents.Dispose();
        sightComponents.Dispose();

        OARays.Dispose();
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
