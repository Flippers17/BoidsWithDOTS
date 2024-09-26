using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;


[CreateAssetMenu]
public class ObstacleAvoidanceBehaviour : SteeringBehaviour
{
    [SerializeField]
    private int numberOfRays = 1000;
    
    [SerializeField]
    private float turnFactor = 1f;

    [SerializeField]
    private float avoidanceForce = 10f;

    [NonSerialized]
    private List<Vector3> avoidanceRaysDirections = new List<Vector3>();

    [NonSerialized]
    private bool hasGeneratedRays = false;


    public override Vector3 CalculateMovement(FlockAgent agentToMove, List<FlockAgent> context, float forceMultiplier)
    {
        if(!hasGeneratedRays)
            GenerateRays();

        Vector3 bestDir = agentToMove.forward;
        float furthestUnobstructedDist = 0f;
        RaycastHit hit;


        Vector3 agentPos = agentToMove.position;
        Vector3 currentDir = Vector3.zero;


        for(int i = 0; i < numberOfRays; i++) 
        {
            currentDir = avoidanceRaysDirections[i];
            //Debug.Log(currentDir);
            currentDir = agentToMove.thisTransform.TransformDirection(currentDir);

            if (Physics.SphereCast(agentPos, .5f, currentDir, out hit, agentToMove.sightRadius, agentToMove.avoidanceMask))
            {
                if(agentToMove._debugAgent)
                    Debug.DrawRay(agentPos, currentDir * agentToMove.sightRadius, Color.red, .02f);
    
                if (hit.distance > furthestUnobstructedDist)
                {
                    bestDir = currentDir;
                    furthestUnobstructedDist = hit.distance;
                    
                }
            }
            else
            {
                if(i == 0)
                    return Vector3.zero;

                if (agentToMove._debugAgent)
                {
                    Debug.DrawRay(agentToMove.position, currentDir * agentToMove.sightRadius, Color.green, .02f);
                    //Debug.DrawRay(agentToMove.position, (currentDir - agentToMove.velocity.normalized).normalized * (avoidanceForce * forceMultiplier), Color.yellow, .1f);

                }
                return currentDir * (avoidanceForce * forceMultiplier);
            }
        }

        return bestDir * (avoidanceForce * forceMultiplier);

    }

    public static float3 CalculateEntityMovement(LocalTransform agentTransform, AgentSight agentSight, float forceMultiplier, ObstacleAvoidanceRays rays)
    {

        float3 bestDir = agentTransform.Forward();
        float furthestUnobstructedDist = 0f;
        RaycastHit hit;


        float3 agentPos = agentTransform.Position;
        Quaternion agentRot = agentTransform.Rotation;
        float3 currentDir = bestDir;
        //Debug.Log(agentSight.sightRadius);

        for (int i = 0; i < rays.numberOfRays; i++)
        {
            //Debug.Log("Not transformed: " + currentDir);
            currentDir = rays.avoidanceRaysDirections[i];
            //Debug.Log(currentDir);
            currentDir = TransformDirection(currentDir, agentPos, agentRot);
            //Debug.Log("Transformed: " + currentDir);

            if (Physics.SphereCast(agentPos, .5f, currentDir, out hit, agentSight.sightRadius, agentSight.obstacleMask))
            {
                //if (agentToMove._debugAgent)
                //    Debug.DrawRay(agentPos, currentDir * agentToMove.sightRadius, Color.red, .02f);

                if (hit.distance > furthestUnobstructedDist)
                {
                    bestDir = currentDir;
                    furthestUnobstructedDist = hit.distance;

                }
            }
            else
            {
                if (i == 0)
                    return float3.zero;

                //if (agentToMove._debugAgent)
                //{
                //    Debug.DrawRay(agentToMove.position, currentDir * agentToMove.sightRadius, Color.green, .02f);
                //    //Debug.DrawRay(agentToMove.position, (currentDir - agentToMove.velocity.normalized).normalized * (avoidanceForce * forceMultiplier), Color.yellow, .1f);

                //}
                return currentDir * forceMultiplier;
            }
        }

        return bestDir * forceMultiplier;

    }


    private static float3 TransformDirection(float3 dir, float3 pos, quaternion rot)
    {
        Matrix4x4 m = Matrix4x4.identity;

        m.SetTRS(pos, rot, Vector3.one);
        
        return m.MultiplyVector(dir);
    }


    private void GenerateRays()
    {
        for(int i = 0; i < numberOfRays; i++)
        {
            //var k = i + .5f;
            var k = i / (numberOfRays - 1f);

            //var phi = Mathf.Acos(1f - 2f * k / numberOfRays);
            //var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;
            var phi = Mathf.Acos(1f - 2f * k);
            var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * i;

            float x = Mathf.Cos(theta) * Mathf.Sin(phi);
            float y = Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = Mathf.Cos(phi);

            avoidanceRaysDirections.Add(new Vector3(x, y, z));
            //Debug.Log(avoidanceRaysDirections.Count);
            //Debug.DrawRay(Vector3.zero, new Vector3(x, y, z), Color.green * (1 - (float)i/numberOfRays), 50f);
            //Debug.Log(new Vector3(x, y, z));

            /*
            //float t = i/ (numberOfRays - 1f);
            float t = i/ (numberOfRays);
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = 2 * Mathf.PI * turnFactor * i;
            
            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Cos(inclination) * Mathf.Cos(azimuth);
            float z = Mathf.Sin(inclination);

            avoidanceRaysDirections.Add(new Vector3(x, y, z));*/
        }

        hasGeneratedRays = true;
    }
}


public struct ObstacleAvoidanceRays
{
    public int numberOfRays;
    public NativeArray<float3> avoidanceRaysDirections;

    public ObstacleAvoidanceRays(int NumberOfRays)
    {
        numberOfRays = NumberOfRays;
        avoidanceRaysDirections = new NativeArray<float3>(NumberOfRays, Allocator.Persistent);

        for(int i = 0; i < numberOfRays; i++)
        {
            var k = i / (numberOfRays - 1f);

            //var phi = Mathf.Acos(1f - 2f * k / numberOfRays);
            //var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;
            var phi = Mathf.Acos(1f - 2f * k);
            var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * i;

            float x = Mathf.Cos(theta) * Mathf.Sin(phi);
            float y = Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = Mathf.Cos(phi);

            avoidanceRaysDirections[i] = new float3(x, y, z);
        }
        
    }

    public void Dispose()
    {
        avoidanceRaysDirections.Dispose();
    }
}
