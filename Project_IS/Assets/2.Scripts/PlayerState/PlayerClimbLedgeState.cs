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
    [SerializeField] private BoxCollider _detectionBoxCollider;

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
    private float mStartMoveInputX;

    private Vector3 mDeltaPosition;

    private Vector3 mHangingVelocity = Vector3.zero;
    private bool mbEnterAnimatorMove = false;
    private bool mbStartClimb = false;
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
        // Debug.Log($"Hanging Velocity: {mHangingVelocity}, deltaX: {mHangingVelocity.x * Time.fixedDeltaTime}");
        mController.Movement.SetVelocity(Vector3.zero);
        mController.Movement.SetKinematic(true);
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);

        if (_animationType == EAnimationType.PositionFixedOnLedge)
        {
            mController.Animator.PlayClimbLedge(mClimbLedgeInfo.checkIndex);
        }
        else
        {
            mController.Animator.SetClimbLedge();
            mController.Animator.SetIndex(mClimbLedgeInfo.checkIndex);
        }

        mController.Animator.SetInputXMagnitude(0f);

        mController.Animator.AnimationEventReceiver.onTouchHand -= onFootStepFromFall;
        mController.Animator.AnimationEventReceiver.onTouchHand += onFootStepFromFall;

        mController.Animator.AnimationEventReceiver.onReleaseHand -= startReleaseHandIKWeight;
        mController.Animator.AnimationEventReceiver.onReleaseHand += startReleaseHandIKWeight;

        // _leftHandIK.weight = 1f;
        // _rightHandIK.weight = 1f;
        mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;
        // mController.Animator.onAnimatorFixedUpdate += updateHandIKWeight;

        mController.Animator.onAnimatorMove -= updateAnimatorMove;
        mController.Animator.onAnimatorMove += updateAnimatorMove;

        mController.Animator.onAnimationIK -= onAnimatorIK;
        mController.Animator.onAnimationIK += onAnimatorIK;

        //mController.Animator.onUpdateState -= onUpdateState;
        //mController.Animator.onUpdateState += onUpdateState;


        mbClimb = false;
        mbLeftHandIK = false;
        mbRightHandIK = false;

        // StartCoroutine(eClimbLedge());
    }

    public override void ExitState()
    {
        mController.Movement.SetKinematic(false);
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);

        mGround = null;
        mbEnterAnimatorMove = false;
        mbStartClimb = false;
        mController.Animator.AnimationEventReceiver.onTouchHand -= onFootStepFromFall;
        mController.Animator.onAnimatorMove -= updateAnimatorMove;
        mController.Animator.onAnimationIK -= onAnimatorIK;
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
        // Debug.Log($"[{Time.frameCount}] Update, Left Hand Weight: {_leftHandIK.weight}");
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
    public void SetLedge(Bounds ledgeBounds)
    {
        mLedgeBounds = ledgeBounds;
    }

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

        Vector3 center = _detectionBoxCollider.transform.position + _detectionBoxCollider.center;
        Vector3 halfExtents = _detectionBoxCollider.size / 2f;
        // Debug.Log($" Detection Box Center: {center}, halfExtents: {_detectionBoxCollider.size}, Character Transform: {transform.position}, Rotation: {_detectionBoxCollider.transform.rotation.eulerAngles}");

        Collider[] colliders = Physics.OverlapBox(center,
                                halfExtents,
                                _detectionBoxCollider.transform.rotation,
                                _raycastLayer,
                                QueryTriggerInteraction.Ignore);

        if (colliders.Length < 1)
        {
            return -1;
        }

        // Debug.Log($"Ledge Checked");

        detectedCollider = colliders[0];
        Bounds detectedColliderBounds = detectedCollider.bounds;
        climbLedgeInfo.ledgeBounds = detectedColliderBounds;
        float ledgeY = detectedColliderBounds.max.y;

        Vector3 ledgePoint = detectedColliderBounds.max;
        float distanceToMin = Mathf.Abs(mController.transform.position.x - detectedColliderBounds.min.x);
        float distanceToMax = Mathf.Abs(mController.transform.position.x - detectedColliderBounds.max.x);
        ledgePoint.x = (distanceToMin < distanceToMax) ? detectedColliderBounds.min.x : detectedColliderBounds.max.x;

        climbLedgeInfo.nearestLedgePoint = ledgePoint;

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

        Vector3 shoulderPos = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        float reachHeight = shoulderPos.y + GetArmLength();
        float maxHeight = reachHeight + .3f;

        // if(ledgePoint.y > _detectionBoxCollider.bounds.max.y)
        if(ledgePoint.y > maxHeight)
        {
            return -1;
        }

        // Debug.Log($"Height Checked");

        if(ledgePoint.y > reachHeight)
        {
            return 0;
        }

        float distanceToLedge = (distanceToMin < distanceToMax) ? distanceToMin : distanceToMax;
        float gapToLedge = distanceToLedge - _raycastDistance;
        climbLedgeInfo.gapToLedge = (gapToLedge < 0f) ? 0f : gapToLedge;
        // climbLedgeInfo.maxGapToLedge = _detectionBoxCollider.size.z - _raycastDistance;
        climbLedgeInfo.maxGapToLedge = _detectionBoxCollider.size.z - GetArmLength();

        if (distanceToLedge > _raycastDistance)
        {
            return 0;
        }

        // Debug.Log($"Distance Checked");
        // Debug.Log($"LedgePoint: {ledgePoint}, Box Bounds.max: {_detectionBoxCollider.bounds.max.y}");

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

        if(climbLedgeInfo.checkIndex == 0)
        {
            if(mController.Movement.Velocity.y < 2.5f)
            {
                climbLedgeInfo.checkIndex = 4;
            }
        }

        return 1;
    }

    public void EndClimbLedge()
    {
        if (!mbClimb)
            return;

        mbClimb = false;

        PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;

        if (Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
        {
            moveState.EnterToRun(mStartMoveInputX);
        }
        else
        {
            // moveState.EnterToIdle();
        }

        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
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

    private void FixedUpdate()
    {
        if (!mbClimb)
            return;

        // updateHandIKWeight();

        //float leftHandWeight = mAnimator.GetFloat("LeftHandWeight");
        //_leftHandIK.weight = leftHandWeight;

        //float rightHandWeight = mAnimator.GetFloat("RightHandWeight");
        //_rightHandIK.weight = rightHandWeight;

        // transform.position += mDeltaPosition;

        // updateHandIK();

        // mDeltaPosition = Vector3.zero;
        // Debug.Log($"[{Time.frameCount}] FixedUpdate, Left Hand Weight: {_leftHandIK.weight}");
    }

    private IEnumerator eClimbLedge()
    {
        // float originY = transform.position.y + mController.Movement.Height;
        // float targetY = mLedgeBounds.max.y - mController.Movement.Height + _lerpYOffset;
        // float targetY = mClimbLedgeInfo.ledgeBounds.max.y - mController.Movement.Height + _lerpYOffset;
        // float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? mClimbLedgeInfo.ledgeBounds.max.x + _lerpXOffset: mClimbLedgeInfo.ledgeBounds.min.x - _lerpXOffset;
        float targetY = mClimbLedgeInfo.ledgeBounds.max.y;

        if (mClimbLedgeInfo.ledgeHandler != null)
            targetY = mClimbLedgeInfo.nearestLedgePoint.y;

        float exitNormalizedTime = 0f;
        float lerpXOffset = 0f;

        switch (mClimbLedgeInfo.checkIndex)
        {
            case 0:
                Debug.Log("OverHead Ledge Climb");
                targetY -= _lerpYOffsetOverHead;
                exitNormalizedTime = _exitNormalizedTimeOverHead;
                lerpXOffset = _lerpXOffsetOverHead;
                break;
            case 1:
                Debug.Log("Chest Ledge Climb");
                targetY -= _lerpYOffsetChest;
                exitNormalizedTime = _exitNormalizedTimeChest;
                lerpXOffset = _lerpXOffsetChest;
                break;
            case 2:
                Debug.Log("Stomach Ledge Climb");
                targetY -= _lerpYOffsetStomach;
                exitNormalizedTime = _exitNormalizedTimeStomach;
                lerpXOffset = _lerpXOffsetStomach;
                break;
            case 3:
                Debug.Log("Knee Ledge Climb");
                targetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
            default:
                Debug.Log("Default Ledge Climb");
                targetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
        }

        //if (mClimbLedgeInfo.checkIndex >= 3)
        //{
        //    float posX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
        //    mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset;

        //    float posY = mClimbLedgeInfo.ledgeBounds.max.y;

        //    Vector3 newPos = transform.position;
        //    newPos.x = posX;
        //    newPos.y = posY;
        //    transform.position = newPos;

        //    yield break;
        //}
        if (_animationType == EAnimationType.PositionFixedOnLedge) // && mClimbLedgeInfo.checkIndex >= 1)
        {
            float posX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
            mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset;

            int indexFromKnee = (_raycastCount - 1 - mClimbLedgeInfo.checkIndex);

            if (indexFromKnee < 1)
                indexFromKnee = 1;

            float startPosY = _raycastDistance * indexFromKnee + (mClimbLedgeInfo.raycastOrigin.y - mClimbLedgeInfo.nearestLedgePoint.y); ;

            Vector3 newPos = transform.position;
            newPos.x = posX;
            newPos.y += startPosY;
            transform.position = newPos;

            targetY = mClimbLedgeInfo.ledgeBounds.max.y;
        }

        //float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
        //                mClimbLedgeInfo.ledgeBounds.max.x + lerpXOffset : mClimbLedgeInfo.ledgeBounds.min.x - lerpXOffset;

        //if(mClimbLedgeInfo.ledgeHandler != null)
        //    targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
        //                mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset;
        float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
                    mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset;

        float timer = 0f;
        float duration = .2f;

        while (timer < duration)
        {
            float t = timer / duration;

            Vector3 newPos = transform.position;
            newPos.x = Mathf.Lerp(transform.position.x, targetX, t);
            newPos.y = Mathf.Lerp(transform.position.y, targetY, t);

            transform.position = newPos;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
            //timer += Time.deltaTime;
            //yield return null;
        }

        Vector3 targetPos = transform.position;
        targetPos.y = targetY;
        transform.position = targetPos;

        if (_animationType == EAnimationType.PositionFixedOnLedge) // && mClimbLedgeInfo.checkIndex >= 1)
        {
            yield break;
        }

        while (mbClimb)
        {
            // normalizedTime이 일정 비율이 넘으면 상태 전환
            AnimatorStateInfo stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            Vector3 deltaPosition = mAnimator.deltaPosition;

            //if (transform.position.y < mClimbLedgeInfo.ledgeBounds.max.y)
            //    deltaPosition.y *= _deltaPositionYSpeed;
            //else
            //    deltaPosition.y = 0f;

            deltaPosition.z = 0f;

            transform.position += deltaPosition;

            //if (transform.position.y > mClimbLedgeInfo.ledgeBounds.max.y)
            //{
            //    targetPos = transform.position;
            //    targetPos.y = mClimbLedgeInfo.ledgeBounds.max.y;
            //    transform.position = targetPos;
            //}

            // mStartMoveInputX = deltaPosition.x / (Time.deltaTime * mController.Movement.MoveSpeed);

            //if (stateInfo.normalizedTime >= exitNormalizedTime)
            //{
            //    EndClimbLedge();
            //    yield break;
            //}

            // yield return null;
            yield return new WaitForFixedUpdate();
        }
    }

    private void enterAnimatorMove()
    {
        // mController.Animator.onAnimatorFixedUpdate += updateHandIKWeight;
        mClimbState = EClimbState.Lerp;
        mbClimb = true;
        mbLerpPosition = true;
        _headAimIK.weight = 0f;
        mStartPos = transform.position;
        mTargetY = mClimbLedgeInfo.ledgeBounds.max.y;

        if (mClimbLedgeInfo.ledgeHandler != null)
            mTargetY = mClimbLedgeInfo.nearestLedgePoint.y;

        float exitNormalizedTime = 0f;
        float lerpXOffset = 0f;

        switch (mClimbLedgeInfo.checkIndex)
        {
            case 4:
                Debug.Log("OverHead Ledge Hanging");
                mClimbState = EClimbState.Hanging;
                mbClimb = false;
                mbLerpPosition = false;
                mController.Animator.SetVertical(0f);
                _headAimIK.weight = 1f;
                mTargetY -= _lerpYOffsetOverHead;
                exitNormalizedTime = _exitNormalizedTimeOverHead;
                lerpXOffset = _lerpXOffsetOverHead;
                break;
            case 0:
                Debug.Log("OverHead Ledge Climb");
                mClimbState = EClimbState.Hanging;
                mbClimb = false;
                mbLerpPosition = false;
                mController.Animator.SetVertical(0f);
                _headAimIK.weight = 1f;
                mTargetY -= _lerpYOffsetOverHead;
                exitNormalizedTime = _exitNormalizedTimeOverHead;
                lerpXOffset = _lerpXOffsetOverHead;
                break;
            case 1:
                Debug.Log("Chest Ledge Climb");
                mTargetY -= _lerpYOffsetChest;
                exitNormalizedTime = _exitNormalizedTimeChest;
                lerpXOffset = _lerpXOffsetChest;
                break;
            case 2:
                Debug.Log("Stomach Ledge Climb");
                mTargetY -= _lerpYOffsetStomach;
                exitNormalizedTime = _exitNormalizedTimeStomach;
                lerpXOffset = _lerpXOffsetStomach;
                break;
            case 3:
                Debug.Log("Knee Ledge Climb");
                mTargetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
            default:
                Debug.Log("Default Ledge Climb");
                mTargetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
        }

        mTargetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
                    mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset;

        mAnimatorMoveTimer = 0f;
        Debug.Log($"Enter Animator Move");
    }

    private void updateAnimatorMove()
    {
        if(!mbEnterAnimatorMove)
        {
            mbEnterAnimatorMove = true;

            enterAnimatorMove();
        }

        float hangingDuration = .2f;

        // if (!mbLerpPosition)
        if(mClimbState == EClimbState.Hanging)
        {
            if (mController.InputHandler.MoveInputRaw.y > .9f)
            {
                mClimbState = EClimbState.Lerp;
                mbClimb = true;
                // mController.Animator.SetClimbLedge();
                mController.Animator.SetVertical(1f);
                mAnimatorMoveTimer = 0f;
                mbLerpPosition = true;
                // mbLerpDelay = true;
                Debug.Log($"Input Y during Hanging");

                return;
            }
            else
            {
                // normalizedTime이 일정 비율이 넘으면 상태 전환
                AnimatorStateInfo stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

                //if(stateInfo.IsTag("Hanging"))
                //{
                //    Vector3 deltaPosition = mAnimator.deltaPosition;

                //    //float deltaX = mHangingVelocity.x * Time.fixedDeltaTime;
                //    //if (deltaPosition.x > deltaX)
                //    //    deltaPosition.x = deltaX;
                //    if (transform.position.x < mClimbLedgeInfo.nearestLedgePoint.x + .15f)
                //    {
                //        deltaPosition.x = 0f;
                //    }

                //    deltaPosition.z = 0f;

                //    transform.position += deltaPosition;
                //    // Debug.Log($"Delta Position: {deltaPosition}");
                //}
                //else
                //{
                //    float deltaX = mHangingVelocity.x * Time.fixedDeltaTime;
                //    // float deltaY = mHangingVelocity.y * Time.fixedDeltaTime;
                //    float deltaY = 0f;

                //    if(transform.position.x < mClimbLedgeInfo.nearestLedgePoint.x + .15f)
                //    {
                //        deltaX = 0f;
                //    }

                //    Vector3 deltaPosition = new Vector3(deltaX, deltaY, 0f);
                //    transform.position += deltaPosition;
                //}

                if (mAnimatorMoveTimer < hangingDuration)
                {
                    Vector3 targetPos = transform.position;
                    targetPos.x = mClimbLedgeInfo.nearestLedgePoint.x;
                    targetPos.x = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? targetPos.x + .15f : targetPos.x - .15f;
                    targetPos.y = mClimbLedgeInfo.nearestLedgePoint.y - 2.03f;

                    float t = mAnimatorMoveTimer / hangingDuration;
                    Vector3 newPos = transform.position;
                    newPos.x = Mathf.Lerp(mStartPos.x, targetPos.x, t);
                    newPos.y = Mathf.Lerp(mStartPos.y, targetPos.y, t);

                    transform.position = newPos;

                    mAnimatorMoveTimer += Time.fixedDeltaTime;

                    updateHandIKTargetPosition();

                    if (mAnimatorMoveTimer > hangingDuration)
                    {
                        mStartPos.x = targetPos.x;
                        mStartPos.y = targetPos.y;

                        if (mClimbLedgeInfo.checkIndex == 0)
                        {
                            mClimbState = EClimbState.Lerp;
                            mbClimb = true;
                            mController.Animator.SetVertical(1f);
                            mAnimatorMoveTimer = 0f;
                            mbLerpPosition = true;
                            // mbLerpDelay = true;

                            Debug.Log($"Immediately Climb as Index 0");
                        }
                    }

                    return;
                }
                else
                {
                    if (mClimbLedgeInfo.checkIndex == 0)
                    {
                        mClimbState = EClimbState.Lerp;
                        mbClimb = true;
                        mController.Animator.SetVertical(1f);
                        mAnimatorMoveTimer = 0f;
                        mbLerpPosition = true;
                        // mbLerpDelay = true;

                        Debug.Log($"Immediately Climb as Index 0");
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        // if(mbLerpDelay)
        if(mClimbState == EClimbState.LerpDelay)
        {
            float delayDuration = .1f;

            if (mAnimatorMoveTimer < delayDuration)
            {
                float t = mAnimatorMoveTimer / delayDuration;

                Vector3 newPos = transform.position;
                // newPos.y = Mathf.Lerp(mStartPos.y, mStartPos.y + (2.03f - .8f), t);
                newPos.y = Mathf.Lerp(mStartPos.y, mStartPos.y + .8f, t);

                transform.position = newPos;
                // mDeltaPosition = newPos - transform.position;

                mAnimatorMoveTimer += Time.fixedDeltaTime;

                if (mAnimatorMoveTimer > delayDuration)
                {
                    mClimbState = EClimbState.Lerp;
                    //Vector3 targetPos = transform.position;
                    //targetPos.y = mTargetY;
                    //transform.position = targetPos;
                    mStartPos.y = transform.position.y;
                    mbClimb = true;
                    mbLerpDelay = false;
                    // mbLerpPosition = true;
                    mAnimatorMoveTimer = 0f;
                    mController.Animator.SetVertical(1f);

                    Debug.Log($"Lerp Delay End");
                }
            }

            updateHandIKTargetPosition();
            Debug.Log($"Lerp Delay");

            return;
        }

        float duration = .2f;
        float duration1 = duration * .8f;
        float duration2 = duration - duration1;

        //if (mbLerpPosition && mAnimatorMoveTimer < duration)
        if(mClimbState == EClimbState.Lerp)
        {
            if(mAnimatorMoveTimer < duration)
            {
                float t = mAnimatorMoveTimer / duration;

                Vector3 newPos = transform.position;
                newPos.x = Mathf.Lerp(mStartPos.x, mTargetX, t);
                // if(mAnimatorMoveTimer > duration1)
                //float xt = Mathf.Clamp01((mAnimatorMoveTimer - duration1) / duration2);
                //newPos.x = Mathf.Lerp(mStartPos.x, mTargetX, xt);
                newPos.y = Mathf.Lerp(mStartPos.y, mTargetY, t);

                // Debug.Log($"xt: {xt}, Timer-duration1: {mAnimatorMoveTimer - duration1}, duration2: {duration2}");

                transform.position = newPos;
                // mDeltaPosition = newPos - transform.position;

                Debug.Log($"Lerp Timer: {mAnimatorMoveTimer}, t: {t}");
                mAnimatorMoveTimer += Time.fixedDeltaTime;

                if(mAnimatorMoveTimer > duration)
                {
                    mClimbState = EClimbState.Climb;
                    Vector3 targetPos = transform.position;
                    targetPos.y = mTargetY;
                    transform.position = targetPos;
                    mbLerpPosition = false;

                    // mController.Animator.onAnimatorFixedUpdate += updateHandIKWeight;
                }
            }
        }
        // else
        else if (mClimbState == EClimbState.Climb)
        {
            if (mbClimb)
            {
                // normalizedTime이 일정 비율이 넘으면 상태 전환
                AnimatorStateInfo stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

                Vector3 deltaPosition = mAnimator.deltaPosition;

                deltaPosition.z = 0f;

                transform.position += deltaPosition;
                // mDeltaPosition = deltaPosition;
            }
        }

        updateHandIKTargetPosition();

        //Transform shoulderL = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        //Vector3 targetLPos = _leftHandIK.data.target.position;
        //float distance = Vector3.Distance(shoulderL.position, targetLPos);
        //Debug.Log($"Left Hand To Left Shoulder distance: {distance}");

        // Debug.Log($"[{Time.frameCount}] onAnimatorMove");
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

        //if(leftHandWeight < .5f)
        //{
        //    _leftHandIK.weight = leftHandWeight;
        //}
        //else
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

            // Debug.Log($"[{Time.frameCount}] Gap To Ledge: {gapToLedge}, Max Gap To Ledge: {maxGapToLedge}, IK Weight: {leftHandWeight}");

            //if (_leftHandIK.weight > .99f)
            //{
            //    _leftHandIK.weight = 1f;

            //    mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;

            //    mController.Animator.onAnimatorFixedUpdate -= releaseHandIKWeight;
            //    mController.Animator.onAnimatorFixedUpdate += releaseHandIKWeight;
            //}

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

            // Debug.Log($"[{Time.frameCount}] Left Hand Distance: {distance}, Hand Size: {handSize}, Hand Distance Ratio: {handDistanceRatio}, Target Rotation Weight: {leftHandRotationWeight}");


            //if (distance < .01f)
            //{
            //    _leftHandIK.weight = 1f;
            //}
            //else if (distance > .01f && distance < handSize)
            //{
            //    float weight = 1 - distance / handSize;

            //    _leftHandIK.weight = weight;
            //}
            //else
            //{
            //    _leftHandIK.weight = 0f;
            //}

            // Debug.Log($"distance: {distance}, handSize: {handSize}");

            // mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;
        }

        //Transform shoulderL = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        //Vector3 targetLPos = _leftHandIK.data.target.position;
        //float distanceL = Vector3.Distance(shoulderL.position, targetLPos);
        //float weightL = 0f;

        //if(distanceL < .5f)
        //{
        //    weightL = 1f;
        //}
        //else if(distanceL > .5f && distanceL < .6f)
        //{
        //    float weight = 1f - (distanceL - .5f) / .1f;

        //    weightL = weight;
        //}
        //else
        //{
        //    weightL = 0f;

        //    mController.Animator.onAnimatorFixedUpdate -= updateHandIKWeight;
        //}

        // _leftHandIK.weight = 1f; // weightL;
        // Debug.Log($"Left Hand To Left Shoulder distance: {distanceL}, weight: {weightL}");

        float rightHandWeight = mAnimator.GetFloat("RightHandWeight");
        // _rightHandIK.weight = rightHandWeight;

        //Vector3 rightHandBone = mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position;
        //Vector3 rightHandTarget = _rightHandIK.data.target.position;
        //float rightHandDistance = Vector3.Distance(rightHandBone, rightHandTarget);

        //Vector3 rightMiddleBone = mAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position;
        //float rightHandSize = Vector3.Distance(rightHandBone, rightMiddleBone);

        //if (rightHandDistance < .01f)
        //{
        //    _rightHandIK.weight = 1f;
        //}
        //else if (rightHandDistance > .01f && rightHandDistance < rightHandSize)
        //{
        //    float weight = 1 - rightHandDistance / rightHandSize;

        //    _rightHandIK.weight = weight;
        //}
        //else
        //{
        //    _rightHandIK.weight = 0f;
        //}

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
                // Debug.Log($"Left Hand IK Released, Weight: {leftHandWeight}");
            }
        }

        //Transform shoulderL = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        //Vector3 targetLPos = _leftHandIK.data.target.position;
        //float distanceL = Vector3.Distance(shoulderL.position, targetLPos);
        //float weightL = 0f;

        //if (distanceL < .5f)
        //{
        //    weightL = 1f;
        //}
        //else if (distanceL > .5f && distanceL < .6f)
        //{
        //    float weight = 1f - (distanceL - .5f) / .1f;

        //    weightL = weight;
        //}
        //else
        //{
        //    weightL = 0f;

        //    mController.Animator.onAnimatorFixedUpdate -= releaseHandIKWeight;
        //}

        //_leftHandIK.weight = weightL;
        //Debug.Log($"Left Hand To Left Shoulder distance: {distanceL}, weight: {weightL}");

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
        //float leftHandWeight = mAnimator.GetFloat("LeftHandWeight");
        //_leftHandIK.weight = leftHandWeight;
        // Debug.Log($"Left Hand Weight: {_leftHandIK.weight}");

        if(!mbLeftHandIK)
        {
            Vector3 leftHandOrigin = _leftHandIK.data.tip.position;

            //if (Physics.Raycast(leftHandOrigin, Vector3.down, out RaycastHit leftHit, .5f, mController.Movement.GroundLayer))
            //{
            //    Vector3 targetPos = leftHit.point;
            //    targetPos.y += _handIKTargetOffsetY;
            //    _leftHandIK.data.target.position = targetPos;

            //    Vector3 normal = leftHit.normal;
            //    Vector3 up = mController.Movement.DirectionToVector();
            //    Vector3 forward = Vector3.Cross(up, normal);
            //    Quaternion targetRot = Quaternion.LookRotation(forward, up);
            //    _leftHandIK.data.target.rotation = targetRot;

            //    Transform shoulderL = mAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            //    float distance = Vector3.Distance(shoulderL.position, targetPos);
            //    Debug.Log($"Left Hand To Left Shoulder distance: {distance}");

            //    mLeftHandIKPos = targetPos;
            //    mbLeftHandIK = true;
            //}

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

            //Transform trElbow = mAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            //Vector3 defaultUp = trElbow.up;
            //Vector3 defaultForward = -trElbow.right;
            //Quaternion defaultRot = Quaternion.LookRotation(defaultForward, defaultUp);
            //_leftHandIK.data.target.rotation = defaultRot;
        }

        //float rightHandWeight = mAnimator.GetFloat("RightHandWeight");
        //_rightHandIK.weight = rightHandWeight;

        if (!mbRightHandIK)
        {
            Vector3 rightHandOrigin = _rightHandIK.data.tip.position;

            //if (Physics.Raycast(rightHandOrigin, Vector3.down, out RaycastHit rightHit, .5f, mController.Movement.GroundLayer))
            //{
            //    Vector3 targetPos = rightHit.point;
            //    targetPos.y += _handIKTargetOffsetY;
            //    _rightHandIK.data.target.position = targetPos;

            //    Vector3 normal = rightHit.normal;
            //    Vector3 up = mController.Movement.DirectionToVector();
            //    Vector3 forward = Vector3.Cross(up, normal);
            //    Quaternion targetRot = Quaternion.LookRotation(-forward, up);
            //    _rightHandIK.data.target.rotation = targetRot;

            //    mRightHandIKPos = targetPos;
            //    mbRightHandIK = true;
            //}

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
        Vector3 tr = transform.position;
        tr.y = mClimbLedgeInfo.ledgeBounds.max.y;
        transform.position = tr;

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

    //private void onUpdateState(string stateName, AnimatorStateInfo animatorStateInfo)
    //{
    //    Debug.Log($"[{Time.frameCount}] onUpdateState");
    //    float leftHandWeight = mAnimator.GetFloat("LeftHandWeight");
    //    _leftHandIK.weight = leftHandWeight;

    //    float rightHandWeight = mAnimator.GetFloat("RightHandWeight");
    //    _rightHandIK.weight = rightHandWeight;
    //    Debug.Log($"Left Hand Weight: {_leftHandIK.weight}");
    //}

    private Vector3 getOrigin()
    {
        Vector3 origin = transform.position;
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
        if (!Application.isPlaying)
            return;

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
        if (!Application.isPlaying)
            return;

        if(_drawRay)
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
        }

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

        //if(mBoundMax != null)
        //{
        //    Gizmos.DrawSphere(mBoundMax.Value, 1f);
        //}

        //if(mBoundMin != null)
        //{
        //    Gizmos.DrawSphere(mBoundMin.Value, 1f);
        //}
    }
}
