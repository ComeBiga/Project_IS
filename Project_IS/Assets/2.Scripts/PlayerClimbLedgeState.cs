using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerClimbLedgeState;

public class PlayerClimbLedgeState : PlayerStateBase
{
    public struct ClimbLedgeInfo
    {
        public Bounds ledgeBounds;
        public int checkIndex;
    }

    [SerializeField] private float _deltaPositionYSpeed = 1f;
    [SerializeField] private float _lerpXOffset = .2f;
    [SerializeField] private float _lerpYOffsetOverHead = 1f;
    [SerializeField] private float _lerpYOffsetChest = 1f;
    [SerializeField] private float _lerpYOffsetStomach = 1f;
    [SerializeField] private float _lerpYOffsetKnee = 1f;
    [SerializeField] private float _raycastDistance = .5f;
    [Range(2, 10)]
    [SerializeField] private int _raycastCount = 2;
    [SerializeField] private float _ledgeRange = .3f;

    private Animator mAnimator;
    private ClimbLedgeInfo mClimbLedgeInfo;
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
        mController.Movement.SetKinematic(true);
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);

        mController.Animator.SetIndex(mClimbLedgeInfo.checkIndex);
        mController.Animator.SetInputXMagnitude(0f);

        mbClimb = true;

        StartCoroutine(eClimbLedge());
    }

    public override void ExitState()
    {
        mController.Movement.SetKinematic(false);
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);        
    }

    public override void Tick()
    {
        
    }

    public void SetInfo(ClimbLedgeInfo climbLedgeInfo)
    {
        mClimbLedgeInfo = climbLedgeInfo;
    }

    [Obsolete]
    public void SetLedge(Bounds ledgeBounds)
    {
        mLedgeBounds = ledgeBounds;
    }

    public bool CheckLedge(out ClimbLedgeInfo climbLedgeInfo)
    {
        climbLedgeInfo = new ClimbLedgeInfo();

        Vector3 origin = getOrigin();
        Vector3 direction = getDirection();

        for(int i = 0; i < _raycastCount; i++)
        {
            Vector3 pos = origin;
            pos.y -= getSpacing() * i;

            bool bCheck = Physics.Raycast(pos, direction, out RaycastHit hitInfo, _raycastDistance, LayerMask.GetMask("Ground"));

            if (bCheck)
            {
                Bounds bounds = hitInfo.collider.bounds;
                float ledgeY = bounds.max.y;
                float range = _ledgeRange;

                if (pos.y > ledgeY - range && pos.y < ledgeY + range)
                {
                    climbLedgeInfo.ledgeBounds = bounds;
                    climbLedgeInfo.checkIndex = i;
                    return true;
                }
            }
        }

        climbLedgeInfo.ledgeBounds = new Bounds();
        climbLedgeInfo.checkIndex = -1;
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
        // float originY = transform.position.y + mController.Movement.Height;
        // float targetY = mLedgeBounds.max.y - mController.Movement.Height + _lerpYOffset;
        // float targetY = mClimbLedgeInfo.ledgeBounds.max.y - mController.Movement.Height + _lerpYOffset;
        float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? mClimbLedgeInfo.ledgeBounds.max.x + _lerpXOffset: mClimbLedgeInfo.ledgeBounds.min.x - _lerpXOffset;
        float targetY = mClimbLedgeInfo.ledgeBounds.max.y;

        switch (mClimbLedgeInfo.checkIndex)
        {
            case 0:
                Debug.Log("OverHead Ledge Climb");
                targetY -= _lerpYOffsetOverHead;
                break;
            case 1:
                Debug.Log("Chest Ledge Climb");
                targetY -= _lerpYOffsetChest;
                break;
            case 2:
                Debug.Log("Stomach Ledge Climb");
                targetY -= _lerpYOffsetStomach;
                break;
            case 3:
                Debug.Log("Knee Ledge Climb");
                targetY -= _lerpYOffsetKnee;
                break;
            default:
                Debug.Log("Default Ledge Climb");
                targetY -= _lerpYOffsetKnee;
                break;
        }

        float timer = 0f;
        float duration = .2f;

        while(timer < duration)
        {
            float t = timer / duration;

            Vector3 newPos = transform.position;
            newPos.x = Mathf.Lerp(transform.position.x, targetX, t);
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

            if (transform.position.y < mClimbLedgeInfo.ledgeBounds.max.y)
                deltaPosition.y *= _deltaPositionYSpeed;
            else
                deltaPosition.y = 0f;

            deltaPosition.z = 0f;

            transform.position += deltaPosition;

            if (transform.position.y > mClimbLedgeInfo.ledgeBounds.max.y)
            {
                targetPos = transform.position;
                targetPos.y = mClimbLedgeInfo.ledgeBounds.max.y;
                transform.position = targetPos;
            }

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

    private float getSpacing()
    {
        return mController.Movement.Height / (_raycastCount - 1);
    }

    [ContextMenu("Print Spacing")]
    private void printSpacing()
    {
        Debug.Log(getSpacing());
        Debug.Log(mController.Movement.Height);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        //Vector3 origin = getOrigin();
        //Gizmos.color = Color.red;

        //for (int i = 0; i < _raycastCount; i++)
        //{
        //    Vector3 pos = origin;
        //    pos.y -= getSpacing() * i;
        //    Gizmos.DrawRay(pos, getDirection() * _raycastDistance);
        //}
        // Gizmos.DrawRay(getOrigin(), getDirection() * _raycastDistance);
    }
}
