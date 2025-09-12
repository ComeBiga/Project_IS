using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerStateBase
{
    [SerializeField]
    private float _heavyLandingVelocityY = -10f;

    private bool mbLanding = false;
    private float mMinVelocityY = 0f;

    public override void EnterState()
    {
        mbLanding = false;
        mMinVelocityY = 0f;

        mController.Animator.ResetLanding();
        mController.Animator.SetInputXMagnitude(0f);
    }

    public override void ExitState()
    {

    }

    public override void Tick()
    {
        if (mbLanding)
            return;

        if(mController.Movement.Velocity.y < mMinVelocityY)
            mMinVelocityY = mController.Movement.Velocity.y;

        if(mController.Movement.IsGrounded)
        {
            mbLanding = true;
            mController.Movement.SetVelocity(Vector3.zero);

            if (mMinVelocityY < _heavyLandingVelocityY)
            {
                mController.Animator.SetHeavyLanding();
            }
            else
            { 
                mController.Animator.SetLanding();
            }

            return;
        }
    }

    public void EndLanding()
    {
        PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        moveState.EnterToIdle();
        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }
}
