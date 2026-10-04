using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayableRotation : RotationBase
{
    private TurnPlayable mTurnPlayable;

    private Quaternion mStartRotation;
    private Quaternion mTargetRotation;
    private Vector3 mStartEulerAngles;
    private Vector3 mTargetEulerAngles;
    private Vector3 mStartPosition;
    private float mRemainAngle;
    private bool mbStartExit = false;

    public PlayableRotation(RotationHandler rotationHandler, PlayerController playerController, TurnPlayable turnPlayable) : base(rotationHandler, playerController)
    {
        mTurnPlayable = turnPlayable;
    }

    public override void Start()
    {
    }

    public override void OnDirectionchanged()
    {
        // mPlayerController.Animation.Play(AnimState.Run);

        mStartPosition = mPlayerController.Movement.Position;

        mStartRotation = getCurrentRotation();
        mStartEulerAngles = getCurrentEulerAngles();
        mPlayerController.Movement.SetDirection(mPlayerController.Movement.OppositeDirection);
        mTargetEulerAngles = getCurrentEulerAngles();

        mRemainAngle = getRemainAngle(mTargetEulerAngles.y);

        mTurnPlayable.StartTurn();
    }

    public override void StandBy()
    {
    }

    public override void FixedUpdate()
    {
        bool bBlending = mTurnPlayable.BlendToTurn();

        float angle = mTurnPlayable.EvaluateAngle();

        Quaternion newRotation = mStartRotation * Quaternion.Euler(0f, angle, 0f);
        mPlayerController.Movement.SetRotation(newRotation);

        Vector3 position = mTurnPlayable.EvaluatePosition();

        // if (!mbStartExit)
        {
            Vector3 newPosition = mStartPosition + position;
            newPosition.x *= PlayerMovement.DirectionToVector(mPlayerController.Movement.Direction).x;
            mPlayerController.Movement.SetPosition(newPosition);
        }
    }

    public override void Update()
    {
        if(mTurnPlayable.StartExit)
        {
            if(!mbStartExit)
            {
                mbStartExit = true;
                mTurnPlayable.ControllerPlayable.Play(AnimStateHash.stateHashes[AnimState.Run], 0, .725f);
                // mPlayerController.Animation.Play(AnimState.Run, .25f);
            }

            bool bBlending = mTurnPlayable.BlendToAnimatorController();

            if(!bBlending)
            {
                mRotationHandler.EndRotation();
            }
        }
    }
}
