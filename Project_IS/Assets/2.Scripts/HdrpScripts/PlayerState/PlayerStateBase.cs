using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerStateBase : MonoBehaviour
{
    public PlayerStateMachine.EState key;

    protected Vector3 mCharacterPosition => mController.Movement.Position;
    protected Quaternion mCharacterRotation => mController.Movement.Rotation;

    protected PlayerController mController;
    protected PlayerInputHandler mInputHandler;
    protected PlayerMovement mMovement;
    protected PlayerAnimator mAnimator;
    protected PlayerStateMachine mStateMachine;
    protected PlayerInteractable mInteractable;
    protected PlayerCharacterSound mCharacterSound;

    public virtual void Initialize(PlayerController controller)
    {
        mController = controller;

        mInputHandler = controller.InputHandler;
        mMovement = controller.Movement;
        mAnimator = controller.Animator;
        mStateMachine = controller.StateMachine;
        mInteractable = controller.Interactable;
        mCharacterSound = controller.CharacterSound;
    }

    public virtual void EnterState() { }

    public virtual void ExitState() { }

    public virtual void Tick() { }

    public virtual void FixedTick() { }

    public virtual void LateFixedTick() { }

    public virtual void AnimatorMoveTick() { }

    public virtual void AnimatorIKTick() { }

    public virtual void Standby() { }
}
