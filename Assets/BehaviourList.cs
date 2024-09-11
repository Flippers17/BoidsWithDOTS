using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BehaviourList : ScriptableObject
{
    public float maxSpeed = 15;
    public List<SteeringBehaviourItems> items;
}
