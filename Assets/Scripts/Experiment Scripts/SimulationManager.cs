using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class SimulationManager : MonoBehaviour
{
    //[SerializeField]
    //private ExperimentPort _experimentPort;
    //[SerializeField]
    //private SimulationPort _simulationPort;
    //[SerializeField]
    //private SphereSpawnerPort _spawnPort;
    //[SerializeField]
    //private EntitySet _entitySet;

    private bool _runningSimulation = false;
    [SerializeField]
    private bool runOnAwake = false;

    [SerializeField]
    private int _testNumberOfCircles = 100;


    private void Start()
    {
        if (runOnAwake)
            BeginSimulation(_testNumberOfCircles);
    }

    private void OnEnable()
    {
        //_experimentPort.OnBeginSimulation += BeginSimulation;
        //_experimentPort.OnEndSimulation += EndSimulation;
    }

    private void OnDisable()
    {
        //_experimentPort.OnBeginSimulation -= BeginSimulation;
        //_experimentPort.OnEndSimulation -= EndSimulation;
    }


    private void BeginSimulation(int circleCount)
    {
        //_spawnPort.SpawnSpheres(circleCount);
        //_runningSimulation = true;
    }


    /*private void EndSimulation()
    {
        for(int i = _entitySet.Count - 1; i >= 0; i--)
        {
            Destroy(_entitySet.Entities[i].gameObject);
        }
        _entitySet.Entities.Clear();
    }*/

    private void FixedUpdate()
    {
        if (!_runningSimulation)
            return;

        Profiler.BeginSample("Simulation", this);
        //_simulationPort.SignalBeginFixedUpdate();
        //_simulationPort.SignalIntegration();
        RecordingManager.StartSample();
        //_simulationPort.SignalDetection();
        //_simulationPort.SignalResolution();
        RecordingManager.EndSample();
        //_simulationPort.SignalEndFixedUpdate();
        Profiler.EndSample();
    }
}
