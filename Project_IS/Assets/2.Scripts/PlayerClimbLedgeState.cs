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
        public LedgeHandler ledgeHandler;
        public Vector3 nearestLedgePoint;
    }

    [Header("Debug")]
    [SerializeField] private bool _drawRay = true;

    [SerializeField] private float _deltaPositionYSpeed = 1f;

    [Header("Lerp X Offset Settings")]
    [SerializeField] private float _lerpXOffset = .2f;
    [SerializeField] private float _lerpXOffsetOverHead = .2f;
    [SerializeField] private float _lerpXOffsetChest = .2f;
    [SerializeField] private float _lerpXOffsetStomach = .2f;
    [SerializeField] private float _lerpXOffsetKnee = .2f;

    [Header("Lerp Y Offset Settings")]
    [SerializeField] private float _lerpYOffsetOverHead = 1f;
    [SerializeField] private float _lerpYOffsetChest = 1f;
    [SerializeField] private float _lerpYOffsetStomach = 1f;
    [SerializeField] private float _lerpYOffsetKnee = 1f;

    [Header("Raycast Settings")]
    [SerializeField] private float _raycastDistance = .5f;
    [Range(2, 10)]
    [SerializeField] private int _raycastCount = 2;
    [SerializeField] private float _ledgeRange = .3f;
    [SerializeField] private LayerMask _raycastLayer;

    [Header("Normalized Time Settings")]
    [SerializeField] private float _exitNormalizedTimeOverHead = .8f;
    [SerializeField] private float _exitNormalizedTimeChest = .745f;
    [SerializeField] private float _exitNormalizedTimeStomach = .68f;
    [SerializeField] private float _exitNormalizedTimeKnee = .53f;

    private Animator mAnimator;
    private ClimbLedgeInfo mClimbLedgeInfo;
    private Bounds mLedgeBounds;
    private Ground mGround;

    private bool mbClimb = false;
    private float mStartMoveInputX;

    private Vector3? mBoundMax = null;
    private Vector3? mBoundMin = null;

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

        mController.Animator.SetClimbLedge();
        mController.Animator.SetIndex(mClimbLedgeInfo.checkIndex);
        mController.Animator.SetInputXMagnitude(0f);

        mController.Animator.AnimationEventReceiver.onTouchHand -= onFootStepFromFall;
        mController.Animator.AnimationEventReceiver.onTouchHand += onFootStepFromFall;

        mbClimb = true;

        StartCoroutine(eClimbLedge());
    }

    public override void ExitState()
    {
        mController.Movement.SetKinematic(false);
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);

        mGround = null;
        mController.Animator.AnimationEventReceiver.onTouchHand -= onFootStepFromFall;
    }

    public override void Tick()
    {
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));
    }

    public void SetInfo(ClimbLedgeInfo climbLedgeInfo)
    {
        mClimbLedgeInfo = climbLedgeInfo;
    }

    public void EnterFromFall(Ground ground)
    {
        mGround = ground;
    }

    [Obsolete]
    public void SetLedge(Bounds ledgeBounds)
    {
        mLedgeBounds = ledgeBounds;
    }

    public bool CheckLedge(out ClimbLedgeInfo climbLedgeInfo, out RaycastHit hitInfo)
    {
        climbLedgeInfo = new ClimbLedgeInfo();
        hitInfo = new RaycastHit();

        Vector3 origin = getOrigin();
        Vector3 direction = getDirection();
        LedgeHandler ledgeHandler = null;

        for(int i = 0; i < _raycastCount; i++)
        {
            Vector3 pos = origin;
            pos.y -= getSpacing() * i;

            // bool bCheck = Physics.Raycast(pos, direction, out RaycastHit hitInfo, _raycastDistance, LayerMask.GetMask("Ground"));
            bool bCheck = Physics.Raycast(pos, direction, out hitInfo, _raycastDistance, _raycastLayer, QueryTriggerInteraction.Ignore);

            if (bCheck)
            {
                Bounds bounds = hitInfo.collider.bounds;
                Vector3 ledgePoint = bounds.max;
                ledgePoint.x = hitInfo.point.x;
                float ledgeY = bounds.max.y;
                float range = _ledgeRange;
                mBoundMax = bounds.max;
                mBoundMin = bounds.min;

                if(ledgeHandler == null)
                    ledgeHandler = hitInfo.collider.GetComponent<LedgeHandler>();

                if(ledgeHandler != null)
                {
                    Vector3? ledgePointOrNull = ledgeHandler.GetNearestLedgePoint(pos);

                    if(ledgePointOrNull != null)
                    {
                        ledgePoint = ledgePointOrNull.Value;
                        ledgeY = ledgePoint.y;
                    }
                }

                if (pos.y > ledgeY - range && pos.y < ledgeY + range)
                {
                    climbLedgeInfo.ledgeBounds = bounds;
                    climbLedgeInfo.checkIndex = i;
                    climbLedgeInfo.ledgeHandler = ledgeHandler;
                    climbLedgeInfo.nearestLedgePoint = ledgePoint;

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
        if (!mbClimb)
            return;

        mbClimb = false;

        PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;

        if (Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
        {
            moveState.EnterToRun(mStartMoveInputX);
        }
        else
        {
            // moveState.EnterToIdle();
        }

        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }

    private void onFootStepFromFall()
    {
        if (mGround != null)
        {
            Debug.Log("LedgeFootStepSoune");
            mGround.PlayFootStepBigSound(volume: .5f);
        }
    }

    private IEnumerator eClimbLedge()
    {
        // float originY = transform.position.y + mController.Movement.Height;
        // float targetY = mLedgeBounds.max.y - mController.Movement.Height + _lerpYOffset;
        // float targetY = mClimbLedgeInfo.ledgeBounds.max.y - mController.Movement.Height + _lerpYOffset;
        // float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ? mClimbLedgeInfo.ledgeBounds.max.x + _lerpXOffset: mClimbLedgeInfo.ledgeBounds.min.x - _lerpXOffset;
        float targetY = mClimbLedgeInfo.ledgeBounds.max.y;

        if (mClimbLedgeInfo.ledgeHandler != null)
            targetY = mClimbLedgeInfo.nearestLedgePoint.y;

        float exitNormalizedTime = 0f;
        float lerpXOffset = 0f;

        switch (mClimbLedgeInfo.checkIndex)
        {
            case 0:
                Debug.Log("OverHead Ledge Climb");
                targetY -= _lerpYOffsetOverHead;
                exitNormalizedTime = _exitNormalizedTimeOverHead;
                lerpXOffset = _lerpXOffsetOverHead;
                break;
            case 1:
                Debug.Log("Chest Ledge Climb");
                targetY -= _lerpYOffsetChest;
                exitNormalizedTime = _exitNormalizedTimeChest;
                lerpXOffset = _lerpXOffsetChest;
                break;
            case 2:
                Debug.Log("Stomach Ledge Climb");
                targetY -= _lerpYOffsetStomach;
                exitNormalizedTime = _exitNormalizedTimeStomach;
                lerpXOffset = _lerpXOffsetStomach;
                break;
            case 3:
                Debug.Log("Knee Ledge Climb");
                targetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
            default:
                Debug.Log("Default Ledge Climb");
                targetY -= _lerpYOffsetKnee;
                exitNormalizedTime = _exitNormalizedTimeKnee;
                lerpXOffset = _lerpXOffsetKnee;
                break;
        }

        //float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
        //                mClimbLedgeInfo.ledgeBounds.max.x + lerpXOffset : mClimbLedgeInfo.ledgeBounds.min.x - lerpXOffset;

        //if(mClimbLedgeInfo.ledgeHandler != null)
        //    targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
        //                mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset;
        float targetX = (mController.Movement.Direction == PlayerMovement.EDirection.Left) ?
                    mClimbLedgeInfo.nearestLedgePoint.x + lerpXOffset : mClimbLedgeInfo.nearestLedgePoint.x - lerpXOffset;


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
            // normalizedTime이 일정 비율이 넘으면 상태 전환
            AnimatorStateInfo stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

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

            // mStartMoveInputX = deltaPosition.x / (Time.deltaTime * mController.Movement.MoveSpeed);

            //if (stateInfo.normalizedTime >= exitNormalizedTime)
            //{
            //    EndClimbLedge();
            //    yield break;
            //}

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

        if(_drawRay)
        {
            Vector3 origin = getOrigin();
            Gizmos.color = Color.red;

            for (int i = 0; i < _raycastCount; i++)
            {
                Vector3 pos = origin;
                pos.y -= getSpacing() * i;
                Gizmos.DrawRay(pos, getDirection() * _raycastDistance);
            }
            Gizmos.DrawRay(getOrigin(), getDirection() * _raycastDistance);
        }

        Bounds ledgeBounds = mClimbLedgeInfo.ledgeBounds;

        Vector3 ledgeMaxPos = ledgeBounds.max;
        ledgeMaxPos.z = getOrigin().z;

        Gizmos.color = Color.blue;
        Vector3 ledgeMaxPosRange = ledgeMaxPos;
        ledgeMaxPosRange.y += _ledgeRange;
        Gizmos.DrawSphere(ledgeMaxPosRange, .1f);
        ledgeMaxPosRange.y -= _ledgeRange;
        ledgeMaxPosRange.y -= _ledgeRange;
        Gizmos.DrawSphere(ledgeMaxPosRange, .1f);

        Vector3 ledgeMinPos = ledgeBounds.max;
        ledgeMinPos.x = ledgeBounds.min.x;
        ledgeMinPos.z = getOrigin().z;

        Gizmos.color = Color.blue;
        Vector3 ledgeMinPosRange = ledgeMinPos;
        ledgeMinPosRange.y += _ledgeRange;
        Gizmos.DrawSphere(ledgeMinPosRange, .1f);
        ledgeMinPosRange.y -= _ledgeRange;
        ledgeMinPosRange.y -= _ledgeRange;
        Gizmos.DrawSphere(ledgeMinPosRange, .1f);

        //if(mBoundMax != null)
        //{
        //    Gizmos.DrawSphere(mBoundMax.Value, 1f);
        //}

        //if(mBoundMin != null)
        //{
        //    Gizmos.DrawSphere(mBoundMin.Value, 1f);
        //}
    }
}
