using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleStaticAgent : MonoBehaviour
{
    public bool enable = false;

    [SerializeField]
    private Transform _target;

    private NavMeshAgent mNavMeshAgent;

    // Start is called before the first frame update
    void Start()
    {
        mNavMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if(enable && _target != null)
            mNavMeshAgent.SetDestination(_target.position);
    }
}
