using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerStateBase
{
    [SerializeField] 
    private PlayerClimbLedgeState _climbLedgeState;
    [SerializeField]
    private float _fallStartVelocityY = -1f;
    [SerializeField]
    private float _heavyLandingVelocityY = -10f;
    [SerializeField]
    private float _runningLandingSpeed = .8f;

    private Animator mAnimator;
    private bool mbLanding = false;
    private float mMinVelocityY = 0f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mbLanding = false;
        mMinVelocityY = 0f;

        mController.Animator.ResetLanding();
        // mController.Animator.SetInputXMagnitude(0f);
    }

    public override void ExitState()
    {
        mbLanding = false;
        // mController.Animator.SetInputXMagnitude(0f);
    }

    public override void Tick()
    {
        if (mbLanding)
        {
            //if(Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
            //{
            //    EndLanding();
            //    return;
            //}

            // mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

            return;
        }

        if(mController.Movement.Velocity.y < mMinVelocityY)
            mMinVelocityY = mController.Movement.Velocity.y;

        if(mController.Movement.IsGrounded)
        {
            mbLanding = true;
            // mController.Animator.SetInputXMagnitude(0f);
            float inputXMagnitude = Mathf.Abs(mController.InputHandler.MoveInput.x);
            mController.Animator.SetInputXMagnitude(inputXMagnitude);

            if (mMinVelocityY < _heavyLandingVelocityY)
            {
                mController.Movement.SetVelocity(Vector3.zero);
                mController.Animator.SetHeavyLanding();

                PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
                moveState.EnterToIdle();
            }
            else
            { 
                mController.Animator.SetLanding();

                StartCoroutine(eRunningLanding());
            }

            return;
        }

        if (_climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo))
        {
            _climbLedgeState.SetInfo(climbLedgeInfo);
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
            return;
        }
    }

    public void EndLanding()
    {
        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }

    public bool CheckFall()
    {
        // Debug.Log($"IsGrounded: {mController.Movement.IsGrounded}, velocity Y: {mController.Movement.Velocity.y}");

        if (!mController.Movement.IsGrounded && mController.Movement.Velocity.y < _fallStartVelocityY)
        {
            return true;
        }

        return false;
    }

    private IEnumerator eRunningLanding()
    {
        while(true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("RunningLanding"))
                break;

            yield return null;
        }

        float distance = 0f;
        PlayerMovement.EDirection direction = mController.Movement.Direction;
        float moveSpeed = mController.Movement.MoveSpeed;

        while(mbLanding)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (!animatorStateInfo.IsTag("RunningLanding"))
                break;

            Vector3 deltaPosition = mAnimator.deltaPosition;
            deltaPosition.x = deltaPosition.x * _runningLandingSpeed;
            deltaPosition.z = 0f;
            // transform.position += deltaPosition;

            Vector3 velocity = mAnimator.velocity;
            velocity.x = velocity.x * _runningLandingSpeed;

            if(direction == PlayerMovement.EDirection.Left)
            {
                if(mController.InputHandler.MoveInput.x > 0f)
                {
                    EndLanding();
                    yield break;
                }

                if (velocity.x > -moveSpeed)
                    velocity.x = -moveSpeed;
            }
            else
            {
                if (mController.InputHandler.MoveInput.x < 0f)
                {
                    EndLanding();
                    yield break;
                }

                if (velocity.x < moveSpeed)
                    velocity.x = moveSpeed;
            }

            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);
            // Debug.Log(velocity);

            distance += deltaPosition.x;

            yield return null;
        }

        // Debug.Log(distance);
    }
}
