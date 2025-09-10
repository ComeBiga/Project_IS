using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushPullObject : InteractableObject
{
    public float PushPullSpeed => _pushPullSpeed;
    // public BoxCollider BoxCollider => mBoxCollider;

    [Header("PushPullObject")]
    [SerializeField] private float _pushPullSpeed = 1f;

    private Rigidbody mRigidbody;
    // private BoxCollider mBoxCollider;

    public void PushPull(Vector3 velocity)
    {
        mRigidbody.velocity = velocity;
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        mRigidbody = GetComponent<Rigidbody>();
        // mBoxCollider = GetComponent<BoxCollider>();
    }
}
