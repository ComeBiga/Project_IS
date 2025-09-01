using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerStateBase : MonoBehaviour
{
    public PlayerStateMachine.EState key;

    protected PlayerController mController;

    public virtual void Initialize(PlayerController controller)
    {
        mController = controller;
    }

    public virtual void EnterState() { }

    public virtual void ExitState() { }

    public virtual void Tick() { }
}
