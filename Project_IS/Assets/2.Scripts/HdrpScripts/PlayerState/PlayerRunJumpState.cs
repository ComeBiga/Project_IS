using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunJumpState : PlayerStateBase
{
    public bool jumpUpward = true;

    [SerializeField] private PlayerClimbLedgeState _climbLedgeState;
    [SerializeField] private float _interactableOffsetY = -.5f;

    private Vector3 mMoveInput;
    private float mDefaultHeight;
    private float mMotionTime = 0f;
    private float mRotationTimer = 0f;
    private float mRotationDuration = .2f;
    private bool mbRotationCW = true;
    private bool mbEnterWithoutAnimation = false;

    // Ladder
    private float mPathZPosition;
    private float mInteractableMaxDistance;
    private float mInteractableOffsetY;
    private float mInteractableDistance;
    private float mSidePassZDistance;

    // Ledge
    private bool mbLedgeDetected = false;
    private Vector3 mLedgePoint;
    private PlayerClimbLedgeState.ClimbLedgeInfo mDetectedLedgeInfo;

    public override void EnterState()
    {
        mMotionTime = 0f;
        mRotationTimer = 0f;

        if (jumpUpward)
            mController.Movement.JumpFoward();

        if(!mbEnterWithoutAnimation)
            mController.Animator.Play(AnimState.RunJump_Blend_Tree);
        // mController.Animator.CrossFadeJump(true);
        // mController.Animator.SetJump(true);
        //mController.Animator.SetIndex(1);
        mController.Animator.SetVertical(0f);

        // var moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        var moveState = mController.StateMachine.GetStateBase<PlayerMoveState>();
        mPathZPosition = moveState.PathZPosition;
        mInteractableMaxDistance = moveState.InteractableMaxDistance;
        mInteractableOffsetY = moveState.InteractableOffsetY + _interactableOffsetY;
        mInteractableDistance = moveState.InteractableDistance;
        mSidePassZDistance = moveState.SidePassZDistance;

        mStateMachine.GetStateBase<PlayerTurnState>().StopStanbyRotation();

        mController.CharacterSound.PlayRandomClothSound();
    }

    public override void ExitState()
    {
        mRotationDuration = .2f;
        jumpUpward = true;
        mbLedgeDetected = false;
        mbRotationCW = true;
        mbEnterWithoutAnimation = false;
        // mController.Animator.SetJump(false);

        // mController.Animator.SetVertical(0f);

        mController.Animator.onAnimatorFixedUpdate -= onAnimatorFixedUpdate;
    }

    public override void Tick()
    {
        mMoveInput = mController.InputHandler.MoveInput;

        if (mController.Movement.Direction == PlayerMovement.EDirection.Right)
        {
            if (mMoveInput.x < 0f)
                mMoveInput.x = 0f;
        }
        else
        {
            if (mMoveInput.x > 0f)
                mMoveInput.x = 0f;
        }

        var currentStateInfo = mController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        // Jump Animation NormalizedTime
        if (currentStateInfo.IsTag("RunJump"))
        {
            float velocityY = mController.Movement.Velocity.y;
            float maxJumpUpVelocity = 4f;
            float resultNormalizedVelocityY = 0f;
            float jumpUpNormalizedTimeDuration = .2f;
            float fallNormalizedTimeDuration = .8f;

            // Jump Up
            if (velocityY > 0f)
            {
                float normalizedVelocityY = 1f - (mController.Movement.Velocity.y / maxJumpUpVelocity);
                // resultNormalizedVelocityY = normalizedVelocityY * jumpUpNormalizedTimeDuration;
                mMotionTime = normalizedVelocityY * jumpUpNormalizedTimeDuration;
            }
            // Fall
            else
            {
                // float normalizedVelocityY = 1f - ((mController.Movement.Velocity.y + maxJumpUpVelocity) / maxJumpUpVelocity);
                // resultNormalizedVelocityY = (normalizedVelocityY * fallNormalizedTimeDuration) + jumpUpNormalizedTimeDuration;
                // mMotionTime += Time.fixedDeltaTime;
                float normalizedVelocityY = 1f - ((mController.Movement.Velocity.y + maxJumpUpVelocity) / maxJumpUpVelocity);
                mMotionTime = (normalizedVelocityY * fallNormalizedTimeDuration) + jumpUpNormalizedTimeDuration;
            }

            // mController.Animator.SetVertical(resultNormalizedVelocityY);
            mController.Animator.SetVertical(mMotionTime);

            // Debug.Log($"[{Time.frameCount}] Velocity Y: {mController.Movement.Velocity.y}, Normalized Velocity Y: {normalizedVelocityY}");
            // Debug.Log($"[{Time.frameCount}] Velocity Y: {mController.Movement.Velocity.y}, Normalized Velocity Y: {resultNormalizedVelocityY}, Current State NormalizedTime: {currentStateInfo.normalizedTime}");
            // Debug.Log($"[{Time.frameCount}] Velocity Y: {mController.Movement.Velocity.y}, Normalized Velocity Y: {mMotionTime}, Current State NormalizedTime: {currentStateInfo.normalizedTime}");
        }

        GameDebug.Log($"IsRunJump: {currentStateInfo.IsTag("RunJump")}, Motion Time: {mMotionTime}");

        // mController.Movement.Move(mMoveInput);
        mController.Movement.UpdateJump(mMoveInput);
        mController.Animator.SetHorizontal(mController.InputHandler.MoveInput.x);
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        // mController.Movement.UpdateRotation(Time.deltaTime * 20f);
        float t = mRotationTimer / mRotationDuration;
        mRotationTimer += Time.deltaTime;
        // GameDebug.Log($"Rotation Timer: {mRotationTimer}, t: {t}");
        // mController.Movement.UpdateRotation(t);
        mMovement.UpdateRotation(mbRotationCW, t);

        if (!mController.Movement.Jumping)
        {
            //if(mController.Movement.CheckInteractableToDown(out RaycastHit interactableHit))
            if(mController.Movement.CheckInteractableByOverlap(out Collider[] hitColliders))
            {
                //var interactableObject = interactableHit.collider.GetComponentInParent<InteractableObject>();
                // Debug.Log($"Land on {interactableHit.collider.name}");

                var fallingGround = hitColliders[0].GetComponentInParent<FallingGround>();
                // Debug.Log($"Land on {hitColliders[0].name}");

                fallingGround?.StepOn();
            }

            //// mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            //mController.StateMachine.SwitchState<PlayerMoveState>();
            mController.StateMachine.SwitchState<PlayerLandingState>((landingState) =>
            {
                landingState.SetLandingType(PlayerLandingState.ELandingType.Soft);
            });

            return;
        }

        int ledgeDetection = _climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out Collider detectedCollider);
        mDetectedLedgeInfo = climbLedgeInfo;

        // if (_climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out RaycastHit hitInfo))
        if(mbLedgeDetected == false && ledgeDetection == 0)
        {
            mbLedgeDetected = true;

            Bounds bounds = detectedCollider.bounds;
            mLedgePoint = climbLedgeInfo.nearestLedgePoint;

            // Left Hand
            Vector3 leftHandBonePos = mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            Vector3 leftHandTargetPos = mLedgePoint;
            leftHandTargetPos.z = leftHandBonePos.z;
            _climbLedgeState.LeftHandIK.data.target.position = leftHandTargetPos;

            // Right Hand
            Vector3 rightHandBonePos = mController.Animator.Animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            Vector3 rightHandTargetPos = mLedgePoint;
            rightHandTargetPos.z = rightHandBonePos.z;
            _climbLedgeState.RightHandIK.data.target.position = rightHandTargetPos;

            mController.Animator.onAnimatorFixedUpdate -= onAnimatorFixedUpdate;
            mController.Animator.onAnimatorFixedUpdate += onAnimatorFixedUpdate;
        }
        else if (ledgeDetection == 1)
        {
            // _climbLedgeState.SetLedge(hitInfo.collider.bounds);
            _climbLedgeState.SetInfo(climbLedgeInfo);
            // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
            mController.StateMachine.SwitchState<PlayerClimbLedgeState>();
            return;
        }

        // fall
        PlayerFallState fallState = mController.StateMachine.GetStateBase<PlayerFallState>();

        if(mMotionTime > .99f)
        // if (fallState.CheckFall())
        // if (transform.position.y < mDefaultHeight - .1f)
        {
            mController.StateMachine.SwitchState<PlayerFallState>((fallState) =>
            {
                fallState.SetFallIndex(1);
                fallState.SetFallType(PlayerFallState.EFallType.FromJump);
            });
            return;
        }

        // To Ladder
        var ladderState = mStateMachine.GetStateBase<PlayerLadderState>();

        if (ladderState.CheckLadder(out PlayerLadderState.LadderInfo ladderInfo))
        {
            GameDebug.Log($"Ladder Checked", tag: "RunJump LadderCheck");
            // if (ladderInfo.part == PlayerLadderState.LadderPart.Bottom && mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Up))
            if(ladderState.IsValidStartInMiddle(ladderInfo))
            {
                GameDebug.Log($"StartInMiddle Validated", tag: "RunJump IsValidStartInMiddle");

                mStateMachine.SwitchState<PlayerLadderState>((state) =>
                {
                    state.SetLadderInMiddle(ladderInfo);
                });

                return;
            }
        }

        // Interactable
        int bHitDirection = checkInteractableObject(out RaycastHit interactableHitInfo);

        updateInteractable(bHitDirection, interactableHitInfo);
    }

    public void SetTurningCW(bool value)
    {
        mController.Animator.SetIndex(value ? 0 : 1);
    }

    public void SetDefaultHeight(float height)
    {
        mDefaultHeight = height;
    }

    public void SetRotationDuration(float duration)
    {
        mRotationDuration = duration;
    }
    
    public void SetRotationCW(bool value)
    {
        mbRotationCW = value;
    }

    public void EnterWithoutAnimation()
    {
        mbEnterWithoutAnimation = true;
    }

    private void onAnimatorFixedUpdate()
    {
        // Left Hand
        // IK Weight
        //Vector3 leftShoulderBonePos = mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        //leftShoulderBonePos.z = 0f;
        //Vector3 ledgePoint = mLedgePoint;
        //ledgePoint.z = 0f;
        //float distanceShoulderToLedge = Vector3.Distance(leftShoulderBonePos, ledgePoint);
        float distanceShoulderToLedge = _climbLedgeState.GetDistanceShoulderToLedge(mLedgePoint);
        //float gapToLedge = distanceShoulderToLedge - _climbLedgeState.RaycastDistance;
        //float shoulderToElbowLength = Vector3.Distance(mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position, mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
        //float elbowToHandLength = Vector3.Distance(mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position, mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
        //float armLength = shoulderToElbowLength + elbowToHandLength;
        float armLength = _climbLedgeState.GetArmLength();
        float gapToLedge = distanceShoulderToLedge - armLength; // _raycastDistance;
        // float gapToLedge = mDetectedLedgeInfo.gapToLedge;
        float maxGapToLedge = mDetectedLedgeInfo.maxGapToLedge;
        float gapToLedgeRatio = Mathf.Clamp01(gapToLedge / maxGapToLedge);
        float weight = 1 - gapToLedgeRatio;

        float ledgeHeightFromFeet = mLedgePoint.y - mCharacterPosition.y;
        float lerpHeightRange = .6f;
        float minLerpHeight = .5f;
        float maxLerpHeight = minLerpHeight + lerpHeightRange;
        float lerpHeightRatio = Mathf.Clamp01((ledgeHeightFromFeet - minLerpHeight) / lerpHeightRange);
        weight = weight * lerpHeightRatio;

        _climbLedgeState.LeftHandIK.weight = weight;

        // Debug.Log($"[{Time.frameCount}] Gap To Ledge: {gapToLedge}, Max Gap To Ledge: {maxGapToLedge}, IK Weight: {weight}");

        // IK Target Position
        // Vector3 leftHandBonePos = mController.Animator.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        // Vector3 leftHandTargetPos = mLedgePoint;
        //// leftHandTargetPos.z = -.2f; //leftHandBonePos.z;
        // leftHandTargetPos.z = _climbLedgeState.GetLeftHandIKTargetPosition().z;
        // _climbLedgeState.LeftHandIK.data.target.position = leftHandTargetPos;
        _climbLedgeState.LeftHandIK.data.target.position = _climbLedgeState.GetLeftHandIKTargetPosition(mDetectedLedgeInfo);

        // Head Aim
        Vector3 aimTarget = mDetectedLedgeInfo.nearestLedgePoint;
        aimTarget.z = 0f;
        _climbLedgeState.HeadAimIK.data.sourceObjects.GetTransform(0).position = aimTarget;
        _climbLedgeState.HeadAimIK.weight = weight;

        // Right Hand
        // IK Weight
        _climbLedgeState.RightHandIK.weight = weight;

        // IK Target Position
        //Vector3 rightHandBonePos = mController.Animator.Animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        //Vector3 rightHandTargetPos = mLedgePoint;
        //// rightHandTargetPos.z = .2f; // rightHandBonePos.z;
        //rightHandTargetPos.z = _climbLedgeState.GetRightHandIKTargetPosition().z;
        //_climbLedgeState.RightHandIK.data.target.position = rightHandTargetPos;
        _climbLedgeState.RightHandIK.data.target.position = _climbLedgeState.GetRightHandIKTargetPosition(mDetectedLedgeInfo);

        // IK Target Rotation
        Vector3 normal = Vector3.up;
        Vector3 up = mController.Movement.DirectionToVector();
        Vector3 forward = -normal;
        Quaternion targetRot = Quaternion.LookRotation(forward, up);
        _climbLedgeState.LeftHandIK.data.target.rotation = targetRot;
        _climbLedgeState.RightHandIK.data.target.rotation = targetRot;
        _climbLedgeState.LeftHandIK.data.targetRotationWeight = 1f;
        _climbLedgeState.RightHandIK.data.targetRotationWeight = 1f;
    }

    // TODO: Interactable 처리 static 클래스 만들기
    private int checkInteractableObject(out RaycastHit hitInfo)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = mCharacterPosition;
        pathOrigin.y += mInteractableOffsetY;
        // pathOrigin.z = 0f;
        pathOrigin.z = mPathZPosition;

        // 현재 캐릭터의 위치
        //Vector3 characterOrigin = transform.position;
        //characterOrigin.y += _interactableOffsetY;

        // 현재 캐릭터 발을 기준으로 한 위치
        Vector3 characterFeetOrigin = mCharacterPosition;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out hitInfo,
                                        mInteractableMaxDistance,
                                        LayerMask.GetMask("Interactable"));

        if (bFrontCasted)
            return 1;

        bool bUnderCasted = Physics.Raycast(characterFeetOrigin,
                                    Vector3.down,
                                    out hitInfo,
                                    .1f,
                                    LayerMask.GetMask("Interactable"));

        if (bUnderCasted)
            return 3;

        bool bBackCasted = Physics.Raycast(pathOrigin,
                                    PlayerMovement.DirectionToVector(mController.Movement.OppositeDirection),
                                    out hitInfo,
                                    mInteractableMaxDistance,
                                    LayerMask.GetMask("Interactable"));

        if (bBackCasted)
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
            Vector3 characterPos = mCharacterPosition;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // 전방의 오브젝트를 옆으로 비켜지나가는 코드
            if (interactableObject.SidePassable && characterPos.z > mPathZPosition - mSidePassZDistance)
            {
                // 가까운 모서리를 기준으로 zDistance 떨어진 점을 targetPos로 설정
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // targetPos.y = 0f;
                targetPos.y = characterPos.y;
                targetPos.z = mPathZPosition - mSidePassZDistance;

                // targetPos까지의 방향을 normalize해서 x:z 비율로 velocity.z를 계산 
                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
            }

            //// Ladder
            //if ((interactableObject.CompareTag("Ladder"))
            //    && distanceToEdge < mInteractableDistance)
            //{
            //    Collider[] ladderCollider = new Collider[1];
            //    ladderCollider[0] = hitInfo.collider;
            //    bool bSwitched = switchToLadderState(ladderCollider);

            //    if (bSwitched)
            //        return;
            //}
        }
        // under
        else if (type == 3)
        {
            // Debug.Log(hitInfo.collider.name);
        }
        // back
        else if (type == 0)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            Bounds bounds = interactableObject.BoxCollider.bounds;
            Vector3 characterPos = mCharacterPosition;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // 오브젝트를 비켜지나가고 나서 z위치를 다시 0으로 맞춰주는 코드
            if (interactableObject.SidePassable && characterPos.z < mPathZPosition)
            {
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // 오브젝트를 감지할 수 있는 최대 거리까지 서서히 맞춰주게 함
                targetPos.x += (characterPos.x < bounds.center.x) ? -mInteractableMaxDistance : mInteractableMaxDistance;
                targetPos.y = characterPos.y;
                targetPos.z = mPathZPosition;

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

    private bool switchToLadderState(Collider[] ladderColliders)
    {
        foreach (Collider ladderCollider in ladderColliders)
        {
            // Bottom
            // Todo: InputHandler.IsUpPressed() 정의하기
            // if (mController.InputHandler.MoveInput.y > .1f)
            {
                if (ladderCollider.tag == "LadderTop")
                    continue;

                // PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase<PlayerLadderState>();
                LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

                // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
                if (ladderStateBase.IsOverRange(ladderHandler))
                    continue;

                // ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);
                bool bClimbLadder = ladderStateBase.SetLadderInMiddle(ladderHandler);

                if (!bClimbLadder)
                    return false;

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                mController.StateMachine.SwitchState<PlayerLadderState>();
                return true;
            }
        }

        return false;
    }

}

