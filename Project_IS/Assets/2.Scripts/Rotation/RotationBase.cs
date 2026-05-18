using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RotationBase
{
    protected RotationHandler mRotationHandler;

    protected PlayerController mPlayerController;
    protected PlayerAnimator mPlayerAnimator;
    protected Animator mAnimator;

    public RotationBase(RotationHandler rotationHandler)
    {
        mRotationHandler = rotationHandler;

        mPlayerController = rotationHandler.PlayerController;
        mPlayerAnimator = mPlayerController.Animator;
        mAnimator = mPlayerController.Animator.Animator;
    }

    public abstract void Start();
    public abstract void OnDirectionchanged();
    public abstract void Update();
    public abstract void FixedUpdate();
    public abstract void StandBy();

    public virtual void OnBeforeRotate()
    {

    }
}
