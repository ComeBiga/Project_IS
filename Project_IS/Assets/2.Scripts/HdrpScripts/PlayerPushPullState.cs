using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPushPullState : PlayerStateBase
{
    private Animator mAnimator;
    private PushPullObject mPushPullObject;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        // mController.Movement.PushPull(Vector2.zero, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public override void ExitState()
    {

    }

    public override void Tick()
    {
        if (!mController.InputHandler.IsInteracting)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            return;
        }

        // mController.Movement.Move(mController.InputHandler.MoveInput);
        // Debug.Log(mAnimator.velocity);
        Vector3 animationVelocity = mAnimator.velocity;
        animationVelocity.z = 0f;
        mController.Movement.SetVelocity(animationVelocity);

        float moveInputX = mController.InputHandler.MoveInput.x;
        float pushPullMultiplier = 0f;

        if(moveInputX > .1f)
        {
            pushPullMultiplier = 1f;
        }
        else if(moveInputX < -.1f)
        {
            pushPullMultiplier = -1f;
        }

        mController.Animator.SetHorizontal(pushPullMultiplier);

        mPushPullObject.PushPull(animationVelocity);
    }

    public void SetPushPullObject(PushPullObject pushPullObject)
    {
        mPushPullObject = pushPullObject;
    }
}
