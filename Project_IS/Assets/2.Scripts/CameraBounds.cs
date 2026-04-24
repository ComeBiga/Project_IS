using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool _drawBounds = false;

    [Header("Settings")]
    [SerializeField] private CameraDirector _cameraDirector;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;

    private BoxCollider mBoxCollider;

    private void Start()
    {
        mBoxCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _cameraDirector.TurnOnCamera(_virtualCamera);
        }
    }

    private void OnDrawGizmos()
    {
        if (_drawBounds)
        {
            var boxCollider = GetComponent<BoxCollider>();
            Gizmos.color = Color.blue;
            Vector3 center = new Vector3(transform.localScale.x * boxCollider.center.x,
                                        transform.localScale.y * boxCollider.center.y,
                                        transform.localScale.z * boxCollider.center.z);
            Vector3 size = new Vector3(transform.localScale.x * boxCollider.size.x,
                                        transform.localScale.y * boxCollider.size.y,
                                        transform.localScale.z * boxCollider.size.z);
            Gizmos.DrawWireCube(transform.position + center, size);
        }
    }
}
