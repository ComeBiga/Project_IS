using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRopeClimbState : PlayerStateBase
{
    public event System.Action onRopeCollision = null;

    [Header("Debug")]
    [SerializeField] private bool _showWireSphere = true;
    [SerializeField] private bool _printDetectedSegmentIndex = false;

    [Header("Detection")]
    [SerializeField] private float _offsetY = 1.5f;
    [SerializeField] private float _detectionRadius = 0.5f;

    [Header("Climb")]
    [SerializeField] private int _jointIndexOffset = -2;
    [SerializeField] private float _climbOnceDistance = .57f;
    [SerializeField] private float _startDistanceFromJoint = .225f;
    [SerializeField] private float _startDistanceToForward = .225f;
    [SerializeField] private Transform _trRightHand;
    [SerializeField] private Transform _trLeftHand;
    [SerializeField] private Transform _trRightToe;

    private Animator mAnimator;
    private RopeHandler mRopeHandler;

    private int mDetectedSegmentIndex = -1;
    private bool mbDetectRope = false;
    private bool mbClimbing = false;
    private bool mbClimbOnce = false;
    private bool mbIsClimbUp = true;
    private float mGoalNormalizedTime = 0f;
    private float mDistanceFromJoint = 0f;
    private float mStretchedDistanceFromJoint = 0f;
    private bool mbDistanceLerped = false;
    private bool mbJumpAway = false;
    private bool mbSwing = false;

    private RopeVerlet.GrabPoint mLastGrabPoint_RH;
    private RopeVerlet.GrabPoint mLastGrabPoint_LH;
    private RopeVerlet.GrabPoint mLastGrabPoint_RT;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mbClimbing = true;
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);
        mController.Movement.SetVelocity(Vector3.zero);

        mGoalNormalizedTime = 0f;
        mDistanceFromJoint = _startDistanceFromJoint;

        StartCoroutine(eStartClimb());
    }

    public override void ExitState()
    {
        mbClimbing = false;
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);

        mRopeHandler.RemoveListenerOnAfterSimulateSegments(updatePos);

        mRopeHandler.EndClimb();
    }

    public override void Tick()
    {
        if (!mbClimbOnce)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
            Vector2 moveInput = mController.InputHandler.MoveInput;

            // if (Input.GetKey(KeyCode.U))
            if (mController.InputHandler.MoveInput.y > 0.1f)
            {
                if(!mRopeHandler.CouldClimbUp())
                {
                    return;
                }

                mbClimbOnce = true;
                mbIsClimbUp = true;
                mGoalNormalizedTime += 1f;
                mController.Animator.SetVertical(1f); // mAnimator.SetFloat(MultiplierHash, 1f);
                // mDeltaDistance = Vector3.zero;

                mLastGrabPoint_RH = mRopeHandler.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                mLastGrabPoint_LH = mRopeHandler.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                mLastGrabPoint_RT = mRopeHandler.GetGrabPoint(HumanBodyBones.RightToes);
                mRopeHandler.ClimbUpByJointIndex(); // mRopeHandler.AddJointIndex(-2);
                mDistanceFromJoint += _climbOnceDistance;
                mStretchedDistanceFromJoint = mDistanceFromJoint;
                // Debug.Log($"{mStretchedDistanceFromJoint}, normalizedTime: {animatorStateInfo.normalizedTime}");
            }

            //if (Input.GetKey(KeyCode.J))
            if (mController.InputHandler.MoveInput.y < -0.1f)
            {
                if(!mRopeHandler.CouldClimbDown())
                {
                    // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
                    mController.StateMachine.SwitchState<PlayerMoveState>();

                    return;
                }

                mbClimbOnce = true;
                mbIsClimbUp = false;
                mGoalNormalizedTime -= 1f;
                mController.Animator.SetVertical(-1f); // mAnimator.SetFloat(MultiplierHash, -1f);
                // mDeltaDistance = Vector3.zero;

                mLastGrabPoint_RH = mRopeHandler.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                mLastGrabPoint_LH = mRopeHandler.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                mLastGrabPoint_RT = mRopeHandler.GetGrabPoint(HumanBodyBones.RightToes);
                mRopeHandler.ClimbDownByJointIndex(); // mRopeHandler.AddJointIndex(2);
                mDistanceFromJoint -= _climbOnceDistance;
                mStretchedDistanceFromJoint = mDistanceFromJoint;
                // Debug.Log($"{mStretchedDistanceFromJoint}, normalizedTime: {animatorStateInfo.normalizedTime}");
            }

            // Jump
            if(mController.InputHandler.JumpPressed)
            {
                StartCoroutine(eJumpAway());

                // PlayerRunJumpState runJumpState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.RunJump) as PlayerRunJumpState;
                PlayerRunJumpState runJumpState = mController.StateMachine.GetStateBase<PlayerRunJumpState>();
                // runJumpState.jumpUpward = false;
                runJumpState.SetDefaultHeight(transform.position.y);

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
                mController.StateMachine.SwitchState<PlayerRunJumpState>();
                mController.InputHandler.ResetJump();

                mController.Movement.ForceJump();

                return;
            }

            // Swing
            if(!mbSwing)
            {
                if (moveInput.x < -.1f)
                {
                    mbSwing = true;
                    mRopeHandler.SwingLeft();
                }
                else if (moveInput.x > .1f)
                {
                    mbSwing = true;
                    mRopeHandler.SwingRight();
                }
            }
            else
            {
                if(moveInput.x > -.1f && moveInput.x < .1f)
                {
                    mbSwing = false;
                    mRopeHandler.StopSwing();
                }
            }
        }

        if (mbClimbOnce)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
            // Debug.Log(animatorStateInfo.normalizedTime);

            float normalizedTime = animatorStateInfo.normalizedTime - (int)animatorStateInfo.normalizedTime;

            if (animatorStateInfo.normalizedTime < 0f)
            {
                normalizedTime = 1f + normalizedTime;
            }

            if (mbIsClimbUp)
            {
                if (normalizedTime > .8f && normalizedTime < .95f)
                {
                    mbDistanceLerped = true;
                    // mDeltaDistance += mAnimator.deltaPosition;
                    float t = (normalizedTime - .8f) / .18f;
                    mDistanceFromJoint = Mathf.Lerp(mStretchedDistanceFromJoint, _startDistanceFromJoint, t);
                    // Debug.Log(normalizedTime - .8f);

                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = false;
                    mRopeHandler.SetGrabPoint(grabPoint);
                }
                else if (mbDistanceLerped && normalizedTime > .95f)
                {
                    mDistanceFromJoint = _startDistanceFromJoint;

                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RH.segmentIndex - 2;
                    mRopeHandler.SetGrabPoint(grabPoint);
                }
                else if (normalizedTime > .78f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_LH.segmentIndex - 2;
                    mRopeHandler.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (normalizedTime > .6f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = false;
                    mRopeHandler.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (normalizedTime > .4f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RT.segmentIndex - 2;
                    mRopeHandler.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (normalizedTime > .1f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = false;
                    // mRopeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
            }
            else
            {
                if (mbDistanceLerped && normalizedTime < .1f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RT.segmentIndex + 2;
                    mRopeHandler.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .4f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = false;
                    // mRopeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .6f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_LH.segmentIndex + 2;
                    mRopeHandler.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .78f)
                {
                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = false;
                    mRopeHandler.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .8f)
                {
                    mDistanceFromJoint = _startDistanceFromJoint;

                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RH.segmentIndex + 2;
                    mRopeHandler.SetGrabPoint(grabPoint);
                }
                else if (normalizedTime > .8f && normalizedTime < .95f)
                {
                    mbDistanceLerped = true;
                    // mDeltaDistance += mAnimator.deltaPosition;
                    float t = (normalizedTime - .8f) / .18f;
                    mDistanceFromJoint = Mathf.Lerp(mStretchedDistanceFromJoint, _startDistanceFromJoint, 1 - t);
                    // Debug.Log(normalizedTime - .8f);

                    RopeVerlet.GrabPoint grabPoint = mRopeHandler.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = false;
                    mRopeHandler.SetGrabPoint(grabPoint);
                }
            }


            if ((mbIsClimbUp && animatorStateInfo.normalizedTime > mGoalNormalizedTime)
                || (!mbIsClimbUp && animatorStateInfo.normalizedTime < mGoalNormalizedTime))
            {
                mbClimbOnce = false;
                mController.Animator.SetVertical(0f); // mAnimator.SetFloat(MultiplierHash, 0f);

                mbDistanceLerped = false;
                mDistanceFromJoint = _startDistanceFromJoint;
                // Debug.Log(mDeltaDistance);
            }
        }
    }

    public void NotifyRopeCollision(int segmentIndex, RopeHandler collidedRopeHandler)
    {
        if (mbDetectRope || mbJumpAway)
            return;

        mbDetectRope = true;
        mDetectedSegmentIndex = segmentIndex;
        mRopeHandler = collidedRopeHandler;
        onRopeCollision?.Invoke();
        // Debug.Log(segmentIndex);
    }

    private void LateUpdate()
    {
        if (mbJumpAway)
            return;

        if (mbClimbing)
            return;

        if(mController.StateMachine.CurrentState == PlayerStateMachine.EState.IdleJump
            || mController.StateMachine.CurrentState == PlayerStateMachine.EState.RunJump
            || mController.StateMachine.CurrentState == PlayerStateMachine.EState.Fall)
        {
            if (mbDetectRope)
            {
                if(_printDetectedSegmentIndex)
                    Debug.Log($"Rope Detected! Segment Index: {mDetectedSegmentIndex}");

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbRope);
                mController.StateMachine.SwitchState<PlayerRopeClimbState>();
            }
        }

        mbDetectRope = false;
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying == false)
            return;

        if (_showWireSphere)
        {
            Vector3 origin = mController.transform.position + Vector3.up * _offsetY;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, _detectionRadius);
        }
    }

    private void updatePos()
    {
        Transform trJointPoint = mRopeHandler.GetJointPointTransform();
        // Vector3 handPos = mAnimator.GetBoneTransform(HumanBodyBones.Chest).position;
        Vector3 handPos = _trRightHand.position;

        Vector3 dirToCenter = trJointPoint.up.normalized;
        Vector3 newForward = Vector3.Cross(transform.right, trJointPoint.up);
        Vector3 handTarget = trJointPoint.position - dirToCenter * mDistanceFromJoint;// + newForward.normalized * _startDistanceToForward;
        Vector3 toTarget = handTarget - handPos;

        transform.position += toTarget;
        transform.rotation = Quaternion.LookRotation(newForward, trJointPoint.up);
    }

    private IEnumerator eStartClimb()
    {
        while(true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("ClimbRope"))
                break;

            yield return null;
        }

        mRopeHandler.RemoveListenerOnAfterSimulateSegments(updatePos);
        mRopeHandler.AddListenerOnAfterSimulateSegments(updatePos);

        mRopeHandler.AddGrabPoint(indexFromJoint: 1, _trRightHand, HumanBodyBones.RightMiddleProximal);
        mRopeHandler.AddGrabPoint(indexFromJoint: 2, _trLeftHand, HumanBodyBones.LeftMiddleProximal);
        mRopeHandler.AddGrabPoint(indexFromJoint: 9, _trRightToe, HumanBodyBones.RightToes);
        mRopeHandler.StartClimb(mDetectedSegmentIndex + _jointIndexOffset);
    }

    private IEnumerator eJumpAway()
    {
        mbJumpAway = true;

        yield return new WaitUntil(() => mController.Movement.IsGrounded);

        mbJumpAway = false;
    }
}
