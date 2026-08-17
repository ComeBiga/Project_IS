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

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
    }

    public override void EnterState()
    {
        mDefaultHeight = mCharacterPosition.y;

        mController.Movement.SetVelocity(Vector3.zero);

        mController.Animator.Play(AnimState.Idle);
        // mController.Animator.CrossFadeIdle();
        mController.Animator.SetInputX(false);
        mController.Animator.SetInputXMagnitude(0f);
    }

    public override void ExitState()
    {

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
        if(mController.CheckOppositeInputX())
        {
            mController.StateMachine.SwitchState<PlayerTurnState>((turnState) =>
            {
                turnState.SetTurnType(PlayerTurnState.ETurnType.Idle);
            });

            return;
        }

        // To Move
        if(mController.InputHandler.GetInputRawMagnitude().x > .1f)
        {
            // mController.StateMachine.SwitchState<PlayerMoveState>();
            mController.StateMachine.SwitchState<PlayerIdleToRunState>();

            return;
        }

        // To Jump
        if(mController.InputHandler.JumpPressed)
        {
            mController.InputHandler.ResetJump();

            var climbLedgeState = mController.StateMachine.GetStateBase<PlayerClimbLedgeState>();

            if (climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo, out Collider detectedCollider) == 1)
            {
                climbLedgeState.SetInfo(climbLedgeInfo);
                mController.StateMachine.SwitchState<PlayerClimbLedgeState>();

                return;
            }
            else
            {
                mController.StateMachine.SwitchState<PlayerJumpState>();

                return;
            }
        }

        // To Fall

        mController.Movement.UpdateRotation();
    }
}
