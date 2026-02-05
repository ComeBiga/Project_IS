using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public Vector3 Velocity => mRigidbody.velocity;
    public float MoveSpeed => _moveSpeed;
    public bool Jumping => mbJumping;
    public bool IsGrounded => mbIsGrounded;
    public Vector3 Position => transform.position;
    public EDirection Direction => mDirection;
    public EDirection OppositeDirection => (mDirection == EDirection.Left) ? EDirection.Right : EDirection.Left;
    public float Height => mCapsuleCollider.height;

    public enum EDirection { Left, Right, Forward };

    [Header("Debug")]
    [SerializeField] private bool _drawSlideDirectionRay = true;
    [SerializeField] private Vector2 _slideDirection = Vector2.right;
    [SerializeField] private float _slideSpeed = 1f;

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotateSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _minJumpVelocityX = 2f;
    [SerializeField] private Transform _trGroundCheck;
    [SerializeField] private float _groundCheckRadius = .1f;
    [SerializeField] private float _groundCheckDisableDuration = .2f;   // 점프 직후 점프 중복 방지를 위해 바닥 체크하지 않는 시간
    [SerializeField] private LayerMask _groundLayer;

    [Header("Animator")]
    [SerializeField] private PlayerAnimator _animator;

    [Header("Interactable")]
    [SerializeField] private float _interactableOffsetY;

    private Rigidbody mRigidbody;
    private CapsuleCollider mCapsuleCollider;
    private PhysicMaterial mPhysicsMaterial;
    private EDirection mDirection = EDirection.Right;
    private bool mbJumping = false;
    private bool mbIsGrounded = false;
    private float mGroundCheckDisableTimer = 0f;

    private Terrain mTerrain;

    public void Initialize()
    {
        mRigidbody = GetComponent<Rigidbody>();
        mCapsuleCollider = GetComponent<CapsuleCollider>();
        mPhysicsMaterial = mCapsuleCollider.material;

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

    public Vector3 DirectionToVector()
    {
        return DirectionToVector(mDirection);
    }

    // 해당 방향을 나타내는 Vector3 값을 반환
    public static Vector3 DirectionToVector(EDirection direction)
    {
        if (direction == EDirection.Right)
            return Vector3.right;
        else if (direction == EDirection.Left)
            return Vector3.left;
        else
            return Vector3.zero;
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
        else if (Mathf.Approximately(rotation.eulerAngles.y, Number.DEG_0))
            return 2;
        else
            return 0;
    }

    public void JumpUp()
    {
        mbJumping = true;
        mGroundCheckDisableTimer = _groundCheckDisableDuration;

        Vector3 direction = Vector3.up;

        mRigidbody.velocity = direction * _jumpForce;
    }

    public void JumpFoward()
    {
        mbJumping = true;
        mGroundCheckDisableTimer = _groundCheckDisableDuration;

        Vector3 direction = Vector3.up;

        if (mDirection == EDirection.Right)
            direction += Vector3.right;
        else if (mDirection == EDirection.Left)
            direction += Vector3.left;

        mRigidbody.velocity = new Vector3(direction.x * _minJumpVelocityX,
                                        direction.y * _jumpForce,
                                        direction.z);
    }

    public void ForceJump()
    {
        mbJumping = true;
        mRigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        mGroundCheckDisableTimer = _groundCheckDisableDuration;
    }

    public void UpdateJump(Vector2 moveInput)
    {
        Vector3 velocity = mRigidbody.velocity;
        velocity.x = moveInput.x * _moveSpeed;

        if (mDirection == EDirection.Left)
        {
            if (velocity.x > -_minJumpVelocityX)
                velocity.x = -_minJumpVelocityX;
        }
        else if(mDirection == EDirection.Right)
        {
            if (velocity.x < _minJumpVelocityX)
                velocity.x = _minJumpVelocityX;
        }

        mRigidbody.velocity = velocity;
    }

    public void StopJump()
    {
        mbJumping = false;
        mbIsGrounded = true;

        mRigidbody.isKinematic = true;
        mRigidbody.velocity = Vector3.zero;
        mRigidbody.isKinematic = false;
    }

    public void SetKinematic(bool value)
    {
        mRigidbody.isKinematic = value;
    }

    public void SetUseGravity(bool value)
    {
        mRigidbody.useGravity = value;
    }

    public void SetColliderActive(bool value)
    {
        mCapsuleCollider.enabled = value;
    }

    public void SetFriction(bool value)
    {
        if (value)
            mCapsuleCollider.material = mPhysicsMaterial;
        else
            mCapsuleCollider.material = null;
    }

    #region Interactable Methods

    public bool CheckInteractableToDown(out RaycastHit hitInfo)
    {
        // z가 0일 때의 위치
        //Vector3 pathOrigin = transform.position;
        //pathOrigin.y += mInteractableOffsetY;
        //// pathOrigin.z = 0f;
        //pathOrigin.z = mPathZPosition;

        // 현재 캐릭터의 위치
        //Vector3 characterOrigin = transform.position;
        //characterOrigin.y += _interactableOffsetY;

        // 현재 캐릭터 발을 기준으로 한 위치
        Vector3 characterFeetOrigin = transform.position;

        bool bUnderCasted = Physics.Raycast(characterFeetOrigin,
                                    Vector3.down,
                                    out hitInfo,
                                    .1f,
                                    LayerMask.GetMask("Interactable"));

        if (bUnderCasted)
            return true;

        return false;
    }

    public bool CheckInteractableByOverlap(out Collider[] hitColliders)
    {
        hitColliders = Physics.OverlapSphere(transform.position, 0.1f, LayerMask.GetMask("Interactable"));

        if (hitColliders.Length > 0)
            return true;

        return false;
    }

    #endregion

    public void Tick()
    {
        _animator.SetVelocityY(mRigidbody.velocity.y);

        // Check Ground
        checkGround();

        // mRigidbody.AddForce(_slideDirection * _slideSpeed, ForceMode.Acceleration);
        //Vector3 deltaVel = _slideDirection * _slideSpeed * Time.fixedDeltaTime;
        //mRigidbody.velocity += deltaVel;
        // Debug.Log($"deltaVel: {deltaVel}, velocity: {mRigidbody.velocity}");
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

    private void OnCollisionStay(Collision collision)
    {
        if(collision.collider != null)
        {
            if (((1 << collision.collider.gameObject.layer) & _groundLayer) != 0)
            {
                mTerrain = collision.collider.GetComponent<Terrain>();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_drawSlideDirectionRay)
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            // origin.y += 1f;
            Gizmos.DrawRay(origin, new Vector3(_slideDirection.x, _slideDirection.y, 0f).normalized * 2f);
        }

        if (mTerrain == null)
            return;

        TerrainData terrainData = mTerrain.terrainData;

        Vector3 terrainLocalPos = transform.position - mTerrain.transform.position;

        float normalizedX = Mathf.InverseLerp(0f, terrainData.size.x, terrainLocalPos.x);
        float normalizedZ = Mathf.InverseLerp(0f, terrainData.size.z, terrainLocalPos.z);
        
        Vector3 normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);

        Vector3 point = transform.position;
        point.y = height;

        Gizmos.color = Color.red;
        // Gizmos.DrawRay(point, normal * 5f);
    }
}
