using PropMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static PlayerMovement;

public class PlayerMoveState : PlayerStateBase
{
    public float PathZPosition => mPathZPosition;
    public float InteractableMaxDistance => _interactableMaxDistance;
    public float InteractableOffsetY => _interactableOffsetY;
    public float InteractableDistance => _interactableDistance;
    public LayerMask FrontWallCheckLayer => _frontWallCheckLayer;
    public float FrontWallCheckDistance => _frontWallCheckDistance;
    public float SidePassZDistance => _sidePassZDistance;

    [Header("Debug")]
    [SerializeField] private bool _drawInteractableRay = true;
    [SerializeField] private bool _drawFrontWallCheckRay = true;
    [SerializeField] private bool _drawGroundNormal = true;

    [Header("Locomotion")]
    [SerializeField] private bool _moveRootMotion = false;
    [SerializeField] private RotationHandler.EType _rotationType = RotationHandler.EType.AnimationCurve;
    [SerializeField] private TwoBoneIKConstraint _leftLegIKConstraint;
    [SerializeField] private TwoBoneIKConstraint _rightLegIKConstraint;
    [SerializeField] private float _footIKMaxDistance = 1f;
    [SerializeField] private AnimationCurve _idleTurnRotationCurve;
    [SerializeField] private AnimationCurve _idleTurnPositionCurve;
    [SerializeField] private AnimationCurve _runTurnRotationCurve;
    [SerializeField] private AnimationCurve _runTurnPositionCurve;

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

    private RotationHandler mRotationHandler = new RotationHandler();

    private bool mbEnterToIdle = false;     // MoveState로 전환될 때 키입력 초기화
    private float mDefaultHeight;           // 낙하 상태로 전환할 때 기준이 되는 높이
    private float mPathZPosition = 0f;    // 캐릭터가 지나가는 길의 z위치를 저장하는 변수
    private bool mbFrontWall = false;
    private Bounds mFrontWallBounds;
    private RaycastHit mFrontWallHitInfo;
    private float mTimeOffset = 0f;

    private Terrain mTerrain;
    private Ground mGround;
    private Collider mGroundCollider;
    private Vector3 mGroundNormal = Vector3.zero;
    private float mSlopeAngle = 30f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        //mRotationHandler.Init(mController);
        //mRotationHandler.SetType(_rotationType);
        //mRotationHandler.AnimationCurveRotation.SetAnimationCurve(_idleTurnPositionCurve, _idleTurnRotationCurve, _runTurnPositionCurve, _runTurnRotationCurve);
    }

    public override void EnterState()
    {
        if(!mController.Animator.Play(AnimState.Run))
        {
            // mController.Animator.CrossFadeRun(mEnterTransitionSettings);
        }

        mController.Animator.SetRunning(true);

        mDefaultHeight = transform.position.y;

        mbLeftFootIK = false;
        mbLeftFootIKFullWeight = false;
        mbLeftTransition = false;
        mbRightFootIK = false;
        mbRightFootIKFullWeight = false;
        mbRightTransition = false;

        //mController.Animator.onAnimatorMove -= updateAnimatorMove;
        //mController.Animator.onAnimatorMove += updateAnimatorMove;

        mController.Animator.onAnimationIK -= updateAnimatorIK;
        mController.Animator.onAnimationIK += updateAnimatorIK;

        mController.Animator.AnimationEventReceiver.onFrontFoot += updateFrontFoot;

        if(mbEnterToIdle)
        {
            mController.InputHandler.ResetMoveInput();
            mbEnterToIdle = false;
        }
    }

    public override void ExitState()
    {
        mController.Animator.SetRunning(false);

        mController.Animator.onAnimatorMove -= updateAnimatorMove;
        mController.Animator.onAnimationIK -= updateAnimatorIK;
        _leftLegIKConstraint.weight = 0f;
        _rightLegIKConstraint.weight = 0f;

        // mRotationHandler.EndRotation();
    }

    public override void Tick()
    {
        // To Turn
        if(mController.CheckOppositeInputX())
        {
            mController.StateMachine.SwitchState<PlayerTurnState>((turnState) =>
            {
                turnState.SetTurnType(PlayerTurnState.ETurnType.Run);
            });

            return;
        }

        // To RunToIdle
        if(mController.InputHandler.GetInputRawMagnitude().x < .1f)
        {
            mController.StateMachine.SwitchState<PlayerRunToIdleState>();

            return;
        }

        // To Jump
        if (mController.InputHandler.JumpPressed)
        {
            mController.InputHandler.ResetJump();

            var climbLedgeState = mController.StateMachine.GetStateBase<PlayerClimbLedgeState>();

            if (climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out Collider detectedCollider) == 1)
            {
                climbLedgeState.SetInfo(climbLedgeInfo);
                mController.StateMachine.SwitchState<PlayerClimbLedgeState>();

                return;
            }
            else
            {
                mController.StateMachine.SwitchState<PlayerRunJumpState>();

                return;
            }
        }

        // Move
        bool bFrontWall = checkWallForMove(out Vector2 resultMoveInput);

        if (bFrontWall)
        {
            if (mController.Movement.IsMoveInputToCharacterDirection(mController.InputHandler.MoveInputRaw))
            {
                mController.Animator.SetFrontWall(true);
            }
            else
            {
                mController.Animator.SetFrontWall(false);
            }
        }
        else
        {
            mController.Animator.SetFrontWall(false);
        }

        if (_moveRootMotion)
        {
            var deltaPosition = mController.Animator.Animator.deltaPosition;
            // deltaPosition.x *= 2f;
            deltaPosition.z = 0f;
            transform.position += deltaPosition;
            // Debug.Log($"deltaPosition: {deltaPosition}, resultPosition: {transform.position}");
        }
        else
        {
            Vector2 finalMoveInput = resultMoveInput;

            //// if (mbRotating)
            //// if(mRotationHandler.State == RotationHandler.EState.Rotating)
            //if(mRotationHandler.AnimationCurveRotation.PositionCurveActive)
            //    finalMoveInput.x = 0f;

            mController.Movement.Move(finalMoveInput);
            // Debug.Log("MoveInputX: " + resultMoveInput.x); 

            // Debug.Log($"velocity: {mController.Movement.Velocity}, moveInput: {resultMoveInput}");

        }

        mController.Animator.SetInputXMagnitude(Mathf.Abs(resultMoveInput.x));
        mController.Animator.SetInputXRaw(mController.InputHandler.MoveInputRaw.x);
        mController.Animator.SetInputX(Mathf.Abs(mController.InputHandler.MoveInputRaw.x) > .1f);
        mController.Animator.SetHorizontal((4f - Mathf.Abs(mController.Movement.Velocity.x) / 4f));
        // mController.Movement.Move(mController.InputHandler.MoveInput);
        //mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));
        // Debug.Log($"moveInput: {resultMoveInput}");

        //AnimatorStateInfo stateInfo = mController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        //if(stateInfo.IsTag("Run"))
        //{
        //    float decimalTime = stateInfo.normalizedTime - (int)stateInfo.normalizedTime;

        //    if (decimalTime > .35f && decimalTime < .9f)
        //    {
        //        mController.Animator.SetIsLeftFoot(true);
        //    }
        //    else
        //    {
        //        mController.Animator.SetIsLeftFoot(false);
        //    }
        //}

        mController.Animator.SetMoveInputXTapped(mController.InputHandler.MoveInputXTapped);
        mController.Animator.SetMoveInputXPressed(mController.InputHandler.MoveInputXPressed);
        mController.Animator.SetMoveInputXHeld(mController.InputHandler.MoveInputXHeld);
        mController.Animator.SetMoveInputYTapped(mController.InputHandler.MoveInputYTapped);
        mController.Animator.SetMoveInputYPressed(mController.InputHandler.MoveInputYPressed);
        mController.Animator.SetMoveInputYHeld(mController.InputHandler.MoveInputYHeld);

        #region Rotation
        //// Rotation
        //mRotationHandler.Update();
        #endregion

        updateFootIK();

        //mController.Animator.SetInputXMagnitude(Mathf.Abs(resultMoveInput.x));
        //mController.Animator.SetInputXRaw(mController.InputHandler.MoveInputRaw.x);
        //mController.Animator.SetInputX(Mathf.Abs(mController.InputHandler.MoveInputRaw.x) > .1f);

        #region Jump
        //// Jump
        //if (mController.InputHandler.JumpPressed)
        //{
        //    if (mController.Movement.IsGrounded)
        //    {
        //        // var climbLedgeState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbLedge) as PlayerClimbLedgeState;
        //        var climbLedgeState = mController.StateMachine.GetStateBase<PlayerClimbLedgeState>();

        //        // if (climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out RaycastHit ledgeHitInfo))
        //        if (climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out Collider detectedCollider) == 1)
        //        {
        //            climbLedgeState.SetInfo(climbLedgeInfo);
        //            // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
        //            mController.StateMachine.SwitchState<PlayerClimbLedgeState>();

        //            return;
        //        }
        //        else
        //        {
        //            // 점프 입력이 됐을 때 이동 입력이 있으면 무조건 RunJump
        //            if (mController.InputHandler.MoveInput.x > .01f || mController.InputHandler.MoveInput.x < -.01f)
        //            {
        //                // PlayerRunJumpState runJumpState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.RunJump) as PlayerRunJumpState;
        //                PlayerRunJumpState runJumpState = mController.StateMachine.GetStateBase<PlayerRunJumpState>();
        //                runJumpState.SetDefaultHeight(mDefaultHeight);
        //                // runJumpState.SetTurningCW(mbRotatingCW);

        //                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
        //                mController.StateMachine.SwitchState<PlayerRunJumpState>();
        //                mController.InputHandler.ResetJump();
        //            }
        //            else
        //            {
        //                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.IdleJump);
        //                mController.StateMachine.SwitchState<PlayerJumpState>();
        //                mController.InputHandler.ResetJump();

        //            }

        //            return;
        //        }
        //    }
        //}
        #endregion

        // Fall
        PlayerFallState fallState = mController.StateMachine.GetStateBase<PlayerFallState>();

        if (!mController.Movement.IsGrounded && !mController.Movement.Jumping)
        // if (fallState.CheckFall())
        // if (mController.Movement.Velocity.y < -1f)
        {
            mController.StateMachine.SwitchState<PlayerFallState>((fallState) =>
            {
                fallState.SetFallIndex(0);
                fallState.SetFallType(PlayerFallState.EFallType.FromRun);
            });

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
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, .1f, LayerMask.GetMask("Ground")))
        {
            mGroundNormal = hitInfo.normal;
            float slopeAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
            // Debug.Log(slopeAngle);

            // var slopeState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Slope) as PlayerSlopeState;
            var slopeState = mController.StateMachine.GetStateBase<PlayerSlopeState>();

            if (slopeAngle > slopeState.SlopeAngle)
            {
                // Slope State로 전환하는 코드 작성
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Slope);
                mController.StateMachine.SwitchState<PlayerSlopeState>();
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

    public void EnterToRun(float startMoveInputX)
    {
        // Debug.Log($"currentMoveInput: {mController.InputHandler.MoveInput}, startMoveInputX: {startMoveInputX}");
        Vector3 moveInput = mController.InputHandler.MoveInput;
        // moveInput.x = mController.Movement.Direction == EDirection.Right ? startMoveInputX : -startMoveInputX;
        moveInput.x = startMoveInputX;
        mController.InputHandler.SetMoveInput(moveInput);
    }

    private void Start()
    {
        mPathZPosition = transform.position.z;
    }

    private void onFootStep()
    {
        if(mController.Movement.Ground == null)
        {
            AudioManager.instance.PlayOneShot("FootStepConcrete");
            return;
        }

        mGround.PlayFootStepSound();

        //switch (mGround.Type)
        //{
        //    case Ground.EGroundType.Concrete:
        //        AudioManager.instance.PlayOneShot("FootStepConcrete");
        //        break;
        //    case Ground.EGroundType.Wood:
        //        AudioManager.instance.PlayOneShot("FootStepWood");
        //        break;
        //    default:
        //        AudioManager.instance.PlayOneShot("FootStepConcrete");
        //        break;
        //}
    }

    private void updateFrontFoot(int footIndex)
    {
        mController.Animator.SetFootPosition(footIndex);

        // Debug.Log($"{footIndex}");
        switch (footIndex)
        {
            case 0:
            case 3:
                mController.Animator.SetIsLeftFoot(true);
                break;
            case 1:
            case 2:
                mController.Animator.SetIsLeftFoot(false);
                break;
        }
    }

    private bool checkOppositeInputX()
    {
        bool bOppositePressed = mController.InputHandler.MoveInputXOppositePressed;
        mController.InputHandler.ResetMoveInputXOppositePressed();

        if (bOppositePressed)
            return true;

        EDirection InputXDirection = PlayerMovement.MoveInputXToDirection(mController.InputHandler.MoveInput.x);

        // if(mController.InputHandler.MoveInputXTapped && InputXDirection == mController.Movement.OppositeDirection)
        if(Mathf.Abs(mController.InputHandler.MoveInput.x) > .001f && InputXDirection == mController.Movement.OppositeDirection)
            return true;

        return false;
    }

    private bool checkGround(out Ground ground)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = transform.position;
        // pathOrigin.y += _interactableOffsetY;
        // pathOrigin.z = 0f;
        pathOrigin.z = mPathZPosition;

        // 현재 캐릭터의 위치
        Vector3 characterOrigin = transform.position;
        characterOrigin.y += _interactableOffsetY;

        // 현재 캐릭터 발을 기준으로 한 위치
        Vector3 characterFeetOrigin = transform.position;

        bool bCasted = Physics.Raycast(pathOrigin,
                                        Vector3.down,
                                        out RaycastHit hitInfo,
                                        mController.Movement.GroundCheckRadius,
                                        mController.Movement.GroundLayer);

        if(bCasted)
        {
            ground = hitInfo.collider.GetComponent<Ground>();
        }
        else
        {
            ground = null;
        }

        return bCasted;
    }

    private bool checkWallForMove(out Vector2 resultMoveInput)
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

            // mController.Animator.SetPush();
            mFrontWallHitInfo = hitInfo;
        }

        resultMoveInput = moveInput;
        mbFrontWall = bFrontCasted;
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

                // PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase<PlayerLadderState>();
                LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

                // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
                if (ladderStateBase.IsOverRange(ladderHandler))
                    continue;

                ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                mController.StateMachine.SwitchState<PlayerLadderState>();
                return true;
            }
            // Top
            else if (mController.InputHandler.MoveInput.y < -.1f)
            {
                if (ladderCollider.tag != "LadderTop")
                    continue;

                // PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase<PlayerLadderState>();
                LadderHandler ladderHandler = ladderCollider.GetComponentInParent<LadderHandler>();
                ladderStateBase.SetLadder(ladderHandler, startFromBottom: false);

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                mController.StateMachine.SwitchState<PlayerLadderState>();
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
            // float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);
            float distanceToEdge = Mathf.Abs(characterPos.x - hitInfo.point.x);

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

            // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
            PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase<PlayerPushPullState>();

            // PushPull Front
            if (interactableObject.Pushable && distanceToEdge < _interactableDistance && mController.InputHandler.IsInteracting)
            {
                // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(1);
                pushPullState.SetPushPoint(hitInfo.point);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                mController.StateMachine.SwitchState<PlayerPushPullState>();
            }

            // PushPull Front (Auto Push)
            if (interactableObject.Pushable && distanceToEdge < pushPullState.FrontPushPullDistance && Mathf.Abs(mController.InputHandler.MoveInput.x)> .1f)
            {
                // mController.Animator.SetPush();
                // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(2);
                pushPullState.SetPushPoint(hitInfo.point);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                mController.StateMachine.SwitchState<PlayerPushPullState>();

            }

            // Climb Object Up
            if (interactableObject.CanClimb && distanceToEdge < _interactableDistance && mController.InputHandler.MoveInput.y > .1f)
            {
                // PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase<PlayerClimbObjectState>();
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                climbObjectState.SetHitPoint(hitInfo.point);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                mController.StateMachine.SwitchState<PlayerClimbObjectState>();
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

            // Interact
            if (distanceToEdge < interactableObject.InteractionDistance && mController.InputHandler.IsInteracting)
            {
                // PlayerInteractState interactState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Interact) as PlayerInteractState;
                PlayerInteractState interactState = mController.StateMachine.GetStateBase<PlayerInteractState>();

                interactState.SetInteractableObject(interactableObject);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Interact);
                mController.StateMachine.SwitchState<PlayerInteractState>();
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
                // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase<PlayerPushPullState>();
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(0);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                mController.StateMachine.SwitchState<PlayerPushPullState>();
            }

            // Climb Object Up
            if (interactableObject.CanClimb && mController.InputHandler.MoveInput.y > .1f)
            {
                // PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase<PlayerClimbObjectState>();
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                mController.StateMachine.SwitchState<PlayerClimbObjectState>();
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

                // PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase<PlayerClimbObjectState>();
                climbObjectState.SetClimbObject(interactableObject, climbUp: false);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                mController.StateMachine.SwitchState<PlayerClimbObjectState>();
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

    private void updateFootIK()
    {
        Animator animator = mController.Animator.Animator;

        float valueLeftFoot = animator.GetFloat("LeftFootCurve");

        float weightLeftFoot = valueLeftFoot;
        // float weightLeftFoot = Mathf.Clamp01(valueLeftFoot / .5f);
        //_LeftLegIKConstraint.weight = (value > 0.01f)? 1f: 0f;
        // _leftLegIKConstraint.weight = weightLeftFoot;
        // _LeftLegIKConstraint.data.targetPositionWeight = weight;
        // Debug.Log($"Left Foot IK Weight: {valueLeftFoot}, multiplied weight: {weightLeftFoot}");

        if(animator.IsInTransition(0))
        {
            if (mbLeftTransition == false)
            {
                mbLeftTransition = true;
                mbLeftFootIK = false;
                mbLeftFootIKFullWeight = false;
            }
        }
        else
        {
            if (mbLeftTransition == true)
                mbLeftTransition = false;
        }

        // if(valueLeftFoot > .01f)
        if (weightLeftFoot > .99f)
        {
            if (mbLeftFootIKFullWeight == false)
            {
                mbLeftFootIKFullWeight = true;
                Vector3 leftFootPos = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

                // if (Physics.Raycast(animator.GetIKPosition(AvatarIKGoal.LeftFoot), Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                if (Physics.Raycast(leftFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {
                    Vector3 footPosition = leftFootPos;
                    //Vector3 footPosition = hitInfo.point;
                    footPosition.y = hitInfo.point.y + .08f;

                    mLeftFootPosition = footPosition;

                    // mLeftFootRotation = Quaternion.LookRotation(mController.Movement.DirectionToVector(), Vector3.up);
                    // Debug.Log($"[{Time.frameCount}] Left Foot IK Updated. Foot Position: {footPosition}");
                }
                else
                {
                    weightLeftFoot = 0f;
                }

                // Debug.Log($"[{Time.frameCount}] Left Foot IK Full Weight. value: {valueLeftFoot}, weight: {weightLeftFoot}");
            }
            else
            {
                Vector3 leftFootPos = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

                if (Physics.Raycast(leftFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {

                }
                else
                {
                    weightLeftFoot = 0f;
                }
            }
        }
        else if (weightLeftFoot > .01f)
        {
            if (mbLeftFootIK == false)
            {
                mbLeftFootIK = true;

                // Debug.Log($"[{Time.frameCount}] Start Left Foot IK.");
            }
            else
            {
                Vector3 leftFootPos = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                Vector3 handlePos = mLeftFootPosition;

                var distance = Vector3.Distance(handlePos, leftFootPos);

                // weight에 거리에 비례한 보정 값을 곱해줌
                float multiplier = 1f - Mathf.Clamp01(distance / _footIKMaxDistance);

                // weightLeftFoot *= multiplier;
                // Debug.Log($"distance: {distance}, multiplier: {multiplier}, weight: {weightLeftFoot}");
                // Debug.Log($"[{Time.frameCount}] Left Foot IK Weight: {valueLeftFoot}, multiplied weight: {weightLeftFoot}");
            }

            if (mbLeftFootIKFullWeight == false)
            {
                Vector3 leftFootPos = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

                // if (Physics.Raycast(animator.GetIKPosition(AvatarIKGoal.LeftFoot), Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                if (Physics.Raycast(leftFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {
                    Vector3 footPosition = leftFootPos;
                    //Vector3 footPosition = hitInfo.point;
                    footPosition.y = hitInfo.point.y + .08f;

                    mLeftFootPosition = footPosition;

                    // mLeftFootRotation = Quaternion.LookRotation(mController.Movement.DirectionToVector(), Vector3.up);
                    // Debug.Log($"[{Time.frameCount}] Left Foot IK Updated. Foot Position: {footPosition}");

                    if (Vector3.Distance(leftFootPos, hitInfo.point) < .09f)
                    {
                        mbLeftFootIKFullWeight = true;
                        // Debug.Log($"[{Time.frameCount}] Left Foot touched ground before Full Weight.");
                    }
                }
                else
                {
                    weightLeftFoot = 0f;
                }
            }
            else
            {
                Vector3 leftFootPos = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

                if (Physics.Raycast(leftFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {
                    
                }
                else
                {
                    weightLeftFoot = 0f;
                }
            }
        }
        else
        {
            if (mbLeftFootIK == true)
            {
                mbLeftFootIK = false;
                mbLeftFootIKFullWeight = false;

                // Debug.Log($"[{Time.frameCount}] End Left Foot IK.");
            }
        }

        _leftLegIKConstraint.weight = weightLeftFoot;
        _leftLegIKConstraint.data.target.position = mLeftFootPosition;
        // _leftLegIKTarget.transform.position = mLeftFootPosition;

        float valueRightFoot = animator.GetFloat("RightFootCurve");

        float weightRightFoot = valueRightFoot;
        // float weightRightFoot = Mathf.Clamp01(valueRightFoot / .5f);
        //_rightLegIKConstraint.weight = (value > 0.01f)? 1f: 0f;
        // _rightLegIKConstraint.weight = weightRightFoot;
        // _rightLegIKConstraint.data.targetPositionWeight = weight;

        if(animator.IsInTransition(0))
        {
            if(mbRightTransition == false)
            {
                mbRightTransition = true;
                mbRightFootIK = false;
                mbRightFootIKFullWeight = false;
            }
        }
        else
        {
            if (mbRightTransition == true)
                mbRightTransition = false;
        }

        // if(valueRightFoot > .01f)
        if (weightRightFoot > .99f)
        {
            if (mbRightFootIKFullWeight == false)
            {
                mbRightFootIKFullWeight = true;

                Vector3 rightFootPos = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                if (Physics.Raycast(rightFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {
                    Vector3 footPosition = rightFootPos;
                    // Vector3 footPosition = hitInfo.point;
                    footPosition.y = hitInfo.point.y + .08f;

                    mRightFootPosition = footPosition;
                    // mRightFootRotation = Quaternion.LookRotation(mController.Movement.DirectionToVector(), Vector3.up);
                }
                else
                {
                    weightRightFoot = 0f;
                }
            }
            else
            {
                Vector3 rightFootPos = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                if (Physics.Raycast(rightFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {

                }
                else
                {
                    weightRightFoot = 0f;
                }
            }
        }
        if(weightRightFoot > .01f)
        {
            if (mbRightFootIK == false)
            {
                mbRightFootIK = true;


            }
            else
            {
                Vector3 rightFootPos = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                Vector3 handlePos = mRightFootPosition;

                

                var distance = Vector3.Distance(handlePos, rightFootPos);

                // weight에 거리에 비례한 보정 값을 곱해줌
                float multiplier = 1f - Mathf.Clamp01(distance / _footIKMaxDistance);

                // weightRightFoot *= multiplier;
                // Debug.Log($"distance: {distance}, multiplier: {multiplier}, weight: {weightLeftFoot}");
            }

            if(mbRightFootIKFullWeight == false)
            {
                Vector3 rightFootPos = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                if (Physics.Raycast(rightFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {
                    Vector3 footPosition = rightFootPos;
                    // Vector3 footPosition = hitInfo.point;
                    footPosition.y = hitInfo.point.y + .08f;

                    mRightFootPosition = footPosition;
                    // mRightFootRotation = Quaternion.LookRotation(mController.Movement.DirectionToVector(), Vector3.up);

                    if (Vector3.Distance(rightFootPos, hitInfo.point) < .09f)
                        mbRightFootIKFullWeight = true;
                }
                else
                {
                    weightRightFoot = 0f;
                }
            }
            else
            {
                Vector3 rightFootPos = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                if (Physics.Raycast(rightFootPos, Vector3.down, out RaycastHit hitInfo, .5f, mController.Movement.GroundLayer))
                {
                    
                }
                else
                {
                    weightRightFoot = 0f;
                }
            }
        }
        else
        {
            if(mbRightFootIK == true)
            {
                mbRightFootIK = false;
                mbRightFootIKFullWeight = false;
            }
        }

        _rightLegIKConstraint.weight = weightRightFoot;
        _rightLegIKConstraint.data.target.position = mRightFootPosition;
        // _rightLegIKTarget.transform.position = mRightFootPosition;

        // Debug.Log($"[{Time.frameCount}] Right Foot IK Weight: {weightRightFoot}");
    }

    private void updateAnimatorMove()
    {
        Animator animator = mController.Animator.Animator;

        Vector3 deltaPosition = animator.deltaPosition;
        deltaPosition.y = 0f; 
        deltaPosition.z = 0f;

        transform.position += deltaPosition;
    }

    private Vector3 mLeftFootPosition;
    private Quaternion mLeftFootRotation;
    private bool mbLeftFootIK = false;
    private Vector3 mRightFootPosition;
    private bool mbRightFootIK = false;
    private bool mbLeftFootIKFullWeight = false;
    private bool mbRightFootIKFullWeight = false;
    private bool mbLeftTransition = false;
    private bool mbRightTransition = false;

    private void updateAnimatorIK()
    {
        //if (mbFrontWall)
        //{
        //    Vector3 leftHandPosition = mController.Animator.Animator.GetIKPosition(AvatarIKGoal.LeftHand);
        //    leftHandPosition.x = mFrontWallHitInfo.point.x;
        //    leftHandPosition.y = mFrontWallHitInfo.collider.bounds.max.y;
        //    mController.Animator.Animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
        //    mController.Animator.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);

        //    //Vector3 rightHandPosition = mAnimator.GetIKPosition(AvatarIKGoal.RightHand);
        //    //rightHandPosition.y = mStepPositions[mRightHandStepNum].y;
        //    //mAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPosition);
        //    //mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, mRightHandIKWeight);
        //}
        //else
        //{
        //    //Vector3 leftHandPosition = mController.Animator.Animator.GetIKPosition(AvatarIKGoal.LeftHand);
        //    //leftHandPosition.x = mFrontWallHitInfo.point.x;
        //    //mController.Animator.Animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
        //    mController.Animator.Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);

        //}
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

    private void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying == false)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.down * .5f);
        //Gizmos.DrawWireSphere(mController.Animator.Animator.GetIKPosition(AvatarIKGoal.LeftFoot), .02f);
        //Gizmos.color = Color.cyan;
        //Gizmos.DrawWireSphere(mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, .02f);

    }

    private void OnDrawGizmosSelected()
    {
        if(EditorApplication.isPlaying == false)
            return;

        if (_drawInteractableRay)
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

            // Debug.Log($"{pathOrigin}, {dir}");

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
