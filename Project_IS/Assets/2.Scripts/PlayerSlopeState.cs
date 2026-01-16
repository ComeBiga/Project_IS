using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSlopeState : PlayerStateBase
{
    public float SlopeAngle => mSlopeAngle;

    [SerializeField] private float mSlopeAngle = 30f;

    private Animator mAnimator;

    private bool mbEndSlope = false;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mController.Animator.SetInputXMagnitude(0f);
        mController.Animator.SetIndex(0);
        mbEndSlope = false;
    }

    public override void ExitState()
    {
        PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        moveState.EnterToIdle();
    }

    public override void Tick()
    {
        if(mbEndSlope)
            return;

        // Terrain Normal
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, .1f, LayerMask.GetMask("Ground")))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
            Debug.Log(slopeAngle);

            if (slopeAngle < mSlopeAngle)
            {
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
                mController.Animator.SetIndex(1);
                mbEndSlope = true;

                StartCoroutine(eEndSlope());

                return;
            }
        }
    }

    private IEnumerator eEndSlope()
    {
        while(true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("EndSlope"))
                break;

            yield return null;
        }

        while (true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if(!animatorStateInfo.IsTag("EndSlope"))
            {
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
                yield break;
            }    

            //if(animatorStateInfo.normalizedTime >= 1f)
            //{
            //    mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            //    yield break;
            //}

            Vector3 deltaPosition = mAnimator.deltaPosition;
            deltaPosition.y = 0f;
            deltaPosition.z = 0f;

            transform.position += deltaPosition;

            mController.Movement.SetVelocity(Vector3.zero);

            yield return null;
        }
    }
}
