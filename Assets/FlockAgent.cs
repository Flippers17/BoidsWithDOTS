using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockAgent : MonoBehaviour
{
    [HideInInspector]
    public Transform thisTransform;
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward = Vector3.forward;

    [HideInInspector]
    public Vector3 velocity;
    [SerializeField]
    private float _maxSpeed = 5f;
    [SerializeField]
    private float _deceleration = 5f;
    [SerializeField]
    private float _acceleration = 10f;

    public float sightRadius = 2f;
    [SerializeField, Range(0f, 180f)]
    private float viewAngle = 180;
    [HideInInspector, SerializeField]
    public float viewAngleCos = 0f;

    public LayerMask avoidanceMask;

    [SerializeField]
    private bool _addAgentToMainFlock = true;
    //public List<SteeringBehaviourItems> steeringBehaviours = new List<SteeringBehaviourItems>();

    //private float _totalWeight = 1f;

    [SerializeField]
    public bool _debugAgent = false;
    private List<FlockAgent> neighbours = new List<FlockAgent>();


    private void OnEnable()
    {
        thisTransform = transform;
        thisTransform.rotation = Quaternion.Euler(Random.Range(0.0f, 180f), Random.Range(0.0f, 180f), Random.Range(0.0f, 180f));
        velocity = thisTransform.forward * _maxSpeed;
        position = thisTransform.position;
    }

    private void Start()
    {
        if (_addAgentToMainFlock)
            FlockManager.mainFlock.AddAgent(this);
    }

    private void OnValidate()
    {
        //if(steeringBehaviours.Count > 0)
        //{
        //    _totalWeight = 0;
        //    for(int i = 0; i < steeringBehaviours.Count; i++)
        //    {
        //        _totalWeight += steeringBehaviours[i].weight;
        //    }
        //}

        viewAngleCos = Mathf.Cos(viewAngle);
    }


    private void Update()
    {
        thisTransform.position += velocity * Time.deltaTime;
        position = thisTransform.position;
        thisTransform.forward = velocity;
        forward = thisTransform.forward;
    }

    public void CalculateMovement(List<FlockAgent> context, List<SteeringBehaviourItems> behaviours, int behaviourCount, float weightMultiplier)
    {
        float deltaTime = Time.deltaTime;
        Vector3 force = Vector3.zero;

        if (_debugAgent)
        {
            neighbours.Clear();
            for (int i = 0; i < context.Count; i++)
            {
                neighbours.Add(context[i]);
            }
        }
        

        for(int i = 0; i < behaviourCount; i++)
        {
            force += behaviours[i].behaviour.CalculateMovement(this, context, behaviours[i].forceMultiplier) * (behaviours[i].weight * weightMultiplier);
        }

        force = force * deltaTime;
        Vector3 newVelocity = velocity + force;

        if(_debugAgent)
        {
            //Debug.DrawRay(position, force, Color.cyan, .02f);
            //Debug.DrawRay(position, newVelocity, Color.blue, .02f);
        }

        float squaredMaxSpeed = _maxSpeed * _maxSpeed;

        if(newVelocity.sqrMagnitude > squaredMaxSpeed && newVelocity.sqrMagnitude > velocity.sqrMagnitude)
            newVelocity = newVelocity.normalized * (velocity.magnitude - (_deceleration * Time.deltaTime));

        velocity = newVelocity;

        //acceleration
        if (velocity.sqrMagnitude < squaredMaxSpeed)
            velocity += velocity.normalized * (_acceleration * deltaTime);
    }

    public void SetMaxSpeed(float speed)
    {
        _maxSpeed = speed;
    }
    
    private void OnDrawGizmos()
    {
        if(!_debugAgent)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, sightRadius);

        Gizmos.color = Color.red;
        for(int i = 0; i < neighbours.Count; i++)
        {
            Gizmos.DrawWireSphere(neighbours[i].position, .5f);
        }
    }
}


[System.Serializable]
public class SteeringBehaviourItems
{
    public SteeringBehaviour behaviour;
    public float weight = 1f;
    public float forceMultiplier = 1f;
}