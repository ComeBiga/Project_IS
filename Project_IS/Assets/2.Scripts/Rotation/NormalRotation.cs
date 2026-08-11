using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalRotation : RotationBase
{
    private float mTimer = float.MaxValue;
    private float mDuration = 0f;
    private float mIdleTurnDuration = .6f;
    private float mRunTurnDuration = .283f;
    private Vector3 mStartEulerAngles;
    private float mTargetYEulerAngle;
    private float mDeltaAngle;
    private PlayerTurnState.ETurnType mTurnType;
    private const float FIXED_ROTATION_ANGLE = -180f;

    public NormalRotation(RotationHandler rotationHandler) : base(rotationHandler)
    {

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

        // if(mTimer >= mDuration)
        //{
        //    if (mPlayerController.Movement.Velocity.x < .1f && mPlayerController.Movement.Velocity.x > -.1f)
        //    {
        //        mDuration = mIdleTurnDuration;
        //    }
        //    else
        //    {
        //        mDuration = mRunTurnDuration;
        //    }
        //}

        mTimer = 0f;

        mStartEulerAngles = mPlayerController.transform.rotation.eulerAngles;
        mTargetYEulerAngle = mPlayerController.Movement.DirectionToRotation(mPlayerController.Movement.Direction).eulerAngles.y;
        mDeltaAngle = Mathf.DeltaAngle(mStartEulerAngles.y, mTargetYEulerAngle);
        // Debug.Log($"a: {mStartEulerAngles.y:F9}, b: {mTargetYEulerAngle:F9}, delta: {mDeltaAngle:F9}");

        Vector3 currentForward = rotateVector(mPlayerController.transform.forward, .01f);
        Vector3 targetDirection = mPlayerController.Movement.DirectionToVector();
        float remainAngles = Vector3.SignedAngle(currentForward, targetDirection, Vector3.up);

        // mDeltaAngle = FIXED_ROTATION_ANGLE;

        if(mDeltaAngle < 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Left);

            mPlayerController.Animator.TurnL(true);
            mPlayerController.Animator.TurnR(false);

            mPlayerController.Animator.Play(mTurnType == PlayerTurnState.ETurnType.Run ? AnimState.RunTurn : AnimState.IdleTurn);
            // mPlayerController.Animator.CrossFadeTurn(mTurnType == PlayerTurnState.ETurnType.Run ? true : false, true);
        }
        else
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Right);

            mPlayerController.Animator.TurnL(false);
            mPlayerController.Animator.TurnR(true);

            mPlayerController.Animator.Play(mTurnType == PlayerTurnState.ETurnType.Run ? AnimState.RunTurn_R : AnimState.IdleTurn_R);
            // mPlayerController.Animator.CrossFadeTurn(mTurnType == PlayerTurnState.ETurnType.Run ? true : false, false);
        }
    }

    public override void OnBeforeFixedUpdate()
    {
        //mPlayerAnimator.TurnL(false);
        //mPlayerAnimator.TurnR(false);
    }

    public override void FixedUpdate()
    {
        //if(mTimer > mDuration)
        //{             
        //    mPlayerController.transform.rotation = Quaternion.Euler(mStartEulerAngles.x, mTargetYEulerAngle, mStartEulerAngles.z);
        //    mRotationHandler.EndRotation();
        //    return;
        //}

        //float t = mTimer / mDuration;
        //float newYEulerAngle = Mathf.LerpAngle(mStartEulerAngles.y, mTargetYEulerAngle, t);
        //mPlayerController.transform.rotation = Quaternion.Euler(mStartEulerAngles.x, newYEulerAngle, mStartEulerAngles.z);

        //mTimer += Time.fixedDeltaTime;
    }

    public override void OnBeforeAnimatorMove()
    {
        //mPlayerAnimator.TurnL(false);
        //mPlayerAnimator.TurnR(false);
    }

    public override void OnAnimatorMove()
    {
        if (mTimer > mDuration)
        {
            mPlayerController.transform.rotation = Quaternion.Euler(mStartEulerAngles.x, mTargetYEulerAngle, mStartEulerAngles.z);
            mRotationHandler.EndRotation();
            // Debug.Log($"[{Time.frameCount}] Rotation completed. Final EulerAngles: {mPlayerController.transform.rotation.eulerAngles}");
            return;
        }

        float t = mTimer / mDuration;
        float newYEulerAngle = Mathf.LerpAngle(mStartEulerAngles.y, mTargetYEulerAngle, t);
        // float newYEulerAngle = lerpFixedAngle(mStartEulerAngles.y, mTargetYEulerAngle, t);
        mPlayerController.transform.rotation = Quaternion.Euler(mStartEulerAngles.x, newYEulerAngle, mStartEulerAngles.z);

        mTimer += Time.fixedDeltaTime;

        // Debug.Log($"[{Time.frameCount}] Entered NormalRotation.OnAnimatorMove, Timer: {mTimer}, Duration: {mDuration}, StartEulerAngles: {mStartEulerAngles}, TargetYEulerAngle: {mTargetYEulerAngle}, NewYEulerAngle: {newYEulerAngle}, DeltaAngle: {mDeltaAngle}");
    }

    public override void Update()
    {

    }

    public override void OnEndRotation()
    {
        //mPlayerAnimator.TurnL(false);
        //mPlayerAnimator.TurnR(false);
    }

    public void SetTurnType(PlayerTurnState.ETurnType turnType)
    {
        mTurnType = turnType;

        switch (turnType)
        {
            case PlayerTurnState.ETurnType.Idle:
                mDuration = mIdleTurnDuration;
                break;
            case PlayerTurnState.ETurnType.Run:
                mDuration = mRunTurnDuration;
                break;
            default:
                mDuration = mIdleTurnDuration;
                break;
        }

        // Debug.Log($"[{Time.frameCount}] SetTurnType: {turnType}, Duration: {mDuration}");
    }

    private float lerpFixedAngle(float a, float b, float t)
    {
        float delta = Mathf.DeltaAngle(a, b);
        delta = FIXED_ROTATION_ANGLE;

        if (Mathf.Abs(Mathf.Abs(delta) - 180f) < 0.01f)
        {
            delta = FIXED_ROTATION_ANGLE;
        }

        float angle = a + delta * Mathf.Clamp01(t);

        return angle;
    }
}
