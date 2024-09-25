using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public struct EntityOctree
{
    private int _maxLevels;
    private int _nodeCapacity;

    //This should maybe be a HashMap
    NativeArray<EntityOctreeNode> _nodes;
    NativeArray<int> _childNodesIndex;
    //Stores the objects. The key is for the node ID and the values are the entities inside that node
    NativeParallelMultiHashMap<int, (int, float3)> _objects;

    private Bounds _bounds;
    private float3 _halfSize;
    private float3 _quarterSize;

    //How much of the _nodes array that has been used up
    private int _nodesSize;

    public EntityOctree(int  maxLevels, int nodeCapacity, Bounds bounds)
    {
        _maxLevels = Mathf.Max(maxLevels, 1);
        _nodeCapacity = nodeCapacity;
        _bounds = bounds;
        _halfSize = _bounds.size / 2;
        _quarterSize = _halfSize / 2;

        int arraySize = 1;
        for(int i = 1; i <= _maxLevels; i++)
        {
            arraySize += (int)Mathf.Pow(8, i);
        }

        _nodes = new NativeArray<EntityOctreeNode>(arraySize, Allocator.Persistent);
        _childNodesIndex = new NativeArray<int>(arraySize, Allocator.Persistent);
        for(int i = 0; i < 8; i++)
        {
            _childNodesIndex[i] = -1;
        }

        _nodes[0] = new EntityOctreeNode(new Bounds((float3)_bounds.center - _quarterSize, _halfSize), 1);
        _nodes[1] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, -_quarterSize.y, -_quarterSize.z), _halfSize), 1);
        _nodes[2] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(-_quarterSize.x, _quarterSize.y, -_quarterSize.z), _halfSize), 1);
        _nodes[3] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, _quarterSize.y, -_quarterSize.z), _halfSize), 1);
        _nodes[4] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(-_quarterSize.x, -_quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodes[5] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, -_quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodes[6] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(-_quarterSize.x, _quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodes[7] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, _quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodesSize = 8;
        

        _objects = new NativeParallelMultiHashMap<int, (int, float3)>(16, Allocator.Persistent);
    }

    //Starts the process of inserting a point into the tree
    public void InsertPointToTree(int e, float3 point)
    {
        TryInsertInNodes(0, e, point, 1);
    }

    //Checks all childnodes of a parent node and recursively goes down levels if the node containg the point is also divided. If not, it is inserted in that node.
    private void TryInsertInNodes(int startIndex, int e, float3 point, int currentLevel)
    {
        for(int i =  startIndex; i < startIndex + 8; i++)
        {
            if (_nodes[i].ContainsPoint(point))
            {
                if (_childNodesIndex[i] == -1)
                    InsertInNode(e, point, i, currentLevel);
                else if(currentLevel < _maxLevels)
                    TryInsertInNodes(_childNodesIndex[i], e, point, currentLevel + 1);

                return;
            }
        }
    }


    private void InsertInNode(int e, float3 point, int index, int currentLevel)
    {
        _objects.Add(index, (e, point));

        //Divide if we have not reached max level and we reached the nodes capacity
        if (_objects.CountValuesForKey(index) > _nodeCapacity && currentLevel < _maxLevels)
        {
            SubdivideNode(index);
        }
    }



    private void SubdivideNode(int nodeIndex)
    {
        int childStartIndex = _nodesSize;
        _childNodesIndex[nodeIndex] = childStartIndex;

        EntityOctreeNode currentNode = _nodes[nodeIndex];
        
        //Creates child nodes
        _nodes[childStartIndex] = new EntityOctreeNode(new Bounds((float3)currentNode._bounds.center - currentNode._quarterSize, currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 1] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(currentNode._quarterSize.x, -currentNode._quarterSize.y, -currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 2] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(-currentNode._quarterSize.x, currentNode._quarterSize.y, -currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 3] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(currentNode._quarterSize.x, currentNode._quarterSize.y, -currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 4] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(-currentNode._quarterSize.x, -currentNode._quarterSize.y, currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 5] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(currentNode._quarterSize.x, -currentNode._quarterSize.y, currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 6] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(-currentNode._quarterSize.x, currentNode._quarterSize.y, currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);
        _nodes[childStartIndex + 7] = new EntityOctreeNode(new Bounds(currentNode._bounds.center + new Vector3(currentNode._quarterSize.x, currentNode._quarterSize.y, currentNode._quarterSize.z), currentNode._halfSize), currentNode.level + 1);

        for(int i = childStartIndex; i < childStartIndex + 8; i++)
        {
            _childNodesIndex[i] = -1;
        }
        _nodesSize += 8;
        //Places every object in the child nodes
        foreach (var entity in _objects.GetValuesForKey(nodeIndex))
        {
            TryInsertInNodes(childStartIndex, entity.Item1, entity.Item2, currentNode.level + 1);
        }

        _objects.Remove(nodeIndex);

        
    }


    public void FindNeighbouringAgents(int entityID, float3 point, ref NativeList<int> neighbours)
    {
        int nodeIndex = FindNeighbouringAgentsFromTop(entityID, point, ref neighbours);

        if(nodeIndex == -1)
        {
            //Debug.Log(point + "Does not exist in tree");
            return;
        }

        EntityOctreeNode parent = _nodes[nodeIndex];
        float3 parentCenter = parent._bounds.center;
        float3 parentHalfSize = parent._halfSize;

        float3 currentPos = parentCenter + new float3(parentHalfSize.x + .1f, 0, 0);
        FindNeighbouringAgentsFromTop(entityID, currentPos, ref neighbours);

        currentPos = parentCenter + new float3(-(parentHalfSize.x + .1f), 0, 0);
        FindNeighbouringAgentsFromTop(entityID, currentPos, ref neighbours);

        currentPos = parentCenter + new float3(0, (parentHalfSize.y + .1f), 0);
        FindNeighbouringAgentsFromTop(entityID, currentPos, ref neighbours);

        currentPos = parentCenter + new float3(0, -(parentHalfSize.y + .1f), 0);
        FindNeighbouringAgentsFromTop(entityID, currentPos, ref neighbours);

        currentPos = parentCenter + new float3(0, 0, parentHalfSize.z + .1f);
        FindNeighbouringAgentsFromTop(entityID, currentPos, ref neighbours);

        currentPos = parentCenter + new float3(0, 0, -(parentHalfSize.z + .1f));
        FindNeighbouringAgentsFromTop(entityID, currentPos, ref neighbours);
    }

    private int FindNeighbouringAgentsFromTop(int entityIndex, float3 point, ref NativeList<int> neighbours)
    {
        int nodeIndex = -1;

        for (int i = 0; i < 8; i++)
        {
            nodeIndex = FindNeighbouringAgentsInNode(i, entityIndex, point, ref neighbours);
            if (nodeIndex != -1)
                return nodeIndex;
        }

        return -1;
    }

    private int FindNeighbouringAgentsInNode(int index, int entityIndex, float3 point, ref NativeList<int> neighbours)
    {
        if (!_nodes[index].ContainsPoint(point))
            return -1;

        int childredIndexStart = _childNodesIndex[index];
        if (childredIndexStart != -1)
        {
            for(int i = childredIndexStart; i < childredIndexStart + 8; i++)
            {
                if (FindNeighbouringAgentsInNode(i, entityIndex, point, ref neighbours) != -1)
                    return i;
            }
        }

        foreach ((int, float3) entity in _objects.GetValuesForKey(index))
        {
            if(entity.Item1 != entityIndex)
                neighbours.Add(entity.Item1);
        }

        return index;
    }


    public NativeArray<EntityOctreeNode> GetNodes()
    {
        return _nodes;
    }


    public void ClearTree()
    {
        for (int i = 0; i < 8; i++)
        {
            _childNodesIndex[i] = -1;
        }

        _nodes[0] = new EntityOctreeNode(new Bounds((float3)_bounds.center - _quarterSize, _halfSize), 1);
        _nodes[1] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, -_quarterSize.y, -_quarterSize.z), _halfSize), 1);
        _nodes[2] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(-_quarterSize.x, _quarterSize.y, -_quarterSize.z), _halfSize), 1);
        _nodes[3] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, _quarterSize.y, -_quarterSize.z), _halfSize), 1);
        _nodes[4] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(-_quarterSize.x, -_quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodes[5] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, -_quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodes[6] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(-_quarterSize.x, _quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodes[7] = new EntityOctreeNode(new Bounds(_bounds.center + new Vector3(_quarterSize.x, _quarterSize.y, _quarterSize.z), _halfSize), 1);
        _nodesSize = 8;


        _objects = new NativeParallelMultiHashMap<int, (int, float3)>(16, Allocator.Persistent);
    }

    public void Dispose()
    {
        _objects.Dispose();
        _nodes.Dispose();
        _childNodesIndex.Dispose();
    }
}




public struct EntityOctreeNode
{
    public Bounds _bounds;

    public float3 _topNorthEastPoint;
    public float3 _bottomSouthWestPoint;
    public float3 _halfSize;
    public float3 _quarterSize;

    public int level;

    public EntityOctreeNode(Bounds newBounds, int Level)
    {
        _bounds = newBounds;
        level = Level;


        _halfSize = _bounds.size / 2;
        _quarterSize = _halfSize / 2;
        _topNorthEastPoint = (float3)_bounds.center + _halfSize;
        _bottomSouthWestPoint = (float3)_bounds.center - _halfSize;

        //Index of -1 means it is not yet divided
    }


    public bool ContainsPoint(float3 point)
    {
        if (point.x > _topNorthEastPoint.x || point.x < _bottomSouthWestPoint.x)
            return false;

        if (point.y > _topNorthEastPoint.y || point.y < _bottomSouthWestPoint.y)
            return false;

        if (point.z > _topNorthEastPoint.z || point.z < _bottomSouthWestPoint.z)
            return false;

        return true;
    }
}
