using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerPushPullState : PlayerStateBase
{
    public struct PushPullInfo
    {
        public PlayerInteractable.InteractedInfo interactedInfo;
        public PushPullObject targetObject;
        public EPushPullType type;
    }

    public enum EPushPullType { Side, Front_Push, Front_PushPull }

    public float FrontPushPullDistance => _frontPushPullDistance;

    [SerializeField]
    private float _frontPushPullDistance = .5f;
    [SerializeField]
    private float _frontPushPullRadius = .5f;
    [SerializeField]
    private float _pushPullForce = 1f;
    [SerializeField]
    private float _pushPullSpeed = 1f;
    [SerializeField]
    private float _pushPullSpeedAdditionalLimit = 1f;

    private Animator Animator => mAnimation.Animator;

    private bool mbPushPull = true;
    private PushPullInfo mPushPullInfo;
    private EPushPullType mPushPullType;
    private PushPullObject mPushPullObject;
    private Vector3 mPushPoint;
    private float mDistanceToObject;
    [Obsolete] private int mType; // side: 0, front: 1
    [Obsolete] private int mAnimType = 0; // idle: 0, push: 1, pull: 2
    private PlayerMovement.EDirection mCharacterDirection;
    private PlayerMovement.EDirection mPushPullDirection;
    private PlayerMovement.EDirection mCurrentDirection;

    private bool mbLerp = false;
    private float mLerpTimer = 0f;
    private float mLerpDuration = .2f;
    private Vector3 mLerpStartPosition;
    private Vector3 mLerpTargetPosition;

    private bool mbActiveIK = false;
    private Vector3 mLeftHandIKPos;
    private Vector3 mRightHandIKPos;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        // Animator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mbLerp = false;
        mbPushPull = false;
        mbActiveIK = false;

        mPushPullType = mPushPullInfo.type;
        mPushPullObject = mPushPullInfo.targetObject;
        mPushPoint = mPushPullInfo.interactedInfo.hitInfo.point;
        mDistanceToObject = mPushPullObject.transform.position.x - mCharacterPosition.x;
        mCharacterDirection = mMovement.Direction;
        mPushPullDirection = mCharacterDirection;
        mCurrentDirection = mCharacterDirection;
        // mController.Animator.SetIndex(mType);
        mAnimation.SetMultiplier(0f);

        mMovement.SetFriction(false);

        if(mPushPullType == EPushPullType.Side)
        {
            setSidePushPullAnimation(mCharacterDirection);
        }
        else if (mPushPullType == EPushPullType.Front_Push || mPushPullType == EPushPullType.Front_PushPull)
        {
            setLerp();

            // mMovement.SetRadius(mFrontPushPullRadius);
            mAnimation.Play((mPushPullType == EPushPullType.Front_Push) ? AnimState.PushPull_Front_Push : AnimState.PushPull_Front_Idle);

            #region PushPull Position (Deprecated)
            //if (mMovement.Direction == PlayerMovement.EDirection.Right)
            //{
            //    Vector3 newPosition = mCharacterPosition;
            //    newPosition.x = mPushPoint.x - _frontPushPullDistance;
            //    mMovement.SetPosition(newPosition);
            //}
            //else if(mMovement.Direction == PlayerMovement.EDirection.Left)
            //{
            //    Vector3 newPosition = mCharacterPosition;
            //    newPosition.x = mPushPoint.x + _frontPushPullDistance;
            //    mMovement.SetPosition(newPosition);
            //}
            #endregion
        }

        //mController.Animator.onAnimationIK -= updateAnimationIK;
        //mController.Animator.onAnimationIK += updateAnimationIK;

        // StartCoroutine(eHandIKPos());
    }

    public override void ExitState()
    {
        mMovement.SetFriction(true);

        if (mPushPullType == EPushPullType.Front_Push || mPushPullType == EPushPullType.Front_PushPull)
        {
            // mMovement.SetRadius();
        }

        // mController.Animator.onAnimationIK -= updateAnimationIK;
    }

    public override void FixedTick()
    {
        if(mbLerp)
        {
            lerp();
            return;
        }

        switch (mPushPullType)
        {
            case EPushPullType.Side:
                sideFixedTick();
                break;
            case EPushPullType.Front_Push:
                frontPushFixedTick();
                break;
            case EPushPullType.Front_PushPull:
                frontPushPull();
                break;
        }
    }

    public override void Tick()
    {
        if(mbLerp)
        {
            // mLerpTimer += Time.deltaTime;
            return;
        }

        switch(mPushPullType)
        {
            case EPushPullType.Side:
                sideTick();
                break;
            case EPushPullType.Front_Push:
                frontPushTick(); 
                break;
            case EPushPullType.Front_PushPull:
                frontPushPull();
                break;
        }

        return;

        #region Front Push Deprecated
        ////if (mType == 2)
        //if(mPushPullType == EPushPullType.Front_Push)
        //{
        //    mMovement.Move(mInputHandler.MoveInput * .2f);
        //    mPushPullObject.StayPushPull();

        //    bool bInteracted = mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Front, out PlayerInteractable.InteractedInfo interactedInfo);
        //    var pushPullDirectionKey = mPushPullDirection == PlayerMovement.EDirection.Left ? PlayerInputHandler.PressKey.Left : PlayerInputHandler.PressKey.Right;

        //    if (!bInteracted || (bInteracted && interactedInfo.distanceToEdge > mFrontPushPullDistance + .1f) 
        //        || !mInputHandler.IsKeyPressed(pushPullDirectionKey))
        //    {
        //        mbPushPull = false;
        //        mPushPullObject.StopPushPull();

        //        mController.StateMachine.SwitchState<PlayerIdleState>();
        //    }

        //    //int hitIndex = checkInteractableObject(out RaycastHit hitInfo);
        //    //// float pushPullDistance = Mathf.Abs(mPushPoint.x - transform.position.x);

        //    //if (Mathf.Abs(mController.InputHandler.MoveInputRaw.x) < .01f
        //    //    || hitIndex == -1)
        //    //{
        //    //    mbPushPull = false;
        //    //    mPushPullObject.StopPushPull();

        //    //    var moveState = mController.StateMachine.GetStateBase<PlayerIdleState>();
        //    //    // moveState.EnterToIdle();

        //    //    mAnimType = 0;
        //    //    mController.Animator.SetIndex(mAnimType);
        //    //    mController.StateMachine.SwitchState<PlayerIdleState>();
        //    //}

        //    return;
        //}
        #endregion

        #region Side, Front PushPull Deprecated

        if (!mController.InputHandler.IsInteracting || !mbPushPull)
        {
            mAnimType = 0;
            mController.Animation.SetIndex(mAnimType);
            mController.StateMachine.SwitchState<PlayerIdleState>();
            return;
        }

        // if(mType == 1)
        if(mPushPullType == EPushPullType.Front_PushPull)
        {
            if (mCharacterDirection == PlayerMovement.EDirection.Left)
            {
                if (mController.InputHandler.MoveInput.x > .1f)
                    mAnimType = 2; // pull
                else if (mController.InputHandler.MoveInput.x < -.1f)
                    mAnimType = 1; // push
                else
                    mAnimType = 0; // idle
            }
            else
            {
                if (mController.InputHandler.MoveInput.x > .1f)
                    mAnimType = 1; // push
                else if (mController.InputHandler.MoveInput.x < -.1f)
                    mAnimType = 2; // pull
                else
                    mAnimType = 0; // idle
            }

            mController.Animation.SetIndex(mAnimType);
        }

        mController.Animation.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        // mController.Movement.Move(mController.InputHandler.MoveInput);
        // Debug.Log(mAnimator.velocity);
        Vector3 animationVelocity = Animator.velocity;
        animationVelocity.z = 0f;
        // mController.Movement.SetVelocity(animationVelocity);

        float moveInputX = mController.InputHandler.MoveInput.x;
        float pushPullMultiplier = 0f;
        PlayerMovement.EDirection currentDirection = PlayerMovement.EDirection.Neutral;

        if(moveInputX > .1f)
        {
            currentDirection = PlayerMovement.EDirection.Right;
            // mPushPullObject.SetFriction(false);
            pushPullMultiplier = 1f;
        }
        else if(moveInputX < -.1f)
        {
            currentDirection = PlayerMovement.EDirection.Left;
            // mPushPullObject.SetFriction(false);
            pushPullMultiplier = -1f;
        }
        else
        {
            // mPushPullObject.SetFriction(true);
        }

        if(currentDirection != PlayerMovement.EDirection.Neutral && currentDirection != mPushPullDirection)
        {
            mPushPullDirection = currentDirection;
            setSidePushPullAnimation(currentDirection);
        }

        // mController.Movement.SetVelocity(pushPullMultiplier * Vector3.right * .8f);
        // mController.Animator.SetHorizontal(pushPullMultiplier);

        // mPushPullObject.PushPull(animationVelocity);
        mbPushPull = mPushPullObject.PushPull(mController, pushPullMultiplier * Vector3.right * .8f);
        float speedMultiplier = mPushPullObject.GetVelocityXRatio();
        // mController.Animator.SetHorizontal(Mathf.Abs(speedMultiplier));
        mController.Animation.SetMultiplier(Mathf.Abs(speedMultiplier));

        Vector3 newPosition = mCharacterPosition;
        newPosition.x = mPushPullObject.transform.position.x - mDistanceToObject;
        mController.Movement.SetPosition(newPosition);

#endregion
    }

    public bool CheckPushPull(EPushPullType type, out PushPullInfo pushPullInfo)
    {
        bool bResult = false;

        switch (type)
        {
            case EPushPullType.Side:
                bResult = checkSide(out pushPullInfo);
                break;
            case EPushPullType.Front_Push:
                bResult = checkFrontPush(out pushPullInfo);
                break;
            case EPushPullType.Front_PushPull:
                bResult = checkFrontPushPull(out pushPullInfo);
                break;
            default:
                pushPullInfo = new PushPullInfo();
                break;
        }

        pushPullInfo.type = type;
        return bResult;
    }

    public void SetPushPullInfo(PushPullInfo pushPullInfo)
    {
        mPushPullInfo = pushPullInfo;
    }

    [Obsolete]
    public void SetPushPullType(EPushPullType type)
    {
        mPushPullType = type;
    }

    [Obsolete]
    public void SetPushPullObject(PushPullObject pushPullObject)
    {
        mPushPullObject = pushPullObject;

        mDistanceToObject = mPushPullObject.transform.position.x - mCharacterPosition.x;
    }

    /// <summary>
    /// side : 0, front : 1
    /// </summary>
    /// <param name="type"></param>
    [Obsolete]
    public void SetType(int type)
    {
        mType = type;
    }

    [Obsolete]
    public void SetPushPoint(Vector3 point)
    {
        mPushPoint = point;
    }

    private void setLerp()
    {
        mbLerp = true;
        mLerpTimer = 0f;

        Vector3 newPosition = mCharacterPosition;

        switch(mMovement.Direction)
        {
            case PlayerMovement.EDirection.Left:
                newPosition.x = mPushPoint.x + _frontPushPullDistance;
                break;
            case PlayerMovement.EDirection.Right:
                newPosition.x = mPushPoint.x - _frontPushPullDistance;
                break;
            default:
                break;
        }

        mLerpStartPosition = mCharacterPosition;
        mLerpTargetPosition = newPosition;
    }

    private void lerp()
    {
        if(mLerpTimer > mLerpDuration)
        {
            mbLerp = false;
            mMovement.SetPosition(mLerpTargetPosition);
            return;
        }

        float t = mLerpTimer / mLerpDuration;
        Vector3 lerpedPosition = Vector3.Lerp(mLerpStartPosition, mLerpTargetPosition, t);
        mMovement.SetPosition(lerpedPosition);

        GameDebug.Log($"Start Position: {mLerpStartPosition}, TargetPosition: {mLerpTargetPosition}, t: {t}, Current Position: {mCharacterPosition}",
            tag: "PushPull Lerp");

        mLerpTimer += Time.fixedDeltaTime;
    }

    private bool checkSide(out PushPullInfo pushPullInfo)
    {
        pushPullInfo = new PushPullInfo();

        if (mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Side, out pushPullInfo.interactedInfo))
        {
            PlayerInteractable.InteractedInfo interactedInfo = pushPullInfo.interactedInfo;
            pushPullInfo.targetObject = interactedInfo.interactableObject as PushPullObject;

            float distanceToEdgeByZ = Mathf.Abs(mCharacterPosition.z - interactedInfo.hitInfo.point.z);

            if (distanceToEdgeByZ < mInteractable.InteractableDistance + .2f)
            {
                if (interactedInfo.interactableObject.Pushable)
                {
                    // pushPullObject = interactedInfo.interactableObject as PushPullObject;
                    return true;
                }
            }
        }

        return false;
    }

    private bool checkFrontPush(out PushPullInfo pushPullInfo)
    {
        pushPullInfo = new PushPullInfo();

        if (mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Front, out pushPullInfo.interactedInfo))
        {
            PlayerInteractable.InteractedInfo interactedInfo = pushPullInfo.interactedInfo;
            InteractableObject interactableObject = interactedInfo.interactableObject;
            pushPullInfo.targetObject = interactableObject as PushPullObject;

            RaycastHit hitInfo = interactedInfo.hitInfo;
            float distanceToEdge = interactedInfo.distanceToEdge;

            if (!interactableObject.SidePassable && interactableObject.Pushable && distanceToEdge < _frontPushPullDistance && mInputHandler.CheckInputX())
            {
                //mController.StateMachine.SwitchState<PlayerPushPullState>((state) =>
                //{
                //    state.SetPushPullObject(interactableObject as PushPullObject);
                //    state.SetPushPullType(PlayerPushPullState.EPushPullType.Front_Push);
                //    state.SetPushPoint(hitInfo.point);
                //});

                return true;
            }
        }
        return false;
    }

    private bool checkFrontPushPull(out PushPullInfo pushPullInfo)
    {
        pushPullInfo = new PushPullInfo();

        if (mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Front, out pushPullInfo.interactedInfo))
        {
            PlayerInteractable.InteractedInfo interactedInfo = pushPullInfo.interactedInfo;
            InteractableObject interactableObject = interactedInfo.interactableObject;
            pushPullInfo.targetObject = interactableObject as PushPullObject;

            RaycastHit hitInfo = interactedInfo.hitInfo;
            float distanceToEdge = interactedInfo.distanceToEdge;

            if (!interactableObject.SidePassable && interactableObject.Pushable && distanceToEdge < mInteractable.InteractableDistance && mInputHandler.IsInteracting)
            {
                //mController.StateMachine.SwitchState<PlayerPushPullState>((state) =>
                //{
                //    state.SetPushPullObject(interactableObject as PushPullObject);
                //    state.SetPushPullType(PlayerPushPullState.EPushPullType.Front_PushPull);
                //    state.SetPushPoint(hitInfo.point);
                //});

                return true;
            }
        }

        return false;
    }

    private void sideFixedTick()
    {
        if (!mbPushPull)
            return;

        float characterVelocityX = mMovement.Velocity.x;
        float targetObjectVelocityX = mPushPullObject.VelocityX;

        float calculatedForce = calculatePushPullForce(characterVelocityX, targetObjectVelocityX);
        Vector3 forceDirection = PlayerMovement.DirectionToVector(mCurrentDirection);
        Vector3 pushPullForce = forceDirection * calculatedForce;
        mPushPullObject.PushPull(pushPullForce);

        #region Calculate Force (Deprecated)
        ////float directionMultiplier = getDirectionMultiplier(mCurrentDirection);
        ////mPushPullObject.PushPull(mController, directionMultiplier * Vector3.right * .8f);
        //Vector3 pushPullForce = mCurrentDirection == PlayerMovement.EDirection.Left ? Vector3.left : Vector3.right;
        //float characterVelocityX = mMovement.Velocity.x;
        //float pushPullObjectVelocityX = mPushPullObject.VelocityX;
        //float characterVelocityXMagitude = Mathf.Abs(characterVelocityX);
        //float pushPullObjectVelocityXMagnitude = Mathf.Abs(pushPullObjectVelocityX);
        //float speedMultiplier = 0f;

        //if (characterVelocityXMagitude > .01f)
        //{
        //    // float t = pushPullObjectVelocityX / _pushPullSpeed * pushPullForce.x;
        //    float t = pushPullObjectVelocityXMagnitude / (characterVelocityXMagitude + _pushPullSpeedAdditionalLimit);
        //    float forceMultiplier = characterVelocityXMagitude / _pushPullSpeed;
        //    //pushPullForce *= _pushPullForce * (1 - Mathf.Clamp01(t));
        //    pushPullForce *= _pushPullForce * Mathf.Clamp01(forceMultiplier) * (1 - Mathf.Clamp01(t));
        //    mPushPullObject.PushPull(pushPullForce);

        //    GameDebug.Log($"pushPullSpeed: {_pushPullSpeed}, character Vel X: {characterVelocityX}, object Vel X: {pushPullObjectVelocityX}, t: {t}, force: {pushPullForce}",
        //        tag: "PushPull Force");

        //    speedMultiplier = pushPullObjectVelocityXMagnitude / characterVelocityXMagitude;
        //}
        #endregion

        // float speedMultiplier = mPushPullObject.GetVelocityXRatio();
        // float speedMultiplier = mInputHandler.GetInputMagnitude().x;
        //mAnimator.SetMultiplier(Mathf.Abs(speedMultiplier));

        float characterVelocityXMagnitude = Mathf.Abs(characterVelocityX);
        float targetObjectVelocityXMagnitude = Mathf.Abs(targetObjectVelocityX);
        float speedMultiplier = (characterVelocityX > .01f) ? targetObjectVelocityX / characterVelocityX : 0.0f;
        mAnimation.SetMultiplier(speedMultiplier);

        //Vector3 newPosition = mCharacterPosition;
        //newPosition.x = mPushPullObject.transform.position.x - mDistanceToObject;
        //mMovement.SetPosition(newPosition);
        mMovement.Move(mInputHandler.MoveInput, _pushPullSpeed);
    }

    private void sideTick()
    {
        if(isEnd())
        {
            endPushPull();
            return;
        }

        if (isDirectionChanged(out mCurrentDirection))
        {
            mPushPullDirection = mCurrentDirection;
            setSidePushPullAnimation(mCurrentDirection);
        }

        if(mCurrentDirection != PlayerMovement.EDirection.Neutral)
        {
            mbPushPull = true;
        }
        else
        {
            mbPushPull = false;
        }

        return;

        float directionMultiplier = getDirectionMultiplier(mCurrentDirection);
        // mbPushPull = mPushPullObject.PushPull(mController, directionMultiplier * Vector3.right * .8f);
        Vector3 pushPullForce = mCurrentDirection == PlayerMovement.EDirection.Left ? Vector3.left : Vector3.right;
        mPushPullObject.PushPull(pushPullForce);

        float speedMultiplier = mPushPullObject.GetVelocityXRatio();
        mAnimation.SetMultiplier(Mathf.Abs(speedMultiplier));

        Vector3 newPosition = mCharacterPosition;
        newPosition.x = mPushPullObject.transform.position.x - mDistanceToObject;
        mMovement.SetPosition(newPosition);
    }

    private void frontPushFixedTick()
    {
        float calculatedForce = calculatePushPullForce(mMovement.Velocity.x, mPushPullObject.VelocityX);
        Vector3 forceDirection = PlayerMovement.DirectionToVector(mCurrentDirection);
        Vector3 pushPullForce = calculatedForce * forceDirection;
        mPushPullObject.PushPull(pushPullForce);

        mMovement.Move(mInputHandler.MoveInput, _pushPullSpeed);
    }

    private void frontPushTick()
    {
        // mMovement.Move(mInputHandler.MoveInput * .2f);
        mPushPullObject.StayPushPull();

        bool bInteracted = mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Front, out PlayerInteractable.InteractedInfo interactedInfo);
        var pushPullDirectionKey = mPushPullDirection == PlayerMovement.EDirection.Left ? PlayerInputHandler.PressKey.Left : PlayerInputHandler.PressKey.Right;

        if (!bInteracted || (bInteracted && interactedInfo.distanceToEdge > _frontPushPullDistance + .1f)
            || !mInputHandler.IsKeyPressed(pushPullDirectionKey))
        {
            mbPushPull = false;
            mPushPullObject.StopPushPull();

            mStateMachine.SwitchState<PlayerIdleState>();
        }
    }

    private void frontPushPull()
    {
        if (isEnd())
        {
            endPushPull();
            return;
        }

        if (isDirectionChanged(out PlayerMovement.EDirection resultDirection))
        {

        }
    }

    private bool isEnd()
    {
        // if (!mInputHandler.IsInteracting || !mbPushPull)
        if (!mInputHandler.IsInteracting)
        {
            return true;
        }

        return false;
    }

    private void endPushPull()
    {
        mStateMachine.SwitchState<PlayerIdleState>();
    }

    private bool isDirectionChanged(out PlayerMovement.EDirection resultDirection)
    {
        float moveInputX = mInputHandler.MoveInput.x;
        resultDirection = PlayerMovement.EDirection.Neutral;

        if (mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Right))
        {
            resultDirection = PlayerMovement.EDirection.Right;
        }
        else if (mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Left))
        {
            resultDirection = PlayerMovement.EDirection.Left;
        }

        if (resultDirection != PlayerMovement.EDirection.Neutral && resultDirection != mPushPullDirection)
        {
            return true;
        }

        return false;
    }

    private float getDirectionMultiplier(PlayerMovement.EDirection direction)
    {
        if (direction == PlayerMovement.EDirection.Right)
            return 1.0f;
        else if (direction == PlayerMovement.EDirection.Left)
            return -1.0f;
        else
            return 0f;
    }

    private float calculatePushPullForce(float characterVelocityX, float pushPullObjectVelocityX)
    {
        float characterVelocityXMagitude = Mathf.Abs(characterVelocityX);
        float pushPullObjectVelocityXMagnitude = Mathf.Abs(pushPullObjectVelocityX);
        float resultForce = 0f;

        if (characterVelocityXMagitude > .01f)
        {
            float multiplierBySpeedDifference = pushPullObjectVelocityXMagnitude / (characterVelocityXMagitude + _pushPullSpeedAdditionalLimit);
            float clampedMultiplierBySpeedDifference = Mathf.Clamp01(multiplierBySpeedDifference);

            float multiplierByCharacterSpeed = characterVelocityXMagitude / _pushPullSpeed;
            float clampedMultiplierByCharacterSpeed = Mathf.Clamp01(multiplierByCharacterSpeed);

            resultForce = _pushPullForce * clampedMultiplierByCharacterSpeed * (1 - multiplierBySpeedDifference);

            GameDebug.Log($"PushPull Force: {resultForce}, " +
                $"multiplier By CharacterSpeed(clamped): {multiplierByCharacterSpeed}({clampedMultiplierByCharacterSpeed}), " +
                $"multiplier By CharacterSpeed(clamped): {multiplierBySpeedDifference}({clampedMultiplierBySpeedDifference})",
                tag: "PushPull Force");
        }

        return resultForce;
    }

    [Obsolete]
    private int checkInteractableObject(out RaycastHit hitInfo)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = mCharacterPosition;
        pathOrigin.y += 1f;
        // pathOrigin.z = 0f;
        pathOrigin.z = 0f;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out hitInfo,
                                        _frontPushPullDistance + .1f,
                                        LayerMask.GetMask("Interactable"));

        if (bFrontCasted)
            return 1;

        return -1;
    }

    private void setSidePushPullAnimation(PlayerMovement.EDirection direction)
    {
        if(mCharacterDirection == PlayerMovement.EDirection.Left)
            mController.Animation.Play((direction == PlayerMovement.EDirection.Left) ? AnimState.PushPull_LL : AnimState.PushPull_LR);
        if(mCharacterDirection == PlayerMovement.EDirection.Right)
            mController.Animation.Play((direction == PlayerMovement.EDirection.Left) ? AnimState.PushPull_RL : AnimState.PushPull_RR);
    }

    private IEnumerator eHandIKPos()
    {
        yield return new WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsTag("PushPull"));

        Bounds bounds = mPushPullObject.BoxCollider.bounds;

        mLeftHandIKPos = Animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        mLeftHandIKPos.z = bounds.min.z;
        mPushPullObject.HandlePointL.position = mLeftHandIKPos;

        mRightHandIKPos = Animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        mRightHandIKPos.z = bounds.min.z;
        mPushPullObject.HandlePointR.position = mRightHandIKPos;

        mbActiveIK = true;
    }

    private void updateAnimationIK()
    {
        if(!mbActiveIK)
            return;

        Animator.SetIKPosition(AvatarIKGoal.LeftHand, mPushPullObject.HandlePointL.position);
        Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
        Animator.SetIKPosition(AvatarIKGoal.RightHand, mPushPullObject.HandlePointR.position);
        Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
    }
}
