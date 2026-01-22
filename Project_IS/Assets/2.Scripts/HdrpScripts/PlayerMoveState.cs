using PropMaker;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static PlayerMovement;

public class PlayerMoveState : PlayerStateBase
{
    public float PathZPosition => mPathZPosition;
    public float InteractableMaxDistance => _interactableMaxDistance;
    public float InteractableOffsetY => _interactableOffsetY;
    public float InteractableDistance => _interactableDistance;
    public float SidePassZDistance => _sidePassZDistance;

    [Header("Debug")]
    [SerializeField] private bool _drawInteractableRay = true;
    [SerializeField] private bool _drawFrontWallCheckRay = true;
    [SerializeField] private bool _drawGroundNormal = true;

    [Header("FrontWallCheck")]
    [SerializeField] private LayerMask _frontWallCheckLayer;
    [SerializeField] private float _frontWallCheckDistance = .3f;     // 전방 벽 체크 거리

    [Header("Interactable")]
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _interactableMaxDistance = 5f;   // 상호작용 탐지 거리
    [SerializeField] private float _interactableOffsetY = 1f;       // 상호작용 origin Y offset
    [SerializeField] private float _interactableDistance = .5f;     // 상호작용 거리
    [SerializeField] private float _sidePassZDistance = .6f;     // 비켜지나가는 z길이

    [Header("Ladder")]
    [SerializeField] private float _ladderRadius = .5f;   // 사다리 탐지 반경

    private bool mbEnterToIdle = false;     // MoveState로 전환될 때 키입력 초기화
    private float mDefaultHeight;           // 낙하 상태로 전환할 때 기준이 되는 높이
    private Vector3 mPreviousForward;       // 회전을 시작하면 어느 방향으로 도는지 체크해야되기 때문에 이전 방향을 저장
    private bool mbDirectionChanged = false;
    private bool mbRotating = false;        // 현재 사용되는 곳은 없지만 회전을 체크하는 변수이기 때문에 유지
    private float mPathZPosition = 0f;    // 캐릭터가 지나가는 길의 z위치를 저장하는 변수

    private Terrain mTerrain;
    private Collider mGroundCollider;
    private Vector3 mGroundNormal = Vector3.zero;
    private float mSlopeAngle = 30f;

    public override void EnterState()
    {
        mDefaultHeight = transform.position.y;

        mPreviousForward = mController.Movement.Direction == PlayerMovement.EDirection.Left ?
                           Vector3.left : Vector3.right;

        if(mbEnterToIdle)
        {
            mController.InputHandler.ResetMoveInput();
            mbEnterToIdle = false;
        }
    }

    public override void ExitState()
    {

    }

    public override void Tick()
    {
        // Move
        checkWallForMove(out Vector2 resultMoveInput);
        mController.Movement.Move(resultMoveInput);
        mController.Animator.SetInputXMagnitude(Mathf.Abs(resultMoveInput.x));
        // mController.Movement.Move(mController.InputHandler.MoveInput);
        //mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        // Set Direction
        // 키입력이 들어오고 방향이 바뀌는 찰나 시점에 대한 코드
        if (mController.InputHandler.MoveInput.x > .001f || mController.InputHandler.MoveInput.x < -.001f)
        {
            EDirection targetDirection = mController.InputHandler.MoveInput.x > 0f ? EDirection.Right : EDirection.Left;

            // 키 입력 방향과 현재 방향이 다르면 방향 전환
            if (targetDirection != mController.Movement.Direction)
            {
                mbDirectionChanged = true;
                mController.Movement.SetDirection(targetDirection);
            }
        }

        // Turn CW/CCW
        Vector3 currentForward = transform.forward;
        float deltaRotatedAngle = Vector3.SignedAngle(mPreviousForward, currentForward, Vector3.up);

        // 회전 시작하면 어느 방향 회전인지 체크 후 Turn 애니메이션 전환
        if (mbDirectionChanged)
        {
            // 반시계방향 회전 트리거
            if (deltaRotatedAngle < -5f)
            {
                mController.Animator.TurnL(true);
                mController.Animator.TurnR(false);
                mbDirectionChanged = false;

                // 회전 각도가 있으면 회전이라고 의도를 갖게 되는데
                // 지금은 사용되는 곳이 없지만 혹시 사용되면 신경을 써야할 것 같다
                mbRotating = true;
            }
            // 시계방향 회전 트리거
            else if (deltaRotatedAngle > 5f)
            {
                mController.Animator.TurnL(false);
                mController.Animator.TurnR(true);
                mbDirectionChanged = false;
                mbRotating = true;
            }
        }
        // 회전 방향 체크 중이 아니면 
        else
        {
            if (deltaRotatedAngle > -1f && deltaRotatedAngle < 1f)
            {
                mController.Animator.TurnL(false);
                mController.Animator.TurnR(false);
                mbRotating = false;
            }
        }

        mPreviousForward = currentForward;

        // Rotate
        mController.Movement.UpdateRotation();

        // Jump
        if (mController.InputHandler.JumpPressed)
        {
            if(mController.Movement.IsGrounded)
            { 
                // 점프 입력이 됐을 때 이동 입력이 있으면 무조건 RunJump
                if (mController.InputHandler.MoveInput.x > .01f || mController.InputHandler.MoveInput.x < -.01f)
                {
                    PlayerRunJumpState runJumpState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.RunJump) as PlayerRunJumpState;
                    runJumpState.SetDefaultHeight(mDefaultHeight);

                    mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
                    mController.InputHandler.ResetJump();
                }
                else
                {
                    mController.StateMachine.SwitchState(PlayerStateMachine.EState.IdleJump);
                    mController.InputHandler.ResetJump();
                }
            }
        }

        // Fall
        PlayerFallState fallState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Fall) as PlayerFallState;

        if(fallState.CheckFall())
        // if (mController.Movement.Velocity.y < -1f)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Fall);

            return;
        }
        //if (!mController.Movement.IsGrounded && transform.position.y < mDefaultHeight - .1f) // 낙하 시작 거리를 변수로 빼는게 좋을 듯
        //{
        //    mController.StateMachine.SwitchState(PlayerStateMachine.EState.Fall);

        //    return;
        //}
        //else
        //{
        //    mDefaultHeight = transform.position.y;
        //}

        // Ladder
        if (checkLadderObject(out Collider[] ladderColliders))
        {
            switchToLadderState(ladderColliders);
            //foreach (Collider ladderCollider in ladderColliders)
            //{
            //    // Bottom
            //    // Todo: InputHandler.IsUpPressed() 정의하기
            //    if (mController.InputHandler.MoveInput.y > .1f)
            //    {
            //        if (ladderCollider.tag == "LadderTop")
            //            continue;

            //        PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
            //        LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

            //        // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
            //        if (ladderStateBase.IsOverRange(ladderHandler))
            //            continue;

            //        ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);

            //        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
            //    }
            //    // Top
            //    else if (mController.InputHandler.MoveInput.y < -.1f)
            //    {
            //        if (ladderCollider.tag != "LadderTop")
            //            continue;

            //        PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
            //        LadderHandler ladderHandler = ladderCollider.GetComponentInParent<LadderHandler>();
            //        ladderStateBase.SetLadder(ladderHandler, startFromBottom: false);

            //        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
            //    }
            //}
        }

        // Interactable
        int bHitDirection = checkInteractableObject(out RaycastHit interactableHitInfo);

        updateInteractable(bHitDirection, interactableHitInfo);

        // Terrain Normal
        if(Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, .1f, LayerMask.GetMask("Ground")))
        {
            mGroundNormal = hitInfo.normal;
            float slopeAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
            // Debug.Log(slopeAngle);

            var slopeState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Slope) as PlayerSlopeState;

            if (slopeAngle > slopeState.SlopeAngle)
            {
                // Slope State로 전환하는 코드 작성
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Slope);
                return;
            }
        }
        else
        {
            mGroundNormal = Vector3.zero;
        }

        if (mTerrain != null)
        {
            //float slopeAngle = Vector3.Angle(Vector3.up, getTerrainNormal());
            //Debug.Log(slopeAngle);
        }
    }

    public void EnterToIdle()
    {
        mbEnterToIdle = true;
    }

    private void Start()
    {
        mPathZPosition = transform.position.z;
    }

    private bool checkWallForMove(out Vector2 resultMoveInput)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = transform.position;
        pathOrigin.y += _interactableOffsetY;
        pathOrigin.z = 0f;

        // 현재 캐릭터의 위치
        Vector3 characterOrigin = transform.position;
        characterOrigin.y += _interactableOffsetY;

        // 현재 캐릭터 발을 기준으로 한 위치
        Vector3 characterFeetOrigin = transform.position;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out RaycastHit hitInfo,
                                        _frontWallCheckDistance,
                                        _frontWallCheckLayer);

        Vector2 moveInput = mController.InputHandler.MoveInput;

        if (bFrontCasted)
        {
            if(mController.Movement.Direction == EDirection.Right)
            {
                if (moveInput.x > 0f)
                    moveInput.x = 0f;
            }
            else
            {
                if (moveInput.x < 0f)
                    moveInput.x = 0f;
            }
        }

        resultMoveInput = moveInput;
        return bFrontCasted;
    }

    private bool checkLadderObject(out Collider[] collider)
    {
        Collider[] ladderColliders = Physics.OverlapSphere(transform.position, _ladderRadius, LayerMask.GetMask("Ladder"));

        if (ladderColliders.Length > 0)
        {
            collider = ladderColliders;
            return true;
        }

        collider = null;
        return false;
    }

    private bool switchToLadderState(Collider[] ladderColliders)
    {
        foreach (Collider ladderCollider in ladderColliders)
        {
            // Bottom
            // Todo: InputHandler.IsUpPressed() 정의하기
            if (mController.InputHandler.MoveInput.y > .1f)
            {
                if (ladderCollider.tag == "LadderTop")
                    continue;

                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

                // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
                if (ladderStateBase.IsOverRange(ladderHandler))
                    continue;

                ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);

                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                return true;
            }
            // Top
            else if (mController.InputHandler.MoveInput.y < -.1f)
            {
                if (ladderCollider.tag != "LadderTop")
                    continue;

                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                LadderHandler ladderHandler = ladderCollider.GetComponentInParent<LadderHandler>();
                ladderStateBase.SetLadder(ladderHandler, startFromBottom: false);

                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                return true;
            }
        }

        return false;
    }

    private int checkInteractableObject(out RaycastHit hitInfo)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = transform.position;
        pathOrigin.y += _interactableOffsetY;
        // pathOrigin.z = 0f;
        pathOrigin.z = mPathZPosition;

        // 현재 캐릭터의 위치
        Vector3 characterOrigin = transform.position;
        characterOrigin.y += _interactableOffsetY;

        // 현재 캐릭터 발을 기준으로 한 위치
        Vector3 characterFeetOrigin = transform.position;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out hitInfo,
                                        _interactableMaxDistance,
                                        _interactableLayer);

        if (bFrontCasted)
            return 1;

        bool bSideCasted = Physics.Raycast(characterOrigin,
                                    Vector3.forward,
                                    out hitInfo,
                                    _interactableMaxDistance,
                                    _interactableLayer);

        if (bSideCasted)
            return 2;

        bool bUnderCasted = Physics.Raycast(characterFeetOrigin,
                                    Vector3.down,
                                    out hitInfo,
                                    .1f,
                                    _interactableLayer);

        if (bUnderCasted)
            return 3;

        bool bBackCasted = Physics.Raycast(pathOrigin,
                                    PlayerMovement.DirectionToVector(mController.Movement.OppositeDirection),
                                    out hitInfo,
                                    _interactableMaxDistance,
                                    _interactableLayer);

        if(bBackCasted)
            return 0;

        return -1;
    }

    private void updateInteractable(int type, RaycastHit hitInfo)
    {
        // front
        if (type == 1)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            // Bounds bounds = interactableObject.BoxCollider.bounds;
            Bounds bounds = hitInfo.collider.bounds;
            Vector3 characterPos = transform.position;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // 전방의 오브젝트를 옆으로 비켜지나가는 코드
            if (interactableObject.SidePassable && characterPos.z > mPathZPosition - _sidePassZDistance)
            {
                // 가까운 모서리를 기준으로 zDistance 떨어진 점을 targetPos로 설정
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // targetPos.y = 0f;
                targetPos.y = characterPos.y;
                targetPos.z = mPathZPosition - _sidePassZDistance;

                // targetPos까지의 방향을 normalize해서 x:z 비율로 velocity.z를 계산 
                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
            }

            // PushPull Front
            if (interactableObject.Pushable && distanceToEdge < _interactableDistance && mController.InputHandler.IsInteracting)
            {
                PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(1);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
            }

            // Climb Object Up
            if (interactableObject.CanClimb && distanceToEdge < _interactableDistance && mController.InputHandler.MoveInput.y > .1f)
            {
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
            }

            // Ladder
            if ((interactableObject.CompareTag("Ladder") || hitInfo.collider.CompareTag("LadderTop"))
                && distanceToEdge < _interactableDistance)
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                bool bSwitched = switchToLadderState(ladderCollider);

                if (bSwitched)
                    return;
            }
        }
        // side
        else if (type == 2)
        {
            // velocity.z를 0으로 해주지 않으면 계속 z축으로 관성?이 남아있음
            Vector3 velocity = mController.Movement.Velocity;
            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);

            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();

            // PushPull Object
            if (interactableObject.Pushable && mController.InputHandler.IsInteracting)
            {
                PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(0);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
            }

            // Climb Object Up
            if (interactableObject.CanClimb && mController.InputHandler.MoveInput.y > .1f)
            {
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
            }

            // Ladder
            if ((interactableObject.CompareTag("Ladder") || hitInfo.collider.CompareTag("LadderTop")))
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                bool bSwitched = switchToLadderState(ladderCollider);

                if (bSwitched)
                    return;
            }
        }
        // under
        else if (type == 3)
        {
            // Climb Object Down
            if (mController.InputHandler.MoveInput.y < -.1f)
            {
                var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                climbObjectState.SetClimbObject(interactableObject, climbUp: false);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
            }

            // Debug.Log(hitInfo.collider.name);
        }
        // back
        else if (type == 0)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            Bounds bounds = interactableObject.BoxCollider.bounds;
            Vector3 characterPos = transform.position;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // 오브젝트를 비켜지나가고 나서 z위치를 다시 0으로 맞춰주는 코드
            if (interactableObject.SidePassable && characterPos.z < mPathZPosition)
            {
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // 오브젝트를 감지할 수 있는 최대 거리까지 서서히 맞춰주게 함
                targetPos.x += (characterPos.x < bounds.center.x) ? -_interactableMaxDistance : _interactableMaxDistance;
                targetPos.y = characterPos.y;
                targetPos.z = mPathZPosition;

                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
            }

            // Ladder
            if ((interactableObject.CompareTag("Ladder") || hitInfo.collider.CompareTag("LadderTop"))
                && distanceToEdge < _interactableDistance)
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                bool bSwitched = switchToLadderState(ladderCollider);

                if (bSwitched)
                    return;
            }
        }
        // none
        else
        {
            // velocity.z를 0으로 해주지 않으면 계속 z축으로 관성?이 남아있음
            Vector3 velocity = mController.Movement.Velocity;
            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);
        }
    }

    private Vector3 getTerrainNormal()
    {
        TerrainData terrainData = mTerrain.terrainData;

        Vector3 terrainLocalPos = transform.position - mTerrain.transform.position;

        float normalizedX = Mathf.InverseLerp(0f, terrainData.size.x, terrainLocalPos.x);
        float normalizedZ = Mathf.InverseLerp(0f, terrainData.size.z, terrainLocalPos.z);

        Vector3 normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);

        return normal;
    }

    private void OnCollisionStay(Collision collision)
    {
        if(((1 << collision.collider.gameObject.layer) & LayerMask.GetMask("Ground")) != 0)
        {
            mGroundCollider = collision.collider;
            mTerrain = collision.collider.GetComponent<Terrain>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(EditorApplication.isPlaying == false)
            return;

        if(_drawInteractableRay)
        {
            Vector3 pathOrigin = transform.position;
            pathOrigin.y += _interactableOffsetY;
            // pathOrigin.z = 0f;
            pathOrigin.z = mPathZPosition;

            Vector3 characterOrigin = transform.position;
            characterOrigin.y += _interactableOffsetY;

            Vector3 characterFeetOrigin = transform.position;

            // front
            Gizmos.color = Color.red;
            Gizmos.DrawRay(pathOrigin, mController.Movement.DirectionToVector() * _interactableMaxDistance);
            // back
            Gizmos.DrawRay(pathOrigin, PlayerMovement.DirectionToVector(mController.Movement.OppositeDirection) * _interactableMaxDistance);
            // side
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(characterOrigin, Vector3.forward * _interactableMaxDistance);
            // under
            Gizmos.color = Color.green;
            Gizmos.DrawRay(characterFeetOrigin, Vector3.down * .1f);
        }

        if(_drawFrontWallCheckRay)
        {
            // z가 0일 때의 위치
            Vector3 pathOrigin = transform.position;
            pathOrigin.y += _interactableOffsetY;
            // pathOrigin.z = 0f;
            pathOrigin.z = mPathZPosition;

            Vector3 dir = mController.Movement.DirectionToVector();

            Debug.Log($"{pathOrigin}, {dir}");

            Gizmos.color = Color.red;
            Gizmos.DrawRay(pathOrigin, dir * _frontWallCheckDistance);
        }

        if(_drawGroundNormal)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.down * .1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, mGroundNormal * 5f);
        }
    }
}
