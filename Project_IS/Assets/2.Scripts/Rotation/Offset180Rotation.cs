using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Offset180Rotation : RotationBase
{
    private Vector3 mPreviousForward;

    public Offset180Rotation(RotationHandler rotationHandler) : base(rotationHandler)
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

        mPlayerController.Movement.SetRotationToCurrentDirection();
        mPlayerAnimator.TurnL(true);
        mPlayerAnimator.TurnR(false);
    }

    public override void Update()
    {
        Vector3 currentForward = mPlayerController.transform.forward;
        // 한 프레임 돌아간 각도
        float deltaRotatedAngle = Vector3.SignedAngle(mPreviousForward, currentForward, Vector3.up);

        if (deltaRotatedAngle > -1f && deltaRotatedAngle < 1f)
        {
            mPlayerAnimator.TurnL(false);
            mPlayerAnimator.TurnR(false);
            // mbRotating = false;
        }

        mPreviousForward = currentForward;

        //var currentStateInfo = mPlayerController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        //if (currentStateInfo.IsTag("Run"))
        //{
        //    Debug.Log(mPlayerController.Animator.Animator.deltaRotation);
        //}
    }

    public override void FixedUpdate()
    {
    }
}
