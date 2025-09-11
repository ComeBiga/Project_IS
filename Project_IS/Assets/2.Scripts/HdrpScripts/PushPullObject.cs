using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushPullObject : InteractableObject
{
    public float PushPullSpeed => _pushPullSpeed;
    // public BoxCollider BoxCollider => mBoxCollider;
    public Transform HandlePointL => _handlePointL;
    public Transform HandlePointR => _handlePointR;

    [Header("PushPullObject")]
    [SerializeField] private float _pushPullSpeed = 1f;
    [SerializeField] private PhysicMaterial _matNoFriction;
    [SerializeField] private Transform _handlePointL;
    [SerializeField] private Transform _handlePointR;

    private Rigidbody mRigidbody;
    // private BoxCollider mBoxCollider;

    public void SetFriction(bool value)
    {
        if (value)
            mBoxCollider.material = null;
        else
            mBoxCollider.material = _matNoFriction;
    }

    public bool PushPull(Vector3 velocity)
    {
        Vector3 finalVelocity = mRigidbody.velocity;
        finalVelocity.x = Mathf.Abs(mRigidbody.velocity.x) > Mathf.Abs(velocity.x) ? mRigidbody.velocity.x : velocity.x;
        mRigidbody.velocity = finalVelocity;
        // mRigidbody.AddForce(velocity, ForceMode.Force);

        // mRigidbody.AddForceAtPosition(velocity, mBoxCollider.bounds.center, ForceMode.Force);
        Debug.Log($"velocity {mRigidbody.velocity}, angular velocity {mRigidbody.angularVelocity}");

        //if (Mathf.Abs(mRigidbody.velocity.y) > 1f)
        //    return false;

        return true;
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        mRigidbody = GetComponent<Rigidbody>();
        // mBoxCollider = GetComponent<BoxCollider>();
    }
}
