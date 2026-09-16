using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTurnState : PlayerStateBase
{
    public enum ETurnType { None = 0, Idle, Run }

    [SerializeField] private TurnPlayable turnPlayable;
    [SerializeField] private RotationHandler.EType _rotationType = RotationHandler.EType.Normal;
    [SerializeField] private float _idleTurnDuration = .6f;
    [SerializeField] private float _runTurnDuration = .4f;
    [SerializeField][Range(0f, 1f)] private float _idleTurnStartRunNormalizedTime = .666f;

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
    private bool mbTurnToIdle = false;
    private float mFixedDeltaTime = 0f;

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
        mbTurnToIdle = false;
        mFixedDeltaTime = 0f;

        mRotationHandler.SetTurnState(RotationHandler.EState.DirectionChanged);

        mController.InputHandler.ResetMoveInput();
        mController.Animation.SetTurn(true);

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

        mController.Animation.onEndTransition -= startTimer;
        mController.Animation.onEndTransition += startTimer;

        //mController.Animator.onAnimatorStateChanged -= startTimer;
        //mController.Animator.onAnimatorStateChanged += startTimer;
    }

    public override void ExitState()
    {
        mbEnableTimer = false;

        mRotationHandler.SetTurnState(RotationHandler.EState.StandBy);

        mController.Animation.SetTurn(false);

        mController.Animation.onEndTransition -= startTimer;

        // mController.Animator.onAnimatorStateChanged -= startTimer;
    }

    public override void FixedTick()
    {
        mRotationHandler.FixedUpdate();

        float rotationNormalizedTime = mRotationHandler.GetRotationBase<NormalRotation>().NormalizedTime;// - .12f;
        float animationRotationLength = 2f / 3f;

        if (!mbTurnToIdle && mController.InputHandler.GetInputRawMagnitude().x < .1f)
        {
            mbTurnToIdle = true;

            if(mTurnType == ETurnType.Idle)
            {
                if (mTimer > mTurnDuration * _idleTurnStartRunNormalizedTime)
                {
                    // To RunToIdle
                    mController.StateMachine.SwitchState<PlayerRunToIdleState>();
                }
                else
                {
                    // mAnimation.Play(AnimState.Idle_Turn_To_Idle_R, true, .25f, mTimer / mTurnDuration * 2f / 3f);
                    AnimState animState = (mRotationHandler.RotationDirection == RotationHandler.ERotationDirection.Right) ? AnimState.Idle_Turn_To_Idle_R : AnimState.Idle_Turn_To_Idle_L;
                    mAnimation.Play(animState, true, .25f, rotationNormalizedTime * animationRotationLength * .75f);
                    mAnimation.SetMotionTime(rotationNormalizedTime * animationRotationLength);
                    GameDebug.Log($"Rotation Normalized Time: {rotationNormalizedTime}, Animation Rotation Length: {animationRotationLength}, Animation Offset: {rotationNormalizedTime * animationRotationLength}", tag: "Idle Turn To Idle");
                }
            }
            else
            {
                var moveInput = mInputHandler.MoveInput;
                moveInput.x = PlayerMovement.DirectionToVector(mMovement.Direction).x * .8f;
                mInputHandler.SetMoveInput(moveInput);

                // To RunToIdle
                mController.StateMachine.SwitchState<PlayerRunToIdleState>();
            }

            //if(mTimer < .1f)
            //{
            //    mAnimation.Play(AnimState.Idle_Turn_To_Idle_R);
            //}
            //else
            //{
            //    var moveInput = mInputHandler.MoveInput;
            //    moveInput.x = PlayerMovement.DirectionToVector(mMovement.Direction).x * .8f;
            //    mInputHandler.SetMoveInput(moveInput);

            //    // To RunToIdle
            //    mController.StateMachine.SwitchState<PlayerRunToIdleState>();
            //}

            return;
        }

        var currentStateInfo = mController.Animation.Animator.GetCurrentAnimatorStateInfo(0);

        float motionTime = 0f;

        if(mbTurnToIdle)
        {
            if (rotationNormalizedTime < 1f)
            {
                motionTime = rotationNormalizedTime * animationRotationLength;
                mAnimation.SetMotionTime(motionTime);
            }
            else
            {
                float deltaMotionTime = mFixedDeltaTime / mTurnDuration;
                motionTime = animationRotationLength + deltaMotionTime;
                mAnimation.SetMotionTime(motionTime);
                mFixedDeltaTime += Time.fixedDeltaTime;
            }
        }

        string currentAnimName = AnimStateNameLookUp.names[currentStateInfo.fullPathHash];
        GameDebug.Log($"Current Animation Name: {currentAnimName}, Anim Normalized Time: {currentStateInfo.normalizedTime}, Animation Rotation: {motionTime}", //{currentStateInfo.normalizedTime / (2f / 3f)}",
                tag: "Turn Normalized Time");
        var nextStateInfo = mController.Animation.Animator.GetNextAnimatorStateInfo(0);
        if (nextStateInfo.fullPathHash != 0)
        {
            string nextAnimName = AnimStateNameLookUp.names[nextStateInfo.fullPathHash];
            GameDebug.Log($"Next Animation Name: {nextAnimName}, Anim Normalized Time: {nextStateInfo.normalizedTime}, Animation Rotation: {motionTime}",
                    tag: "Turn Normalized Time");
        }

        // if(mTurnType == ETurnType.Run && mTimer > mTurnDuration)
        if (mTimer > mTurnDuration || Mathf.Approximately(mTimer, mTurnDuration))
        {
            if (mbTurnToIdle)
            {
                mController.StateMachine.SwitchState<PlayerIdleState>();
                return;
            }

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

        //GameDebug.Log($"timer/duration: {mTimer.ToString("G9")}/{mTurnDuration}, {mTimer.CompareTo(mTurnDuration)}, normalized time: {currentStateInfo.normalizedTime}", 
        //    tag: "", category: GameDebug.LogCategory.State, level: GameDebug.LogLevel.Verbose);

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
        mController.Animation.SetInputXMagnitude(mController.InputHandler.GetInputRawMagnitude().x);

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
        // mRotationHandler.Standby();
    }

    public void SetTurnType(ETurnType type)
    {
        mTurnType = type;

        mRotationHandler.SetTurnType(mTurnType);
    }

    public void StopStanbyRotation()
    {
        mRotationHandler.GetRotationBase<NormalRotation>().StopStanbyRotation();
    }

    private void switchToMoveState()
    {
        mController.StateMachine.SwitchState<PlayerMoveState>();
    }

    private void startTimer()
    {
        mbEnableTimer = true;

        var currentStateInfo = mController.Animation.Animator.GetCurrentAnimatorStateInfo(0);

        mTimer = mTurnDuration * currentStateInfo.normalizedTime;

        GameDebug.Log($"Turn Timer(G9): {mTimer.ToString("G9")}, Turn Animator Normalized Time: {currentStateInfo.normalizedTime}",
                    category: GameDebug.LogCategory.State);
    }

    private void startTimer(bool selfTransition)
    {
        mbEnableTimer = true;

        var currentStateInfo = mController.Animation.Animator.GetCurrentAnimatorStateInfo(0);

        mTimer = mTurnDuration * currentStateInfo.normalizedTime;

        GameDebug.Log($"Turn Timer(G9): {mTimer.ToString("G9")}, Turn Animator Normalized Time: {currentStateInfo.normalizedTime}", 
            category: GameDebug.LogCategory.State);
        // GameDebug.Log($"TurnState.onEndTransition", GameDebug.LogCategory.State, GameDebug.LogLevel.Info);
    }
}
