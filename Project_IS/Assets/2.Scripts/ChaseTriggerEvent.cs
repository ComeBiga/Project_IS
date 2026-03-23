using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseTriggerEvent : MonoBehaviour
{
    [SerializeField]
    private BoxCollider _boxCollider;
    [SerializeField]
    private SimpleStaticAgent _agent;
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private ChaserBrain _chaserBrain;

    // Start is called before the first frame update
    void Start()
    {
        _chaserBrain.SetTriggerEvent(this);
    }

    public void EnableTrigger()
    {
        _boxCollider.enabled = true;
    }

    public void DisableTrigger()
    {
        _boxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _chaserBrain.StartDetecting();
        }
    }
     private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _boxCollider.enabled = false;
            _chaserBrain.StartChase();
        }
    }
}
