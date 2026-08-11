using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbLedgeToRunState : PlayerStateBase
{
    private float mClimbTimer = 0f;
    private float mClimbDuration = 0f;

    public override void EnterState()
    {
        mController.Animator.Play(AnimState.ClimbLedgeToRun);

        mController.InputHandler.ResetMoveInput();
    }

    public override void ExitState()
    {
        
    }

    public override void FixedTick()
    {

    }

    public override void Tick()
    {
        // To Turn
        if (mController.CheckOppositeInputX())
        {
            mController.StateMachine.SwitchState<PlayerTurnState>((turnState) =>
            {
                turnState.SetTurnType(PlayerTurnState.ETurnType.Run);
            });
            return;
        }

        // To Jump
        if (mController.InputHandler.JumpPressed)
        {
            mController.InputHandler.ResetJump();
            mController.StateMachine.SwitchState<PlayerRunJumpState>();
            return;
        }

        mController.Movement.Move(mController.InputHandler.MoveInput);

        // To RunToIdle
        if (mController.InputHandler.GetInputRawMagnitude().x < .1f)
        {
            mController.StateMachine.SwitchState<PlayerRunToIdleState>();
            return;
        }

        // To Move
        if(mClimbTimer > mClimbDuration)
        {
            mController.StateMachine.SwitchState<PlayerMoveState>();
            return;
        }

        mClimbTimer += Time.deltaTime;
    }

    public void SetClimbTimer(float climbTimer, float climbDuration)
    {
        mClimbTimer = climbTimer;
        mClimbDuration = climbDuration;
    }
}
