using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushPullObject : InteractableObject
{
    public float PushPullSpeed => _pushPullSpeed;
    // public BoxCollider BoxCollider => mBoxCollider;
    public float VelocityX => mRigidbody.velocity.x;
    public Transform HandlePointL => _handlePointL;
    public Transform HandlePointR => _handlePointR;

    [Header("PushPullObject")]
    [SerializeField] private bool _logVelocity = false;
    [SerializeField] private float _pushPullSpeed = 1f;
    [SerializeField] private float _pushPullMaxSpeed = 1f;
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

    public bool PushPull(PlayerController playerController, Vector3 velocity)
    {
        // Debug.Log($"velocity {mRigidbody.velocity}, angular velocity {mRigidbody.angularVelocity}");

        // mRigidbody.AddForce(velocity * Time.fixedDeltaTime, ForceMode.Acceleration);
        mRigidbody.AddForce(velocity * 10f, ForceMode.Acceleration);

        Vector3 finalVelocity = mRigidbody.velocity;
        if (Mathf.Abs(mRigidbody.velocity.x) > _pushPullMaxSpeed)
            finalVelocity.x = Mathf.Sign(mRigidbody.velocity.x) * _pushPullMaxSpeed;
        mRigidbody.velocity = finalVelocity;

        // Debug.Log($"sign X:{Mathf.Sign(mRigidbody.velocity.x)} velocity: {mRigidbody.velocity}");

        //Vector3 finalVelocity = mRigidbody.velocity;
        //finalVelocity.x = Mathf.Abs(mRigidbody.velocity.x) > Mathf.Abs(velocity.x) ? mRigidbody.velocity.x : velocity.x;
        //mRigidbody.velocity = finalVelocity;

        // mRigidbody.AddForce(velocity, ForceMode.Force);

        // mRigidbody.AddForceAtPosition(velocity, mBoxCollider.bounds.center, ForceMode.Force);

        //if (Mathf.Abs(mRigidbody.velocity.y) > 1f)
        //    return false;

        // playerController.Movement.SetVelocity(mRigidbody.velocity);

        return true;
    }

    public float GetVelocityXRatio()
    {
        return mRigidbody.velocity.x / _pushPullMaxSpeed;
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        mRigidbody = GetComponent<Rigidbody>();
        // mBoxCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if(_logVelocity)
            Debug.Log($"velocity {mRigidbody.velocity}");
    }
}
