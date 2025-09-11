using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPushPullState : PlayerStateBase
{
    private Animator mAnimator;
    private PushPullObject mPushPullObject;

    private bool mbPushPull = true;

    private bool mbActiveIK = false;
    private Vector3 mLeftHandIKPos;
    private Vector3 mRightHandIKPos;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mbActiveIK = false;

        // mController.Movement.PushPull(Vector2.zero, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        // mPushPullObject.SetFriction(false);

        //mController.Animator.onAnimationIK -= updateAnimationIK;
        //mController.Animator.onAnimationIK += updateAnimationIK;

        // StartCoroutine(eHandIKPos());
    }

    public override void ExitState()
    {
        // mPushPullObject.SetFriction(true);

        // mController.Animator.onAnimationIK -= updateAnimationIK;
    }

    public override void Tick()
    {
        if (!mController.InputHandler.IsInteracting || !mbPushPull)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            return;
        }

        // mController.Movement.Move(mController.InputHandler.MoveInput);
        // Debug.Log(mAnimator.velocity);
        Vector3 animationVelocity = mAnimator.velocity;
        animationVelocity.z = 0f;
        // mController.Movement.SetVelocity(animationVelocity);

        float moveInputX = mController.InputHandler.MoveInput.x;
        float pushPullMultiplier = 0f;

        if(moveInputX > .1f)
        {
            mPushPullObject.SetFriction(false);
            pushPullMultiplier = 1f;
        }
        else if(moveInputX < -.1f)
        {
            mPushPullObject.SetFriction(false);
            pushPullMultiplier = -1f;
        }
        else
        {
            mPushPullObject.SetFriction(true);
        }

        mController.Movement.SetVelocity(pushPullMultiplier * Vector3.right * .8f);
        mController.Animator.SetHorizontal(pushPullMultiplier);

        // Debug.Log(animationVelocity);
        // mPushPullObject.PushPull(animationVelocity);
        mbPushPull = mPushPullObject.PushPull(pushPullMultiplier * Vector3.right * .8f);
    }

    public void SetPushPullObject(PushPullObject pushPullObject)
    {
        mPushPullObject = pushPullObject;
    }

    private IEnumerator eHandIKPos()
    {
        yield return new WaitUntil(() => mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("PushPull"));

        Bounds bounds = mPushPullObject.BoxCollider.bounds;

        mLeftHandIKPos = mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        mLeftHandIKPos.z = bounds.min.z;
        mPushPullObject.HandlePointL.position = mLeftHandIKPos;

        mRightHandIKPos = mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position;
        mRightHandIKPos.z = bounds.min.z;
        mPushPullObject.HandlePointR.position = mRightHandIKPos;

        mbActiveIK = true;
    }

    private void updateAnimationIK()
    {
        if(!mbActiveIK)
            return;

        mAnimator.SetIKPosition(AvatarIKGoal.LeftHand, mPushPullObject.HandlePointL.position);
        mAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
        mAnimator.SetIKPosition(AvatarIKGoal.RightHand, mPushPullObject.HandlePointR.position);
        mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
    }
}
