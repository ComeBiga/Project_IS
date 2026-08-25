using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleToRunState : PlayerStateBase
{
    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
    }

    public override void EnterState()
    {
        mController.Animator.SetIdleToRun(true);

        mController.Animator.Play(AnimState.IdleToRun);
    }

    public override void ExitState()
    {
        mController.Animator.SetIdleToRun(false);
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
        if(mController.InputHandler.JumpPressed)
        {
            mController.InputHandler.ResetJump();
            mController.StateMachine.SwitchState<PlayerRunJumpState>();
            return;
        }

        // To Ladder
        var ladderState = mStateMachine.GetStateBase<PlayerLadderState>();

        if (ladderState.CheckLadder(out PlayerLadderState.LadderInfo ladderInfo))
        {
            if (ladderInfo.part == PlayerLadderState.LadderPart.Bottom && mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Up))
            {
                mStateMachine.SwitchState<PlayerLadderState>((state) =>
                {
                    state.SetLadder(ladderInfo);
                });

                return;
            }
        }

        // To RunToIdle
        if (mController.InputHandler.GetInputRawMagnitude().x < .1f)
        {
            mController.StateMachine.SwitchState<PlayerRunToIdleState>();
            return;
        }

        mController.Movement.Move(mController.InputHandler.MoveInput);

        // To Move
        if(Mathf.Abs(mController.Movement.Velocity.x) > mController.Movement.MoveSpeed - .1f)
        {
            mController.StateMachine.SwitchState<PlayerMoveState>();
            return;
        }
    }
}
