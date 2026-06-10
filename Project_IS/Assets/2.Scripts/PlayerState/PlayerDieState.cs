using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDieState : PlayerStateBase
{
    [SerializeField]
    private RagdollHandler _ragDollHandler;
    [SerializeField]
    private float _fallForce = 50f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
    }

    public override void EnterState()
    {
        _ragDollHandler.EnableRagdoll();
        _ragDollHandler.SetVelocity(mController.Movement.Velocity);

        mController.Movement.SetColliderActive(false);
        mController.Movement.SetKinematic(true);

        _ragDollHandler.AddForce(Vector3.down * _fallForce);
    }

    public override void ExitState()
    {
        mController.Movement.SetColliderActive(true);
        mController.Movement.SetKinematic(false);
        _ragDollHandler.DisableRagdoll();
    }

    public override void Tick()
    {
        _ragDollHandler.DebugVelocity();
    }
}
