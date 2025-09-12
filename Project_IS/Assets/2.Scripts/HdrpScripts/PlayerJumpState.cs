using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerStateBase
{
    public enum EType { Idle, Run }

    public EType type = EType.Idle;

    [SerializeField] private PlayerClimbLedgeState _climbLedgeState;

    private Vector3 mMoveInput;
    private Animator mAnimator;
    private float mMaxDistance = .5f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mController.Movement.Jump();

        mController.Animator.SetJump();
    }

    public override void ExitState()
    {
        mController.Animator.SetLanding();
    }

    public override void Tick()
    {
        mController.Animator.SetHorizontal(mController.InputHandler.MoveInput.x);

        if (!mController.Movement.Jumping)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            return;
        }

        // if(checkLedge(out RaycastHit hitInfo))
        if(_climbLedgeState.CheckLedge(out RaycastHit hitInfo))
        {
            // var climbLedgeState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbLedge) as PlayerClimbLedgeState;
            _climbLedgeState.SetLedge(hitInfo.collider.bounds);
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
            return;
        }
    }

    private bool checkLedge(out RaycastHit hitInfo)
    {
        Vector3 origin = getOrigin();
        Vector3 direction = getDirection();

        bool bCheck = Physics.Raycast(origin, direction, out hitInfo, mMaxDistance, LayerMask.GetMask("Ground"));

        if(bCheck)
        {
            Bounds bounds = hitInfo.collider.bounds;
            float ledgeY = bounds.max.y;
            float range = .3f;

            if(origin.y > ledgeY - range && origin.y < ledgeY + range)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 getOrigin()
    {
        Vector3 origin = transform.position;
        origin.y += mController.Movement.Height;
        return origin;
    }

    private Vector3 getDirection()
    {
        return mController.Movement.DirectionToVector();
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        //Vector3 headPos = mAnimator.GetBoneTransform(HumanBodyBones.Head).position;
        //Vector3 footPos = mAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        //Vector3 direction = Vector3.up * (headPos.y - footPos.y);

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(footPos + Vector3.forward * -1f, direction);

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(getOrigin(), getDirection() * mMaxDistance);
    }
}
