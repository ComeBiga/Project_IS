using UnityEngine;
using UnityEngine.Serialization;

public class PlayerTurnState : PlayerStateBase
{
    public enum ETurnType { None = 0, Idle, Run }

    public bool HasRemainingRotation => mbContinueRemainingRotation;
    public float ControllerStartNormalizedTime => _turnPlayable.ControllerStartNormalizedTime;

    [FormerlySerializedAs("turnPlayable")]
    [SerializeField] private TurnPlayable _turnPlayable;
    [SerializeField] private RotationHandler.EType _rotationType = RotationHandler.EType.Normal;

    [Header("TurnAnimationData")]
    [SerializeField] private TurnAnimationData _turnAnimationData;

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

    private ETurnType mTurnType = ETurnType.None;
    private RotationHandler mRotationHandler = new RotationHandler();
    private TurnAnimationData.TurnAnimation mTurnAnimation;
    private Vector3 mStartEulerAngles;
    private Vector3 mTargetEulerAngles;
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
            _turnPlayable.ReleaseClipTime();
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
                _turnPlayable.ChangeClip(mTurnType, mRotationHandler.RotationDirection);
                _turnPlayable.ResetClip(mTurnTime);
                _turnPlayable.SetClipTime(mTurnTime);
                mbIdleStopPlayback = false;
                mbControllerIdlePrepared = false;
                _turnPlayable.StartClip();
            }
            else
            {
                mMovement.Move(Vector2.zero);

                if (_turnPlayable.NeedsControllerBlend)
                {
                    mAnimation.SetInputX(false);
                    mAnimation.SetInputXMagnitude(0f);

                    mbControllerIdlePrepared = TryPlayControllerBlend(AnimState.Idle, ControllerStartNormalizedTime);
                }

                if (_turnPlayable.IsTurnPlaybackFinished && mRotationHandler.NormalizedTime >= 1f)
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
                _turnPlayable.TryStartIdleTurnToIdle(
                    mRotationHandler.RotationDirection,
                    rotationProgress,
                    out mStopRotationDuration))
            {
                mbIdleStopPlayback = true;
                mStopClipTime = rotationProgress * mStopRotationDuration;
                _turnPlayable.SetClipTime(mStopClipTime);
                mMovement.Move(Vector2.zero);
                return;
            }

            PrepareRemainingRotationTransfer();

            mStateMachine.SwitchState<PlayerRunToIdleState>();
            return;
        }

        if (_turnPlayable.NeedsControllerBlend || _turnPlayable.IsTurnExitStarted)
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
        _turnPlayable.ReleaseClipTime();
        mRotationHandler.SetTurnState(RotationHandler.EState.StandBy);
    }

    public bool TryPlayControllerBlend(AnimState state, float normalizedTime = 0f)
    {
        if (!_turnPlayable.HasTurnOutput)
            return false;

        if (!mAnimation.PlayForControllerBlend(_turnPlayable.ControllerPlayable, state, normalizedTime))
            mAnimation.Play(state);

        _turnPlayable.BeginControllerBlend();

        return true;
    }

    public void BeginControllerBlend()
    {
        _turnPlayable.BeginControllerBlend();
    }

    public void ContinueRemainingRotation()
    {
        if (mbContinueRemainingRotation && !advanceNormalRotation())
        {
            mbContinueRemainingRotation = false;
            _turnPlayable.ReleaseClipTime();
        }
    }

    public void CancelRemainingRotation()
    {
        if (!mbContinueRemainingRotation)
            return;

        mbContinueRemainingRotation = false;
        _turnPlayable.ReleaseClipTime();
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
        _turnPlayable.SetClipTime(mbIdleStopPlayback ? mStopClipTime : mTurnTime);

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
        }

        return rotation;
    }

    private void changeDirection()
    {
        mStartEulerAngles = getCurrentDirectionEulerAngles();
        mController.Movement.SetDirection(mController.Movement.OppositeDirection);

        mTargetEulerAngles = getCurrentDirectionEulerAngles();
        float remainAngle = getRemainAngle(mTargetEulerAngles.y);

        if(remainAngle < 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Left);

            _turnPlayable.ChangeClip(mTurnType, RotationHandler.ERotationDirection.Left);
        }
        else if(remainAngle > 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Right);

            _turnPlayable.ChangeClip(mTurnType, RotationHandler.ERotationDirection.Right);
        }

        float normalizedTime = Mathf.Clamp01(1f - Mathf.Abs(remainAngle) / Number.DEG_180);
        mRotationHandler.SetNormalizedTime(normalizedTime);

        mTurnAnimation = getTurnAnimation(mTurnType, mRotationHandler.RotationDirection);
        float startTime = mRotationHandler.NormalizedTime * mTurnAnimation.GetRotationDuration();
        mTurnTime = startTime;

        _turnPlayable.ResetClip(startTime);

        if (_rotationType == RotationHandler.EType.Normal)
            _turnPlayable.SetClipTime(mTurnTime);

        _turnPlayable.StartClip();

        GameDebug.Log($"Turn Type: {mTurnType}, Rotation Direction: {mRotationHandler.RotationDirection}, Remain Angle: {remainAngle}, NormalizedTime: {normalizedTime}",
            tag: "Change Direction");
    }

    private TurnAnimationData.TurnAnimation getTurnAnimation(ETurnType type, RotationHandler.ERotationDirection direction)
    {
        return _turnAnimationData.Get(type, direction);
    }

    private Vector3 getCurrentDirectionEulerAngles()
    {
        return mMovement.DirectionToRotation(mMovement.Direction).eulerAngles;
    }

    private float getRemainAngle(float targetAngle)
    {
        return Mathf.DeltaAngle(mMovement.Rotation.eulerAngles.y, targetAngle);
    }
}
