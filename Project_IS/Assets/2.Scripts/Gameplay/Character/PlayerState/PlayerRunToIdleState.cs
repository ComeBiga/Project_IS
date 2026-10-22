using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunToIdleState : PlayerStateBase
{
    private PlayerTurnState mTurnState;
    private bool mbAnimationFinished;
    private uint mEntryEvaluationCount;
    private int mEntryFrame;

    public override void EnterState()
    {
        mTurnState = mStateMachine.GetStateBase<PlayerTurnState>();
        mbAnimationFinished = false;
        mController.Animation.SetRunToIdle(true);

        if (!mTurnState.TryPlayControllerBlend(AnimState.RunToIdle_R))
            mController.Animation.Play(AnimState.RunToIdle_R);

        mEntryEvaluationCount = mAnimation.StateEvaluationCount;
        mEntryFrame = Time.frameCount;
    }

    public override void ExitState()
    {
        mTurnState.CancelRemainingRotation();
        mController.Animation.SetRunToIdle(false);
    }

    public override void FixedTick()
    {
        mTurnState.ContinueRemainingRotation();
    }

    public override void Tick()
    {
        updateAnimationCompletion();
        bool stopCompleted = mbAnimationFinished && !mTurnState.HasRemainingRotation;

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
                turnState.SetTurnType(stopCompleted ? PlayerTurnState.ETurnType.Idle : PlayerTurnState.ETurnType.Run);
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

        if (!mTurnState.HasRemainingRotation)
            mController.Movement.UpdateRotation();

        if (stopCompleted)
        {
            mStateMachine.SwitchState<PlayerIdleState>();
        }
    }

    private void updateAnimationCompletion()
    {
        bool awaitingEvaluation = mAnimation.UsesControllerPlayable
            ? mAnimation.StateEvaluationCount == mEntryEvaluationCount
            : Time.frameCount == mEntryFrame;

        if (awaitingEvaluation || mbAnimationFinished)
            return;

        AnimatorStateInfo current = mAnimation.GetCurrentStateInfo(0);

        if (current.IsTag("RunToIdle"))
        {
            mbAnimationFinished = current.normalizedTime >= 1f;
        }
        else if (current.fullPathHash == AnimStateHash.stateHashes[AnimState.Idle] &&
            !mAnimation.IsInTransition(0))
        {
            mbAnimationFinished = true;
        }
    }
}
