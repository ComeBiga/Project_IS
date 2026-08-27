using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerClimbLedgeState : PlayerStateBase
{
    public struct ClimbLedgeInfo
    {
        public Bounds ledgeBounds;
        public int checkIndex;
        public LedgeHandler ledgeHandler;
        public Vector3 nearestLedgePoint;
        public Vector3 raycastOrigin;
        public float gapToLedge;
        public float maxGapToLedge;
        public Vector3 normal;
    }

    public enum EAnimationType { RootMotion, PositionFixedOnLedge }

    public MultiAimConstraint HeadAimIK => _headAimIK;
    public TwoBoneIKConstraint LeftHandIK => _leftHandIK;
    public TwoBoneIKConstraint RightHandIK => _rightHandIK;
    public float RaycastDistance => _raycastDistance;
    public float HandIKTargetOffsetZ => _handIKTargetOffsetZ;

    [Header("Debug")]
    [SerializeField] private bool _drawRay = true;

    [SerializeField] private float _deltaPositionYSpeed = 1f;

    [Header("Animation Setting")]
    [SerializeField] private EAnimationType _animationType = EAnimationType.RootMotion;
    [SerializeField] private float _hangingCriticalDuration = .833f;
    [SerializeField] private float _hangingClimbLedgeDuration = 1f;
    [SerializeField] private float _criticalDuration = .333f;
    [SerializeField] private float _ClimbLedgeDuration = .5f;

    [Header("Lerp X Offset Settings")]
    [SerializeField] private float _lerpXOffset = .2f;
    [SerializeField] private float _lerpXOffsetOverHead = .2f;
    [SerializeField] private float _lerpXOffsetChest = .2f;
    [SerializeField] private float _lerpXOffsetStomach = .2f;
    [SerializeField] private float _lerpXOffsetKnee = .2f;

    [Header("Lerp Y Offset Settings")]
    [SerializeField] private float _lerpYOffsetOverHead = 1f;
    [SerializeField] private float _lerpYOffsetChest = 1f;
    [SerializeField] private float _lerpYOffsetStomach = 1f;
    [SerializeField] private float _lerpYOffsetKnee = 1f;

    [Header("Raycast Settings")]
    [SerializeField] private float _raycastDistance = .5f;
    [Range(2, 10)]
    [SerializeField] private int _raycastCount = 2;
    [SerializeField] private float _ledgeRange = .3f;
    [SerializeField] private LayerMask _raycastLayer;

    [Header("Detection Setting")]
    [Obsolete] [SerializeField] private BoxCollider _detectionBoxCollider;
    [SerializeField] private float _detectionBoxCenterHeight;
    [SerializeField] private float _detectionBoxCenterDistance;
    [SerializeField] private Vector3 _detectionBoxSize;

    [Header("Normalized Time Settings")]
    [SerializeField] private float _exitNormalizedTimeOverHead = .8f;
    [SerializeField] private float _exitNormalizedTimeChest = .745f;
    [SerializeField] private float _exitNormalizedTimeStomach = .68f;
    [SerializeField] private float _exitNormalizedTimeKnee = .53f;

    [Header("IK Settings")]
    [SerializeField] private MultiAimConstraint _headAimIK;
    [SerializeField] private TwoBoneIKConstraint _leftHandIK;
    [SerializeField] private TwoBoneIKConstraint _rightHandIK;
    [SerializeField] private float _handIKTargetOffsetX = .1f;
    [SerializeField] private float _handIKTargetOffsetY = .02f;
    [SerializeField] private float _handIKTargetOffsetZ = .2f;

    private enum EClimbState { Hanging, LerpDelay, Lerp, Climb }

    private Animator mAnimator;
    private ClimbLedgeInfo mClimbLedgeInfo;
    private Bounds mLedgeBounds;
    private Ground mGround;
    private EClimbState mClimbState;

    private bool mbClimb = false;
    private bool mbClimbWithoutInput = false;
    private float mStartMoveInputX;
    private float mClimbTimer = 0f;
    private float mCriticalDuration = float.MaxValue;
    private float mClimbDuration = float.MaxValue;

    private Vector3 mDeltaPosition;

    private Vector3 mHangingVelocity = Vector3.zero;
    private bool mbEnterAnimatorMove = false;
    private bool mbStartClimb = false;
    private bool mbStartLerp = false;
    private bool mbLerpDelay = false;
    private bool mbLerpPosition = false;
    private float mAnimatorMoveTimer = 0f;
    private float mTargetX = 0f;
    private float mTargetY = 0f;
    private Vector3 mStartPos = Vector3.zero;
    private bool mbLeftHandIK = false;
    private bool mbRightHandIK = false;
    private Vector3 mLeftHandIKPos;
    private Vector3 mRightHandIKPos;
    private Quaternion mLeftHandTargetRot;

    private Vector3 mDebugLedgePoint;

    private Vector3? mBoundMax = null;
    private Vector3? mBoundMin = null;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mHangingVelocity = mController.Movement.Velocity;
        mController.Movement.SetVelocity(Vector3.zero);
        mController.Movement.SetKinematic(true);
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);

        mController.Animator.SetIndex(mClimbLedgeInfo.checkIndex);

        // bool bHanging = mClimbLedgeInfo.checkIndex == 0 || mClimbLedgeInfo.checkIndex == 4 ? true : false;
        bool bHanging = mClimbLedgeInfo.checkIndex == 0 ? true : false;
        mController.Animator.Play(bHanging ? AnimState.ClimbLedge_OverHead_Hanging : AnimState.ClimbLedge_Directly_Critical);

        mController.Animator.SetInputXMagnitude(0f);

        mController.Animator.AnimationEventReceiver.onTouchHand -= onFootStepFromFall;
        mController.Animator.AnimationEventReceiver.onTouchHand += onFootStepFromFall;

        mController.Animator.AnimationEventReceiver.onReleaseHand -= startReleaseHandIKWeight;
        mController.Animator.AnimationEventReceiver.onReleaseHand += startReleaseHandIKWeight;

        mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;
        // mController.Animator.onAnimatorFixedUpdate += updateHandIKWeight;

        mController.Animator.onAnimatorMove -= updateAnimatorMove;
        mController.Animator.onAnimatorMove += updateAnimatorMove;

        mController.Animator.onAnimatorIK -= onAnimatorIK;
        mController.Animator.onAnimatorIK += onAnimatorIK;

        //mController.Animator.onUpdateState -= onUpdateState;
        //mController.Animator.onUpdateState += onUpdateState;

        mbClimb = false;
        mbLeftHandIK = false;
        mbRightHandIK = false;

        mClimbTimer = 0f;
    }

    public override void ExitState()
    {
        mController.Movement.SetKinematic(false);
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);

        mGround = null;
        mbEnterAnimatorMove = false;
        mbStartClimb = false;
        mbClimbWithoutInput = false;
        mController.Animator.AnimationEventReceiver.onTouchHand -= onFootStepFromFall;
        mController.Animator.onAnimatorMove -= updateAnimatorMove;
        mController.Animator.onAnimatorIK -= onAnimatorIK;
        //mController.Animator.onUpdateState -= onUpdateState;

        mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;
        //mController.Animator.onAnimatorFixedUpdate -= releaseHandIKWeight;
        //mController.Animator.onAnimatorFixedUpdate += releaseHandIKWeight;

        StartCoroutine(eApplyTargetPositionAfterPhysicsUpdate());
    }

    public override void Tick()
    {
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        // updateHandIK();
        if (mClimbState != EClimbState.Hanging)
        {
            if(mClimbTimer > mCriticalDuration)
            {
                // To Turn
                if (mController.CheckOppositeInputX())
                {
                    mController.StateMachine.SwitchState<PlayerTurnState>((turnState) =>
                    {
                        turnState.SetTurnType(PlayerTurnState.ETurnType.Run);
                    });
                    return;
                }

                // To Jump
                if(mController.InputHandler.JumpPressed)
                {
                    mController.InputHandler.ResetJump();
                    mController.StateMachine.SwitchState<PlayerRunJumpState>();
                    return;
                }

                // To ClimbLedgeToRun
                if(mController.InputHandler.GetInputRawMagnitude().x > .1f)
                {
                    mController.StateMachine.SwitchState<PlayerClimbLedgeToRunState>((state) =>
                    {
                        state.SetClimbTimer(mClimbTimer, mClimbDuration);
                    });
                    return;
                }

                // To ClimbLedgeToIdle
                // if(mClimbTimer > mClimbDuration)
                {
                    mController.StateMachine.SwitchState<PlayerClimbLedgeToIdleState>((state) =>
                    {
                        state.SetClimbTimer(mClimbTimer, mClimbDuration);
                    });
                    return;
                }
            }

            mClimbTimer += Time.deltaTime;
        }
    }

    public void SetInfo(ClimbLedgeInfo climbLedgeInfo)
    {
        mClimbLedgeInfo = climbLedgeInfo;
    }

    public void EnterFromFall(Ground ground)
    {
        mGround = ground;
    }

    [Obsolete]
    public bool CheckLedge(out ClimbLedgeInfo climbLedgeInfo, out RaycastHit hitInfo)
    {
        climbLedgeInfo = new ClimbLedgeInfo();
        hitInfo = new RaycastHit();

        Vector3 origin = getOrigin();
        Vector3 direction = getDirection();
        LedgeHandler ledgeHandler = null;

        for(int i = 0; i < _raycastCount; i++)
        {
            Vector3 pos = origin;
            pos.y -= getSpacing() * i;

            // bool bCheck = Physics.Raycast(pos, direction, out RaycastHit hitInfo, _raycastDistance, LayerMask.GetMask("Ground"));
            bool bCheck = Physics.Raycast(pos, direction, out hitInfo, _raycastDistance, _raycastLayer, QueryTriggerInteraction.Ignore);

            if (bCheck)
            {
                Bounds bounds = hitInfo.collider.bounds;
                Vector3 ledgePoint = bounds.max;
                ledgePoint.x = hitInfo.point.x;
                float ledgeY = bounds.max.y;
                float range = _ledgeRange;
                mBoundMax = bounds.max;
                mBoundMin = bounds.min;

                if(ledgeHandler == null)
                    ledgeHandler = hitInfo.collider.GetComponent<LedgeHandler>();

                if(ledgeHandler != null)
                {
                    Vector3? ledgePointOrNull = ledgeHandler.GetNearestLedgePoint(pos);

                    if(ledgePointOrNull != null)
                    {
                        ledgePoint = ledgePointOrNull.Value;
                        ledgeY = ledgePoint.y;
                    }
                }

                // if (pos.y > ledgeY - range && pos.y < ledgeY + range)
                {
                    climbLedgeInfo.ledgeBounds = bounds;
                    climbLedgeInfo.checkIndex = i;
                    climbLedgeInfo.ledgeHandler = ledgeHandler;
                    climbLedgeInfo.nearestLedgePoint = ledgePoint;
                    climbLedgeInfo.raycastOrigin = pos;

                    return true;
                }
            }
        }

        climbLedgeInfo.ledgeBounds = new Bounds();
        climbLedgeInfo.checkIndex = -1;
        return false;
    }

    /// <summary>
    /// -1: No Ledge, 0: Too Far, 1: Climbable
    /// </summary>
    /// <param name="climbLedgeInfo"></param>
    /// <param name="detectedCollider"></param>
    /// <returns></returns>
    public int CheckLedge(out ClimbLedgeInfo climbLedgeInfo, out Collider detectedCollider) // , out RaycastHit hitInfo)
    {
        climbLedgeInfo = new ClimbLedgeInfo();
        climbLedgeInfo.ledgeBounds = new Bounds();
        climbLedgeInfo.checkIndex = -1;

        detectedCollider = null;

        //Vector3 center = _detectionBoxCollider.transform.position + _detectionBoxCollider.center;
        //Vector3 halfExtents = _detectionBoxCollider.size / 2f;
        Vector3 center = mCharacterPosition + mMovement.DirectionToVector() * _detectionBoxCenterDistance + Vector3.up * _detectionBoxCenterHeight;
        Vector3 halfExtents = _detectionBoxSize / 2f;
        Quaternion orientation = mMovement.DirectionToRotation();

        Collider[] colliders = Physics.OverlapBox(center,
                                halfExtents,
                                orientation, //_detectionBoxCollider.transform.rotation,
                                _raycastLayer,
                                QueryTriggerInteraction.Ignore);

        if (colliders.Length < 1)
        {
            return -1;
        }


        detectedCollider = colliders[0];
        Bounds detectedColliderBounds = detectedCollider.bounds;
        climbLedgeInfo.ledgeBounds = detectedColliderBounds;
        float ledgeY = detectedColliderBounds.max.y;

        Vector3 ledgePointOrigin = mCharacterPosition;
        ledgePointOrigin.y = detectedColliderBounds.center.y;

        Physics.Raycast(ledgePointOrigin,
                        PlayerMovement.DirectionToVector(mMovement.Direction),
                        out RaycastHit hitInfo,
                        _raycastDistance,
                        _raycastLayer,
                        QueryTriggerInteraction.Ignore);

        Vector3 ledgePoint = new Vector3(hitInfo.point.x, ledgeY, mCharacterPosition.z);
        //Vector3 ledgePoint = detectedColliderBounds.max;
        //float distanceToMin = Mathf.Abs(mCharacterPosition.x - detectedColliderBounds.min.x);
        //float distanceToMax = Mathf.Abs(mCharacterPosition.x - detectedColliderBounds.max.x);
        //ledgePoint.x = (distanceToMin < distanceToMax) ? detectedColliderBounds.min.x : detectedColliderBounds.max.x;

        climbLedgeInfo.nearestLedgePoint = ledgePoint;
        mDebugLedgePoint = hitInfo.point;
        // mDebugLedgePoint = ledgePoint;

        // LedgeHandler
        LedgeHandler ledgeHandler = detectedCollider.GetComponent<LedgeHandler>();

        if (ledgeHandler != null)
        {
            climbLedgeInfo.ledgeHandler = ledgeHandler;
            Vector3? ledgePointOrNull = ledgeHandler.GetNearestLedgePoint(mController.transform.position);

            if (ledgePointOrNull != null)
            {
                ledgePoint = ledgePointOrNull.Value;
                ledgeY = ledgePoint.y;
            }
        }

        //if(ledgePoint.y < mController.transform.position.y + mController.Movement.StepOffset)
        //{
        //    return -1;
        //}

        Vector3 shoulderPos = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        float reachHeight = shoulderPos.y + GetArmLength();
        float maxHeight = reachHeight + .3f;

        if(ledgePoint.y > maxHeight)
        {
            GameDebug.Log($"Ledge Height is Too High! - Ledge Point Y: {ledgePoint.y}, Max Height: {maxHeight}", tag: "CheckLedge MaxHeight");
            return -1;
        }

        if(ledgePoint.y > reachHeight)
        {
            GameDebug.Log($"Ledge Height is Close! - Ledge Point Y: {ledgePoint.y}, Reachable Height: {reachHeight}", tag: "CheckLedge ReachableHeight");
            return 0;
        }

        float distanceToLedge = Mathf.Abs(mCharacterPosition.x - ledgePoint.x);
        // float distanceToLedge = (distanceToMin < distanceToMax) ? distanceToMin : distanceToMax;
        float gapToLedge = distanceToLedge - _raycastDistance;
        climbLedgeInfo.gapToLedge = (gapToLedge < 0f) ? 0f : gapToLedge;
        //climbLedgeInfo.maxGapToLedge = _detectionBoxCollider.size.z - GetArmLength();
        climbLedgeInfo.maxGapToLedge = _detectionBoxSize.z - GetArmLength();

        if (distanceToLedge > _raycastDistance)
        {
            GameDebug.Log($"Ledge distance is Close! - Distance To Ledge: {distanceToLedge}, Raycast Distance: {_raycastDistance}", tag: "CheckLedge Distance");
            return 0;
        }

        Vector3 origin = getOrigin();
        Vector3 direction = getDirection();

        for (int i = 0; i < _raycastCount; i++)
        {
            Vector3 pos = origin;
            pos.y -= getSpacing() * i;

            if(ledgePoint.y > pos.y)
            {
                climbLedgeInfo.checkIndex = i;
                climbLedgeInfo.raycastOrigin = pos;

                break;
            }
        }

        //if(climbLedgeInfo.checkIndex == 0)
        //{
        //    if(mController.Movement.Velocity.y < 2.5f)
        //    {
        //        climbLedgeInfo.checkIndex = 4;
        //    }
        //}

        if (climbLedgeInfo.checkIndex == 0 && mMovement.Velocity.y > 2.5f)
        {
            mbClimbWithoutInput = true;
        }

        GameDebug.Log($"checkIndex: {climbLedgeInfo.checkIndex}, raycastOrigin: {climbLedgeInfo.raycastOrigin}",
            tag: "CheckLedge Detected", category: GameDebug.LogCategory.State, level: GameDebug.LogLevel.Verbose);

        GameDebug.Log($"climb without Input: {mbClimbWithoutInput}, velocity Y: {mMovement.Velocity.y}",
            tag: "CheckLedge Detected", category: GameDebug.LogCategory.State);

        return 1;
    }

    public void EndClimbLedge()
    {
        return;

        if (!mbClimb)
            return;

        mbClimb = false;

        PlayerMoveState moveState = mController.StateMachine.GetStateBase<PlayerMoveState>();

        if (Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
        {
            moveState.EnterToRun(mStartMoveInputX);
        }
        else
        {
            // moveState.EnterToIdle();
        }

        mController.StateMachine.SwitchState<PlayerMoveState>();
    }

    public void ClimbWithoutInput()
    {
        mbClimbWithoutInput = true;
    }

    public Vector3 GetLeftHandIKTargetPosition(ClimbLedgeInfo climbLedgeInfo)
    {
        Vector3 targetPos = climbLedgeInfo.nearestLedgePoint;
        targetPos.x += (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? -_handIKTargetOffsetX : _handIKTargetOffsetX;
        targetPos.y += _handIKTargetOffsetY;
        targetPos.z = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? -_handIKTargetOffsetZ : _handIKTargetOffsetZ;

        if(climbLedgeInfo.checkIndex == 0 || climbLedgeInfo.checkIndex == 4)
        {
            targetPos.x = climbLedgeInfo.nearestLedgePoint.x;
        }

        return targetPos;
    }

    public Vector3 GetRightHandIKTargetPosition(ClimbLedgeInfo climbLedgeInfo)
    {
        Vector3 targetPos = GetLeftHandIKTargetPosition(climbLedgeInfo);
        targetPos.z = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? _handIKTargetOffsetZ : -_handIKTargetOffsetZ;

        return targetPos;
    }

    public float GetDistanceShoulderToLedge(Vector3 ledgePos)
    {
        Vector3 leftShoulderBonePos = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        leftShoulderBonePos.z = 0f;
        Vector3 ledgePoint = ledgePos;
        ledgePoint.z = 0f;
        float distanceShoulderToLedge = Vector3.Distance(leftShoulderBonePos, ledgePoint);

        return distanceShoulderToLedge;
    }

    public float GetArmLength()
    {
        float shoulderToElbowLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position, mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
        float elbowToHandLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position, mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position);
        float armLength = shoulderToElbowLength + elbowToHandLength;

        return armLength;
    }

    private void onFootStepFromFall()
    {
        if (mGround != null)
        {
            Debug.Log("LedgeFootStepSound");
            mGround.PlayFootStepBigSound(volume: .5f);
        }
    }

    private void enterAnimatorMove()
    {
        mClimbState = EClimbState.Lerp;
        mbClimb = true;
        mbStartLerp = false;
        mbLerpPosition = true;
        _headAimIK.weight = 0f;
        mStartPos = mController.transform.position;
        mTargetY = mClimbLedgeInfo.ledgeBounds.max.y;
        mCriticalDuration = _criticalDuration;
        mClimbDuration = _ClimbLedgeDuration;

        if (mClimbLedgeInfo.ledgeHandler != null)
            mTargetY = mClimbLedgeInfo.nearestLedgePoint.y;

        float exitNormalizedTime = 0f;
        float lerpXOffset = 0f;

        switch (mClimbLedgeInfo.checkIndex)
        {
            //case 4:
            //    mClimbState = EClimbState.Hanging;
            //    mbClimb = false;
            //    mbLerpPosition = false;
            //    mController.Animator.SetVertical(0f);
            //    _headAimIK.weight = 1f;
            //    mTargetY -= _lerpYOffsetOverHead;
            //    exitNormalizedTime = _exitNormalizedTimeOverHead;
            //    lerpXOffset = _lerpXOffsetOverHead;
            //    mCriticalDuration = _hangingCriticalDuration;
            //    mClimbDuration = _hangingClimbLedgeDuration;
            //    break;
            case 0:
                mClimbState = EClimbState.Hanging;
                mbClimb = false;
                // mbClimbWithoutInput = mController.Movement.Velocity.y > 2.5f ? true : false;
                mbLerpPosition = false;
                mController.Animator.SetVertical(0f);
                _headAimIK.weight = 1f;
                mTargetY -= _lerpYOffsetOverHead;
                exitNormalizedTime = _exitNormalizedTimeOverHead;
                lerpXOffset = _lerpXOffsetOverHead;
                mCriticalDuration = _hangingCriticalDuration;
                mClimbDuration = _hangingClimbLedgeDuration;
                break;
            case 1:
                mTargetY -= _lerpYOffsetChest;
                exitNormalizedTime = _exitNormalizedTimeChest;
                lerpXOffset = _lerpXOffsetChest;
                break;
            case 2:
                mTargetY -= _lerpYOffsetStomach;
                exitNormalizedTime = _exitNormalizedTimeStomach;
                lerpXOffset = _lerpXOffsetStomach;
                break;
            case 3:
                mTargetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
            default:
                mTargetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
        }

        mTargetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
                    mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset;

        mAnimatorMoveTimer = 0f;
    }

    private void updateAnimatorMove()
    {
        if(!mbEnterAnimatorMove)
        {
            mbEnterAnimatorMove = true;

            enterAnimatorMove();
        }

        float hangingDuration = .2f;

        if(mClimbState == EClimbState.Hanging)
        {
            GameDebug.Log($"{mClimbState}", tag: "ClimbLedge ClimbState");
            if (mController.InputHandler.MoveInputRaw.y > .9f)
            {
                mClimbState = EClimbState.Lerp;
                mbClimb = true;
                mbStartLerp = true;
                // mController.Animator.SetVertical(1f);
                mController.Animator.SetActivate();
                // mController.Animator.Play(AnimState.ClimbLedge_Directly_OverHead);
                mAnimatorMoveTimer = 0f;
                mbLerpPosition = true;

                return;
            }
            else
            {
                // normalizedTime이 일정 비율이 넘으면 상태 전환
                AnimatorStateInfo stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

                if (mAnimatorMoveTimer < hangingDuration)
                {
                    Vector3 targetPos = mController.transform.position;
                    targetPos.x = mClimbLedgeInfo.nearestLedgePoint.x;
                    targetPos.x = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? targetPos.x + .15f : targetPos.x - .15f;
                    targetPos.y = mClimbLedgeInfo.nearestLedgePoint.y - 2.03f;

                    float t = mAnimatorMoveTimer / hangingDuration;
                    Vector3 newPos = mController.transform.position;
                    newPos.x = Mathf.Lerp(mStartPos.x, targetPos.x, t);
                    newPos.y = Mathf.Lerp(mStartPos.y, targetPos.y, t);

                    mController.transform.position = newPos;

                    mAnimatorMoveTimer += Time.fixedDeltaTime;

                    updateHandIKTargetPosition();

                    if (mAnimatorMoveTimer > hangingDuration)
                    {
                        mStartPos.x = targetPos.x;
                        mStartPos.y = targetPos.y;

                        // if (mClimbLedgeInfo.checkIndex == 0)
                        if (mbClimbWithoutInput)
                        {
                            mClimbState = EClimbState.Lerp;
                            mbClimb = true;
                            // mController.Animator.SetVertical(1f);
                            mController.Animator.SetActivate();
                            mAnimatorMoveTimer = 0f;
                            mbLerpPosition = true;
                        }
                    }

                    return;
                }
                else
                {
                    // if (mClimbLedgeInfo.checkIndex == 0)
                    if (mbClimbWithoutInput)
                    {
                        mClimbState = EClimbState.Lerp;
                        mbClimb = true;
                        // mController.Animator.SetVertical(1f);
                        mController.Animator.SetActivate();
                        mAnimatorMoveTimer = 0f;
                        mbLerpPosition = true;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        if(mClimbState == EClimbState.LerpDelay)
        {
            GameDebug.Log($"{mClimbState}", tag: "ClimbLedge ClimbState");
            float delayDuration = .1f;

            if (mAnimatorMoveTimer < delayDuration)
            {
                float t = mAnimatorMoveTimer / delayDuration;

                Vector3 newPos = mController.transform.position;
                // newPos.y = Mathf.Lerp(mStartPos.y, mStartPos.y + (2.03f - .8f), t);
                newPos.y = Mathf.Lerp(mStartPos.y, mStartPos.y + .8f, t);

                mController.transform.position = newPos;

                mAnimatorMoveTimer += Time.fixedDeltaTime;

                if (mAnimatorMoveTimer > delayDuration)
                {
                    mClimbState = EClimbState.Lerp;
                    mStartPos.y = mController.transform.position.y;
                    mbClimb = true;
                    mbLerpDelay = false;
                    mAnimatorMoveTimer = 0f;
                    mController.Animator.SetVertical(1f);
                }
            }

            updateHandIKTargetPosition();

            return;
        }

        if(mbStartLerp)
        {
            mbStartLerp = false;
            mController.Animator.Play(AnimState.ClimbLedge_Directly_OverHead);
        }

        float duration = .2f;
        float duration1 = duration * .8f;
        float duration2 = duration - duration1;

        if(mClimbState == EClimbState.Lerp)
        {
            GameDebug.Log($"{mClimbState}", tag: "ClimbLedge ClimbState");
            if (mAnimatorMoveTimer < duration)
            {
                float t = mAnimatorMoveTimer / duration;

                Vector3 newPos = mCharacterPosition;
                newPos.x = Mathf.Lerp(mStartPos.x, mTargetX, t);
                newPos.y = Mathf.Lerp(mStartPos.y, mTargetY, t);

                // mController.transform.position = newPos;
                mMovement.SetPosition(newPos);
                // mDeltaPosition = newPos - transform.position;

                GameDebug.Log($"StartPos: {mStartPos}, TargetPos: ({mTargetX}, {mTargetY}), newPos: {newPos}",
                    tag: "ClimbLedge Lerp Pos");

                if (mAnimatorMoveTimer > duration)
                {
                    mClimbState = EClimbState.Climb;
                    Vector3 targetPos = mCharacterPosition;
                    targetPos.y = mTargetY;
                    // mController.transform.position = targetPos;
                    mMovement.SetPosition(targetPos);
                    mbLerpPosition = false;
                }

                mAnimatorMoveTimer += Time.fixedDeltaTime;
            }
            else
            {
                mClimbState = EClimbState.Climb;
                Vector3 targetPos = mCharacterPosition;
                targetPos.y = mTargetY;
                mMovement.SetPosition(targetPos);
                mbLerpPosition = false;
            }
        }
        // else
        else if (mClimbState == EClimbState.Climb)
        {
            GameDebug.Log($"{mClimbState}", tag: "ClimbLedge ClimbState");
            if (mbClimb)
            {
                // normalizedTime이 일정 비율이 넘으면 상태 전환
                AnimatorStateInfo stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

                Vector3 deltaPosition = mAnimator.deltaPosition;

                deltaPosition.z = 0f;

                // mController.transform.position += deltaPosition;
                mMovement.AddPosition(deltaPosition);
                // mDeltaPosition = deltaPosition;
            }
        }

        updateHandIKTargetPosition();
    }

    private void startReleaseHandIKWeight()
    {
        mController.Animator.AnimationEventReceiver.onReleaseHand -= startReleaseHandIKWeight;
        mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;

        mController.Animator.onAnimatorFixedUpdate -= releaseHandIKWeight;
        mController.Animator.onAnimatorFixedUpdate += releaseHandIKWeight;
    }

    private void updateHandIKWeight()
    {
        if(mbClimb && !mbStartClimb)
        {
            mbStartClimb = true;

            // _leftHandIK.data.targetPositionWeight = 0f;
        }

        if (mbClimb)
            _headAimIK.weight = 0f;

        float leftHandWeight = mAnimator.GetFloat("LeftHandWeight");

        {
            Vector3 leftShoulderBonePos = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
            leftShoulderBonePos.z = 0f;
            Vector3 ledgePoint = mLeftHandIKPos;
            ledgePoint.z = 0f;
            float distanceShoulderToLedge = Vector3.Distance(leftShoulderBonePos, ledgePoint);
            float shoulderToElbowLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position, mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
            float elbowToHandLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position, mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position);
            float armLength = shoulderToElbowLength + elbowToHandLength;
            float gapToLedge = distanceShoulderToLedge - armLength; // _raycastDistance;
            float maxGapToLedge = mClimbLedgeInfo.maxGapToLedge;
            float gapToLedgeRatio = Mathf.Clamp01(gapToLedge / maxGapToLedge);
            float gapRatio = 1 - gapToLedgeRatio;
            _leftHandIK.weight = gapRatio;

            if(!mbLerpPosition && mbClimb)
                _leftHandIK.data.targetPositionWeight = leftHandWeight;

            Vector3 leftHandBone = mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            Vector3 leftHandTarget = _leftHandIK.data.target.position;
            float distance = Vector3.Distance(leftHandBone, leftHandTarget);

            Vector3 leftMiddleBone = mAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal).position;
            float handSize = Vector3.Distance(leftHandBone, leftMiddleBone);

            float handDistanceRatio = Mathf.Clamp01(distance / handSize);
            float leftHandRotationWeight = 1 - handDistanceRatio;
            // _leftHandIK.data.targetRotationWeight = leftHandRotationWeight;
            _leftHandIK.data.targetRotationWeight = 1f;
        }

        float rightHandWeight = mAnimator.GetFloat("RightHandWeight");

        {
            Vector3 rightShoulderBonePos = mAnimator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
            rightShoulderBonePos.z = 0f;
            Vector3 ledgePoint = mRightHandIKPos;
            ledgePoint.z = 0f;
            float distanceShoulderToLedge = Vector3.Distance(rightShoulderBonePos, ledgePoint);
            float shoulderToElbowLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.RightShoulder).position, mAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm).position);
            float elbowToHandLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm).position, mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position);
            float armLength = shoulderToElbowLength + elbowToHandLength;
            float gapToLedge = distanceShoulderToLedge - armLength; // _raycastDistance;
            float maxGapToLedge = mClimbLedgeInfo.maxGapToLedge;
            float gapToLedgeRatio = Mathf.Clamp01(gapToLedge / maxGapToLedge);
            float resultWeight = 1 - gapToLedgeRatio;
            _rightHandIK.weight = resultWeight;

            if (!mbLerpPosition && mbClimb)
                _rightHandIK.data.targetPositionWeight = rightHandWeight;
        }
    }

    private void releaseHandIKWeight()
    {
        {
            Vector3 leftShoulderBonePos = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
            leftShoulderBonePos.z = 0f;
            Vector3 ledgePoint = mLeftHandIKPos;
            ledgePoint.z = 0f;
            float distanceShoulderToLedge = Vector3.Distance(leftShoulderBonePos, ledgePoint);
            float shoulderToElbowLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position, mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
            float elbowToHandLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position, mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position);
            float armLength = shoulderToElbowLength + elbowToHandLength;
            float gapToLedge = distanceShoulderToLedge - armLength; // _raycastDistance;
            float maxGapToLedge = mClimbLedgeInfo.maxGapToLedge;
            float gapToLedgeRatio = Mathf.Clamp01(gapToLedge / maxGapToLedge);
            float leftHandWeight = 1 - gapToLedgeRatio;
            _leftHandIK.weight = leftHandWeight;

            Vector3 leftHandBone = mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            Vector3 leftHandTarget = _leftHandIK.data.target.position;
            float distance = Vector3.Distance(leftHandBone, leftHandTarget);

            Vector3 leftMiddleBone = mAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal).position;
            float handSize = Vector3.Distance(leftHandBone, leftMiddleBone);

            float handDistanceRatio = Mathf.Clamp01(distance / handSize);
            float leftHandRotationWeight = 1 - handDistanceRatio;
            _leftHandIK.data.targetRotationWeight = leftHandRotationWeight;

            if (leftHandWeight < 0.01f)
            {
                _leftHandIK.weight = 0f;
                _leftHandIK.data.targetPositionWeight = 1f;
                _leftHandIK.data.targetRotationWeight = 1f;
                _rightHandIK.weight = 0f;
                _rightHandIK.data.targetPositionWeight = 1f;
                _rightHandIK.data.targetRotationWeight = 1f;
                mController.Animator.onAnimatorFixedUpdate -= releaseHandIKWeight;
            }
        }

        {
            Vector3 rightShoulderBonePos = mAnimator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
            rightShoulderBonePos.z = 0f;
            Vector3 ledgePoint = mRightHandIKPos;
            ledgePoint.z = 0f;
            float distanceShoulderToLedge = Vector3.Distance(rightShoulderBonePos, ledgePoint);
            float shoulderToElbowLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.RightShoulder).position, mAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm).position);
            float elbowToHandLength = Vector3.Distance(mAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm).position, mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position);
            float armLength = shoulderToElbowLength + elbowToHandLength;
            float gapToLedge = distanceShoulderToLedge - armLength; // _raycastDistance;
            float maxGapToLedge = mClimbLedgeInfo.maxGapToLedge;
            float gapToLedgeRatio = Mathf.Clamp01(gapToLedge / maxGapToLedge);
            float rightHandWeight = 1 - gapToLedgeRatio;
            _rightHandIK.weight = rightHandWeight;

            Vector3 rightHandBone = mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position;
            Vector3 rightHandTarget = _rightHandIK.data.target.position;
            float distance = Vector3.Distance(rightHandBone, rightHandTarget);

            Vector3 rightMiddleBone = mAnimator.GetBoneTransform(HumanBodyBones.RightMiddleDistal).position;
            float handSize = Vector3.Distance(rightHandBone, rightMiddleBone);

            float handDistanceRatio = Mathf.Clamp01(distance / handSize);
            float rightHandRotationWeight = 1 - handDistanceRatio;
            _rightHandIK.data.targetRotationWeight = rightHandRotationWeight;
        }
    }

    private void updateHandIKTargetPosition()
    {
        if(!mbLeftHandIK)
        {
            Vector3 leftHandOrigin = _leftHandIK.data.tip.position;

            //Vector3 targetPos = mClimbLedgeInfo.ledgeBounds.max;
            Vector3 targetPos = GetLeftHandIKTargetPosition(mClimbLedgeInfo);
            //targetPos.x -= .1f;
            //targetPos.y += .02f;
            //// targetPos.z = mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position.z;
            //targetPos.z = -.2f;
            //_leftHandIK.data.target.position = targetPos;
            _leftHandIK.data.target.position = targetPos;
            mLeftHandIKPos = targetPos;

            Transform trElbow = mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Vector3 defaultUp = trElbow.up;
            Vector3 defaultForward = -trElbow.right;
            Quaternion defaultRot = Quaternion.LookRotation(defaultForward, defaultUp);
            // _leftHandIK.data.target.rotation = defaultRot;

            Vector3 normal = Vector3.up;
            Vector3 up = mController.Movement.DirectionToVector();
            // Vector3 forward = Vector3.Cross(up, normal);
            Vector3 forward = -normal;
            Quaternion targetRot = Quaternion.LookRotation(forward, up);
            mLeftHandTargetRot = targetRot;
            _leftHandIK.data.target.rotation = targetRot;

            mController.Animator.onAnimatorFixedUpdate += updateHandIKWeight;

            mbLeftHandIK = true;
        }
        else
        {
            _leftHandIK.data.target.position = mLeftHandIKPos;
        }

        if (!mbRightHandIK)
        {
            Vector3 rightHandOrigin = _rightHandIK.data.tip.position;

            //Vector3 targetPos = mClimbLedgeInfo.ledgeBounds.max;
            Vector3 targetPos = GetRightHandIKTargetPosition(mClimbLedgeInfo);
            //targetPos.x -= .1f;
            //targetPos.y += .02f;
            //// targetPos.z = mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position.z;
            //targetPos.z = .2f;
            //_rightHandIK.data.target.position = targetPos;
            _rightHandIK.data.target.position = targetPos;
            mRightHandIKPos = targetPos;

            Vector3 normal = Vector3.up;
            Vector3 up = mController.Movement.DirectionToVector();
            // Vector3 forward = Vector3.Cross(up, normal);
            Vector3 forward = -normal;
            // Quaternion targetRot = Quaternion.LookRotation(-forward, up);
            Quaternion targetRot = Quaternion.LookRotation(forward, up);
            _rightHandIK.data.target.rotation = targetRot;

            mbRightHandIK = true;
        }
        else
        {
            _rightHandIK.data.target.position = mRightHandIKPos;
        }
    }

    private void applyHandIKTargetPosition()
    {
        Vector3 tr = mController.transform.position;
        tr.y = mClimbLedgeInfo.ledgeBounds.max.y;
        mController.transform.position = tr;

        if (mbLeftHandIK)
        {
            _leftHandIK.data.target.position = mLeftHandIKPos;
        }

        if(mbRightHandIK)
        {
            _rightHandIK.data.target.position = mRightHandIKPos;
        }
    }

    private IEnumerator eApplyTargetPositionAfterPhysicsUpdate()
    {
        yield return new WaitForFixedUpdate();

        applyHandIKTargetPosition();
    }

    private void onAnimatorIK()
    {
        // Left Hand
        mAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f - _leftHandIK.data.targetPositionWeight);

        Vector3 leftHandIKPosition = mAnimator.GetIKPosition(AvatarIKGoal.LeftHand);

        if(Physics.Raycast(leftHandIKPosition, Vector3.down, out RaycastHit leftHit, .5f, mController.Movement.GroundLayer))
        {
            Vector3 targetPos = GetLeftHandIKTargetPosition(mClimbLedgeInfo);
            targetPos.x = leftHit.point.x;
            mAnimator.SetIKPosition(AvatarIKGoal.LeftHand, targetPos);
        }

        // Right Hand
        mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f - _rightHandIK.data.targetPositionWeight);

        Vector3 rightHandIKPosition = mAnimator.GetIKPosition(AvatarIKGoal.RightHand);

        if(Physics.Raycast(rightHandIKPosition, Vector3.down, out RaycastHit rightHit, .5f, mController.Movement.GroundLayer))
        {
            Vector3 targetPos = GetRightHandIKTargetPosition(mClimbLedgeInfo);
            targetPos.x = rightHit.point.x;
            mAnimator.SetIKPosition(AvatarIKGoal.RightHand, targetPos);
        }
    }

    private Vector3 getOrigin()
    {
        Vector3 origin = mController.transform.position;
        origin.y += mController.Movement.Height;
        return origin;
    }

    private Vector3 getDirection()
    {
        return mController.Movement.DirectionToVector();
    }

    private float getSpacing()
    {
        return mController.Movement.Height / (_raycastCount - 1);
    }

    [ContextMenu("Print Spacing")]
    private void printSpacing()
    {
        Debug.Log(getSpacing());
        Debug.Log(mController.Movement.Height);
    }

    private void OnDrawGizmos()
    {
        var climbTypeRayGizmosInfo = GameDebug.GizmosInfo.normal;
        climbTypeRayGizmosInfo.tag = "CheckLedge ClimbType Ray";

        GameDebug.DrawGizmos(climbTypeRayGizmosInfo, () =>
        {
            Vector3 origin = getOrigin();
            Gizmos.color = Color.red;

            for (int i = 0; i < _raycastCount; i++)
            {
                Vector3 pos = origin;
                pos.y -= getSpacing() * i;
                Gizmos.DrawRay(pos, getDirection() * _raycastDistance);
            }
            Gizmos.DrawRay(getOrigin(), getDirection() * _raycastDistance);
        });

        var ledgePointRayGizmosInfo = GameDebug.GizmosInfo.normal;
        ledgePointRayGizmosInfo.tag = "CheckLedge LedgePoint Ray";

        GameDebug.DrawGizmos(ledgePointRayGizmosInfo, () =>
        {
            Vector3 origin = mCharacterPosition;
            Vector3 direction = PlayerMovement.DirectionToVector(mMovement.Direction);

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(origin, direction * _raycastDistance);
        });

        var ledgeRangeGizmosInfo = GameDebug.GizmosInfo.normal;
        ledgeRangeGizmosInfo.tag = "Ledge Range";

        GameDebug.DrawGizmos(ledgeRangeGizmosInfo, () =>
        {
            Bounds ledgeBounds = mClimbLedgeInfo.ledgeBounds;

            Vector3 ledgeMaxPos = ledgeBounds.max;
            ledgeMaxPos.z = getOrigin().z;

            Gizmos.color = Color.blue;
            Vector3 ledgeMaxPosRange = ledgeMaxPos;
            ledgeMaxPosRange.y += _ledgeRange;
            Gizmos.DrawSphere(ledgeMaxPosRange, .1f);
            ledgeMaxPosRange.y -= _ledgeRange;
            ledgeMaxPosRange.y -= _ledgeRange;
            Gizmos.DrawSphere(ledgeMaxPosRange, .1f);

            Vector3 ledgeMinPos = ledgeBounds.max;
            ledgeMinPos.x = ledgeBounds.min.x;
            ledgeMinPos.z = getOrigin().z;

            Gizmos.color = Color.blue;
            Vector3 ledgeMinPosRange = ledgeMinPos;
            ledgeMinPosRange.y += _ledgeRange;
            Gizmos.DrawSphere(ledgeMinPosRange, .1f);
            ledgeMinPosRange.y -= _ledgeRange;
            ledgeMinPosRange.y -= _ledgeRange;
            Gizmos.DrawSphere(ledgeMinPosRange, .1f);
        });

        var ledgeTargetGizmosInfo = GameDebug.GizmosInfo.normal;
        ledgeTargetGizmosInfo.tag = "Ledge NearestLedgePoint";

        GameDebug.DrawGizmos(ledgeTargetGizmosInfo, () =>
        {
            var nearestLedgePoint = mClimbLedgeInfo.nearestLedgePoint;
            // var nearestLedgePoint = mDebugLedgePoint;
            // var targetPoint = new Vector3(mTargetX, mTargetY, 0f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(nearestLedgePoint, .1f);
        });

        var CheckBoundGizmosInfo = GameDebug.GizmosInfo.normal;
        CheckBoundGizmosInfo.tag = "Ledge Check Bound";

        GameDebug.DrawGizmos(CheckBoundGizmosInfo, () =>
        {
            // Bounds Cube
            Vector3 center = mController.transform.position + mController.Movement.DirectionToVector() * _detectionBoxCenterDistance + Vector3.up * _detectionBoxCenterHeight;
            Vector3 halfExtents = _detectionBoxSize / 2f;
            Quaternion orientation = mController.Movement.DirectionToRotation();
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(center, orientation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _detectionBoxSize);
        });

        //if (!Application.isPlaying)
        //    return;

        //Transform trLeftElbow = mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(trLeftElbow.position, trLeftElbow.up * .1f);
        //Gizmos.color = Color.green;
        //Gizmos.DrawRay(trLeftElbow.position, trLeftElbow.forward * .1f);
        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(trLeftElbow.position, trLeftElbow.right * .1f);

        //Transform trLeftHand = mAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(trLeftHand.position, trLeftHand.up * .1f);
        //Gizmos.color = Color.green;
        //Gizmos.DrawRay(trLeftHand.position, trLeftHand.forward * .1f);
        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(trLeftHand.position, trLeftHand.right * .1f);

        //float minLerpHeight = .8f;
        //Gizmos.DrawRay(transform.position + Vector3.up * minLerpHeight, mController.Movement.DirectionToVector());
        //Gizmos.DrawRay(transform.position + Vector3.up * (minLerpHeight + .3f), mController.Movement.DirectionToVector());

        //Transform trRightHand = mAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(trRightHand.position, trRightHand.up * .1f);
        //Gizmos.color = Color.green;
        //Gizmos.DrawRay(trRightHand.position, trRightHand.forward * .1f);
        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(trRightHand.position, trRightHand.right * .1f);

        //Vector3 leftHandBone = mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        //Vector3 leftHandTarget = _leftHandIK.data.target.position;
        //float distance = Vector3.Distance(leftHandBone, leftHandTarget);

        //Vector3 leftMiddleBone = mAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).position;
        //float handSize = Vector3.Distance(leftHandBone, leftMiddleBone);

        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(leftHandBone, .02f);
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(leftHandTarget, .02f);
        //Gizmos.color = Color.magenta;
        //Gizmos.DrawWireSphere(leftMiddleBone, .02f);

        // Bounds Cube
        //Vector3 center = _detectionBoxCollider.transform.position + _detectionBoxCollider.center;
        //Vector3 halfExtents = _detectionBoxCollider.size / 2f;
        //Gizmos.color = Color.red;
        //Gizmos.matrix = Matrix4x4.TRS(center, _detectionBoxCollider.transform.rotation, Vector3.one);
        //Gizmos.DrawWireCube(Vector3.zero, _detectionBoxCollider.size);

        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(debugLeftHandIKTargetPos, debugLeftHandIKHitNormal * .5f);
        //Gizmos.color = Color.green;
        //Gizmos.DrawRay(debugLeftHandIKTargetPos, debugLeftHandIKForward * .5f);
    }

    private void OnDrawGizmosSelected()
    {
        //GameDebug.DrawGizmos(GameDebug.GizmosInfo.normal, () =>
        //{
        //    Vector3 origin = getOrigin();
        //    Gizmos.color = Color.red;

        //    for (int i = 0; i < _raycastCount; i++)
        //    {
        //        Vector3 pos = origin;
        //        pos.y -= getSpacing() * i;
        //        Gizmos.DrawRay(pos, getDirection() * _raycastDistance);
        //    }
        //    Gizmos.DrawRay(getOrigin(), getDirection() * _raycastDistance);
        //});

        //var ledgeRangeGizmosInfo = GameDebug.GizmosInfo.normal;
        //ledgeRangeGizmosInfo.tag = "Ledge Range";

        //GameDebug.DrawGizmos(ledgeRangeGizmosInfo, () =>
        //{
        //    Bounds ledgeBounds = mClimbLedgeInfo.ledgeBounds;

        //    Vector3 ledgeMaxPos = ledgeBounds.max;
        //    ledgeMaxPos.z = getOrigin().z;

        //    Gizmos.color = Color.blue;
        //    Vector3 ledgeMaxPosRange = ledgeMaxPos;
        //    ledgeMaxPosRange.y += _ledgeRange;
        //    Gizmos.DrawSphere(ledgeMaxPosRange, .1f);
        //    ledgeMaxPosRange.y -= _ledgeRange;
        //    ledgeMaxPosRange.y -= _ledgeRange;
        //    Gizmos.DrawSphere(ledgeMaxPosRange, .1f);

        //    Vector3 ledgeMinPos = ledgeBounds.max;
        //    ledgeMinPos.x = ledgeBounds.min.x;
        //    ledgeMinPos.z = getOrigin().z;

        //    Gizmos.color = Color.blue;
        //    Vector3 ledgeMinPosRange = ledgeMinPos;
        //    ledgeMinPosRange.y += _ledgeRange;
        //    Gizmos.DrawSphere(ledgeMinPosRange, .1f);
        //    ledgeMinPosRange.y -= _ledgeRange;
        //    ledgeMinPosRange.y -= _ledgeRange;
        //    Gizmos.DrawSphere(ledgeMinPosRange, .1f);
        //});

        //var ledgeTargetGizmosInfo = GameDebug.GizmosInfo.normal;
        //ledgeTargetGizmosInfo.tag = "Ledge TargetPoint";

        //GameDebug.DrawGizmos(ledgeTargetGizmosInfo, () =>
        //{
        //    // var nearestLedgePoint = mClimbLedgeInfo.nearestLedgePoint;
        //    var nearestLedgePoint = mDebugLedgePoint;
        //    var targetPoint = new Vector3(mTargetX, mTargetY, 0f);

        //    Gizmos.color = Color.red;
        //    Gizmos.DrawSphere(targetPoint, .1f);
        //});

        //var CheckBoundGizmosInfo = GameDebug.GizmosInfo.normal;
        //CheckBoundGizmosInfo.tag = "Ledge Check Bound";

        //GameDebug.DrawGizmos(CheckBoundGizmosInfo, () =>
        //{
        //    // Bounds Cube
        //    Vector3 center = mController.transform.position + mController.Movement.DirectionToVector() * _detectionBoxCenterDistance + Vector3.up * _detectionBoxCenterHeight;
        //    Vector3 halfExtents = _detectionBoxSize / 2f;
        //    Quaternion orientation = mController.Movement.DirectionToRotation();
        //    Gizmos.color = Color.red;
        //    Gizmos.matrix = Matrix4x4.TRS(center, orientation, Vector3.one);
        //    Gizmos.DrawWireCube(Vector3.zero, _detectionBoxSize);
        //});
    }
}
