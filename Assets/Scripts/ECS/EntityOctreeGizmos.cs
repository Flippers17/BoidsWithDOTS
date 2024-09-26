using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class EntityOctreeGizmos : MonoBehaviour
{
    public static List<EntityOctreeNode> nodes = new List<EntityOctreeNode>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < nodes.Count; i++)
        {
            Gizmos.DrawWireCube(nodes[i]._bounds.center, nodes[i]._bounds.size);
        }
    }
}
