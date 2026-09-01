using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionEffector : MonoBehaviour
{
    [SerializeField]
    private bool _explodeOnStart = false;
    [SerializeField]
    private float _force = 10f;
    [SerializeField]
    private float _radius = 5f;
    [SerializeField]
    private Rigidbody[] _targets;

    public void Explode()
    {
        foreach (Rigidbody target in _targets)
        {
            //Vector3 explosionDirection = target.transform.position - transform.position;
            //explosionDirection.Normalize();
            //float explosionForce = 5f;
            //target.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);

            target.AddExplosionForce(_force, transform.position, _radius);
        }
    }

    private void Start()
    {
        if (_explodeOnStart)
        {
            Explode();
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
