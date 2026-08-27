using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerIdleState : PlayerStateBase
{
    [SerializeField]
    private TwoBoneIKConstraint _leftLegIKConstraint;
    [SerializeField]
    private TwoBoneIKConstraint _rightLegIKConstraint;

    private float mDefaultHeight;
    private bool mbEnterWithoutAnimation = false;

    public override void EnterState()
    {
        mDefaultHeight = mCharacterPosition.y;

        mMovement.SetVelocity(Vector3.zero);

        if(!mbEnterWithoutAnimation)
            mAnimation.Play(AnimState.Idle);
        // mController.Animator.CrossFadeIdle();
        mAnimation.SetInputX(false);
        mAnimation.SetInputXMagnitude(0f);
    }

    public override void ExitState()
    {
        mbEnterWithoutAnimation = false;
    }

    public override void FixedTick()
    {
        _leftLegIKConstraint.weight = 0f;
        _rightLegIKConstraint.weight = 0f;
    }

    public override void AnimatorMoveTick()
    {
        
    }

    public override void AnimatorIKTick()
    {
        
    }

    public override void LateFixedTick()
    {
        
    }

    public override void Tick()
    {
        // To Turn
        if (mController.CheckOppositeInputX())
        {
            mStateMachine.SwitchState<PlayerTurnState>((turnState) =>
            {
                turnState.SetTurnType(PlayerTurnState.ETurnType.Idle);
            });

            return;
        }

        // To Move
        if(mInputHandler.GetInputRawMagnitude().x > .1f)
        {
            // mController.StateMachine.SwitchState<PlayerMoveState>();
            mStateMachine.SwitchState<PlayerIdleToRunState>();

            return;
        }

        // To Jump
        if(mInputHandler.JumpPressed)
        {
            mInputHandler.ResetJump();

            var climbLedgeState = mStateMachine.GetStateBase<PlayerClimbLedgeState>();

            if (climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out Collider detectedCollider) == 1)
            {
                climbLedgeState.SetInfo(climbLedgeInfo);
                mStateMachine.SwitchState<PlayerClimbLedgeState>();

                return;
            }
            else
            {
                mStateMachine.SwitchState<PlayerJumpState>();

                return;
            }
        }

        // To Fall

        // To Ladder
        var ladderState = mStateMachine.GetStateBase<PlayerLadderState>();

        if(ladderState.CheckLadder(out PlayerLadderState.LadderInfo ladderInfo))
        {
            if(ladderInfo.part == PlayerLadderState.LadderPart.Bottom && mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Up))
            {
                mStateMachine.SwitchState<PlayerLadderState>((state) =>
                {
                    state.SetLadder(ladderInfo);
                });

                return;
            }

            if(ladderInfo.part == PlayerLadderState.LadderPart.Top && mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Down))
            {
                mStateMachine.SwitchState<PlayerLadderState>((state) =>
                {
                    state.SetLadder(ladderInfo);
                });

                return;
            }
        }

        // PushPull
        var pushPullState = mStateMachine.GetStateBase<PlayerPushPullState>();

        if(mInputHandler.IsInteracting && pushPullState.CheckPushPull(PlayerPushPullState.EPushPullType.Side, out PlayerPushPullState.PushPullInfo pushPullInfo))
        {
            mController.StateMachine.SwitchState<PlayerPushPullState>((state) =>
            {
                //state.SetPushPullObject(pushPullObject);
                //state.SetPushPullType(PlayerPushPullState.EPushPullType.Side);
                state.SetPushPullInfo(pushPullInfo);
            });

            return;
        }

        // Interactable
        if(mInputHandler.IsInteracting && mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Front, out PlayerInteractable.InteractedInfo interactedInfo))
        {
            mStateMachine.SwitchState<PlayerInteractState>((state) =>
            {
                state.SetInteractableObject(interactedInfo.interactableObject);
            });

            return;
        }

        mMovement.UpdateRotation();
    }

    public void EnterWithoutAnimation()
    {
        mbEnterWithoutAnimation = true;
    }
}
