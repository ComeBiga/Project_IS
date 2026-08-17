using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
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
    public Quaternion Rotation => transform.rotation;
    public EDirection Direction => mDirection;
    public EDirection OppositeDirection => (mDirection == EDirection.Left) ? EDirection.Right : EDirection.Left;
    public float Height => mCapsuleCollider.height;
    public LayerMask GroundLayer => _groundLayer;
    public float GroundCheckRadius => _groundCheckRadius;
    public Ground Ground => mGround;
    public float StepOffset => _stepOffset;

    public enum EDirection { Left, Right, Forward, Neutral = 100 };

    [Header("Debug")]
    [SerializeField] private bool _drawStepRay = false;
    [SerializeField] private bool _drawGroundCheckRay = false;
    [SerializeField] private bool _drawSlideDirectionRay = true;
    [SerializeField] private Vector2 _slideDirection = Vector2.right;
    [SerializeField] private float _slideSpeed = 1f;

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotateSpeed = 10f;
    [SerializeField] private float _stepOffset = .1f;
    [SerializeField] private int _stepCheckRaycastCount = 5;
    [SerializeField] private float _stepCheckDistance = .3f;
    [SerializeField] private float _stepCheckDistanceMultiplier = 1f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _minJumpVelocityX = 2f;

    [Header("GroundCheck")]
    [SerializeField] private int _groundCheckRaycastCount = 5;
    [SerializeField] private Transform _trGroundCheck;
    [SerializeField] private Vector3 _groundCheckPosOffset;
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
    private bool mbIsGrounded = true;
    private bool mbIsGroundedEnter = false;
    private float mGroundCheckDisableTimer = 0f;
    private Ground mGround;
    private RaycastHit? mGroundHitInfo = null;
    private Vector3 mGroundedVelocity = Vector3.zero;

    private Terrain mTerrain;

    public void Initialize()
    {
        mRigidbody = GetComponent<Rigidbody>();
        mCapsuleCollider = GetComponent<CapsuleCollider>();
        mPhysicsMaterial = mCapsuleCollider.material;

        mRigidbody.MoveRotation(DirectionToRotation(mDirection));
    }

    public void SetPosition(Vector3 position)
    {
        mRigidbody.MovePosition(position);
    }

    public void AddPosition(Vector3 position)
    {
        Vector3 newPosition = Position + position;
        mRigidbody.MovePosition(newPosition);
    }

    public void SetRotation(Quaternion rotation)
    {
        mRigidbody.MoveRotation(rotation);
    }

    public void Move(Vector2 moveInput)
    {
        Vector3 velocity = mRigidbody.velocity;
        velocity.x = moveInput.x * _moveSpeed;
        mRigidbody.velocity = velocity;

        checkStep();
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
        // Debug.Log($"[{Time.frameCount}] SetVelocity - Velocity: {mRigidbody.velocity}");
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

    public void ChangeDirectionRotation()
    {
        ChangeDirection();

        Quaternion targetRotation = DirectionToRotation(mDirection);

        transform.rotation = targetRotation;
    }

    public void SetRotationToCurrentDirection()
    {
        Quaternion targetRotation = DirectionToRotation(mDirection);

        transform.rotation = targetRotation;
    }

    public bool IsMoveInputToCharacterDirection(Vector2 moveInput)
    {
        if(moveInput.x < 0 && mDirection == EDirection.Left)
        {
            return true;
        }

        if (moveInput.x > 0 && mDirection == EDirection.Right)
        {
            return true;
        }

        return false;
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

    public void UpdateRotation(float t)
    {
        UpdateRotation(mDirection, t);
    }

    public void UpdateRotation(EDirection direction)
    {
        UpdateRotation(direction, Time.deltaTime * _rotateSpeed);
    }
    public Quaternion DirectionToRotation()
    {
        return DirectionToRotation(mDirection);
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

    public static Vector3 DirectionToEulerAngles(EDirection direction)
    {
        if (direction == EDirection.Right)
            return new Vector3(0f, 90f, 0f);
        else if (direction == EDirection.Left)
            return new Vector3(0f, -90f, 0f);
        else
            return Vector3.zero;
    }

    // 1 : right(90), -1 : left(-90, 270), 0 : 그 외
    public static EDirection MoveInputXToDirection(float moveInputX)
    {
        if (moveInputX < -.001f)
        {
            return EDirection.Left;
        }
        else if (moveInputX > .001f)
        {
            return EDirection.Right;
        }

        return EDirection.Neutral;
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

    public void SetColliderTrigger(bool value)
    {
        mCapsuleCollider.isTrigger = value;
    }

    public void SetFriction(bool value)
    {
        if (value)
            mCapsuleCollider.material = mPhysicsMaterial;
        else
            mCapsuleCollider.material = null;
    }

    public void SetRadius(float radius = .2f)
    {
        mCapsuleCollider.radius = radius;
    }
    public void SetGround(Ground ground)
    {
        mGround = ground;
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
        Vector3 characterFeetOrigin = Position;

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
        hitColliders = Physics.OverlapSphere(Position, 0.1f, LayerMask.GetMask("Interactable"));

        if (hitColliders.Length > 0)
            return true;

        return false;
    }

    #endregion

    public void FixedTick()
    {
        // Check Ground
        calculateGrounded();
    }

    public void LateFixedTick()
    {
        //if (mbIsGroundedEnter)
        //{
        //    Debug.Log($"[{Time.frameCount}] eLateFixedUpdate - IsGrounded: {mbIsGrounded}");

        //    float deltaPositionX = mGroundedVelocity.x * Time.fixedDeltaTime;
        //    float currentFrameXpos = transform.position.x + deltaPositionX;
        //    float nextFrameXpos = currentFrameXpos + deltaPositionX;
        //    float yPos = mGroundHitInfo.Value.point.y;
        //    transform.position = new Vector3(currentFrameXpos, yPos, transform.position.z);

        //    Debug.Log($"[{Time.frameCount}] LateFixedUpdate - GroundedVelocity: {mGroundedVelocity}, deltaPositionX: {deltaPositionX}");

        //    mGroundHitInfo = null;
        //}
    }

    public void Tick()
    {
        _animator.SetVelocityY(mRigidbody.velocity.y);

        GameDebug.Log($"Velocity: {Velocity}",
                        tag: "Velocity",
                        category: GameDebug.LogCategory.Movement,
                        level: GameDebug.LogLevel.Verbose);

        // mRigidbody.AddForce(_slideDirection * _slideSpeed, ForceMode.Acceleration);
        //Vector3 deltaVel = _slideDirection * _slideSpeed * Time.fixedDeltaTime;
        //mRigidbody.velocity += deltaVel;
        // Debug.Log($"deltaVel: {deltaVel}, velocity: {mRigidbody.velocity}");
        //if(mbJumping)
        //    Debug.Log($"velocity: {mRigidbody.velocity}");
    }

    //private void FixedUpdate()
    //{
    //    Debug.Log($"[{Time.frameCount}] PlayerMovement FixedUpdate");

    //    // Check Ground
    //    // checkGround();
    //    calculateGrounded();
    //}

    private void OnCollisionStay(Collision collision)
    {
        if(collision.collider != null)
        {
            if(((1 << collision.collider.gameObject.layer) & _groundLayer) != 0)
            {
                GameDebug.Log($"Collision Stay, Collider Name: {collision.collider.gameObject.name}",
                    tag: "OnCollisionStay");
            }
        }
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
            mbIsGrounded = Physics.CheckSphere(_trGroundCheck.position + _groundCheckPosOffset, _groundCheckRadius, _groundLayer);
            Collider[] groundColliders = Physics.OverlapSphere(_trGroundCheck.position, _groundCheckRadius, _groundLayer, QueryTriggerInteraction.Ignore);
            if(groundColliders.Length > 0)
            {
                mGround = groundColliders[0].GetComponent<Ground>();
            }

            if (mbIsGrounded)
            {
                mbJumping = false;
            }

            // _animator.SetIsGrounded(mbIsGrounded);
        }

        _animator.SetIsGrounded(mbIsGrounded);
    }

    private void calculateGrounded()
    {
        if (mGroundCheckDisableTimer > 0f)
        {
            mGroundCheckDisableTimer -= Time.deltaTime;
            mbIsGrounded = false;

            _animator.SetIsGrounded(mbIsGrounded);

            return;
        }

        if (mbIsGroundedEnter)
        {
            mbIsGroundedEnter = false;

            float deltaPositionX = mGroundedVelocity.x * Time.fixedDeltaTime;
            float posX = Position.x;
            float currentFrameXpos = Position.x + deltaPositionX;
            // float nextFrameXpos = currentFrameXpos + deltaPositionX;
            float yPos = mGroundHitInfo.Value.point.y;
            // transform.position = new Vector3(currentFrameXpos, yPos, Position.z);
            SetPosition(new Vector3(currentFrameXpos, yPos, Position.z));

            mGroundHitInfo = null;

            GameDebug.Log($"Grounded Velocity: {mGroundedVelocity}, lastPosX: {posX}, resultPosX: {currentFrameXpos}, gap: {currentFrameXpos - posX}",
                tag: "GroundedEnter Pos",
                category: GameDebug.LogCategory.Movement);
        }

        // Vector3 pos = transform.position + Vector3.up * .01f;
        Vector3 pos = Position + Vector3.up * mCapsuleCollider.radius;
        float spacing = mCapsuleCollider.radius * 2f / (_groundCheckRaycastCount - 1);

        Vector3 startPos = pos + DirectionToVector() * mCapsuleCollider.radius;
        bool bGrounded = false;
        RaycastHit hitInfo = new();

        for(int i = 0; i < _groundCheckRaycastCount; ++i)
        {
            Vector3 origin = startPos + DirectionToVector(OppositeDirection) * spacing * i;

            if (Physics.Raycast(origin, Vector3.down, out hitInfo, 5f, _groundLayer, QueryTriggerInteraction.Ignore))
            {
                float deltaPositionY = Velocity.y * Time.fixedDeltaTime;
                float currentFrameYpos = Position.y + deltaPositionY;
                float nextFrameYpos = currentFrameYpos + deltaPositionY;

                if (nextFrameYpos < hitInfo.point.y || Position.y < hitInfo.point.y + _stepOffset)
                {
                    

                    bGrounded = true;

                    GameDebug.Log($"Grounded, index: {i}",
                        tag: "GroundedTrue",
                        category: GameDebug.LogCategory.Movement,
                        level: GameDebug.LogLevel.Info);

                    break;
                }
                //else
                //{
                //    mbIsGrounded = false;
                //}
            }
            //else
            //{
            //    mbIsGrounded = false;
            //}
        }

        if(bGrounded)
        {
            if (mbIsGrounded == false)
            {
                mbIsGroundedEnter = true;
                mbIsGrounded = true;
                mbJumping = false;

                mGroundHitInfo = hitInfo;
                mGroundedVelocity = Velocity;
            }
        }
        else
        {
            mbIsGrounded = false;
        }

        _animator.SetIsGrounded(mbIsGrounded);
    }

    private void checkStep()
    {
        float radius = mCapsuleCollider.radius;
        Vector3 startPos = Position + Vector3.up * radius;
        int raycastCount = _stepCheckRaycastCount;
        // float spacing = radius / (raycastCount - 1);
        float deltaAngle = Number.DEG_90 / (raycastCount - 1);
        float startAngle = Number.DEG_0;
        
        bool bCheck = false;
        RaycastHit hit = new RaycastHit();
        int checkIndex = -1;
        float deltaY = 0;

        for(int i = 0; i < raycastCount; ++i)
        {
            float angle = startAngle + deltaAngle * i;
            float distance = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            deltaY = radius * Mathf.Sin(angle * Mathf.Deg2Rad);
            Vector3 origin = startPos + Vector3.down * deltaY;
            // origin.y += _stepOffset;

            bCheck = Physics.Raycast(origin,
                                    DirectionToVector(),
                                    out hit,
                                    distance * _stepCheckDistanceMultiplier,
                                    _groundLayer);

            checkIndex = i;

            if (bCheck)
                break;
        }

        if (!bCheck)
            return;

        Vector3 hitPoint = hit.point;
        Bounds bounds = hit.collider.bounds;

        if (bounds.max.y < Position.y + _stepOffset)
        {
            Vector3 newPosition = Position;
            newPosition.y = bounds.max.y;
            // transform.position = newPosition;
            SetPosition(newPosition);

            GameDebug.Log($"Step Checked", tag: "Step Checked");
        }
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireSphere(_trGroundCheck.position + _groundCheckPosOffset, _groundCheckRadius);

        // if(_drawStepRay)
        GameDebug.DrawGizmos(GameDebug.GizmosInfo.normal, () =>
        {
            //Vector3 origin = transform.position;
            //origin.y += _stepOffset;

            //Gizmos.color = Color.red;
            //Gizmos.DrawRay(origin, DirectionToVector() * _stepCheckDistance);

            float radius = mCapsuleCollider.radius;
            Vector3 startPos = Position + Vector3.up * radius;
            int raycastCount = _stepCheckRaycastCount;
            float deltaAngle = Number.DEG_90 / (raycastCount - 1);
            float startAngle = Number.DEG_0;

            for (int i = 0; i < raycastCount; ++i)
            {
                float angle = startAngle + deltaAngle * i;
                float distance = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                float deltaY = radius * Mathf.Sin(angle * Mathf.Deg2Rad);
                Vector3 origin = startPos + Vector3.down * deltaY;
                // origin.y += _stepOffset;

                Gizmos.color = Color.red;
                // Gizmos.DrawRay(origin, DirectionToVector() * distance * _stepCheckDistanceMultiplier);
                GameDebug.DrawRay(origin, DirectionToVector() * distance * _stepCheckDistanceMultiplier, GameDebug.GizmosInfo.normal);
            }
        });

        // if(_drawGroundCheckRay)
        GameDebug.DrawGizmos(GameDebug.GizmosInfo.normal, () =>
        {
            Vector3 offsetY = Vector3.up * mCapsuleCollider.radius;
            Vector3 pos = Position + offsetY;
            float spacing = mCapsuleCollider.radius * 2f / (_groundCheckRaycastCount - 1);
            Vector3 startPos = pos + DirectionToVector() * mCapsuleCollider.radius;

            for (int i = 0; i < _groundCheckRaycastCount; ++i)
            {
                Vector3 origin = startPos + DirectionToVector(OppositeDirection) * spacing * i;
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(origin, Vector3.down * 5f);
            }

            Vector3 groundCheckLinePos = Position + DirectionToVector() * mCapsuleCollider.radius + Vector3.down * _stepOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(groundCheckLinePos, DirectionToVector(OppositeDirection) * mCapsuleCollider.radius * 2f);
        });

        // if (_drawSlideDirectionRay)
        GameDebug.DrawGizmos(GameDebug.GizmosInfo.normal, () =>
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = Position;
            // origin.y += 1f;
            Gizmos.DrawRay(origin, new Vector3(_slideDirection.x, _slideDirection.y, 0f).normalized * 2f);
        });

        if (mTerrain == null)
            return;

        TerrainData terrainData = mTerrain.terrainData;

        Vector3 terrainLocalPos = Position - mTerrain.transform.position;

        float normalizedX = Mathf.InverseLerp(0f, terrainData.size.x, terrainLocalPos.x);
        float normalizedZ = Mathf.InverseLerp(0f, terrainData.size.z, terrainLocalPos.z);
        
        Vector3 normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);

        Vector3 point = Position;
        point.y = height;

        Gizmos.color = Color.red;
        // Gizmos.DrawRay(point, normal * 5f);
    }
}
