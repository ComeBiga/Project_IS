using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public Vector3 Velocity => mRigidbody.velocity;
    public bool Jumping => mbJumping;
    public bool IsGrounded => mbIsGrounded;
    public Vector3 Position => transform.position;
    public EDirection Direction => mDirection;
    public EDirection OppositeDirection => (mDirection == EDirection.Left) ? EDirection.Right : EDirection.Left;

    public enum EDirection { Left, Right };

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotateSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private Transform _trGroundCheck;
    [SerializeField] private float _groundCheckRadius = .1f;
    [SerializeField] private float _groundCheckDisableDuration = .2f;   // 점프 직후 점프 중복 방지를 위해 바닥 체크하지 않는 시간
    [SerializeField] private LayerMask _groundLayer;

    [Header("Animator")]
    [SerializeField] private PlayerAnimator _animator;

    private Rigidbody mRigidbody;
    private CapsuleCollider mCapsuleCollider;
    private EDirection mDirection = EDirection.Right;
    private bool mbJumping = false;
    private bool mbIsGrounded = false;
    private float mGroundCheckDisableTimer = 0f;

    public void Initialize()
    {
        mRigidbody = GetComponent<Rigidbody>();
        mCapsuleCollider = GetComponent<CapsuleCollider>();

        mRigidbody.MoveRotation(DirectionToRotation(mDirection));
    }

    public void Move(Vector2 moveInput)
    {
        Vector3 velocity = mRigidbody.velocity;
        velocity.x = moveInput.x * _moveSpeed;
        mRigidbody.velocity = velocity;
    }
    
    public void Move(Vector2 moveInput, float speed)
    {
        Vector3 velocity = mRigidbody.velocity;
        velocity.x = moveInput.x * speed;
        mRigidbody.velocity = velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        mRigidbody.velocity = velocity;
    }

    // 현재 방향과 반대 방향으로 변수를 설정하는 함수
    // 캐릭터의 회전을 Update 하지는 않음
    public void ChangeDirection()
    {
        mDirection = mDirection == EDirection.Right ? EDirection.Left : EDirection.Right;
    }

    // 방향을 설정하는 함수
    // 캐릭터의 회전을 Update 하지는 않음
    public void SetDirection(EDirection direction)
    {
        mDirection = direction;
    }

    // 캐릭터의 회전을 Update하는 함수
    public void UpdateRotation()
    {
        Quaternion targetRotation = DirectionToRotation(mDirection);

        mRigidbody.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed));
    }

    public void UpdateRotation(Quaternion from, EDirection direction, float t)
    {
        Quaternion targetRotation = DirectionToRotation(direction);

        mRigidbody.MoveRotation(Quaternion.Lerp(from, targetRotation, t));
    }

    public void UpdateRotation(EDirection fromDirection, EDirection toDirection, float t)
    {
        Quaternion fromRotation = DirectionToRotation(fromDirection);
        Quaternion toRotation = DirectionToRotation(toDirection);

        mRigidbody.MoveRotation(Quaternion.Lerp(fromRotation, toRotation, t));
    }

    public void UpdateRotation(EDirection direction, float t)
    {
        Quaternion targetRotation = DirectionToRotation(direction);

        mRigidbody.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, t));
    }

    public void UpdateRotation(EDirection direction)
    {
        UpdateRotation(direction, Time.deltaTime * _rotateSpeed);
    }

    // 해당 방향을 나타내는 Quaternion 값을 반환
    public Quaternion DirectionToRotation(EDirection direction)
    {
        Quaternion targetRotation = Quaternion.identity;

        if (direction == EDirection.Right)
        {
            targetRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else if (direction == EDirection.Left)
        {
            targetRotation = Quaternion.Euler(0f, -90f, 0f);
        }

        return targetRotation;
    }

    // Quaternion 값으로부터 방향을 반환
    // 1 : right(90), -1 : left(-90, 270), 0 : 그 외
    public static int RotationToDirection(Quaternion rotation)
    {
        if (Mathf.Approximately(rotation.eulerAngles.y, 90f))
            return 1;
        else if (Mathf.Approximately(rotation.eulerAngles.y, -90f))
            return -1;
        else if (Mathf.Approximately(rotation.eulerAngles.y, 270f))
            return -1;
        else
            return 0;
    }

    public void Jump()
    {
        if(mbIsGrounded)
        {
            mbJumping = true;
            mRigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            mGroundCheckDisableTimer = _groundCheckDisableDuration;

            // _animator.SetJump();
        }
    }

    public void StopJump()
    {
        mbJumping = false;
        mbIsGrounded = true;

        mRigidbody.isKinematic = true;
        mRigidbody.velocity = Vector3.zero;
        mRigidbody.isKinematic = false;
    }

    public void SetUseGravity(bool value)
    {
        mRigidbody.useGravity = value;
    }

    public void SetColliderActive(bool value)
    {
        mCapsuleCollider.enabled = value;
    }

    public void Tick()
    {
        _animator.SetVelocityY(mRigidbody.velocity.y);

        // Check Ground
        checkGround();
    }

    private void checkGround()
    {
        if (mGroundCheckDisableTimer > 0f)
        {
            mGroundCheckDisableTimer -= Time.deltaTime;
            mbIsGrounded = false;

            // _animator.SetIsGrounded(mbIsGrounded);
        }
        else
        {
            // 바닥을 Sphere로 체크하고 있는데 다른 기능들을 생각해서 Laycast로 바꿔야 될 것 같음
            mbIsGrounded = Physics.CheckSphere(_trGroundCheck.position, _groundCheckRadius, _groundLayer);

            if (mbIsGrounded)
            {
                mbJumping = false;
            }

            // _animator.SetIsGrounded(mbIsGrounded);
        }

        _animator.SetIsGrounded(mbIsGrounded);
    }
}
