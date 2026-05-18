using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCurveRotation : RotationBase
{
    private float mRunTurnMoveSpeed = 1f;

    private Vector3 mPivotPosition;
    private float mMoveDistance;
    private float mRemainAngles;

    public AnimationCurveRotation(RotationHandler rotationHandler) : base(rotationHandler)
    {

    }

    public override void Start()
    {

    }

    public override void OnDirectionchanged()
    {
        mPlayerController.Movement.SetDirection(mPlayerController.Movement.OppositeDirection);
        mPlayerController.Animator.TurnL(true);
        mPlayerController.Animator.TurnR(false);
        mPivotPosition = mPlayerController.transform.position;
        mMoveDistance = 0f;
        mRemainAngles = Vector3.SignedAngle(mPlayerController.transform.forward, mPlayerController.Movement.DirectionToVector(), Vector3.up);
        // mRemainAngles = (mRemainAngles + Number.DEG_360) % Number.DEG_360;
        mRemainAngles = Mathf.Abs(mRemainAngles);

        Debug.Log($"Direction Changed");
    }

    public override void OnBeforeRotate()
    {
        mPlayerController.Animator.TurnL(false);
        mPlayerController.Animator.TurnR(false);
    }

    public override void Update()
    {

    }

    public override void FixedUpdate()
    {
        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

        if(currentStateInfo.IsTag("Turn"))
        {
            //float positionValue = _runTurnPositionCurve.Evaluate(currentStateInfo.normalizedTime);
            float positionValue = mAnimator.GetFloat("PositionCurve");
            float currentPosition = positionValue * mRunTurnMoveSpeed;

            //float lastPositionValue = _runTurnPositionCurve.Evaluate(currentStateInfo.normalizedTime - Time.deltaTime);
            //float deltaPosition = positionValue - lastPositionValue;
            // float velocity = deltaPosition / Time.deltaTime;
            float velocity = positionValue * mPlayerController.Movement.MoveSpeed * mRunTurnMoveSpeed;

            Vector3 moveDirection = mPlayerController.Movement.DirectionToVector();
            mPlayerController.transform.position = mPivotPosition + moveDirection * currentPosition;
            // transform.position += deltaPosition * moveDirection;
            // mController.Movement.SetVelocity(moveDirection * velocity * _runTurnMoveSpeed);
            // mController.Movement.SetVelocity(moveDirection * velocity);

            //if (positionValue < 0.01f)
            //{
            //    transform.position = mRotationPivotPosition + moveDirection * currentPosition;
            //}
            //else
            //{
            //    mController.Movement.SetVelocity(moveDirection * velocity);
            //}

            if (mAnimator.IsInTransition(0))
            {
                // mbRotating = false;
                mRotationHandler.EndRotation();
            }

            mMoveDistance += velocity * Time.deltaTime;

            // Debug.Log($"normalizedTime: {currentStateInfo.normalizedTime}, positionValue: {positionValue}, velocity: {velocity}, rigidbody velocity: {mController.Movement.Velocity}, moveDirection: {moveDirection}, deltaPosition: {velocity * Time.deltaTime}, moveDistance: {mRotationMoveDistance}");
        }

        // float rotationValue = _runTurnRotationCurve.Evaluate(currentStateInfo.normalizedTime);
        float rotationValue = mAnimator.GetFloat("RotationCurve");

        if (currentStateInfo.IsTag("Turn") && rotationValue < .99f)
        {
            //float positionValue = _runTurnPositionCurve.Evaluate(currentStateInfo.normalizedTime);
            //float currentPosition = positionValue * _runTurnMoveSpeed;

            //float lastPositionValue = _runTurnPositionCurve.Evaluate(currentStateInfo.normalizedTime - Time.deltaTime);
            //float deltaPosition = positionValue - lastPositionValue;
            //// float velocity = deltaPosition / Time.deltaTime;
            //float velocity = positionValue * mController.Movement.MoveSpeed;

            //Vector3 moveDirection = mController.Movement.DirectionToVector();
            //// transform.position = mRotationPivotPosition + moveDirection * currentPosition;
            //// mController.Movement.SetVelocity(moveDirection * velocity * _runTurnMoveSpeed);
            //mController.Movement.SetVelocity(moveDirection * velocity);

            // float rotationValue = _runTurnRotationCurve.Evaluate(currentStateInfo.normalizedTime);
            // float currentAngles = rotationValue * Number.DEG_180;
            float currentAngles = rotationValue * mRemainAngles;

            PlayerMovement.EDirection previousDirection = mPlayerController.Movement.OppositeDirection;
            Vector3 newEulerAngles = PlayerMovement.DirectionToEulerAngles(previousDirection);
            newEulerAngles.y -= currentAngles;

            Quaternion targetRotation = Quaternion.Euler(newEulerAngles);
            mPlayerController.transform.rotation = targetRotation;

            // Debug.Log($"normalizedTime: {currentStateInfo.normalizedTime}, velocity: {mController.Movement.Velocity},  currentAngles: {currentAngles}, targetAngles: {newEulerAngles.y}");

            if (rotationValue > .99f)
            {
                // mbRotating = false;
                mRotationHandler.EndRotation();

                // mController.Movement.SetRotationToCurrentDirection();
                //Debug.Log("Rotation Snapped!");
            }
        }
    }

    public override void StandBy()
    {
        mPlayerController.Movement.UpdateRotation();
    }
}
