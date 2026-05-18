using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionRotation : RotationBase
{
    private float mDeltaRotatedAngle;
    private float mRootMotionVelocity;
    private float mRootMotionRotationSnapAngle = 150f;
    private float mRootMotionRotationSpeed = 1f;

    public RootMotionRotation(RotationHandler rotationHandler) : base(rotationHandler)
    {
    }

    public override void Start()
    {
    }

    public override void StandBy()
    {
    }

    public override void OnDirectionchanged()
    {
        mPlayerController.Movement.SetDirection(mPlayerController.Movement.OppositeDirection);

        mPlayerAnimator.TurnL(true);
        mPlayerAnimator.TurnR(false);
        mDeltaRotatedAngle = Number.DEG_0;
        mRootMotionVelocity = 0f;
    }

    public override void OnBeforeRotate()
    {
        mPlayerAnimator.TurnL(false);
        mPlayerAnimator.TurnR(false);
    }

    public override void Update()
    {
        if (Mathf.Abs(mDeltaRotatedAngle) > mRootMotionRotationSnapAngle)
        {
            mPlayerController.Movement.UpdateRotation();
        }

        rotate();
    }

    public override void FixedUpdate()
    {
    }

    private void rotate()
    {
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
        mPlayerController.transform.rotation *= finalDeltaRotation;

        mDeltaRotatedAngle += normalizedY;

        if (!currentStateInfo.IsTag("Turn"))
        {
            // mbRotating = false;
            mRotationHandler.EndRotation();
        }

    }
}
