using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbLedgeState : PlayerStateBase
{
    [SerializeField] private float _lerpYOffset = 1f;
    [SerializeField] private float _raycastDistance = .5f;
    [SerializeField] private float _ledgeRange = .3f;

    private Animator mAnimator;
    private Bounds mLedgeBounds;

    private bool mbClimb = false;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mController.Movement.SetVelocity(Vector3.zero);
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);

        mController.Animator.SetInputXMagnitude(0f);

        mbClimb = true;

        StartCoroutine(eClimbLedge());
    }

    public override void ExitState()
    {
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);        
    }

    public override void Tick()
    {
        
    }

    public void SetLedge(Bounds ledgeBounds)
    {
        mLedgeBounds = ledgeBounds;
    }

    public bool CheckLedge(out RaycastHit hitInfo)
    {
        Vector3 origin = getOrigin();
        Vector3 direction = getDirection();

        bool bCheck = Physics.Raycast(origin, direction, out hitInfo, _raycastDistance, LayerMask.GetMask("Ground"));

        if (bCheck)
        {
            Bounds bounds = hitInfo.collider.bounds;
            float ledgeY = bounds.max.y;
            float range = _ledgeRange;

            if (origin.y > ledgeY - range && origin.y < ledgeY + range)
            {
                return true;
            }
        }

        return false;
    }

    public void EndClimbLedge()
    {
        mbClimb = false;

        PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        moveState.EnterToIdle();
        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }

    private IEnumerator eClimbLedge()
    {
        float originY = transform.position.y + mController.Movement.Height;
        float targetY = mLedgeBounds.max.y - mController.Movement.Height + _lerpYOffset;

        float timer = 0f;
        float duration = .2f;

        while(timer < duration)
        {
            float t = timer / duration;

            Vector3 newPos = transform.position;
            newPos.y = Mathf.Lerp(transform.position.y, targetY, t);

            transform.position = newPos;

            timer += Time.deltaTime;
            yield return null;
        }

        Vector3 targetPos = transform.position;
        targetPos.y = targetY;
        transform.position = targetPos;

        while(mbClimb)
        {
            Vector3 deltaPosition = mAnimator.deltaPosition;
            deltaPosition.z = 0f;

            transform.position += deltaPosition;

            yield return null;
        }
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

        Gizmos.color = Color.red;
        Gizmos.DrawRay(getOrigin(), getDirection() * _raycastDistance);
    }
}
