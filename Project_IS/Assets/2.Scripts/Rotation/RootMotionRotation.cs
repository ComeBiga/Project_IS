using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionRotation : RotationBase
{
    private float mDeltaRotatedAngle;
    private float mRootMotionVelocity;
    private float mRootMotionRotationSnapAngle = 150f;
    private float mRootMotionRotationSpeed = 1f;

    private float mRemainAngles;
    private Vector3 mPreviousEulerAngles;

    public RootMotionRotation(RotationHandler rotationHandler) : base(rotationHandler)
    {
        // mPlayerAnimator.onAnimatorMove += onAnimatorMove;
        // mPlayerAnimator.onEnterState += OnEnterState;
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

        //mPlayerAnimator.TurnL(true);
        //mPlayerAnimator.TurnR(false);
        mDeltaRotatedAngle = Number.DEG_0;
        mRootMotionVelocity = 0f;

        Vector3 currentForward = rotateVector(mPlayerController.transform.forward, .01f);
        Vector3 targetDirection = mPlayerController.Movement.DirectionToVector();
        mRemainAngles = Vector3.SignedAngle(currentForward, targetDirection, Vector3.up);
        mPreviousEulerAngles = mPlayerController.transform.eulerAngles;
        // Debug.Log($"Current Forward: {currentForward}, targetDirection: {targetDirection}, Signed Angle: {mRemainAngles}");

        if (mRemainAngles < 0f)
        {
            mPlayerController.Animator.TurnL(true);
            mPlayerController.Animator.TurnR(false);
        }
        else
        {
            mPlayerController.Animator.TurnL(false);
            mPlayerController.Animator.TurnR(true);
        }


        // Debug.Log($"[{Time.frameCount}] Entered RootMotionRotation.OnDirectionChanged, Direction: {mPlayerController.Movement.Direction}");
    }

    public override void OnBeforeFixedUpdate()
    {
        //mPlayerAnimator.TurnL(false);
        //mPlayerAnimator.TurnR(false);
    }

    public override void OnBeforeAnimatorMove()
    {
        mPlayerAnimator.TurnL(false);
        mPlayerAnimator.TurnR(false);

        // Debug.Log($"[{Time.frameCount}] Entered RootMotionRotation.OnBeforeAnimatorMove");
    }

    public override void Update()
    {
        if (Mathf.Abs(mDeltaRotatedAngle) > mRootMotionRotationSnapAngle)
        {
            // mPlayerController.Movement.UpdateRotation();
        }

        // rotate();
    }

    public override void FixedUpdate()
    {
        // rotate();
    }

    public override void OnAnimatorMove()
    {
        rotate();
    }

    public override void OnEndRotation()
    {
        mPlayerAnimator.TurnL(false);
        mPlayerAnimator.TurnR(false);
    }

    private void rotate()
    {
        // Debug.Log($"[{Time.frameCount}] Entered RootMotionRotation.rotate");

        var currentStateInfo = mPlayerController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        var deltaPosition = mPlayerController.Animator.Animator.deltaPosition;
        deltaPosition.z = 0f;
        // transform.position += deltaPosition;
        float rootMotionVelocity = deltaPosition.x / Time.deltaTime;
        rootMotionVelocity = Mathf.Clamp(rootMotionVelocity, -mPlayerController.Movement.MoveSpeed, mPlayerController.Movement.MoveSpeed);

        if (!(currentStateInfo.IsTag("Turn") && mPlayerController.Animator.Animator.IsInTransition(0)))
            mPlayerController.Movement.SetVelocity(Vector3.right * rootMotionVelocity);
        if (Mathf.Abs(rootMotionVelocity) > Mathf.Abs(mRootMotionVelocity))
            mRootMotionVelocity = rootMotionVelocity;

        var deltaRotation = mPlayerController.Animator.Animator.deltaRotation;
        var normalizedX = (deltaRotation.eulerAngles.x < 180f) ? deltaRotation.eulerAngles.x : deltaRotation.eulerAngles.x - 360f;
        var normalizedY = (deltaRotation.eulerAngles.y < 180f) ? deltaRotation.eulerAngles.y : deltaRotation.eulerAngles.y - 360f;
        var normalizedZ = (deltaRotation.eulerAngles.z < 180f) ? deltaRotation.eulerAngles.z : deltaRotation.eulerAngles.z - 360f;
        var normalizedDeltaRotationEuler = new Vector3(normalizedX, normalizedY, normalizedZ);
        var finalDeltaRotationEuler = normalizedDeltaRotationEuler * mRootMotionRotationSpeed;
        var finalDeltaRotation = Quaternion.Euler(finalDeltaRotationEuler);

        mDeltaRotatedAngle += normalizedY;

        if(Mathf.Abs(mDeltaRotatedAngle) < Mathf.Abs(mRemainAngles))
        {
            mPlayerController.transform.rotation *= finalDeltaRotation;
        }


        //if (!currentStateInfo.IsTag("Turn"))
        //{
        //    // mbRotating = false;
        //    mRotationHandler.EndRotation();
        //    Debug.Log($"[{Time.frameCount}] Ended Rotation");
        //}

    }

    //private Vector3 rotateVector(Vector3 vector3, float angle)
    //{
    //    float rad = angle * Mathf.Deg2Rad;
    //    float cos = Mathf.Cos(rad);
    //    float sin = Mathf.Sin(rad);
    //    float newX = vector3.x * cos - vector3.z * sin;
    //    float newZ = vector3.x * sin + vector3.z * cos;

    //    return new Vector3(newX, vector3.y, newZ);
    //}

    private void onAnimatorMove()
    {
        if (mRotationHandler.State == RotationHandler.EState.Rotating)
        {
            rotate();
        }
    }

    private void OnEnterState(string stateName, AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsTag("Turn"))
        {
            
        }
        else
        {
            if(mRotationHandler.State == RotationHandler.EState.Rotating)
            {
                // mbRotating = false;
                mRotationHandler.EndRotation();
                // Debug.Log($"[{Time.frameCount}] Ended Rotation");
            }
        }
    }

}
