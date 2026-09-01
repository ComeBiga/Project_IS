using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RagdollHandler : MonoBehaviour
{
    [SerializeField]
    private bool _startRagdollEnabled = false;

    private Animator mAnimator;
    private Rigidbody[] mRagdollRigidbodies;
    private Collider[] mRagdollColliders;

    public void EnableRagdoll()
    {
        mAnimator.enabled = false;

        foreach (Rigidbody rb in mRagdollRigidbodies)
        {
            rb.isKinematic = false;
        }

        foreach (Collider col in mRagdollColliders)
        {
            col.enabled = true;
        }
    }

    public void DisableRagdoll()
    {
        mAnimator.enabled = true;

        foreach (Rigidbody rb in mRagdollRigidbodies)
        {
            rb.isKinematic = true;
        }

        foreach (Collider col in mRagdollColliders)
        {
            col.enabled = false;
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        // mRagdollRigidbodies[0].velocity = velocity;
        foreach (Rigidbody rb in mRagdollRigidbodies)
        {
            rb.velocity = velocity;
        }
        Debug.Log($"{mRagdollRigidbodies[0].name} Ragdoll Velocity: " + mRagdollRigidbodies[0].velocity);
    }

    public void AddForce(Vector3 force)
    {
        mRagdollRigidbodies[0].AddForce(force, ForceMode.VelocityChange);
    }

    public void DebugVelocity()
    {
        Debug.Log("Ragdoll Velocity: " + mRagdollRigidbodies[0].velocity);
    }

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mRagdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        mRagdollColliders = GetComponentsInChildren<Collider>();

        if (_startRagdollEnabled)
            EnableRagdoll();
        else
            DisableRagdoll();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            EnableRagdoll();
        }
    }
}
