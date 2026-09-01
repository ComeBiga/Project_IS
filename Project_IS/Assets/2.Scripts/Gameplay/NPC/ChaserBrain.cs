using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

public class ChaserBrain : MonoBehaviour
{
    public SimpleStaticAgent agent;

    [SerializeField]
    private PlayableDirector _playableDirector;
    [SerializeField]
    private NavMeshAgent _navMeshAgent;

    private ChaseTriggerEvent mChaseTriggerEvent;

    public void SetTriggerEvent(ChaseTriggerEvent chaseTriggerEvent)
    {
        mChaseTriggerEvent = chaseTriggerEvent;
    }

    public void StartDetecting()
    {
        _playableDirector.Play();
    }

    public void StartChase()
    {
        _playableDirector.Stop();
        agent.enable = true;
    }

    public void DisableDetecting()
    {
        mChaseTriggerEvent.DisableTrigger();
        _navMeshAgent.enabled = false;
    }
}
