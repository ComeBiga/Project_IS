using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RotationBase
{
    public float NormalizedTime { get; protected set; }

    protected RotationHandler mRotationHandler;

    protected PlayerController mPlayerController;
    protected PlayerAnimation mPlayerAnimator;
    protected Animator mAnimator;

    protected PlayerTurnState.ETurnType mTurnType;

    public RotationBase(RotationHandler rotationHandler, PlayerController playerController)
    {
        mRotationHandler = rotationHandler;

        mPlayerController = playerController;
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

    public virtual void Rotate(Vector3 fromEulerAngles, Vector3 toEulerAngles, float deltaTime)
    {

    }

    public virtual void SetTurnType(PlayerTurnState.ETurnType turnType)
    {
        mTurnType = turnType;
    }

    public virtual void SetNormalizedTime(float normalizedTime)
    {
        NormalizedTime = normalizedTime;
    }

    protected Quaternion getCurrentRotation()
    {
        return mPlayerController.Movement.DirectionToRotation(mPlayerController.Movement.Direction);
    }

    protected Vector3 getCurrentEulerAngles()
    {
        return mPlayerController.Movement.DirectionToRotation(mPlayerController.Movement.Direction).eulerAngles;
    }

    protected float getRemainAngle(float targetAngle)
    {
        return Mathf.DeltaAngle(mPlayerController.Movement.Rotation.eulerAngles.y, targetAngle);
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
