using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _agentPrefab;

    [SerializeField]
    private float radius = 20f;
    [SerializeField]
    private int agentCount = 20;

    // Start is called before the first frame update
    void Start()
    {
        for(int i= 0; i < agentCount; i++)
        {
            Instantiate(_agentPrefab, transform.position + (Random.insideUnitSphere * radius), Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
