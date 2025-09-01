using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerStateBase
{
    public enum EType { Idle, Run }

    public EType type = EType.Idle;

    private Vector3 mMoveInput;

    public override void EnterState()
    {
        mController.Movement.Jump();

        mController.Animator.SetJump();
    }

    public override void ExitState()
    {
        mController.Animator.SetLanding();
    }

    public override void Tick()
    {
        mController.Animator.SetHorizontal(mController.InputHandler.MoveInput.x);

        if (!mController.Movement.Jumping)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
        }
    }
}
