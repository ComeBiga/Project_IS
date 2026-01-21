using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerStateBase
{
    [SerializeField] 
    private PlayerClimbLedgeState _climbLedgeState;
    [SerializeField]
    private float _fallStartVelocityY = -1f;
    [SerializeField]
    private float _heavyLandingVelocityY = -10f;
    [SerializeField]
    private float _runningLandingSpeed = .8f;

    private Animator mAnimator;
    private Coroutine mRunningLandingRoutine = null;
    private bool mbLanding = false;
    private float mMinVelocityY = 0f;

    // Ladder
    private float mPathZPosition;
    private float mInteractableMaxDistance;
    private float mInteractableOffsetY;
    private float mInteractableDistance;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mbLanding = false;
        mMinVelocityY = 0f;

        // mController.Animator.ResetLanding();
        mController.Animator.SetLanding(false);
        // mController.Animator.SetInputXMagnitude(0f);

        var moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        mPathZPosition = moveState.PathZPosition;
        mInteractableMaxDistance = moveState.InteractableMaxDistance;
        mInteractableOffsetY = moveState.InteractableOffsetY;
        mInteractableDistance = moveState.InteractableDistance;
    }

    public override void ExitState()
    {
        mbLanding = false;
        // mController.Animator.SetInputXMagnitude(0f);
        mController.Animator.SetLanding(false);

        if (mRunningLandingRoutine != null)
        {
            StopCoroutine(mRunningLandingRoutine);
            mRunningLandingRoutine = null;
        }
    }

    public override void Tick()
    {
        if (mbLanding)
        {
            //if(Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
            //{
            //    EndLanding();
            //    return;
            //}

            // mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

            return;
        }

        // Terrain Normal
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, .1f, LayerMask.GetMask("Ground")))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
            // Debug.Log(slopeAngle);

            var slopeState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Slope) as PlayerSlopeState;

            if (slopeAngle > slopeState.SlopeAngle)
            {
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Slope);
                return;
            }
        }

        if (mController.Movement.Velocity.y < mMinVelocityY)
            mMinVelocityY = mController.Movement.Velocity.y;

        if(mController.Movement.IsGrounded)
        {
            mbLanding = true;
            // mController.Animator.SetInputXMagnitude(0f);
            float inputXMagnitude = Mathf.Abs(mController.InputHandler.MoveInput.x);
            mController.Animator.SetInputXMagnitude(inputXMagnitude);

            if (mMinVelocityY < _heavyLandingVelocityY)
            {
                mController.Movement.SetVelocity(Vector3.zero);
                mController.Animator.SetHeavyLanding();

                PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
                moveState.EnterToIdle();
            }
            else
            {
                if (inputXMagnitude < .1f)
                {
                    // mController.Animator.SetLanding();
                    mController.Animator.SetLanding(true);
                    mController.Animator.PlayIdleLanding(0f);
                }
                else
                {
                    // mController.Animator.SetLanding();
                    mController.Animator.SetLanding(true);
                    mController.Animator.CrossFadeRunningLanding(.1f);

                    mRunningLandingRoutine = StartCoroutine(eRunningLanding());
                }
            }

            if (mController.Movement.CheckInteractableByOverlap(out Collider[] hitColliders))
            {
                var fallingGround = hitColliders[0].GetComponentInParent<FallingGround>();
                // Debug.Log($"Land on {hitColliders[0].name}");

                fallingGround.StepOn();
            }

            return;
        }

        if (_climbLedgeState.CheckLedge(out PlayerClimbLedgeState.ClimbLedgeInfo climbLedgeInfo))
        {
            _climbLedgeState.SetInfo(climbLedgeInfo);
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbLedge);
            return;
        }

        // Interactable
        int bHitDirection = checkInteractableObject(out RaycastHit interactableHitInfo);

        updateInteractable(bHitDirection, interactableHitInfo);
    }

    public void EndLanding()
    {
        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }

    public bool CheckFall()
    {
        // Debug.Log($"IsGrounded: {mController.Movement.IsGrounded}, velocity Y: {mController.Movement.Velocity.y}");

        if (!mController.Movement.IsGrounded && mController.Movement.Velocity.y < _fallStartVelocityY)
        {
            return true;
        }

        return false;
    }

    private IEnumerator eRunningLanding()
    {
        while(true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("RunningLanding"))
                break;

            if (animatorStateInfo.IsTag("IdleLanding"))
                yield break;

            yield return null;
        }

        float distance = 0f;
        PlayerMovement.EDirection direction = mController.Movement.Direction;
        float moveSpeed = mController.Movement.MoveSpeed;

        while(mbLanding)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (!animatorStateInfo.IsTag("RunningLanding"))
                break;

            Vector3 deltaPosition = mAnimator.deltaPosition;
            deltaPosition.x = deltaPosition.x * _runningLandingSpeed;
            deltaPosition.z = 0f;
            // transform.position += deltaPosition;

            Vector3 velocity = mAnimator.velocity;
            velocity.x = velocity.x * _runningLandingSpeed;

            if(direction == PlayerMovement.EDirection.Left)
            {
                if(mController.InputHandler.MoveInput.x > 0f)
                {
                    EndLanding();
                    yield break;
                }

                if (velocity.x > -moveSpeed)
                    velocity.x = -moveSpeed;
            }
            else
            {
                if (mController.InputHandler.MoveInput.x < 0f)
                {
                    EndLanding();
                    yield break;
                }

                if (velocity.x < moveSpeed)
                    velocity.x = moveSpeed;
            }

            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);
            // Debug.Log(velocity);

            distance += deltaPosition.x;

            yield return null;
        }

        // Debug.Log(distance);
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
