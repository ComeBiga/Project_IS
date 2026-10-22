using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalRotation : RotationBase
{
    private TurnAnimationData mTurnAnimData;
    private TurnAnimationData.TurnAnimation mTurnAnimation;
    private float mTimer = float.MaxValue;
    private float mDuration = 0f;
    private float mIdleTurnDuration = .6f;
    private float mRunTurnDuration = .283f;
    private Vector3 mStartEulerAngles;
    private float mTargetYEulerAngle;
    private float mDeltaAngle;
    private bool mbRotationFinished = true;
    // private PlayerTurnState.ETurnType mTurnType;

    private bool mbFixedRotationDirection = false;

    private const float FIXED_ROTATION_ANGLE = -180f;

    public NormalRotation(RotationHandler rotationHandler, PlayerController playerController) : base(rotationHandler, playerController)
    {

    }

    public override void Start()
    {

    }

    public override void StandBy()
    {
        if(!mbRotationFinished)
        { 
            mPlayerController.Movement.UpdateRotation();

            float angleY = mPlayerController.Movement.transform.rotation.eulerAngles.y;

            if (angleY > mTargetYEulerAngle - 1f && angleY < mTargetYEulerAngle + 1f)
            {
                mbRotationFinished = true;
                // mPlayerController.transform.rotation = Quaternion.Euler(mStartEulerAngles.x, mTargetYEulerAngle, mStartEulerAngles.z);
                mPlayerController.Movement.SetRotation(Quaternion.Euler(mStartEulerAngles.x, mTargetYEulerAngle, mStartEulerAngles.z));
            }
        }
    }

    public override void OnDirectionchanged()
    {
        mbRotationFinished = false;
        mTimer = 0f;
        NormalizedTime = 0f;

        //if (mTurnAnimData.useRotationDuration)
        //    mDuration = mTurnAnimData.rotationDuration;
        //else
        //    mDuration = mTurnAnimData.sourceClip.length;

        mStartEulerAngles = mPlayerController.Movement.DirectionToRotation(mPlayerController.Movement.Direction).eulerAngles;
        mPlayerController.Movement.SetDirection(mPlayerController.Movement.OppositeDirection);

        // mStartEulerAngles = mPlayerController.Movement.Rotation.eulerAngles;
        mTargetYEulerAngle = mPlayerController.Movement.DirectionToRotation(mPlayerController.Movement.Direction).eulerAngles.y;
        // mDeltaAngle = Mathf.DeltaAngle(mStartEulerAngles.y, mTargetYEulerAngle);
        mDeltaAngle = Mathf.DeltaAngle(mPlayerController.Movement.Rotation.eulerAngles.y, mTargetYEulerAngle);
        // Debug.Log($"a: {mStartEulerAngles.y:F9}, b: {mTargetYEulerAngle:F9}, delta: {mDeltaAngle:F9}");

        Vector3 currentForward = rotateVector(mPlayerController.Movement.transform.forward, .01f);
        Vector3 targetDirection = mPlayerController.Movement.DirectionToVector();
        float remainAngles = Vector3.SignedAngle(currentForward, targetDirection, Vector3.up);

        // mDeltaAngle = FIXED_ROTATION_ANGLE;
        //if (Mathf.Abs(Mathf.Abs(mDeltaAngle) - 180f) < 0.01f)
        //{
        //    mDeltaAngle = FIXED_ROTATION_ANGLE;
        //}

        if (mDeltaAngle < 0f)
        {
            mTurnAnimation = mTurnAnimData.Get(mTurnType, RotationHandler.ERotationDirection.Left);

            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Left);

            mPlayerController.Animation.TurnL(true);
            mPlayerController.Animation.TurnR(false);

            AnimState animState = mTurnType == PlayerTurnState.ETurnType.Run ? AnimState.RunTurn : AnimState.IdleTurn;
            // mPlayerController.Animation.Play(animState, true, .25f, mTimer);
            mPlayerController.Animation.Play(animState, mTimer);
            // mPlayerController.Animator.CrossFadeTurn(mTurnType == PlayerTurnState.ETurnType.Run ? true : false, true);
        }
        else
        {
            mTurnAnimation = mTurnAnimData.Get(mTurnType, RotationHandler.ERotationDirection.Right);

            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Right);

            mPlayerController.Animation.TurnL(false);
            mPlayerController.Animation.TurnR(true);
            
            AnimState animState = mTurnType == PlayerTurnState.ETurnType.Run ? AnimState.RunTurn_R : AnimState.IdleTurn_R;
            // mPlayerController.Animation.Play(animState, true, .25f, mTimer);
            mPlayerController.Animation.Play(animState, mTimer);
            // mPlayerController.Animator.CrossFadeTurn(mTurnType == PlayerTurnState.ETurnType.Run ? true : false, false);
        }

        NormalizedTime = 1f - Mathf.Abs(mDeltaAngle) / Number.DEG_180;
        //mTimer = NormalizedTime * mDuration;
        mTimer = NormalizedTime * mTurnAnimation.GetRotationDuration();

        GameDebug.Log($"DeltaAngle: {mDeltaAngle}, RemainAngles: {remainAngles}, timer: {mTimer}, Rotation Normalized Time: {NormalizedTime}", tag: "Normal Rotation");
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
        //if (mTimer > mDuration)
        if (mTimer > mTurnAnimation.GetRotationDuration())
        {
            NormalizedTime = 1f;
            mbRotationFinished = true;
            mPlayerController.Movement.SetRotation(Quaternion.Euler(mStartEulerAngles.x, mTargetYEulerAngle, mStartEulerAngles.z));
            mRotationHandler.EndRotation();
            return;
        }

        //float t = mTimer / mDuration;
        float t = mTimer / mTurnAnimation.GetRotationDuration();
        NormalizedTime = t;

        float newYEulerAngle;

        if(mbFixedRotationDirection)
        {
            newYEulerAngle = lerpFixedAngle(mStartEulerAngles.y, mTargetYEulerAngle, t);
        }
        else
        {
            // float newYEulerAngle = Mathf.LerpAngle(mStartEulerAngles.y, mTargetYEulerAngle, t);
            newYEulerAngle = lerpAngle(mRotationHandler.RotationDirection, mStartEulerAngles.y, mTargetYEulerAngle, t);
        }

        mPlayerController.Movement.SetRotation(Quaternion.Euler(mStartEulerAngles.x, newYEulerAngle, mStartEulerAngles.z));

        float remainAngle = Mathf.DeltaAngle(newYEulerAngle, mTargetYEulerAngle);
        float angleT = 1 - Mathf.Abs(remainAngle) / Number.DEG_180;

        GameDebug.Log($"Rotation Normalized Time: {NormalizedTime}, Start Angle: {mStartEulerAngles.y}, Target Angle: {mTargetYEulerAngle}, New Angle: {newYEulerAngle}, Remain Angle: {remainAngle}, Angle T: {angleT}", tag: "Normal Rotation");

        mTimer += Time.fixedDeltaTime;
    }

    public override void Update()
    {

    }

    public override void OnEndRotation()
    {
        //mPlayerAnimator.TurnL(false);
        //mPlayerAnimator.TurnR(false);
    }

    public override void Rotate(Vector3 fromEulerAngles, Vector3 toEulerAngles, float deltaTime)
    {
        float duration = getTurnAnimation().GetRotationDuration();
        if (duration <= 0f || NormalizedTime >= 1f || mTimer >= duration)
        {
            NormalizedTime = 1f;
            mbRotationFinished = true;
            mPlayerController.Movement.SetRotation(Quaternion.Euler(fromEulerAngles.x, toEulerAngles.y, fromEulerAngles.z));

            mRotationHandler.EndRotation();
            return;
        }

        float t = Mathf.Clamp01(mTimer / duration);
        NormalizedTime = t;

        float newYEulerAngle;

        if (mbFixedRotationDirection)
        {
            newYEulerAngle = lerpFixedAngle(fromEulerAngles.y, toEulerAngles.y, t);
        }
        else
        {
            // float newYEulerAngle = Mathf.LerpAngle(mStartEulerAngles.y, mTargetYEulerAngle, t);
            newYEulerAngle = lerpAngle(mRotationHandler.RotationDirection, fromEulerAngles.y, toEulerAngles.y, t);
        }

        mPlayerController.Movement.SetRotation(Quaternion.Euler(fromEulerAngles.x, newYEulerAngle, fromEulerAngles.z));

        float remainAngle = Mathf.DeltaAngle(newYEulerAngle, toEulerAngles.y);
        float angleT = 1 - Mathf.Abs(remainAngle) / Number.DEG_180;

        GameDebug.Log($"Rotation Timer: {mTimer}, Rotation Normalized Time: {NormalizedTime}, Start Angle: {fromEulerAngles.y}, Target Angle: {toEulerAngles.y}, New Angle: {newYEulerAngle}, Remain Angle: {remainAngle}, Angle T: {angleT}", tag: "Normal Rotation");

        // mTimer += Time.fixedDeltaTime;
        mTimer += deltaTime;
    }

    public void SetTurnAnimData(TurnAnimationData turnAnimData)
    {
        mTurnAnimData = turnAnimData;
    }

    //public void SetTurnType(PlayerTurnState.ETurnType turnType)
    //{
    //    mTurnType = turnType;

    //    switch (turnType)
    //    {
    //        case PlayerTurnState.ETurnType.Idle:
    //            mDuration = mIdleTurnDuration;
    //            break;
    //        case PlayerTurnState.ETurnType.Run:
    //            mDuration = mRunTurnDuration;
    //            break;
    //        default:
    //            mDuration = mIdleTurnDuration;
    //            break;
    //    }
    //}

    public override void SetNormalizedTime(float normalizedTime)
    {
        base.SetNormalizedTime(normalizedTime);

        mTimer = normalizedTime * getTurnAnimation().GetRotationDuration();
    }

    public void StopStanbyRotation()
    {
        mbRotationFinished = true;
    }

    private TurnAnimationData.TurnAnimation getTurnAnimation()
    {
        return mTurnAnimData.Get(mTurnType, mRotationHandler.RotationDirection);
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

    private float lerpAngle(RotationHandler.ERotationDirection rotationDirection, float a, float b, float t)
    {
        float delta = Mathf.DeltaAngle(a, b);
        float deltaMagnitude = Mathf.Abs(delta);

        if (Mathf.Abs(deltaMagnitude - 180f) < 0.01f)
        {
            delta = FIXED_ROTATION_ANGLE;
        }

        float direction = (rotationDirection == RotationHandler.ERotationDirection.Left) ? -1f : 1f;
        float angle = a + direction * deltaMagnitude * Mathf.Clamp01(t);

        return angle;
    }
}
