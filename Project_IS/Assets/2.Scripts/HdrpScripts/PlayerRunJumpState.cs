using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class PlayerRunJumpState : PlayerStateBase
{
    public bool jumpUpward = true;

    [SerializeField] private PlayerClimbLedgeState _climbLedgeState;

    private Vector3 mMoveInput;
    private float mDefaultHeight;

    // Ladder
    private float mPathZPosition;
    private float mInteractableMaxDistance;
    private float mInteractableOffsetY;
    private float mInteractableDistance;

    public override void EnterState()
    {
        if(jumpUpward)
            mController.Movement.JumpFoward();

        //mController.Animator.SetRunJump();
        mController.Animator.SetJump();

        var moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        mPathZPosition = moveState.PathZPosition;
        mInteractableMaxDistance = moveState.InteractableMaxDistance;
        mInteractableOffsetY = moveState.InteractableOffsetY;
        mInteractableDistance = moveState.InteractableDistance;
    }

    public override void ExitState()
    {
        jumpUpward = true;
        // mController.Animator.SetLanding();
    }

    public override void Tick()
    {
        mMoveInput = mController.InputHandler.MoveInput;

        if (mController.Movement.Direction == PlayerMovement.EDirection.Right)
        {
            if (mMoveInput.x < 0f)
                mMoveInput.x = 0f;
        }
        else
        {
            if (mMoveInput.x > 0f)
                mMoveInput.x = 0f;
        }

        // mController.Movement.Move(mMoveInput);
        mController.Movement.UpdateJump(mMoveInput);
        mController.Animator.SetHorizontal(mController.InputHandler.MoveInput.x);
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        mController.Movement.UpdateRotation();

        if (!mController.Movement.Jumping)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            mController.Animator.SetLanding();

            return;
        }

        if (_climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo))
        {
            // _climbLedgeState.SetLedge(hitInfo.collider.bounds);
            _climbLedgeState.SetInfo(climbLedgeInfo);
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
            return;
        }

        // fall
        PlayerFallState fallState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Fall) as PlayerFallState;

        if (fallState.CheckFall())
        // if (transform.position.y < mDefaultHeight - .1f)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Fall);
            return;
        }

        // Interactable
        int bHitDirection = checkInteractableObject(out RaycastHit interactableHitInfo);

        updateInteractable(bHitDirection, interactableHitInfo);
    }

    public void SetDefaultHeight(float height)
    {
        mDefaultHeight = height;
    }

    private int checkInteractableObject(out RaycastHit hitInfo)
    {
        // z가 0일 때의 위치
        Vector3 pathOrigin = transform.position;
        pathOrigin.y += mInteractableOffsetY;
        // pathOrigin.z = 0f;
        pathOrigin.z = mPathZPosition;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out hitInfo,
                                        mInteractableMaxDistance,
                                        LayerMask.GetMask("Interactable"));

        if (bFrontCasted)
            return 1;

        return -1;
    }

    private void updateInteractable(int type, RaycastHit hitInfo)
    {
        // front
        if (type == 1)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            // Bounds bounds = interactableObject.BoxCollider.bounds;
            Bounds bounds = hitInfo.collider.bounds;
            Vector3 characterPos = transform.position;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // Ladder
            if ((interactableObject.CompareTag("Ladder"))
                && distanceToEdge < mInteractableDistance)
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                bool bSwitched = switchToLadderState(ladderCollider);

                if (bSwitched)
                    return;
            }
        }
        // none
        else
        {
            // velocity.z를 0으로 해주지 않으면 계속 z축으로 관성?이 남아있음
            Vector3 velocity = mController.Movement.Velocity;
            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);
        }
    }

    private bool switchToLadderState(Collider[] ladderColliders)
    {
        foreach (Collider ladderCollider in ladderColliders)
        {
            // Bottom
            // Todo: InputHandler.IsUpPressed() 정의하기
            // if (mController.InputHandler.MoveInput.y > .1f)
            {
                if (ladderCollider.tag == "LadderTop")
                    continue;

                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

                // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
                if (ladderStateBase.IsOverRange(ladderHandler))
                    continue;

                // ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);
                bool bClimbLadder = ladderStateBase.SetLadderInMiddle(ladderHandler);

                if (!bClimbLadder)
                    return false;

                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                return true;
            }
        }

        return false;
    }

}

