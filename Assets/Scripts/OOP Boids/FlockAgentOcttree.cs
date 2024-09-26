using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class FlockAgentOcttree : MonoBehaviour
{
    public static FlockAgentOcttree instance;

    [SerializeField, HideInInspector]
    private Bounds rootBounds = new Bounds();

    [SerializeField]
    private Vector3 _rootSize = new Vector3(5, 5, 5);

    private FlockAgentOcttreeNode _rootNode;

    [SerializeField]
    private int capacity = 4;
    
    [SerializeField]
    private int maxLevels = 6;
    [SerializeField]
    private bool _createChildNodesFromStart = true;

    private List<Vector3> neighbouringNodesTemp = new List<Vector3>() { Vector3.zero, Vector3.zero , Vector3.zero , Vector3.zero , Vector3.zero , Vector3.zero };

    [Space(15), SerializeField] private bool _showGizmos = false;

    private void OnEnable()
    {
        instance = this;

        rootBounds.center = transform.position;
        rootBounds.size = _rootSize;
        _rootNode = new FlockAgentOcttreeNode(rootBounds, capacity, maxLevels, _createChildNodesFromStart);
    }

    public void CreateNewTree()
    {
        rootBounds.center = transform.position;
        _rootNode.ClearTree();
        _rootNode.SetValues(rootBounds, capacity, maxLevels);
    }


    public void AddAgent(FlockAgent agent)
    {
        _rootNode.AddAgent(agent, agent.position);
    }

    public void GetAgentsInNode(FlockAgent agent, ref List<FlockAgent> agents)
    {
        
        _rootNode.GetNeighbouringAgents(agent, agent.position, ref agents, ref neighbouringNodesTemp);

        for (int i = 0; i < neighbouringNodesTemp.Count; i++)
        {
            _rootNode.GetNeighbouringAgents(agent, neighbouringNodesTemp[i], ref agents);
        }
    }

    private void OnDrawGizmos()
    {
        if(!_showGizmos)
            return;
        
        Gizmos.color = Color.green;
        if (_rootNode == null)
        {
            Gizmos.DrawWireCube(transform.position, _rootSize);
            return;
        }

        List<Bounds> allBounds = new List<Bounds>();
        _rootNode.GetWholeTree(ref allBounds);
        
        for(int i = 0; i < allBounds.Count; ++i)
        {
            Gizmos.DrawWireCube(allBounds[i].center, allBounds[i].size);
        }
    }
}


public class FlockAgentOcttreeNode 
{ 
    public bool isDivided = false;

    public Bounds bounds;

    private Vector3 boundsPosition;
    private Vector3 halfSize;
    private Vector3 quarterSize;

    private Vector3 topNorthEastPoint;
    private Vector3 bottomSouthWestPoint;

    public int capacity = 4;

    public FlockAgentOcttreeNode topNorthEast = null;
    public FlockAgentOcttreeNode topNorthWest = null;
    public FlockAgentOcttreeNode topSouthEast = null;
    public FlockAgentOcttreeNode topSouthWest = null;
    public FlockAgentOcttreeNode bottomNorthEast = null;
    public FlockAgentOcttreeNode bottomNorthWest = null;
    public FlockAgentOcttreeNode bottomSouthEast = null;
    public FlockAgentOcttreeNode bottomSouthWest = null;

    public List<FlockAgent> agents = new List<FlockAgent>();
    private int currentCount = 0;

    private int levelsLeft = 0;

    private bool hasCreatedChildNodes = false;


    public FlockAgentOcttreeNode(Bounds Bounds, int capacity, int levelsLeft, bool createChildNodes)
    {
        SetValues(Bounds, capacity, levelsLeft);

        if (levelsLeft == 0)
            return;

        if (!createChildNodes)
            return;

        topNorthEast = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        topNorthWest = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        topSouthEast = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        topSouthWest = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        bottomNorthEast = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        bottomNorthWest = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        bottomSouthEast = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        bottomSouthWest = new FlockAgentOcttreeNode(bounds, capacity, levelsLeft - 1, createChildNodes);
        hasCreatedChildNodes = true;
    }

    public void SetValues(Bounds Bounds, int capacity, int levelsLeft)
    {
        this.bounds = Bounds;
        boundsPosition = Bounds.center;
        halfSize = new Vector3(bounds.size.x / 2, bounds.size.y / 2, bounds.size.z / 2);
        quarterSize = new Vector3(halfSize.x / 2, halfSize.z / 2, halfSize.z / 2);
        this.capacity = capacity;
        this.levelsLeft = levelsLeft;

        topNorthEastPoint = boundsPosition + halfSize;
        bottomSouthWestPoint = boundsPosition - halfSize;
    }


    public virtual void AddAgent(FlockAgent agent, Vector3 position) 
    {
        if(!ContainsPoint(position)) 
            return;

        if(isDivided)
        {
            topNorthEast.AddAgent(agent, position);
            topNorthWest.AddAgent(agent, position);
            topSouthEast.AddAgent(agent, position);
            topSouthWest.AddAgent(agent, position);
            bottomNorthEast.AddAgent(agent, position);
            bottomNorthWest.AddAgent(agent, position);
            bottomSouthEast.AddAgent(agent, position);
            bottomSouthWest.AddAgent(agent, position);

            return;
        }

        agents.Add(agent);
        currentCount++;

        if(currentCount > capacity && levelsLeft > 0)
        {
            Divide();
        }
    }


    //Gives neighbouring agents in the same node and fills a list with neighbouring nodes that can also be searche later
    public void GetNeighbouringAgents(FlockAgent agent, Vector3 position, ref List<FlockAgent> agentsInNodes, ref List<Vector3> neighbouringNodes)
    {
        if (!ContainsPoint(position))
        {
            return;
        }

        if (isDivided)
        {
            topNorthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            topNorthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            topSouthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            topSouthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            bottomNorthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            bottomNorthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            bottomSouthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            bottomSouthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes, ref neighbouringNodes);
            return;
        }

        neighbouringNodes[0] = (boundsPosition + new Vector3(halfSize.x, 0, 0));
        neighbouringNodes[1] = (boundsPosition + new Vector3(-halfSize.x, 0, 0));
        neighbouringNodes[2] = (boundsPosition + new Vector3(0, halfSize.y, 0));
        neighbouringNodes[3] = (boundsPosition + new Vector3(0, -halfSize.y, 0));
        neighbouringNodes[4] = (boundsPosition + new Vector3(0, 0, halfSize.z));
        neighbouringNodes[5] = (boundsPosition + new Vector3(0, 0, -halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(halfSize.x, halfSize.y, halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
        //neighbouringNodes.Add(boundsPosition + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));

        Vector3 currentVector;
        float squaredRadius = agent.sightRadius * agent.sightRadius;
        Vector3 agentForward = agent.forward;
        Vector3 agentPos = agent.position;
        float agentViewAngleCos = agent.viewAngleCos;

        for (int i = 0; i < currentCount; i++)
        {
            currentVector = (agentPos - agents[i].position);
            if (currentVector.sqrMagnitude <= squaredRadius && Vector3.Dot(agentForward, currentVector) > (agentViewAngleCos * currentVector.magnitude) && agents[i] != agent)
                agentsInNodes.Add(agents[i]);
        }

        //Vector3 currentVector;
        //float squaredRadius = agent.sightRadius * agent.sightRadius;

        //for (int i = 0; i < currentCount; i++)
        //{
        //    currentVector = (agent.position - agents[i].position);
        //    if (currentVector.sqrMagnitude <= squaredRadius && Vector3.Dot(agent.forward, currentVector.normalized) > (agent.viewAngleCos) && agents[i] != agent)
        //        agentsInNodes.Add(agents[i]);
        //}
    }

    //Is called for remaining neighbouring nodes
    public void GetNeighbouringAgents(FlockAgent agent, Vector3 position, ref List<FlockAgent> agentsInNodes)
    {
        if (!ContainsPoint(position))
            return;

        if (isDivided)
        {
            topNorthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            topNorthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            topSouthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            topSouthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            bottomNorthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            bottomNorthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            bottomSouthEast.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            bottomSouthWest.GetNeighbouringAgents(agent, position, ref agentsInNodes);
            return;
        }

        Vector3 currentVector;
        float squaredRadius = agent.sightRadius * agent.sightRadius;
        Vector3 agentForward = agent.forward;
        Vector3 agentPos = agent.position;
        float agentViewAngleCos = agent.viewAngleCos;

        for (int i = 0; i < currentCount; i++)
        {
            currentVector = (agentPos - agents[i].position);
            if (currentVector.sqrMagnitude <= squaredRadius && Vector3.Dot(agentForward, currentVector) > (agentViewAngleCos * currentVector.magnitude) && agents[i] != agent)
                agentsInNodes.Add(agents[i]);
        }

        //Vector3 currentVector;
        //float squaredRadius = agent.sightRadius * agent.sightRadius;

        //for (int i = 0; i < currentCount; i++)
        //{
        //    currentVector = (agent.position - agents[i].position);
        //    if (currentVector.sqrMagnitude <= squaredRadius && Vector3.Dot(agent.forward, currentVector.normalized) > (agent.viewAngleCos) && agents[i] != agent)
        //        agentsInNodes.Add(agents[i]);
        //}
    }


    public void Divide()
    {
        if (isDivided)
            return;

        if(hasCreatedChildNodes)
        {
            Bounds current = new Bounds(boundsPosition + new Vector3(quarterSize.x, quarterSize.y, quarterSize.z), halfSize);
            topNorthEast.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(-quarterSize.x, quarterSize.y, quarterSize.z);
            topNorthWest.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(quarterSize.x, quarterSize.y, -quarterSize.z);
            topSouthEast.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(-quarterSize.x, quarterSize.y, -quarterSize.z);
            topSouthWest.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(quarterSize.x, -quarterSize.y, quarterSize.z);
            bottomNorthEast.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(-quarterSize.x, -quarterSize.y, quarterSize.z);
            bottomNorthWest.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(quarterSize.x, -quarterSize.y, -quarterSize.z);
            bottomSouthEast.SetValues(current, capacity, levelsLeft - 1);

            current.center = boundsPosition + new Vector3(-quarterSize.x, -quarterSize.y, -quarterSize.z);
            bottomSouthWest.SetValues(current, capacity, levelsLeft - 1);
        }
        else
        {
            Bounds current = new Bounds(boundsPosition + new Vector3(quarterSize.x, quarterSize.y, quarterSize.z), halfSize);
            topNorthEast = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(-quarterSize.x, quarterSize.y, quarterSize.z);
            topNorthWest = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(quarterSize.x, quarterSize.y, -quarterSize.z);
            topSouthEast = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(-quarterSize.x, quarterSize.y, -quarterSize.z);
            topSouthWest = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(quarterSize.x, -quarterSize.y, quarterSize.z);
            bottomNorthEast = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(-quarterSize.x, -quarterSize.y, quarterSize.z);
            bottomNorthWest = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(quarterSize.x, -quarterSize.y, -quarterSize.z);
            bottomSouthEast = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);

            current.center = boundsPosition + new Vector3(-quarterSize.x, -quarterSize.y, -quarterSize.z);
            bottomSouthWest = new FlockAgentOcttreeNode(current, capacity, levelsLeft - 1, false);
        }
        

        isDivided = true;
        hasCreatedChildNodes = true;

        for(int i = agents.Count - 1; i >= 0; i--)
        {
            FlockAgent agent = agents[i];
            agents.RemoveAt(i);
            currentCount--;
            AddAgent(agent, agent.position);
        }
    }


    //public bool ContainsSphere(Vector3 center, float radius)
    //{
    //    if (radius == 0)
    //        return bounds.Contains(center);
    //
    //    if()
    //}

    public void GetWholeTree(ref List<Bounds> allBounds)
    {
        if (isDivided)
        {
            topNorthEast.GetWholeTree(ref allBounds);
            topNorthWest.GetWholeTree(ref allBounds);
            topSouthEast.GetWholeTree(ref allBounds);
            topSouthWest.GetWholeTree(ref allBounds);
            bottomNorthEast.GetWholeTree(ref allBounds);
            bottomNorthWest.GetWholeTree(ref allBounds);
            bottomSouthEast.GetWholeTree(ref allBounds);
            bottomSouthWest.GetWholeTree(ref allBounds);
            return;
        }

        allBounds.Add(bounds);
    }

    public bool ContainsPoint(Vector3 point)
    {
        if(point.x > topNorthEastPoint.x || point.x < bottomSouthWestPoint.x)
            return false;

        if (point.y > topNorthEastPoint.y || point.y < bottomSouthWestPoint.y)
            return false;

        if (point.z > topNorthEastPoint.z || point.z < bottomSouthWestPoint.z)
            return false;

        return true;
    }


    public void ClearTree()
    {
        if (isDivided)
        {
            topNorthEast.ClearTree();
            topNorthWest.ClearTree();
            topSouthEast.ClearTree();
            topSouthWest.ClearTree();
            bottomNorthEast.ClearTree();
            bottomNorthWest.ClearTree();
            bottomSouthEast.ClearTree();
            bottomSouthWest.ClearTree();
        }
        else
            agents.Clear();

        isDivided = false;
        currentCount = 0;
    }
}
