using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TruckDirector : MonoBehaviour
{
    [SerializeField]
    private PlayableDirector _playableDirector;
    [SerializeField]
    private BoxCollider _boxCollider;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playableDirector.Play();
            _boxCollider.enabled = false;
        }
    }
}
