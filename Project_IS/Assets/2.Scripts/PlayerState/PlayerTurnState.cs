using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTurnState : PlayerStateBase
{
    public enum ETurnType { None = 0, Idle, Run }

    [SerializeField] private RotationHandler.EType _rotationType = RotationHandler.EType.Normal;
    [SerializeField] private float _idleTurnDuration = .6f;
    [SerializeField] private float _runTurnDuration = .4f;

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

    private ETurnType mTurnType = ETurnType.None;
    private RotationHandler mRotationHandler = new RotationHandler();
    private bool mbEnableTimer = false;
    private float mTimer = 0f;
    private float mTurnDuration = 0f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mRotationHandler.Init(mController);
        mRotationHandler.SetType(_rotationType);
        mRotationHandler.AnimationCurveRotation.SetAnimationCurve(_idleTurnPositionCurve, _idleTurnRotationCurve, _runTurnPositionCurve, _runTurnRotationCurve);
    }

    public override void EnterState()
    {
        mbEnableTimer = false;

        mRotationHandler.SetTurnState(RotationHandler.EState.DirectionChanged);

        mController.InputHandler.ResetMoveInput();
        mController.Animator.SetTurn(true);

        switch(mTurnType)
        {
            case ETurnType.Idle:
                mTurnDuration = _idleTurnDuration;
                break;
            case ETurnType.Run:
                mTurnDuration = _runTurnDuration;
                break;
            default:
                mTurnDuration = 0f;
                break;
        }

        mTimer = 0f;

        mController.Animator.onEndTransition -= startTimer;
        mController.Animator.onEndTransition += startTimer;

        //mController.Animator.onAnimatorStateChanged -= startTimer;
        //mController.Animator.onAnimatorStateChanged += startTimer;
    }

    public override void ExitState()
    {
        mbEnableTimer = false;

        mRotationHandler.SetTurnState(RotationHandler.EState.StandBy);

        mController.Animator.SetTurn(false);

        mController.Animator.onEndTransition -= startTimer;

        // mController.Animator.onAnimatorStateChanged -= startTimer;
    }

    public override void FixedTick()
    {
        mRotationHandler.FixedUpdate();

        if(mController.InputHandler.GetInputRawMagnitude().x < .1f)
        {
            // To RunToIdle
            mController.StateMachine.SwitchState<PlayerRunToIdleState>();
            return;
        }

        var currentStateInfo = mController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        // if(mTurnType == ETurnType.Run && mTimer > mTurnDuration)
        if(mTimer > mTurnDuration || Mathf.Approximately(mTimer, mTurnDuration))
        {
            // To Move
            if (mController.InputHandler.GetInputRawMagnitude().x > .1f)
            {
                // GameDebug.LogAndPause($"To Move - timer/duration: {mTimer.ToString("G9")}/{mTurnDuration}, {mTimer.CompareTo(mTurnDuration)}, normalized time: {currentStateInfo.normalizedTime}", GameDebug.LogCategory.State, GameDebug.LogLevel.Info);
                switchToMoveState();
                return;
            }

            // To RunToIdle
            mController.StateMachine.SwitchState<PlayerRunToIdleState>();
            return;
        }

        GameDebug.Log($"timer/duration: {mTimer.ToString("G9")}/{mTurnDuration}, {mTimer.CompareTo(mTurnDuration)}, normalized time: {currentStateInfo.normalizedTime}", 
            category: GameDebug.LogCategory.State, level: GameDebug.LogLevel.Verbose);

        if(mbEnableTimer)
            mTimer += Time.fixedDeltaTime;
    }

    public override void Tick()
    {
        // To Jump
        if(mController.InputHandler.JumpPressed)
        {
            mController.InputHandler.ResetJump();
            mController.StateMachine.SwitchState<PlayerRunJumpState>();
            return;
        }

        // To Fall
        if(!mController.Movement.IsGrounded)
        {
            mController.StateMachine.SwitchState<PlayerFallState>((fallState) =>
            {
                fallState.SetFallType(PlayerFallState.EFallType.FromRun);
            });
            return;
        }

        //// Rotation End
        //if (mTurnType == ETurnType.Idle && mRotationHandler.State == RotationHandler.EState.StandBy)
        //{
        //    // To Move
        //    if (mController.InputHandler.GetInputRawMagnitude().x > .1f)
        //    {
        //        switchToMoveState();
        //        return;
        //    }

        //    // To RunToIdle
        //    mController.StateMachine.SwitchState<PlayerRunToIdleState>();
        //    return;
        //}

        mController.Movement.Move(mController.InputHandler.MoveInput);
        mRotationHandler.UpdateTurnState();
        mController.Animator.SetInputXMagnitude(mController.InputHandler.GetInputRawMagnitude().x);

        // To Turn
        if (mController.CheckOppositeInputX())
        {
            mController.StateMachine.SwitchState<PlayerTurnState>((turnType) =>
            {
                turnType.SetTurnType(mTurnType);
            });

            return;
        }
    }

    public override void Standby()
    {
        mRotationHandler.Standby();
    }

    public void SetTurnType(ETurnType type)
    {
        mTurnType = type;

        mRotationHandler.SetTurnType(mTurnType);
    }

    private void switchToMoveState()
    {
        mController.StateMachine.SwitchState<PlayerMoveState>();
    }

    private void startTimer()
    {
        mbEnableTimer = true;

        var currentStateInfo = mController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        mTimer = mTurnDuration * currentStateInfo.normalizedTime;

        GameDebug.Log($"Turn Timer(G9): {mTimer.ToString("G9")}, Turn Animator Normalized Time: {currentStateInfo.normalizedTime}",
                    category: GameDebug.LogCategory.State);
    }

    private void startTimer(bool selfTransition)
    {
        mbEnableTimer = true;

        var currentStateInfo = mController.Animator.Animator.GetCurrentAnimatorStateInfo(0);

        mTimer = mTurnDuration * currentStateInfo.normalizedTime;

        GameDebug.Log($"Turn Timer(G9): {mTimer.ToString("G9")}, Turn Animator Normalized Time: {currentStateInfo.normalizedTime}", 
            category: GameDebug.LogCategory.State);
        // GameDebug.Log($"TurnState.onEndTransition", GameDebug.LogCategory.State, GameDebug.LogLevel.Info);
    }
}
