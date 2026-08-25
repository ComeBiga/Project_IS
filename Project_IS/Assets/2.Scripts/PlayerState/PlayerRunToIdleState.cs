using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunToIdleState : PlayerStateBase
{
    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
    }

    public override void EnterState()
    {
        mController.Animator.SetRunToIdle(true);

        mController.Animator.Play(AnimState.RunToIdle_R);
        // mController.Animator.CrossFadeRunToIdle(false);
    }

    public override void ExitState()
    {
        mController.Animator.SetRunToIdle(false);
    }

    public override void FixedTick()
    {
        
    }

    public override void Tick()
    {
        // To Fall
        if(!mController.Movement.IsGrounded)
        {
            mController.StateMachine.SwitchState<PlayerFallState>((fallState) =>
            {
                fallState.SetFallType(PlayerFallState.EFallType.FromRun);
            });
            return;
        }

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

        // To IdleToMove
        if (mController.InputHandler.GetInputRawMagnitude().x > .1f)
        {
            mController.StateMachine.SwitchState<PlayerIdleToRunState>();
            return;
        }

        mController.Movement.Move(mController.InputHandler.MoveInput);
        mController.Movement.UpdateRotation();

        // To Idle
        if (Mathf.Abs(mController.Movement.Velocity.x) < .01f)
        {
            mController.StateMachine.SwitchState<PlayerIdleState>();
            return;
        }
    }
}
