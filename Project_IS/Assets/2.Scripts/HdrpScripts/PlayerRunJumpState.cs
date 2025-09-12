using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunJumpState : PlayerStateBase
{
    [SerializeField] private PlayerClimbLedgeState _climbLedgeState;

    private Vector3 mMoveInput;
    private float mDefaultHeight;

    public override void EnterState()
    {
        mController.Movement.Jump();

        //mController.Animator.SetRunJump();
        mController.Animator.SetJump();
    }

    public override void ExitState()
    {
        // mController.Animator.SetLanding();
    }

    public override void Tick()
    {
        mMoveInput = mController.InputHandler.MoveInput;

        if (mController.Movement.Direction == PlayerMovement.EDirection.Right)
        {
            if (mMoveInput.x < 0f)
                mMoveInput.x = 0f;
        }
        else
        {
            if (mMoveInput.x > 0f)
                mMoveInput.x = 0f;
        }

        mController.Movement.Move(mMoveInput);
        mController.Animator.SetHorizontal(mController.InputHandler.MoveInput.x);
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        mController.Movement.UpdateRotation();

        if (!mController.Movement.Jumping)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            mController.Animator.SetLanding();

            return;
        }

        if (_climbLedgeState.CheckLedge(out RaycastHit hitInfo))
        {
            _climbLedgeState.SetLedge(hitInfo.collider.bounds);
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
            return;
        }

        if (transform.position.y < mDefaultHeight - .5f)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Fall);
            return;
        }
    }

    public void SetDefaultHeight(float height)
    {
        mDefaultHeight = height;
    }
}

