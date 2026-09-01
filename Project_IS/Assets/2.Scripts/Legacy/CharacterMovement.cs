using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _rotateSpeed = 10f;
    [SerializeField] private Transform _trGroundCheck;
    [SerializeField] private float _groundCheckRadius = .2f;
    [SerializeField] private float _groundCheckDisableDuration = .2f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Animator _animator;

    [Header("Push/Pull")]
    [SerializeField] private Transform _trPushPullOrigin;
    [SerializeField] private float _pushPullRange = .5f;
    [SerializeField] private float _pushPullSpeed = 2f;
    [SerializeField] private LayerMask _pushPullLayer;

    [Header("Ladder")]
    [SerializeField] private Transform _trLadderOrigin;
    [SerializeField] private float _ladderRange = .5f;
    [SerializeField] private float _ladderSpeed = 2f;
    [SerializeField] private LayerMask _ladderLayer;

    private Rigidbody mRigidbody;
    private bool mIsGrounded = true;
    private bool mJumping = false;
    private float mGroundCheckDisableTimer = 0f;
    private Vector3 mDirection = Vector3.right;
    private GameObject mPushPullObject;
    private bool mbPushPull = false;
    private Collider mTestCollider = null;
    private Bounds mBounds;
    private bool mbHit = false;
    private RaycastHit mHit;
    private bool mbClimbing = false;

    // Start is called before the first frame update
    private void Start()
    {
        mRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void Update()
    {
        // Move
        float horizontal = Input.GetAxis("Horizontal");
        float finalMoveSpeed = mbPushPull ? _pushPullSpeed : _moveSpeed;
        mRigidbody.velocity = new Vector3(horizontal * finalMoveSpeed, mRigidbody.velocity.y, 0f);
        _animator.SetFloat("Horizontal", horizontal);   // Move Anim

        if((horizontal > .001f || horizontal < -.001f) && !mbPushPull)
        {
            // Run Anim
            _animator.SetTrigger("Run");

            // Rotate
            mDirection = horizontal > 0f ? Vector3.right : Vector3.left;

            rotateTowards(mDirection, _rotateSpeed);
        }

        // Check Ground
        if (mGroundCheckDisableTimer > 0f)
        {
            mGroundCheckDisableTimer -= Time.deltaTime;
            mIsGrounded = false;
            _animator.SetBool("IsGrounded", mIsGrounded);
        }
        else
        {
            mIsGrounded = Physics.CheckSphere(_trGroundCheck.position, _groundCheckRadius, _groundLayer);
            if(mIsGrounded)
            {
                mJumping = false;
            }

            _animator.SetBool("IsGrounded", mIsGrounded);
        }

        // Jump
        if(Input.GetButtonDown("Jump") && mIsGrounded)
        {
            mJumping = true;
            mRigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            mGroundCheckDisableTimer = _groundCheckDisableDuration;
            _animator.SetTrigger("Jump");

            // _animator.MatchTarget(_trGroundCheck.position, _trGroundCheck.rotation, AvatarTarget.LeftFoot, new MatchTargetWeightMask(Vector3.one, 1f), 0.1f, 0.0f);
        }

        // Climb Object
        if (mJumping)
        {
            Collider[] pushPullColliders = Physics.OverlapSphere(_trPushPullOrigin.position, _pushPullRange, _pushPullLayer);

            if (pushPullColliders.Length > 0)
            {
                Bounds bounds = pushPullColliders[0].bounds;

                mRigidbody.isKinematic = true;
                transform.position = new Vector3(transform.position.x, bounds.max.y, bounds.min.z + (bounds.size.z / 2f));
                mRigidbody.velocity = Vector3.zero; 
                mRigidbody.isKinematic = false;

                mIsGrounded = true;
                mJumping = false;
                // mGroundCheckDisableTimer = 0f;
            }
        }

        // Push/Pull
        if (Input.GetButton("Fire1"))
        {
            Collider[] pushPullColliders = Physics.OverlapSphere(_trPushPullOrigin.position, _pushPullRange, _pushPullLayer);

            if (pushPullColliders.Length > 0)
            {
                mTestCollider = pushPullColliders[0];
                mBounds = pushPullColliders[0].bounds;
                rotateTowards(Vector3.forward, 0f);

                mPushPullObject = pushPullColliders[0].gameObject;
                mPushPullObject.GetComponent<Rigidbody>().velocity = mRigidbody.velocity;

                mbPushPull = true;
                _animator.SetBool("PushPull", true);
            }
        }
        else
        {
            mbPushPull = false;
            _animator.SetBool("PushPull", false);
        }

        if(Physics.Raycast(transform.position, Vector3.down, out mHit, 1.5f))
        {
            mbHit = true;
            float slope = Vector3.Angle(mHit.normal, Vector3.up);

            Debug.Log(slope);
        }
        else
        {
            mbHit = false;
        }

        // Climb Ladder
        if (mbClimbing)
        {
            float vertical = Input.GetAxis("Vertical");
            mRigidbody.velocity = new Vector3(mRigidbody.velocity.x, vertical * _ladderSpeed, mRigidbody.velocity.z);

            if (Input.GetButtonDown("Fire1"))
            {
                mbClimbing = false;
                mRigidbody.useGravity = true;
            }
        }
        else
        {
            if (Input.GetButtonDown("Fire1"))
            {
                Collider[] ladderCollider = Physics.OverlapSphere(_trLadderOrigin.position, _ladderRange, _ladderLayer);

                if (ladderCollider.Length > 0)
                {
                    mbClimbing = true;
                    mRigidbody.useGravity = false;
                }
            }
        }
    }

    private void FixedUpdate()
    {

    }

    private void OnDrawGizmosSelected()
    {
        if (_trGroundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_trGroundCheck.position, _groundCheckRadius);
        }

        if (_trPushPullOrigin != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_trPushPullOrigin.position, _pushPullRange);
        }

        if (mTestCollider != null)
        {
            Bounds bounds = mTestCollider.bounds;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(bounds.max, bounds.min);
        }
    }

    private void OnDrawGizmos()
    {
        var ray = new Ray(transform.position, Vector3.right);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray);

        if (mbHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(new Ray(mHit.point, mHit.normal));
        }
    }

    private void rotateTowards(Vector3 targetDirection, float rotateSpeed)
    {
        // Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        Quaternion targetRotation = Quaternion.identity;

        if(targetDirection == Vector3.right)
        {
            targetRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else if(targetDirection == Vector3.left)
        {
            targetRotation = Quaternion.Euler(0f, -90f, 0f);
        }

        mRigidbody.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed));
    }
}
