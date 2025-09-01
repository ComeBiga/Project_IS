using PropMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static PlayerMovement;

public class PlayerMoveState : PlayerStateBase
{
    [Header("Ladder")]
    [SerializeField] private float _ladderRadius = .5f;

    [Header("PushPull")]
    [SerializeField] private Transform _trPushPullOrigin;
    [SerializeField] private float _pushPullRadius;
    [SerializeField] private LayerMask _pushPullLayer;
    [SerializeField] private float _pushPullZDistance;
    [SerializeField] private float _pushPullPassByZSpeed = 2f;

    private float mDefaultHeight;
    private Vector3 mPreviousForward;       // 회전을 시작하면 어느 방향으로 도는지 체크해야되기 때문에 이전 방향을 저장
    private bool mbDirectionChanged = false;
    private bool mbRotating = false;        // 현재 사용되는 곳은 없지만 회전을 체크하는 변수이기 때문에 유지
    private PushPullObject mPushPullObject = null;

    private bool mbCheckPushPullObject = false;

    public override void EnterState()
    {
        mDefaultHeight = transform.position.y;

        mPreviousForward = mController.Movement.Direction == PlayerMovement.EDirection.Left ?
                           Vector3.left : Vector3.right;
    }

    public override void ExitState() 
    {

    }

    public override void Tick()
    {
        // Move
        mController.Movement.Move(mController.InputHandler.MoveInput);
        mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

        // Set Direction
        // 키입력이 들어오고 방향이 바뀌는 찰나 시점에 대한 코드
        if (mController.InputHandler.MoveInput.x > .001f || mController.InputHandler.MoveInput.x < -.001f)
        {
            EDirection targetDirection = mController.InputHandler.MoveInput.x > 0f ? EDirection.Right : EDirection.Left;

            // 키 입력 방향과 현재 방향이 다르면 방향 전환
            if (targetDirection != mController.Movement.Direction)
            {
                mbDirectionChanged = true;
                mController.Movement.SetDirection(targetDirection);
            }
        }

        // Turn CW/CCW
        Vector3 currentForward = transform.forward;
        float deltaRotatedAngle = Vector3.SignedAngle(mPreviousForward, currentForward, Vector3.up);
        
        // 회전 시작하면 어느 방향 회전인지 체크 후 Turn 애니메이션 전환
        if (mbDirectionChanged)
        {
            // 반시계방향 회전 트리거
            if (deltaRotatedAngle < -5f)
            {
                mController.Animator.TurnL(true);
                mController.Animator.TurnR(false);
                mbDirectionChanged = false;

                // 회전 각도가 있으면 회전이라고 의도를 갖게 되는데
                // 지금은 사용되는 곳이 없지만 혹시 사용되면 신경을 써야할 것 같다
                mbRotating = true;
            }
            // 시계방향 회전 트리거
            else if (deltaRotatedAngle > 5f)
            {
                mController.Animator.TurnL(false);
                mController.Animator.TurnR(true);
                mbDirectionChanged = false;
                mbRotating = true;
            }
        }
        // 회전 방향 체크 중이 아니면 
        else
        {
            if (deltaRotatedAngle > -1f && deltaRotatedAngle < 1f)
            {
                mController.Animator.TurnL(false);
                mController.Animator.TurnR(false);
                mbRotating = false;
            }
        }

        mPreviousForward = currentForward;

        // Rotate
        mController.Movement.UpdateRotation();

        // Jump
        if (mController.InputHandler.JumpPressed)
        {
            // 점프 입력이 됐을 때 이동 입력이 있으면 무조건 RunJump
            if (mController.InputHandler.MoveInput.x > .01f || mController.InputHandler.MoveInput.x < -.01f)
            {
                PlayerRunJumpState runJumpState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.RunJump) as PlayerRunJumpState;
                runJumpState.SetDefaultHeight(mDefaultHeight);

                mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
                mController.InputHandler.ResetJump();
            }
            else
            {
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.IdleJump);
                mController.InputHandler.ResetJump();
            }
        }

        // Fall
        if(transform.position.y < mDefaultHeight - 2f)
        {
            mController.StateMachine.SwitchState(PlayerStateMachine.EState.Fall);

            return;
        }

        // Ladder
        if (checkLadderObject(out Collider[] ladderColliders))
        {
            foreach (Collider ladderCollider in ladderColliders)
            {
                // Bottom
                if (mController.InputHandler.MoveInput.y > .1f)
                {
                    if (ladderCollider.tag == "LadderTop")
                        continue;

                    PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                    LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

                    // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
                    if (ladderStateBase.IsOverRange(ladderHandler))
                        continue;

                    ladderStateBase.SetLadder(ladderHandler, startFromBottom : true);

                    mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                }
                // Top
                else if (mController.InputHandler.MoveInput.y < -.1f)
                {
                    if (ladderCollider.tag != "LadderTop")
                        continue;

                    PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                    LadderHandler ladderHandler = ladderCollider.GetComponentInParent<LadderHandler>();
                    ladderStateBase.SetLadder(ladderHandler, startFromBottom: false);

                    mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                }
            }
        }

        //// Check PushPull Object
        //if(!mbCheckPushPullObject)
        //{
        //    if (checkPushPullObject(out PushPullObject[] pushPullObjects))
        //    { 
        //        mbCheckPushPullObject = true;
        //        mPushPullObject = pushPullObjects[0];
        //    }
        //}
        //else
        //{
        //    if (!checkPushPullObject(out PushPullObject[] pushPullObjects))
        //    { 
        //        mbCheckPushPullObject = false;
        //    }
        //}

        if(checkPushPullObject(out PushPullObject[] pushPullObjects))
        // if(mbCheckPushPullObject)
        {
            PushPullObject pushPullObject = pushPullObjects[0];
            mPushPullObject = pushPullObject;
            Bounds pushPullObjectBounds = pushPullObject.BoxCollider.bounds;
            Vector3 characterPos = transform.position;

            if (characterPos.y < pushPullObjectBounds.center.y)
            {
                float distanceToPPO = 0f;

                // Pass by
                if (characterPos.x < pushPullObjectBounds.min.x)
                {
                    distanceToPPO = pushPullObjectBounds.min.x - characterPos.x;

                    characterPos.z = -((_pushPullRadius - distanceToPPO) / _pushPullRadius) * _pushPullZDistance;
                    transform.position = characterPos;
                }
                else if (characterPos.x > pushPullObjectBounds.max.x)
                {
                    distanceToPPO = characterPos.x - pushPullObjectBounds.max.x;

                    characterPos.z = -((_pushPullRadius - distanceToPPO) / _pushPullRadius) * _pushPullZDistance;
                    transform.position = characterPos;
                }
                else
                {
                    distanceToPPO = characterPos.z - pushPullObjectBounds.min.z;

                    // PushPull
                    if (distanceToPPO < _pushPullZDistance && mController.InputHandler.IsInteracting)
                    {
                        PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                        pushPullState.SetPushPullObject(pushPullObjects[0]);
                        mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                    }
                }

                // Climb Object Up
                if (mController.InputHandler.MoveInput.y > .1f)
                {
                    PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                    pushPullState.SetClimbObject(pushPullObjects[0], climbUp : true);
                    mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                }
            }
            else
            {
                // Climb Object Down
                if(mController.InputHandler.MoveInput.y < -.1f)
                {
                    PlayerClimbObjectState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                    pushPullState.SetClimbObject(pushPullObjects[0], climbUp: false);
                    mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                }
            }
        }
    }

    private bool checkLadderObject(out Collider[] collider)
    {
        Collider[] ladderColliders = Physics.OverlapSphere(transform.position, _ladderRadius, LayerMask.GetMask("Ladder"));

        if (ladderColliders.Length > 0)
        {
            collider = ladderColliders;
            return true;
        }

        collider = null;
        return false;
    }

    private bool checkPushPullObject(out PushPullObject[] pushPullObjects)
    {
        Collider[] pushPullColliders = Physics.OverlapSphere(_trPushPullOrigin.position, _pushPullRadius, _pushPullLayer);

        if (pushPullColliders.Length > 0)
        {
            pushPullObjects = new PushPullObject[pushPullColliders.Length];

            for(int i = 0; i < pushPullColliders.Length; ++i)
            {
                pushPullObjects[i] = pushPullColliders[i].GetComponent<PushPullObject>();
            }

            return true;
        }

        pushPullObjects = null;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        // Gizmos.DrawWireSphere(_trPushPullOrigin.position, _pushPullRadius);
    }
}
