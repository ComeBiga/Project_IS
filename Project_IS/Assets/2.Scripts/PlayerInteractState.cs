using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractState : PlayerStateBase
{
    private InteractableObject mInteractableObject;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
    }

    public override void EnterState()
    {
        mInteractableObject.Enter(mController);
    }

    public override void ExitState()
    {
        mInteractableObject.Exit(mController);
    }

    public override void Tick()
    {
        mInteractableObject.Tick(mController);
    }

    public void SetInteractableObject(InteractableObject interactableObject)
    {
        mInteractableObject = interactableObject;
    }
}
