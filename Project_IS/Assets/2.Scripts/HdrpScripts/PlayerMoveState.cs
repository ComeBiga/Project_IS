using PropMaker;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static PlayerMovement;

public class PlayerMoveState : PlayerStateBase
{
    [Header("Interactable")]
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private float _interactableMaxDistance = 5f;   // 상호작용 탐지 거리
    [SerializeField] private float _interactableOffsetY = 1f;       // 상호작용 origin Y offset
    [SerializeField] private float _interactableDistance = .5f;     // 상호작용 거리

    [Header("Ladder")]
    [SerializeField] private float _ladderRadius = .5f;   // 사다리 탐지 반경

    [Header("PushPull")]
    [SerializeField] private Transform _trPushPullOrigin;
    // Interactable로 통합하면서 수정되지 않은 부분이 남아있음
    [SerializeField] private float _pushPullMaxDistance = 5f;
    [SerializeField] private LayerMask _pushPullLayer;
    // Interactable로 통합하면서 수정되지 않은 부분이 남아있음
    // ZDistance는 새 변수를 추가 해줘야할 듯
    [SerializeField] private float _pushPullZDistance;

    private float mDefaultHeight;           // 낙하 상태로 전환할 때 기준이 되는 높이
    private Vector3 mPreviousForward;       // 회전을 시작하면 어느 방향으로 도는지 체크해야되기 때문에 이전 방향을 저장
    private bool mbDirectionChanged = false;
    private bool mbRotating = false;        // 현재 사용되는 곳은 없지만 회전을 체크하는 변수이기 때문에 유지

    private PushPullObject mPushPullObject = null;  // 현재 사용되지 않음

    private bool mbCheckPushPullObject = false;  // 현재 사용되지 않음

    public override void EnterState()
    {
        mDefaultHeight = transform.position.y;

        mPreviousForward = mController.Movement.Direction == PlayerMovement.EDirection.Left ?
                           Vector3.left : Vector3.right;
    }

    public override void ExitState()
    {

    }

    public override void Tick()
    {
        // Move
        mController.Movement.Move(mController.InputHandler.MoveInput);
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

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

        // Fall
        if (transform.position.y < mDefaultHeight - 2f) // 낙하 시작 거리를 변수로 빼는게 좋을 듯
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Fall);

            return;
        }

        // Ladder
        if (checkLadderObject(out Collider[] ladderColliders))
        {
            foreach (Collider ladderCollider in ladderColliders)
            {
                // Bottom
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
                }
            }
        }

        // Interactable
        int bHitDirection = checkInteractableObject(out RaycastHit interactableHitInfo);

        updateInteractable(bHitDirection, interactableHitInfo);

        //// PushPull
        //int bPushPullCheck = checkPushPullObject(out RaycastHit pushPullHitInfo);

        //updatePushPull(bPushPullCheck, pushPullHitInfo);

        //// Side Passable Obstacle
        //int bSidePassbleObstacleCheck = checkSidePassableObstacle(out RaycastHit sidePassableHitInfo);

        //updateSidePassableObstacle(bSidePassbleObstacleCheck, sidePassableHitInfo);
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

    private int checkInteractableObject(out RaycastHit hitInfo)
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
            Bounds bounds = interactableObject.BoxCollider.bounds;
            Vector3 characterPos = transform.position;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // 전방의 오브젝트를 옆으로 비켜지나가는 코드
            if (interactableObject.SidePassable && characterPos.z > -_pushPullZDistance)
            {
                // 가까운 모서리를 기준으로 zDistance 떨어진 점을 targetPos로 설정
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                targetPos.y = 0f;
                targetPos.z = -_pushPullZDistance;

                // targetPos까지의 방향을 normalize해서 x:z 비율로 velocity.z를 계산 
                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
            }

            //// Ladder
            //if(hitInfo.collider.tag == "Ladder")
            //{
            //    if (distanceToEdge < _interactableDistance && mController.InputHandler.MoveInput.y > .1f)
            //    {
            //        PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
            //        var ladderHandler = interactableObject as LadderHandler;

            //        //// Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
            //        //if (ladderStateBase.IsOverRange(ladderHandler))
            //        //    return;

            //        ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);
            //        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);

            //        return;
            //    }
            //}
            //else if (hitInfo.collider.tag == "LadderTop")
            //{
            //    if (distanceToEdge < _interactableDistance && mController.InputHandler.MoveInput.y < -.1f)
            //    {
            //        PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
            //        var ladderHandler = interactableObject.GetComponentInParent<LadderHandler>();
            //        ladderStateBase.SetLadder(ladderHandler, startFromBottom: false);
            //        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);

            //        return;
            //    }
            //}

            // Climb Object Up
            if (interactableObject.CanClimb && distanceToEdge < _interactableDistance && mController.InputHandler.MoveInput.y > .1f)
            {
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
            }
        }
        // side
        else if (type == 2)
        {
            // velocity.z를 0으로 해주지 않으면 계속 z축으로 관성?이 남아있음
            Vector3 velocity = mController.Movement.Velocity;
            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);

            var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

            // PushPull Object
            if (interactableObject.Pushable && mController.InputHandler.IsInteracting)
            {
                PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
            }

            // Climb Object Up
            if (interactableObject.CanClimb && mController.InputHandler.MoveInput.y > .1f)
            {
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
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
        }
        // back
        else if (type == 0)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            Bounds bounds = interactableObject.BoxCollider.bounds;
            Vector3 characterPos = transform.position;

            // 오브젝트를 비켜지나가고 나서 z위치를 다시 0으로 맞춰주는 코드
            if (interactableObject.SidePassable && characterPos.z < 0f)
            {
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // 오브젝트를 감지할 수 있는 최대 거리까지 서서히 맞춰주게 함
                targetPos.x += (characterPos.x < bounds.center.x) ? -_pushPullMaxDistance : _pushPullMaxDistance;
                targetPos.y = 0f;
                targetPos.z = 0f;

                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
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

    // PushPull은 Interactable로 통합하면서 사용되지 않음
    //private int checkPushPullObject(out RaycastHit hitInfo)
    //{
    //    Vector3 origin = _trPushPullOrigin.position;
    //    origin.z = 0f;

    //    // RaycastHit hitInfo;

    //    bool bFrontCasted = Physics.Raycast(origin,
    //                                    mController.Movement.DirectionToVector(),
    //                                    out hitInfo,
    //                                    _pushPullMaxDistance,
    //                                    _pushPullLayer);

    //    if (bFrontCasted)
    //        return 1;

    //    bool bSideCasted = Physics.Raycast(_trPushPullOrigin.position,
    //                                Vector3.forward,
    //                                out hitInfo,
    //                                _pushPullMaxDistance,
    //                                _pushPullLayer);

    //    if (bSideCasted)
    //        return 2;

    //    bool bUnderCasted = Physics.Raycast(transform.position,
    //                                Vector3.down,
    //                                out hitInfo,
    //                                .1f,
    //                                _pushPullLayer);

    //    if (bUnderCasted)
    //        return 3;

    //    bool bBackCasted = Physics.Raycast(origin,
    //                                PlayerMovement.DirectionToVector(mController.Movement.OppositeDirection),
    //                                out hitInfo,
    //                                _pushPullMaxDistance,
    //                                _pushPullLayer);

    //    if(bBackCasted)
    //        return 0;

    //    return -1;
    //}

    //private void updatePushPull(int type, RaycastHit hitInfo)
    //{
    //    // front
    //    if (type == 1)
    //    {
    //        PushPullObject pushPullObject = hitInfo.collider.GetComponent<PushPullObject>();
    //        Bounds bounds = pushPullObject.BoxCollider.bounds;
    //        Vector3 characterPos = transform.position;

    //        if (characterPos.z > -_pushPullZDistance)
    //        {
    //            Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
    //            targetPos.y = 0f;
    //            targetPos.z = -_pushPullZDistance;

    //            Vector3 direction = targetPos - characterPos;
    //            Vector3 normalized = direction.normalized;

    //            Debug.Log(normalized);

    //            Vector3 velocity = mController.Movement.Velocity;
    //            velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
    //            Debug.Log(velocity);
    //            mController.Movement.SetVelocity(velocity);
    //        }

    //        // Climb Object Up
    //        if (mController.InputHandler.MoveInput.y > .1f)
    //        {
    //            PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
    //            pushPullState.SetClimbObject(pushPullObject, climbUp: true);
    //            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
    //        }
    //    }
    //    // side
    //    else if (type == 2)
    //    {
    //        Vector3 velocity = mController.Movement.Velocity;
    //        velocity.z = 0f;
    //        mController.Movement.SetVelocity(velocity);

    //        PushPullObject pushPullObject = hitInfo.collider.GetComponent<PushPullObject>();

    //        if (mController.InputHandler.IsInteracting)
    //        {
    //            PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
    //            pushPullState.SetPushPullObject(pushPullObject);
    //            mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
    //        }

    //        // Climb Object Up
    //        if (mController.InputHandler.MoveInput.y > .1f)
    //        {
    //            PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
    //            pushPullState.SetClimbObject(pushPullObject, climbUp: true);
    //            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
    //        }
    //    }
    //    // under
    //    else if (type == 3)
    //    {
    //        // Climb Object Down
    //        if (mController.InputHandler.MoveInput.y < -.1f)
    //        {
    //            PushPullObject pushPullObject = hitInfo.collider.GetComponent<PushPullObject>();

    //            PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
    //            pushPullState.SetClimbObject(pushPullObject, climbUp: false);
    //            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
    //        }
    //    }
    //    // back
    //    else if (type == 0)
    //    {
    //        PushPullObject pushPullObject = hitInfo.collider.GetComponent<PushPullObject>();
    //        Bounds bounds = pushPullObject.BoxCollider.bounds;
    //        Vector3 characterPos = transform.position;

    //        if (characterPos.z < 0f)
    //        {
    //            Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
    //            targetPos.x += (characterPos.x < bounds.center.x) ? -_pushPullMaxDistance : _pushPullMaxDistance;
    //            targetPos.y = 0f;
    //            targetPos.z = 0f;

    //            Vector3 direction = targetPos - characterPos;
    //            Vector3 normalized = direction.normalized;

    //            // Debug.Log(normalized);

    //            Vector3 velocity = mController.Movement.Velocity;
    //            velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
    //            // Debug.Log(velocity);
    //            mController.Movement.SetVelocity(velocity);
    //        }
    //    }
    //    // none
    //    else
    //    {
    //        Vector3 velocity = mController.Movement.Velocity;
    //        velocity.z = 0f;
    //        mController.Movement.SetVelocity(velocity);
    //    }
    //}

    //private int checkSidePassableObstacle(out RaycastHit hitInfo)
    //{
    //    Vector3 origin = _trPushPullOrigin.position;
    //    origin.z = 0f;

    //    // RaycastHit hitInfo;

    //    bool bFrontCasted = Physics.Raycast(origin,
    //                                    mController.Movement.DirectionToVector(),
    //                                    out hitInfo,
    //                                    _pushPullMaxDistance);
    //                                    // _pushPullLayer);

    //    if (bFrontCasted)
    //    {
    //        if(hitInfo.collider.GetComponent<InteractableObject>() != null)
    //        {
    //            var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

    //            if(interactableObject.SidePassable)
    //                return 1;
    //        }
    //    }

    //    bool bSideCasted = Physics.Raycast(_trPushPullOrigin.position,
    //                                    Vector3.forward,
    //                                    out hitInfo,
    //                                    _pushPullMaxDistance);
    //                                    // _pushPullLayer);

    //    if (bSideCasted)
    //    {
    //        if (hitInfo.collider.GetComponent<InteractableObject>() != null)
    //        {
    //            var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

    //            if (interactableObject.SidePassable)
    //                return 2;
    //        }
    //    }

    //    bool bUnderCasted = Physics.Raycast(transform.position,
    //                                        Vector3.down,
    //                                        out hitInfo,
    //                                        .1f);
    //                                        // _pushPullLayer);

    //    if (bUnderCasted)
    //    {
    //        if (hitInfo.collider.GetComponent<InteractableObject>() != null)
    //        {
    //            var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

    //            if (interactableObject.SidePassable)
    //                return 3;
    //        }
    //    }

    //    bool bBackCasted = Physics.Raycast(origin,
    //                                PlayerMovement.DirectionToVector(mController.Movement.OppositeDirection),
    //                                out hitInfo,
    //                                _pushPullMaxDistance);
    //                                // _pushPullLayer);

    //    if(bBackCasted)
    //    {
    //        if (hitInfo.collider.GetComponent<InteractableObject>() != null)
    //        {
    //            var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

    //            if (interactableObject.SidePassable)
    //                return 0;
    //        }
    //    }

    //    return -1;
    //}

    //private void updateSidePassableObstacle(int type, RaycastHit hitInfo)
    //{
    //    if(hitInfo.collider == null)
    //        return;

    //    if (hitInfo.collider.GetComponent<InteractableObject>() == null)
    //        return;

    //    // front
    //    if (type == 1)
    //    {
    //        BoxCollider boxCollider = hitInfo.collider.GetComponent<BoxCollider>();
    //        Bounds bounds = boxCollider.bounds;
    //        Vector3 characterPos = transform.position;

    //        if (characterPos.z > -_pushPullZDistance)
    //        {
    //            Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
    //            targetPos.y = 0f;
    //            targetPos.z = -_pushPullZDistance;

    //            Vector3 direction = targetPos - characterPos;
    //            Vector3 normalized = direction.normalized;

    //            Vector3 velocity = mController.Movement.Velocity;
    //            velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
    //            mController.Movement.SetVelocity(velocity);
    //        }
    //    }
    //    // side
    //    else if (type == 2)
    //    {
    //        Vector3 velocity = mController.Movement.Velocity;
    //        velocity.z = 0f;
    //        mController.Movement.SetVelocity(velocity);

    //        //PushPullObject pushPullObject = hitInfo.collider.GetComponent<PushPullObject>();

    //        //if (mController.InputHandler.IsInteracting)
    //        //{
    //        //    PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
    //        //    pushPullState.SetPushPullObject(pushPullObject);
    //        //    mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
    //        //}

    //        //// Climb Object Up
    //        //if (mController.InputHandler.MoveInput.y > .1f)
    //        //{
    //        //    PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
    //        //    pushPullState.SetClimbObject(pushPullObject, climbUp: true);
    //        //    mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
    //        //}
    //    }
    //    // under
    //    else if (type == 3)
    //    {
    //        //// Climb Object Down
    //        //if (mController.InputHandler.MoveInput.y < -.1f)
    //        //{
    //        //    PushPullObject pushPullObject = hitInfo.collider.GetComponent<PushPullObject>();

    //        //    PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
    //        //    pushPullState.SetClimbObject(pushPullObject, climbUp: false);
    //        //    mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
    //        //}
    //    }
    //    // back
    //    else if (type == 0)
    //    {
    //        BoxCollider boxCollider = hitInfo.collider.GetComponent<BoxCollider>();
    //        Bounds bounds = boxCollider.bounds;
    //        Vector3 characterPos = transform.position;

    //        if (characterPos.z < 0f)
    //        {
    //            Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
    //            targetPos.x += (characterPos.x < bounds.center.x) ? -_pushPullMaxDistance : _pushPullMaxDistance;
    //            targetPos.y = 0f;
    //            targetPos.z = 0f;

    //            Vector3 direction = targetPos - characterPos;
    //            Vector3 normalized = direction.normalized;

    //            Vector3 velocity = mController.Movement.Velocity;
    //            velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
    //            mController.Movement.SetVelocity(velocity);
    //        }
    //    }
    //    // none
    //    else
    //    {
    //        Vector3 velocity = mController.Movement.Velocity;
    //        velocity.z = 0f;
    //        mController.Movement.SetVelocity(velocity);
    //    }
    //}

    private void OnDrawGizmosSelected()
    {
        if(EditorApplication.isPlaying == false)
            return;


        Vector3 pathOrigin = transform.position;
        pathOrigin.y += _interactableOffsetY;
        pathOrigin.z = 0f;

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
}
