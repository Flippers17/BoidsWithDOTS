using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;

public class RecordingManager : MonoBehaviour
{
    //[SerializeField]
    //private ExperimentPort _experimentPort;
    //[SerializeField]
    //private SimulationPort _simulationPort;
    [SerializeField]
    private string _resultsFilename;
    private static string fileName;

    private static string resultsString = "";

    private static StreamWriter streamWriter;

    private static double currentSampleStart = 0;
    private static List<TimeSample> currentSamples = new List<TimeSample>();

    private void OnEnable()
    {
        fileName = _resultsFilename;
        streamWriter = new StreamWriter(fileName);
        resultsString = $"Agent Count; Naive ECS; Octree ECS; Octree ECS+Jobs; Octree ECS+Jobs+Burst; Regular Octree; 120 FPS; 60 FPS; 30 FPS; 15 FPS";
        WriteResultsToFile();
        ResetResults();
    }

    private void OnDisable()
    {
        streamWriter.Close();
    }


    public static void WriteResultsToFile()
    {
        streamWriter.Write(resultsString);
        streamWriter.WriteLine();
    }

    public static void ResetResults()
    {
        resultsString = "";
    }

    public static void AddCurrentResults()
    {
        resultsString += $"{GetRecorderFrameAverage():F7}; ";
        currentSamples.Clear();
    }
    
    public static void AddResults(float value)
    {
        resultsString += $"{value:F7}; ";
    }


    public static void StartSample()
    {
        currentSampleStart = Time.realtimeSinceStartupAsDouble;
    }

    public static void EndSample()
    {
        double stopTime = Time.realtimeSinceStartupAsDouble;
        currentSamples.Add(new TimeSample(stopTime - currentSampleStart));
    }


    static double GetRecorderFrameAverage()
    {
        var samplesCount = currentSamples.Count;
        if (samplesCount == 0)
            return 4;

        double r = 0;
            
            
        for (int i = 0; i < samplesCount; ++i)
        {
            r += currentSamples[i].value;
            if (r == 0)
                return 5;
        }
        r /= samplesCount;
            

        return r * 1000;
    }
}
