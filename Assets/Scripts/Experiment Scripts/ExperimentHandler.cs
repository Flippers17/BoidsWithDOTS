using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using UnityEngine;

public class ExperimentHandler : MonoBehaviour
{
    [SerializeField]
    private int minAgentCount = 100;
    [SerializeField]
    private int maxAgentCount = 10000;
    [SerializeField]
    private int agentCountInterval = 100;

    [Space(20), SerializeField, Min(1)]
    private float _simulationDuration = 60f;
    [SerializeField]
    private int _cancelDurationFactor = 4;

    [SerializeField]
    private FlockManager _normalOctree;
    [SerializeField]
    private AgentSpawner _normalSpawner;

    [SerializeField]
    private int _randomSeed;
    //[Space(20), SerializeField]
    //private List<GameObject> _detectionPrefabs = new List<GameObject>();

    //[SerializeField]
    //private ExperimentPort _experimentPort;

    private float fixedStartTime = 0;
    private float realTimeStart = 0;


    private void Start()
    {

        
        StartCoroutine(Run());
    }


    public IEnumerator Run()
    {
        List<int> circleCounts = GetCircleCounts();

        yield return new WaitForSeconds(2);
        //World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SystemHandlerSystem>().DisableSystem(0);
        //yield break;
        //List<GameObject> detections = new List<GameObject>();

        //for(int i = _detectionPrefabs.Count - 1; i >= 0; i--)
        //{
        //    GameObject current = Instantiate(_detectionPrefabs[i]);
        //    current.SetActive(false);

        //    detections.Add(current);
        //}
        var SystemHandler = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SystemHandlerSystem>();

        HashSet<int> removedAlgorithms = new HashSet<int>();

        //bool isCanceled = false;
        foreach(int circleCount in circleCounts)
        {
            RecordingManager.AddResults(circleCount);
            

            for(int i = 0; i < 4; i++)
            {
                if (removedAlgorithms.Contains(i))
                {
                    RecordingManager.AddResults(0);
                    Debug.Log("Algorithm " + i + " was removed");
                    continue;
                }
                Random.InitState(_randomSeed);
                //Sets amount to spawn and also spawns in new ones
                SystemHandler.SpawnEntities(circleCount);

                SystemHandler.EnableSystem(i);

                //_experimentPort.BeginSimulation(circleCount);
                fixedStartTime = Time.fixedTime;
                realTimeStart = Time.unscaledTime;

                while(Time.fixedTime < fixedStartTime + _simulationDuration)
                {
                    yield return null;
                }

                //_experimentPort.EndSimulation();

                SystemHandler.DisableSystem(i);
                SystemHandler.ResetSystem(i);
                if (Time.unscaledTime > (realTimeStart + (_simulationDuration * _cancelDurationFactor)))
                {
                    Debug.Log("Removing algorithm " + i);
                    removedAlgorithms.Add(i);
                }

                RecordingManager.AddCurrentResults();
                SystemHandler.DestroyEntities();
            }

            //SystemHandler.DestroyEntities();


            if (removedAlgorithms.Contains(4))
            {
                RecordingManager.AddResults(0);
                //continue;
            }
            else
            {
                _normalSpawner.SpawnAgents(circleCount);

                _normalOctree.runSimulation = true;

                //_experimentPort.BeginSimulation(circleCount);
                fixedStartTime = Time.fixedTime;
                realTimeStart = Time.unscaledTime;

                while (Time.fixedTime < fixedStartTime + _simulationDuration)
                {
                    yield return null;
                }

                //_experimentPort.EndSimulation();

                _normalOctree.runSimulation = false;
                _normalOctree.ResetSystem();
                if (Time.unscaledTime > (realTimeStart + (_simulationDuration * _cancelDurationFactor)))
                {
                    removedAlgorithms.Add(4);
                }

                RecordingManager.AddCurrentResults();
                _normalSpawner.DespawnAgents();
            }
            

            RecordingManager.AddResults(1000 / 120f);
            RecordingManager.AddResults(1000 / 60f);
            RecordingManager.AddResults(1000 / 30f);
            RecordingManager.AddResults(1000 / 15f);
            RecordingManager.WriteResultsToFile();
            
            
            RecordingManager.ResetResults();

        }

        Quit();
    }


    public List<int> GetCircleCounts()
    {
        List<int> results = new List<int>();

        for(int i = minAgentCount; i <= maxAgentCount; i += agentCountInterval)
        {
            results.Add(i);
        }

        return results;
    }


    public void Quit()
    {
        Application.Quit();
    }
}
