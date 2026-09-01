using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RotationBase
{
    protected RotationHandler mRotationHandler;

    protected PlayerController mPlayerController;
    protected PlayerAnimation mPlayerAnimator;
    protected Animator mAnimator;

    public RotationBase(RotationHandler rotationHandler)
    {
        mRotationHandler = rotationHandler;

        mPlayerController = rotationHandler.PlayerController;
        mPlayerAnimator = mPlayerController.Animation;
        mAnimator = mPlayerController.Animation.Animator;
    }

    public abstract void Start();
    public abstract void StandBy();
    public abstract void OnDirectionchanged();
    public abstract void FixedUpdate();
    public abstract void Update();

    public virtual void OnAnimatorMove()
    {

    }

    public virtual void OnBeforeFixedUpdate()
    {

    }

    public virtual void OnBeforeUpdate()
    {

    }

    public virtual void OnBeforeAnimatorMove()
    {

    }

    public virtual void OnEndRotation()
    {

    }

    protected Vector3 rotateVector(Vector3 vector3, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float newX = vector3.x * cos - vector3.z * sin;
        float newZ = vector3.x * sin + vector3.z * cos;

        return new Vector3(newX, vector3.y, newZ);
    }
}
