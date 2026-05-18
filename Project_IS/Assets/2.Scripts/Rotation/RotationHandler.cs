using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

public class RotationHandler
{
    public enum EState { StandBy, DirectionChanged, Rotating }
    public enum EType { AnimationCurve, RootMotion, Offset180 }

    public PlayerController PlayerController => mPlayerController;
    public EState State => mState;
    public bool IsRotating => mbIsRotating;

    private RotationBase mCurrentRotation;
    private AnimationCurveRotation mAnimationCurveRotation;
    private RootMotionRotation mRootMotionRotation;
    private Offset180Rotation mOffset180Rotation;

    private PlayerController mPlayerController;
    private EState mState = EState.StandBy;
    private EType mType = EType.AnimationCurve;
    private bool mbIsRotating = false;
    private bool mbDirectionChanged = false;
    private bool mbEnterRotate = false;


    public void Init(PlayerController playerController)
    {
        mPlayerController = playerController;

        mAnimationCurveRotation = new AnimationCurveRotation(this);
        mRootMotionRotation = new RootMotionRotation(this);
        mOffset180Rotation = new Offset180Rotation(this);

        mState = EState.StandBy;
        mbIsRotating = false;
        mbDirectionChanged = false;
        mbEnterRotate = false;
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
        }
    }

    public void StartRotation()
    {
        mState = EState.Rotating;
        mbIsRotating = true;
    }

    public void EndRotation()
    {
        mState = EState.StandBy;
        mbIsRotating = false;
        mbEnterRotate = false;
    }

    public void Update()
    {
        if (checkOppositeInputX())
        {
            mState = EState.DirectionChanged;
        }

        switch(mState)
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

    public void FixedUpdate()
    {
        if (mState == EState.Rotating)
        {
            mCurrentRotation.FixedUpdate();
        }
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
        if(!mbEnterRotate)
        {
            mbEnterRotate = true;
            mCurrentRotation.OnBeforeRotate();
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
