using UnityEngine;

public class PlayerTurnState : PlayerStateBase
{
    public enum ETurnType { None = 0, Idle, Run }

    public bool HasRemainingRotation => mbContinueRemainingRotation;
    public float ControllerStartNormalizedTime => turnPlayable.ControllerStartNormalizedTime;

    [SerializeField] private TurnPlayable turnPlayable;
    [SerializeField] private RotationHandler.EType _rotationType = RotationHandler.EType.Normal;
    [SerializeField] private float _idleTurnDuration = .6f;
    [SerializeField] private float _runTurnDuration = .4f;
    [SerializeField][Range(0f, 1f)] private float _idleTurnStartRunNormalizedTime = .666f;

    [Header("TurnAnimationData")]
    [SerializeField] private TurnAnimationData _turnAnimationData;

    [Header("TimeOffset")]
    [SerializeField] private float _idleTurnLTimeOffset = .615f;
    [SerializeField] private float _idleTurnRTimeOffset = .615f;
    [SerializeField] private float _runTurnLTimeOffset = .16f;
    [SerializeField] private float _runTurnRTimeOffset = .68f;

    [Header("Animation Curve")]
    [SerializeField] private AnimationCurve _idleTurnRotationCurve;
    [SerializeField] private AnimationCurve _idleTurnPositionCurve;
    [SerializeField] private AnimationCurve _runTurnRotationCurve;
    [SerializeField] private AnimationCurve _runTurnPositionCurve;

    // Rotation ��ü
    private AnimationCurveRotation mAnimationCurveRotation;
    private RootMotionRotation mRootMotionRotation;
    private Offset180Rotation mOffset180Rotation;
    private NormalRotation mNormalRotation;
    private PlayableRotation mPlayableRotation;

    private ETurnType mTurnType = ETurnType.None;
    private RotationHandler mRotationHandler = new RotationHandler();
    private TurnAnimationData.TurnAnimation mTurnAnimation;
    private Vector3 mStartEulerAngles;
    private Vector3 mTargetEulerAngles;
    private float mRemainAngle;
    private float mTurnTime;
    private bool mbContinueRemainingRotation;
    private bool mbIdleStopPlayback;
    private bool mbControllerIdlePrepared;
    private float mStopClipTime;
    private float mStopRotationDuration;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        initRotation();
        mRotationHandler.Init(mController);
        mRotationHandler.SetRotation(getRotation(_rotationType));
    }

    public override void EnterState()
    {
        mbContinueRemainingRotation = false;
        mbIdleStopPlayback = false;
        mbControllerIdlePrepared = false;

        changeDirection();
        mInputHandler.ResetMoveInput();
    }

    public override void ExitState()
    {
        if (!mbContinueRemainingRotation)
        {
            turnPlayable.ReleaseClipTime();
            mRotationHandler.SetTurnState(RotationHandler.EState.StandBy);
        }

        mAnimation.SetTurn(false);
    }

    public override void FixedTick()
    {
        if (_rotationType == RotationHandler.EType.Normal)
            advanceNormalRotation();
        else
            mRotationHandler.Rotate(mStartEulerAngles, mTargetEulerAngles, Time.fixedDeltaTime);
    }

    public override void Tick()
    {
        if (mInputHandler.JumpPressed)
        {
            mInputHandler.ResetJump();

            PlayerMovement.EDirection jumpDirection =
                PlayerMovement.MoveInputXToDirection(mInputHandler.MoveInput.x);

            if (jumpDirection == PlayerMovement.EDirection.Neutral)
                jumpDirection = mMovement.Direction;

            mStateMachine.SwitchState<PlayerRunJumpState>((state) => state.SetJumpDirection(jumpDirection));
            return;
        }

        if (!mMovement.IsGrounded)
        {
            mStateMachine.SwitchState<PlayerFallState>(
                (state) => state.SetFallType(PlayerFallState.EFallType.FromRun));
            return;
        }

        if (mController.CheckOppositeInputX())
        {
            mStateMachine.SwitchState<PlayerTurnState>((state) => state.SetTurnType(mTurnType));
            return;
        }

        bool hasInput = mInputHandler.GetInputRawMagnitude().x >= .1f;

        // IdleTurn 애니메이션이 멈췄을 때
        if (mbIdleStopPlayback)
        {
            if (hasInput)
            {
                // Retain the original rotation clock when resuming toward the same target.
                turnPlayable.ChangeClip(mTurnType, mRotationHandler.RotationDirection);
                turnPlayable.ResetClip(mTurnTime);
                turnPlayable.SetClipTime(mTurnTime);
                mbIdleStopPlayback = false;
                mbControllerIdlePrepared = false;
                turnPlayable.StartClip();
            }
            else
            {
                mMovement.Move(Vector2.zero);

                if (turnPlayable.NeedsControllerBlend)
                {
                    mAnimation.SetInputX(false);
                    mAnimation.SetInputXMagnitude(0f);

                    mbControllerIdlePrepared = TryPlayControllerBlend(AnimState.Idle, ControllerStartNormalizedTime);
                }

                if (turnPlayable.IsTurnPlaybackFinished && mRotationHandler.NormalizedTime >= 1f)
                {
                    mStateMachine.SwitchState<PlayerIdleState>((state) =>
                    {
                        if (mbControllerIdlePrepared)
                            state.EnterWithoutAnimation();
                    });
                }

                return;
            }
        }

        // 이동 인풋 해제 시
        if (!hasInput && _rotationType == RotationHandler.EType.Normal)
        {
            float rotationProgress = mTurnAnimation.GetRotationDuration() > 0f ? mRotationHandler.NormalizedTime : 1f;

            if (mTurnType == ETurnType.Idle && mTurnTime < mTurnAnimation.startRunTime &&
                turnPlayable.TryStartIdleTurnToIdle(
                    mRotationHandler.RotationDirection,
                    rotationProgress,
                    out mStopRotationDuration))
            {
                mbIdleStopPlayback = true;
                mStopClipTime = rotationProgress * mStopRotationDuration;
                turnPlayable.SetClipTime(mStopClipTime);
                mMovement.Move(Vector2.zero);
                return;
            }

            PrepareRemainingRotationTransfer();

            mStateMachine.SwitchState<PlayerRunToIdleState>();
            return;
        }

        if (turnPlayable.NeedsControllerBlend || turnPlayable.IsTurnExitStarted)
        {
            PrepareRemainingRotationTransfer();
            mStateMachine.SwitchState<PlayerMoveState>();
            return;
        }

        // mMovement.Move(mMovement.DirectionToVector());
        mMovement.Move(mInputHandler.MoveInput);
        mRotationHandler.UpdateTurnState();
        mAnimation.SetInputXMagnitude(mInputHandler.GetInputRawMagnitude().x);
    }

    /// <summary>
    /// 남은 회전이 있는지 체크
    /// </summary>
    public void PrepareRemainingRotationTransfer()
    {
        mbContinueRemainingRotation = _rotationType == RotationHandler.EType.Normal &&
            mRotationHandler.NormalizedTime < 1f;
    }

    public void InterruptTurn()
    {
        CancelRemainingRotation();
        turnPlayable.ReleaseClipTime();
        mRotationHandler.SetTurnState(RotationHandler.EState.StandBy);
    }

    public bool TryPlayControllerBlend(AnimState state, float normalizedTime = 0f)
    {
        if (!turnPlayable.HasTurnOutput)
            return false;

        if (!mAnimation.PlayForControllerBlend(turnPlayable.ControllerPlayable, state, normalizedTime))
            mAnimation.Play(state);

        turnPlayable.BeginControllerBlend();

        return true;
    }

    public void LogControllerPlayable()
    {
        var currentStateInfo = turnPlayable.ControllerPlayable.GetCurrentAnimatorStateInfo(0);
        var currentState = AnimStateNameLookUp.names[currentStateInfo.fullPathHash];
        var nextStateInfo = turnPlayable.ControllerPlayable.GetNextAnimatorStateInfo(0);
        var nextState = nextStateInfo.fullPathHash == 0 ? "" : AnimStateNameLookUp.names[nextStateInfo.fullPathHash];
        GameDebug.Log($"Enter RunToIdleState Playable - Current State: {currentState}, Next State: {nextState}, normalizedTime: {currentStateInfo.normalizedTime}",
        tag: "Enter RunToIdleState");
    }

    public void BeginControllerBlend()
    {
        turnPlayable.BeginControllerBlend();
    }

    public void ContinueRemainingRotation()
    {
        if (mbContinueRemainingRotation && !advanceNormalRotation())
        {
            mbContinueRemainingRotation = false;
            turnPlayable.ReleaseClipTime();
        }
    }

    public void CancelRemainingRotation()
    {
        if (!mbContinueRemainingRotation)
            return;

        mbContinueRemainingRotation = false;
        turnPlayable.ReleaseClipTime();
        mRotationHandler.SetTurnState(RotationHandler.EState.StandBy);
    }

    public void SetTurnType(ETurnType type)
    {
        mTurnType = type;
        mRotationHandler.SetTurnType(type);
        mAnimation.SetIndex(type == ETurnType.Idle ? 0 : 1);
    }

    public void StopStanbyRotation()
    {
        mNormalRotation.StopStanbyRotation();
    }

    private bool advanceNormalRotation()
    {
        float duration = mTurnAnimation.GetRotationDuration();

        if (mbIdleStopPlayback)
        {
            float rotationStep = Mathf.Min(Time.fixedDeltaTime, Mathf.Max(0f, duration - mTurnTime));
            mStopClipTime += (duration > 0f ? rotationStep * mStopRotationDuration / duration : 0f) +
                Time.fixedDeltaTime - rotationStep;
        }

        mTurnTime += Time.fixedDeltaTime;
        turnPlayable.SetClipTime(mbIdleStopPlayback ? mStopClipTime : mTurnTime);

        float normalizedTime = duration > 0f ? mTurnTime / duration : 1f;
        return mRotationHandler.RotateAtNormalizedTime(mStartEulerAngles, mTargetEulerAngles, normalizedTime);
    }

    private void initRotation()
    {
        mAnimationCurveRotation = new AnimationCurveRotation(mRotationHandler, mController);
        mAnimationCurveRotation.SetAnimationCurve(
            _idleTurnPositionCurve,
            _idleTurnRotationCurve,
            _runTurnPositionCurve,
            _runTurnRotationCurve);

        mRootMotionRotation = new RootMotionRotation(mRotationHandler, mController);
        mOffset180Rotation = new Offset180Rotation(mRotationHandler, mController);
        mNormalRotation = new NormalRotation(mRotationHandler, mController);
        mNormalRotation.SetTurnAnimData(_turnAnimationData);

        mPlayableRotation = new PlayableRotation(mRotationHandler, mController, turnPlayable);
    }

    private RotationBase getRotation(RotationHandler.EType type)
    {
        RotationBase rotation = null;

        switch (type)
        {
            case RotationHandler.EType.AnimationCurve:
                rotation = mAnimationCurveRotation;
                break;
            case RotationHandler.EType.RootMotion:
                rotation = mRootMotionRotation;
                break;
            case RotationHandler.EType.Offset180:
                rotation = mOffset180Rotation;
                break;
            case RotationHandler.EType.Normal:
                rotation = mNormalRotation;
                break;
            case RotationHandler.EType.Playable:
                rotation = mPlayableRotation;
                break;
        }

        return rotation;
    }

    private void changeDirection()
    {
        mStartEulerAngles = getCurrentEulerAngles();
        mController.Movement.SetDirection(mController.Movement.OppositeDirection);

        mTargetEulerAngles = getCurrentEulerAngles();
        mRemainAngle = getRemainAngle(mTargetEulerAngles.y);

        if(mRemainAngle < 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Left);

            turnPlayable.ChangeClip(mTurnType, RotationHandler.ERotationDirection.Left);
        }
        else if(mRemainAngle > 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Right);

            turnPlayable.ChangeClip(mTurnType, RotationHandler.ERotationDirection.Right);
        }

        float normalizedTime = Mathf.Clamp01(1f - Mathf.Abs(mRemainAngle) / Number.DEG_180);
        mRotationHandler.SetNormalizedTime(normalizedTime);

        mTurnAnimation = getTurnAnimation(mTurnType, mRotationHandler.RotationDirection);
        float startTime = mRotationHandler.NormalizedTime * mTurnAnimation.GetRotationDuration();
        mTurnTime = startTime;

        turnPlayable.ResetClip(startTime);

        if (_rotationType == RotationHandler.EType.Normal)
            turnPlayable.SetClipTime(mTurnTime);

        turnPlayable.StartClip();

        GameDebug.Log($"Turn Type: {mTurnType}, Rotation Direction: {mRotationHandler.RotationDirection}, Remain Angle: {mRemainAngle}, NormalizedTime: {normalizedTime}, Rotation Direction: {mRotationHandler.RotationDirection}",
            tag: "Change Direction");
    }

    private TurnAnimationData.TurnAnimation getTurnAnimation(ETurnType type, RotationHandler.ERotationDirection direction)
    {
        return _turnAnimationData.Get(type, direction);
    }

    private Vector3 getCurrentEulerAngles()
    {
        return mMovement.DirectionToRotation(mMovement.Direction).eulerAngles;
    }

    private float getRemainAngle(float targetAngle)
    {
        return Mathf.DeltaAngle(mMovement.Rotation.eulerAngles.y, targetAngle);
    }
}
