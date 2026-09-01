using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavTrigger : MonoBehaviour
{
    [SerializeField]
    private BoxCollider _boxCollider;
    [SerializeField]
    private SimpleStaticAgent _agent;
    [SerializeField]
    private Transform _target;

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Trigger stay detected with: " + other.name);

        //if (other.CompareTag("Player"))
        //{
        //    // _agent.SetDestination(_target.position);
        //    Debug.Log("Player entered trigger, but agent is not enabled");
        //}
        //else
        //{
        //    _boxCollider.enabled = false;
        //    _agent.enable = true;

        //    Debug.Log("Non-player entered trigger, enabling agent");
        //}
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _boxCollider.enabled = false;
            _agent.enable = true;
            Debug.Log("Player exited trigger, enabling agent");
        }
    }
}
