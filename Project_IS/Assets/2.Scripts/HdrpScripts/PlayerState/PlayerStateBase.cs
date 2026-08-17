using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerStateBase : MonoBehaviour
{
    public PlayerStateMachine.EState key;

    protected Vector3 mCharacterPosition => mController.Movement.Position;
    protected Quaternion mCharacterRotation => mController.Movement.Rotation;

    protected PlayerController mController;

    public virtual void Initialize(PlayerController controller)
    {
        mController = controller;
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
