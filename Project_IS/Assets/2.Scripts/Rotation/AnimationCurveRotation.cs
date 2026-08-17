using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCurveRotation : RotationBase
{
    public bool PositionCurveActive => mbPositionCurve;
    public bool RotationCurveActive => mbRotationCurve;

    private float mRunTurnMoveSpeed = 1f;

    private Vector3 mPivotPosition;
    private float mMoveDistance;
    private float mRemainAngles;
    private Vector3 mPreviousEulerAngles;
    private bool mbTransitionToTurn = false;
    private bool mbPositionCurve = false;
    private bool mbRotationCurve = false;
    private float mAnimationCurveTimer = 0f;
    private float mAnimationCurveDuration = 0f;
    private Vector3 mPreviousPosition;
    private float mAnimationTimer = 0f;
    private float mAnimationTimerFixedUpdate = 0f;

    private AnimationCurve mCurrentPositionCurve;
    private AnimationCurve mCurrentRotationCurve;
    private AnimationCurve mIdleTurnRotationCurve;
    private AnimationCurve mIdleTurnPositionCurve;
    private AnimationCurve mRunTurnRotationCurve;
    private AnimationCurve mRunTurnPositionCurve;

    private const float IDLE_TURN_DURATION = 0.36f;
    private const float RUN_TURN_DURATION = 0.23f;


    public AnimationCurveRotation(RotationHandler rotationHandler) : base(rotationHandler)
    {
        mPlayerAnimator.onEnterState += OnEnterState;
        mPlayerAnimator.onUpdateState += OnUpdateState;
        mPlayerAnimator.onExitState += OnExitState;
    }

    public void SetAnimationCurve(AnimationCurve idleTurnPosition, AnimationCurve idleTurnRotation, AnimationCurve runTurnPosition, AnimationCurve runTurnRotation)
    {
        mIdleTurnPositionCurve = idleTurnPosition;
        mIdleTurnRotationCurve = idleTurnRotation;
        mRunTurnPositionCurve = runTurnPosition;
        mRunTurnRotationCurve = runTurnRotation;
    }

    public override void Start()
    {

    }

    public override void StandBy()
    {
        mPlayerController.Movement.UpdateRotation();
    }

    public override void OnDirectionchanged()
    {
        mPlayerController.Movement.SetDirection(mPlayerController.Movement.OppositeDirection);
        mPivotPosition = mPlayerController.Movement.Position;
        mPreviousPosition = mPivotPosition;
        // mMoveDistance = 0f;
        mbPositionCurve = true;
        mbRotationCurve = true;
        mAnimationCurveTimer = 0f;

        // 180도 일 때 SignedAngle 부호가 부정확하므로 반시계 방향으로 약간의 오차를 주어서 계산
        // -1도 이상 차이를 둬야 계산이 제대로 됨
        // Vector3 currentForward = Quaternion.AngleAxis(-1f, Vector3.up) * mPlayerController.transform.forward;
        Vector3 currentForward = rotateVector(mPlayerController.Movement.transform.forward, .01f);
        Vector3 targetDirection = mPlayerController.Movement.DirectionToVector();
        mRemainAngles = Vector3.SignedAngle(currentForward, targetDirection, Vector3.up);
        mPreviousEulerAngles = mPlayerController.Movement.transform.eulerAngles;
        Debug.Log($"Current Forward: {currentForward}, targetDirection: {targetDirection}, Signed Angle: {mRemainAngles}");
        // mRemainAngles = (mRemainAngles + Number.DEG_360) % Number.DEG_360;
        // mRemainAngles = Mathf.Abs(mRemainAngles);

        if(mRemainAngles < 0f)
        {
            mPlayerController.Animator.TurnL(true);
            mPlayerController.Animator.TurnR(false);
        }
        else
        {
            mPlayerController.Animator.TurnL(false);
            mPlayerController.Animator.TurnR(true);
        }

        Debug.Log($"[{Time.frameCount}] Direction Changed");
    }

    public override void OnBeforeFixedUpdate()
    {
        mPlayerController.Animator.TurnL(false);
        mPlayerController.Animator.TurnR(false);

        Debug.Log($"[{Time.frameCount}] Before Rotate");
    }

    public override void OnBeforeUpdate()
    {
        //mPlayerController.Animator.TurnL(false);
        //mPlayerController.Animator.TurnR(false);

        //Debug.Log("Before Rotate");
    }

    public override void FixedUpdate()
    {
        Debug.Log($"[{Time.frameCount}] Entered AnimationCurveRotation.FixedUpdate");
        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
        // Debug.Log($"Animation Curve Nomalized Time: {currentStateInfo.normalizedTime}");
        // float t = mAnimationCurveTimer / mAnimationCurveDuration;
        float t = mAnimationTimerFixedUpdate / mAnimationCurveDuration;

        if (mbPositionCurve) // && currentStateInfo.IsTag("Turn"))
        {
            Debug.Log($"[{Time.frameCount}] Update Position Curve");
            float positionValue = mCurrentPositionCurve.Evaluate(t);
            // float positionValue = mAnimator.GetFloat("PositionCurve");
            float currentPosition = positionValue * mRunTurnMoveSpeed;

            // float velocity = positionValue * mPlayerController.Movement.MoveSpeed * mRunTurnMoveSpeed;
            mPreviousPosition = mPlayerController.Movement.Position;

            Vector3 moveDirection = mPlayerController.Movement.DirectionToVector();
            // mPlayerController.transform.position = mPivotPosition + moveDirection * currentPosition;

            Debug.Log($"Curve Time: {t}, Position Value: {positionValue}, currentPosition: {currentPosition}, moveDirection: {moveDirection}, PivotPosition: {mPivotPosition}, newPosition: {mPlayerController.Movement.Position}");

            // if (mAnimator.IsInTransition(0) && !mbTransitionToTurn)
            if (positionValue > .99f)
            {
                // mbRotating = false;
                // mRotationHandler.EndRotation();
                mbPositionCurve = false;

                float velocityX = (mPlayerController.Movement.Position.x - mPreviousPosition.x) / Time.fixedDeltaTime;

                Vector2 moveInput = mPlayerController.InputHandler.MoveInput;
                moveInput.x = velocityX / mPlayerController.Movement.MoveSpeed;
                mPlayerController.InputHandler.SetMoveInput(moveInput);
                

                Debug.Log($"[{Time.frameCount}] End Position Curve. velocityX: {velocityX}, moveInput: {moveInput}");
            }

            // mMoveDistance += velocity * Time.deltaTime;

            // Debug.Log($"normalizedTime: {currentStateInfo.normalizedTime}, positionValue: {positionValue}, velocity: {velocity}, rigidbody velocity: {mController.Movement.Velocity}, moveDirection: {moveDirection}, deltaPosition: {velocity * Time.deltaTime}, moveDistance: {mRotationMoveDistance}");
        }

        float rotationValue = mCurrentRotationCurve.Evaluate(t);
        // float rotationValue = mAnimator.GetFloat("RotationCurve");

        if (mbRotationCurve) // && currentStateInfo.IsTag("Turn"))// && rotationValue < .99f)
        {
            Debug.Log($"[{Time.frameCount}] Update Rotation Curve");

            // float currentAngles = rotationValue * Number.DEG_180;
            float currentAngles = rotationValue * mRemainAngles;

            //PlayerMovement.EDirection previousDirection = mPlayerController.Movement.OppositeDirection;
            //Vector3 newEulerAngles = PlayerMovement.DirectionToEulerAngles(previousDirection);
            Vector3 newEulerAngles = mPreviousEulerAngles;
            newEulerAngles.y += currentAngles;
            Debug.Log($"Animation Curve Time: {t}, Remain Angles: {mRemainAngles}, rotationValue: {rotationValue}, currentAngles: {currentAngles}, PreviousEnlerAngles: {mPreviousEulerAngles}, newEulerAngles: {newEulerAngles}");

            Quaternion targetRotation = Quaternion.Euler(newEulerAngles);
            mPlayerController.Movement.transform.rotation = targetRotation;

            // Debug.Log($"normalizedTime: {currentStateInfo.normalizedTime}, velocity: {mController.Movement.Velocity},  currentAngles: {currentAngles}, targetAngles: {newEulerAngles.y}");

            if (rotationValue > .99f)
            {
                // mbRotating = false;
                // mRotationHandler.EndRotation();
                mbRotationCurve = false;

                // mController.Movement.SetRotationToCurrentDirection();
                Debug.Log($"[{Time.frameCount}] End Rotation Curve");
            }
        }

        mAnimationTimerFixedUpdate += Time.fixedDeltaTime;

        if(mAnimationTimerFixedUpdate > mAnimationCurveDuration)
        {
            mAnimationTimerFixedUpdate = mAnimationCurveDuration;
        }

        // if(mAnimator.IsInTransition(0) && !mbTransitionToTurn)
        if ((!mbPositionCurve && !mbRotationCurve) || (mAnimationCurveTimer > mAnimationCurveDuration))
        {
            mRotationHandler.EndRotation();

            //float velocityX = (mPlayerController.transform.position - mPreviousPosition).x / Time.deltaTime;
            //mPlayerController.Movement.SetVelocity(new Vector3(velocityX, mPlayerController.Movement.Velocity.y, mPlayerController.Movement.Velocity.z));


            Debug.Log($"[{Time.frameCount}] End Rotation");
        }
    }

    public override void Update()
    {
        mAnimationCurveTimer += Time.deltaTime;

        if(mAnimationCurveTimer > mAnimationCurveDuration)
        {
            mAnimationCurveTimer = mAnimationCurveDuration;
        }

        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Animation Curve Nomalized Time: {currentStateInfo.normalizedTime}");
        Debug.Log($"[{Time.frameCount}] Animation Curve Timer: {mAnimationCurveTimer}, Curve Duration: {mAnimationCurveDuration}, t: {mAnimationCurveTimer/mAnimationCurveDuration}");
        mAnimationTimer += Time.deltaTime;
    }

    public override void OnEndRotation()
    {
        mbPositionCurve = false;
        mbRotationCurve = false;
    }

    private Vector3 rotateVector(Vector3 vector3, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float newX = vector3.x * cos - vector3.z * sin;
        float newZ = vector3.x * sin + vector3.z * cos;

        return new Vector3(newX, vector3.y, newZ);
    }

    private void OnEnterState(string stateName, AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsTag("Turn"))
        {
            // mRotationHandler.StartRotation();
            mbTransitionToTurn = true;

            if(stateName.Contains("Idle"))
            {
                mCurrentPositionCurve = mIdleTurnPositionCurve;
                mCurrentRotationCurve = mIdleTurnRotationCurve;
                mAnimationCurveDuration = IDLE_TURN_DURATION;
            }
            else
            {
                mCurrentPositionCurve = mRunTurnPositionCurve;
                mCurrentRotationCurve = mRunTurnRotationCurve;
                mAnimationCurveDuration = RUN_TURN_DURATION;
            }

            mAnimationTimer = 0f;
            mAnimationTimerFixedUpdate = 0f;
        }
        else
        {
            mbTransitionToTurn = false;
        }
    }

    private void OnUpdateState(string stateName, AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsTag("Turn"))
        {

        }
    }

    private void OnExitState(string stateName, AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsTag("Turn"))
        {
            // mRotationHandler.EndRotation();
            // mbTransitionToTurn = false;
        }
    }
}
