using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPushPullState : PlayerStateBase
{
    public float FrontPushPullDistance => mFrontPushPullDistance;

    [SerializeField]
    private float mFrontPushPullDistance = .5f;
    [SerializeField]
    private float mFrontPushPullRadius = .5f;

    private Animator mAnimator;
    private PushPullObject mPushPullObject;
    private float mDistanceToObject;
    private int mType; // side: 0, front: 1
    private int mAnimType = 0; // idle: 0, push: 1, pull: 2
    private PlayerMovement.EDirection mDirection;
    private Vector3 mPushPoint;

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

        mDirection = mController.Movement.Direction;
        mController.Animator.SetIndex(mType);

        // mController.Movement.PushPull(Vector2.zero, 0f);
        if (mType == 0)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        // mPushPullObject.SetFriction(false);
        mController.Movement.SetFriction(false);

        if(mType == 1 || mType == 2)
        {
            mController.Movement.SetRadius(mFrontPushPullRadius);

            if (mController.Movement.Direction == PlayerMovement.EDirection.Right)
            {
                Vector3 newPosition = transform.position;
                newPosition.x = mPushPoint.x - mFrontPushPullDistance;
                transform.position = newPosition;
            }
            else if(mController.Movement.Direction == PlayerMovement.EDirection.Left)
            {
                Vector3 newPosition = transform.position;
                newPosition.x = mPushPoint.x + mFrontPushPullDistance;
                transform.position = newPosition;
            }
        }

        //mController.Animator.onAnimationIK -= updateAnimationIK;
        //mController.Animator.onAnimationIK += updateAnimationIK;

        // StartCoroutine(eHandIKPos());
    }

    public override void ExitState()
    {
        // mPushPullObject.SetFriction(true);
        mController.Movement.SetFriction(true);

        if (mType == 1 || mType == 2)
        {
            mController.Movement.SetRadius();
        }

        // mController.Animator.onAnimationIK -= updateAnimationIK;
    }

    public override void Tick()
    {
        if (mType == 2)
        {
            mController.Movement.Move(mController.InputHandler.MoveInput * .2f);
            mPushPullObject.StayPushPull();

            int hitIndex = checkInteractableObject(out RaycastHit hitInfo);
            // float pushPullDistance = Mathf.Abs(mPushPoint.x - transform.position.x);

            if (Mathf.Abs(mController.InputHandler.MoveInputRaw.x) < .01f
                || hitIndex == -1)
            {
                mbPushPull = false;
                mPushPullObject.StopPushPull();

                // var moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
                var moveState = mController.StateMachine.GetStateBase<PlayerMoveState>();
                moveState.EnterToIdle();

                mAnimType = 0;
                mController.Animator.SetIndex(mAnimType);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
                mController.StateMachine.SwitchState<PlayerMoveState>();
            }

            return;
        }

        if (!mController.InputHandler.IsInteracting || !mbPushPull)
        {
            mAnimType = 0;
            mController.Animator.SetIndex(mAnimType);
            // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            mController.StateMachine.SwitchState<PlayerMoveState>();
            return;
        }

        if(mType == 1)
        {
            if (mDirection == PlayerMovement.EDirection.Left)
            {
                if (mController.InputHandler.MoveInput.x > .1f)
                    mAnimType = 2; // pull
                else if (mController.InputHandler.MoveInput.x < -.1f)
                    mAnimType = 1; // push
                else
                    mAnimType = 0; // idle
            }
            else
            {
                if (mController.InputHandler.MoveInput.x > .1f)
                    mAnimType = 1; // push
                else if (mController.InputHandler.MoveInput.x < -.1f)
                    mAnimType = 2; // pull
                else
                    mAnimType = 0; // idle
            }

            mController.Animator.SetIndex(mAnimType);
        }

        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        // mController.Movement.Move(mController.InputHandler.MoveInput);
        // Debug.Log(mAnimator.velocity);
        Vector3 animationVelocity = mAnimator.velocity;
        animationVelocity.z = 0f;
        // mController.Movement.SetVelocity(animationVelocity);

        float moveInputX = mController.InputHandler.MoveInput.x;
        float pushPullMultiplier = 0f;

        if(moveInputX > .1f)
        {
            // mPushPullObject.SetFriction(false);
            pushPullMultiplier = 1f;
        }
        else if(moveInputX < -.1f)
        {
            // mPushPullObject.SetFriction(false);
            pushPullMultiplier = -1f;
        }
        else
        {
            // mPushPullObject.SetFriction(true);
        }

        // mController.Movement.SetVelocity(pushPullMultiplier * Vector3.right * .8f);
        // mController.Animator.SetHorizontal(pushPullMultiplier);

        // Debug.Log(mController.Movement.Velocity);

        // Debug.Log(animationVelocity);
        // mPushPullObject.PushPull(animationVelocity);
        mbPushPull = mPushPullObject.PushPull(mController, pushPullMultiplier * Vector3.right * .8f);
        mController.Animator.SetHorizontal(mPushPullObject.GetVelocityXRatio());

        Vector3 newPosition = transform.position;
        newPosition.x = mPushPullObject.transform.position.x - mDistanceToObject;
        transform.position = newPosition;
    }

    public void SetPushPullObject(PushPullObject pushPullObject)
    {
        mPushPullObject = pushPullObject;

        mDistanceToObject = mPushPullObject.transform.position.x - transform.position.x;
    }

    /// <summary>
    /// side : 0, front : 1
    /// </summary>
    /// <param name="type"></param>
    public void SetType(int type)
    {
        mType = type;
    }

    public void SetPushPoint(Vector3 point)
    {
        mPushPoint = point;
    }

    private int checkInteractableObject(out RaycastHit hitInfo)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = transform.position;
        pathOrigin.y += 1f;
        // pathOrigin.z = 0f;
        pathOrigin.z = 0f;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out hitInfo,
                                        mFrontPushPullDistance + .1f,
                                        LayerMask.GetMask("Interactable"));

        if (bFrontCasted)
            return 1;

        return -1;
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
