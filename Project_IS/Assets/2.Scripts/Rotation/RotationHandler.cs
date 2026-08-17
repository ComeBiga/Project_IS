using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class RotationHandler
{
    public enum EState { StandBy, DirectionChanged, Rotating }
    public enum EType { AnimationCurve, RootMotion, Offset180, Normal }
    public enum ERotationDirection { Left, Right }

    public PlayerController PlayerController => mPlayerController;
    public EState State => mState;
    public ERotationDirection RotationDirection => mRotationDirection;
    public bool IsRotating => mbIsRotating;
    public bool TurnToTurn => mbTurnToTurn;
    public AnimationCurveRotation AnimationCurveRotation => mAnimationCurveRotation;

    private RotationBase mCurrentRotation;
    private AnimationCurveRotation mAnimationCurveRotation;
    private RootMotionRotation mRootMotionRotation;
    private Offset180Rotation mOffset180Rotation;
    private NormalRotation mNormalRotation;

    private PlayerController mPlayerController;
    private EState mState = EState.StandBy;
    private EType mType = EType.AnimationCurve;
    private ERotationDirection mRotationDirection;
    private bool mbIsRotating = false;
    private bool mbDirectionChanged = false;
    private bool mbEnterUpdate = false;
    private bool mbEnterFixedUpdate = false;
    private bool mbEnterAnimatorMove = false;
    private bool mbTurnToTurn = false;
    private float mInputInterval = 0f;
    private float mInputTimer = 0f;

    public void Init(PlayerController playerController)
    {
        mPlayerController = playerController;

        mAnimationCurveRotation = new AnimationCurveRotation(this);
        mRootMotionRotation = new RootMotionRotation(this);
        mOffset180Rotation = new Offset180Rotation(this);
        mNormalRotation = new NormalRotation(this);

        mPlayerController.Animator.onAnimatorMove += onAnimatorMove;

        mState = EState.StandBy;
        mbIsRotating = false;
        mbDirectionChanged = false;
        mbEnterUpdate = false;
        mbEnterFixedUpdate = false;
        mInputTimer = mInputInterval;
    }

    public void SetType(EType type)
    {
        mType = type;

        switch(mType)
        {
            case EType.AnimationCurve:
                mCurrentRotation = mAnimationCurveRotation;
                break;
            case EType.RootMotion:
                mCurrentRotation = mRootMotionRotation;
                break;
            case EType.Offset180:
                mCurrentRotation = mOffset180Rotation;
                break;
            case EType.Normal:
                mCurrentRotation = mNormalRotation;
                break;
        }
    }

    public void StartRotation()
    {
        mState = EState.Rotating;
        mbIsRotating = true;
        mbEnterUpdate = false;
        mbEnterFixedUpdate = false;
        mbEnterAnimatorMove = false;

        // mPlayerController.Animator.SetTurnTrigger();
    }

    public void EndRotation()
    {
        mState = EState.StandBy;
        mbIsRotating = false;
        mbEnterUpdate = false;
        mbEnterFixedUpdate = false;
        mbEnterAnimatorMove = false;

        mCurrentRotation.OnEndRotation();
    }

    public void SetRotationDirection(ERotationDirection rotationDirection)
    {
        mRotationDirection = rotationDirection;
    }

    public void FixedUpdate()
    {
        if (mState != EState.Rotating)
            return;

        if (!mbEnterFixedUpdate)
        {
            mbEnterFixedUpdate = true;
            mCurrentRotation.OnBeforeFixedUpdate();
        }

        mCurrentRotation.FixedUpdate();
    }

    public void Update()
    {
        mInputTimer += Time.deltaTime;

        if (mInputTimer > mInputInterval && checkOppositeInputX())
        {
            mInputTimer = 0f;

            if (mState == EState.Rotating)
            {
                mbTurnToTurn = true;
            }
            else
            {
                mbTurnToTurn = false;
            }

            mState = EState.DirectionChanged;
        }

        UpdateTurnState();
    }

    public void UpdateTurnState()
    {
        switch (mState)
        {
            case EState.StandBy:
                standBy();
                break;
            case EState.DirectionChanged:
                directionChanged();
                break;
            case EState.Rotating:
                rotate();
                break;
        }
    }

    public void Standby()
    {
        if (mState != EState.StandBy)
            return;

        standBy();
    }

    public void SetTurnState(EState state)
    {
        mState = state;
    }

    public void SetTurnType(PlayerTurnState.ETurnType turnType)
    {
        mNormalRotation.SetTurnType(turnType);

        mPlayerController.Animator.SetIndex((turnType == PlayerTurnState.ETurnType.Idle) ? 0 : 1);
    }

    private void onAnimatorMove()
    {
        if (mState != EState.Rotating)
            return;

        if (!mbEnterAnimatorMove)
        {
            mbEnterAnimatorMove = true;
            mCurrentRotation.OnBeforeAnimatorMove();
        }

        mCurrentRotation.OnAnimatorMove();
    }

    private void standBy()
    {
        mCurrentRotation.StandBy();
    }

    private void directionChanged()
    {
        mCurrentRotation.OnDirectionchanged();

        StartRotation();
    }

    private void rotate()
    {
        if (!mbEnterUpdate)
        {
            mbEnterUpdate = true;
            mCurrentRotation.OnBeforeUpdate();
        }

        mCurrentRotation.Update();
    }

    private bool checkOppositeInputX()
    {
        bool bOppositePressed = mPlayerController.InputHandler.MoveInputXOppositePressed;
        mPlayerController.InputHandler.ResetMoveInputXOppositePressed();

        if (bOppositePressed)
        {
            return true;
        }

        EDirection InputXDirection = PlayerMovement.MoveInputXToDirection(mPlayerController.InputHandler.MoveInput.x);

        if (Mathf.Abs(mPlayerController.InputHandler.MoveInput.x) > .001f && InputXDirection == mPlayerController.Movement.OppositeDirection)
        {
            return true;
        }

        return false;
    }
}
