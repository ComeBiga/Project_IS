using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using static PlayerTurnState;

public class PlayerTurnState : PlayerStateBase
{
    public enum ETurnType { None = 0, Idle, Run }

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

    // Rotation 객체
    private AnimationCurveRotation mAnimationCurveRotation;
    private RootMotionRotation mRootMotionRotation;
    private Offset180Rotation mOffset180Rotation;
    private NormalRotation mNormalRotation;
    private PlayableRotation mPlayableRotation;

    private ETurnType mTurnType = ETurnType.None;
    private RotationHandler mRotationHandler = new RotationHandler();
    private bool mbEnableTimer = false;
    /// <summary>
    /// 이전 AnimatorState에서 TurnState로 트랜지션이 끝난 시점부터의 시간
    /// </summary>
    private float mTimerFromTransitionEnd = 0f;
    private TurnAnimationData mTurnAnimData;
    private TurnAnimationData.TurnAnimation mTurnAnimation;
    private float mTurnDuration = 0f;
    private bool mbTurnToIdle = false;
    private float mFixedDeltaTime = 0f;

    private Vector3 mStartEulerAngles;
    private Vector3 mTargetEulerAngles;
    private float mRemainAngle;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        initRotation();

        mRotationHandler.Init(mController);
        // mRotationHandler.InitPlayableRotation(turnPlayable);
        // mRotationHandler.SetType(_rotationType);
        mRotationHandler.SetRotation(getRotation(_rotationType));
        // mRotationHandler.AnimationCurveRotation.SetAnimationCurve(_idleTurnPositionCurve, _idleTurnRotationCurve, _runTurnPositionCurve, _runTurnRotationCurve);

    }

    public override void EnterState()
    {
        mbEnableTimer = false;
        mbTurnToIdle = false;
        mFixedDeltaTime = 0f;

        // mRotationHandler.SetTurnState(RotationHandler.EState.DirectionChanged);
        // mRotationHandler.ChangeDirection();
        changeDirection();

        mController.InputHandler.ResetMoveInput();

        //switch(mTurnType)
        //{
        //    case ETurnType.Idle:
        //        mTurnAnimData = _turnAnimationData;
        //        // mTurnDuration = _idleTurnDuration;
        //        mTurnDuration = mTurnAnimData.sourceClip.length;
        //        break;
        //    case ETurnType.Run:
        //        mTurnAnimData = _turnAnimationData;
        //        //mTurnDuration = _runTurnDuration;
        //        mTurnDuration = mTurnAnimData.sourceClip.length;
        //        break;
        //    default:
        //        mTurnDuration = 0f;
        //        break;
        //}

        //mTurnAnimation = _turnAnimationData.Get(mTurnType, mRotationHandler.RotationDirection);
        mTimerFromTransitionEnd = 0f;

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
        // mRotationHandler.FixedUpdate();
        // return;

        bool bRotating = mRotationHandler.Rotate(mStartEulerAngles, mTargetEulerAngles, Time.fixedDeltaTime);

        // Turn Animation에서 Master가 회전하는 동안에 대한 normalizedTime
        // float rotationNormalizedTime = mRotationHandler.GetRotationBase<NormalRotation>().NormalizedTime;// - .12f;
        float rotationNormalizedTime = mRotationHandler.NormalizedTime;// - .12f;

        // IdleTurn To Idle 애니메이션에서 Master가 회전하는 시간
        float idleTurnToIdleAnimRotationLength = 2f / 3f;

        // Turn 중에 Input X가 0이 되면 Idle로 전환
        if (false && !mbTurnToIdle && mController.InputHandler.GetInputRawMagnitude().x < .1f)
        {
            mbTurnToIdle = true;

            if(mTurnType == ETurnType.Idle)
            {
                // Run 동작으로 연결됐을 때 RunToIdle로 전환
                // if (mTimer > mTurnDuration * _idleTurnStartRunNormalizedTime)
                // if (mTimerFromTransitionEnd > mTurnAnimation.GetDuration() * _idleTurnStartRunNormalizedTime)
                if(!bRotating)
                {
                    // To RunToIdle
                    mController.StateMachine.SwitchState<PlayerRunToIdleState>();
                }
                // IdleTurn To Idle로 전환
                else
                {
                    // mAnimation.Play(AnimState.Idle_Turn_To_Idle_R, true, .25f, mTimer / mTurnDuration * 2f / 3f);
                    AnimState animState = (mRotationHandler.RotationDirection == RotationHandler.ERotationDirection.Right) ? AnimState.Idle_Turn_To_Idle_R : AnimState.Idle_Turn_To_Idle_L;
                    mAnimation.Play(animState, true, .25f, rotationNormalizedTime * idleTurnToIdleAnimRotationLength * .75f);

                    // 0 ~ 0.6667, IdleTurn To Idle 애니메이션의 회전 구간에 맞게 매핑
                    float motionTime_IdleTurnToIdle = rotationNormalizedTime * idleTurnToIdleAnimRotationLength;
                    mAnimation.SetMotionTime(motionTime_IdleTurnToIdle);
                    GameDebug.Log($"Rotation Normalized Time: {rotationNormalizedTime}, Animation Rotation Length: {idleTurnToIdleAnimRotationLength}, Animation Offset: {rotationNormalizedTime * idleTurnToIdleAnimRotationLength}", tag: "Idle Turn To Idle");
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

        // Turn To Idle 중일 때
        if(mbTurnToIdle)
        {
            // Master가 회전하는 동안
            if (rotationNormalizedTime < 1f)
            {
                motionTime = rotationNormalizedTime * idleTurnToIdleAnimRotationLength;
                mAnimation.SetMotionTime(motionTime);
            }
            // Master가 회전을 끝낸 후
            else
            {
                //float deltaMotionTime = mFixedDeltaTime / mTurnDuration;
                float deltaMotionTime = mFixedDeltaTime / mTurnAnimation.GetDuration();
                motionTime = idleTurnToIdleAnimRotationLength + deltaMotionTime;
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
        // if (mTimer > mTurnDuration || Mathf.Approximately(mTimer, mTurnDuration))
        // if (mTimer > mTurnAnimation.GetDuration() || Mathf.Approximately(mTimer, mTurnAnimation.GetDuration()))
        if (false && mTimerFromTransitionEnd > mTurnAnimation.sourceClip.length || Mathf.Approximately(mTimerFromTransitionEnd, mTurnAnimation.GetDuration()))
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
            mTimerFromTransitionEnd += Time.fixedDeltaTime;
    }

    public override void Tick()
    {
        // turnPlayable.Evaluate(mRotationHandler.NormalizedTime, Time.deltaTime);

        //turnPlayable.BlendToTurn();
        //turnPlayable.BlendToAnimatorController();

        //if(turnPlayable.GetTurnClipTime() > turnPlayable.BlendDuration)
        //{
        //    GameDebug.Log($"TurnPlayable Turn Clip Time: {turnPlayable.GetTurnClipTime()}, Bleding Time: {turnPlayable.BlendDuration}", 
        //        tag: "End Turn Blending");
        //    turnPlayable.SetControllerState(AnimStateHash.stateHashes[AnimState.Run], 0, .75f);
        //    return;
        //}

        // if (turnPlayable.GetTurnClipTime() > turnPlayable.GetTurnClipLength() - turnPlayable.BlendDuration)
        if(turnPlayable.ClipState == TurnPlayable.EClipState.BlendOut)
        {
            //GameDebug.Log($"TurnPlayable Turn Clip Time: {turnPlayable.GetTurnClipTime()}, Bleding Time: {turnPlayable.GetTurnClipLength() - turnPlayable.BlendDuration}", 
            //    tag: "Into Controller Blending");

            // turnPlayable.SetControllerState(AnimStateHash.stateHashes[AnimState.Run], 0, .75f);
            switchToMoveState();
            return;
        }
        // if(turnPlayable.StartExit)
            // mController.Movement.Move(mController.InputHandler.MoveInput);

        //mRotationHandler.UpdateTurnState();
        //return;

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


        mController.Movement.Move(mMovement.DirectionToVector());
        // mController.Movement.Move(mController.InputHandler.MoveInput);
        mRotationHandler.UpdateTurnState();
        mController.Animation.SetInputXMagnitude(mController.InputHandler.GetInputRawMagnitude().x);

        // To Turn
        if (mController.CheckOppositeInputX())
        {
            //float time = (float)turnPlayable.getTurnClipPlayable().GetTime();
            //GameDebug.Log($"CheckOppositeInput - clipTime: {time}");
            //UnityEditor.EditorApplication.isPaused = true;
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

        // TurnAnimationData turnAnimData = _turnAnimationData;

        //switch(type)
        //{
        //    case ETurnType.Idle:
        //        turnAnimData = _turnAnimationData;
        //        break;
        //    case ETurnType.Run:
        //        turnAnimData = _turnAnimationData;
        //        break;
        //    default:
        //        turnAnimData = _turnAnimationData;
        //        break;
        //}

        mRotationHandler.SetTurnType(mTurnType); //, turnAnimData);

        mAnimation.SetIndex((mTurnType == PlayerTurnState.ETurnType.Idle) ? 0 : 1);
    }

    public void StopStanbyRotation()
    {
        // mRotationHandler.GetRotationBase<NormalRotation>().StopStanbyRotation();
        mNormalRotation.StopStanbyRotation();
    }

    private void initRotation()
    {
        mAnimationCurveRotation = new AnimationCurveRotation(mRotationHandler, mController);
        mAnimationCurveRotation.SetAnimationCurve(_idleTurnPositionCurve, 
                                                _idleTurnRotationCurve, 
                                                _runTurnPositionCurve, 
                                                _runTurnRotationCurve);

        mRootMotionRotation = new RootMotionRotation(mRotationHandler, mController);
        mOffset180Rotation = new Offset180Rotation(mRotationHandler, mController);
        mNormalRotation = new NormalRotation(mRotationHandler, mController);
        mNormalRotation.SetTurnAnimData(_turnAnimationData);

        mPlayableRotation = new PlayableRotation(mRotationHandler, mController, turnPlayable);
    }

    public RotationBase getRotation(RotationHandler.EType type)
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

    private void switchToMoveState()
    {
        mController.StateMachine.SwitchState<PlayerMoveState>();
    }

    private void changeDirection()
    {
        mStartEulerAngles = getCurrentEulerAngles();
        mController.Movement.SetDirection(mController.Movement.OppositeDirection);

        mTargetEulerAngles = getCurrentEulerAngles();
        mRemainAngle = getRemainAngle(mTargetEulerAngles.y);

        // Rotation L
        if(mRemainAngle < 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Left);

            // playAnimation(mTurnType, RotationHandler.ERotationDirection.Left);
            turnPlayable.ChangeClip(mTurnType, RotationHandler.ERotationDirection.Left);
        }
        // Rotation R
        else if(mRemainAngle > 0f)
        {
            mRotationHandler.SetRotationDirection(RotationHandler.ERotationDirection.Right);

            // playAnimation(mTurnType, RotationHandler.ERotationDirection.Right);
            turnPlayable.ChangeClip(mTurnType, RotationHandler.ERotationDirection.Right);
        }

        float normalizedTime = 1f - Mathf.Abs(mRemainAngle) / Number.DEG_180;
        mRotationHandler.SetNormalizedTime(normalizedTime);

        mTurnAnimation = getTurnAnimation(mTurnType, mRotationHandler.RotationDirection);
        turnPlayable.ResetClip(mRotationHandler.NormalizedTime);
        turnPlayable.StartClip();

        GameDebug.Log($"Remain Angle: {mRemainAngle}, NormalizedTime: {normalizedTime}, Rotation Direction: {mRotationHandler.RotationDirection}", 
            tag: "Change Direction");
    }

    private void playAnimation(ETurnType type, RotationHandler.ERotationDirection rotationDirection)
    {
        AnimState animState = mTurnType == ETurnType.Run ? AnimState.RunTurn : AnimState.IdleTurn;

        switch(mTurnType)
        {
            case ETurnType.Idle:
                animState = (rotationDirection == RotationHandler.ERotationDirection.Left) ? AnimState.IdleTurn : AnimState.IdleTurn_R;
                break;
            case ETurnType.Run:
                animState = (rotationDirection == RotationHandler.ERotationDirection.Left) ? AnimState.RunTurn : AnimState.RunTurn_R;
                break;
        }

        mAnimation.Play(animState);
    }

    private TurnAnimationData.TurnAnimation getTurnAnimation(ETurnType type, RotationHandler.ERotationDirection rotationDirection)
    {
        return _turnAnimationData.Get(type, rotationDirection);
    }

    private void startTimer()
    {
        mbEnableTimer = true;

        var currentStateInfo = mController.Animation.Animator.GetCurrentAnimatorStateInfo(0);

        // mTimer = mTurnDuration * currentStateInfo.normalizedTime;
        mTimerFromTransitionEnd = mTurnAnimation.GetDuration() * currentStateInfo.normalizedTime;

        GameDebug.Log($"Turn Timer(G9): {mTimerFromTransitionEnd.ToString("G9")}, Turn Animator Normalized Time: {currentStateInfo.normalizedTime}",
                    category: GameDebug.LogCategory.State);
    }

    private void startTimer(bool selfTransition)
    {
        mbEnableTimer = true;

        var currentStateInfo = mController.Animation.Animator.GetCurrentAnimatorStateInfo(0);

        // mTimer = mTurnDuration * currentStateInfo.normalizedTime;
        mTimerFromTransitionEnd = mTurnAnimation.GetDuration() * currentStateInfo.normalizedTime;

        GameDebug.Log($"Turn Timer(G9): {mTimerFromTransitionEnd.ToString("G9")}, Turn Animator Normalized Time: {currentStateInfo.normalizedTime}", 
            category: GameDebug.LogCategory.State);
        // GameDebug.Log($"TurnState.onEndTransition", GameDebug.LogCategory.State, GameDebug.LogLevel.Info);
    }

    private Quaternion getCurrentRotation()
    {
        return mController.Movement.DirectionToRotation(mController.Movement.Direction);
    }

    private Vector3 getCurrentEulerAngles()
    {
        return mController.Movement.DirectionToRotation(mController.Movement.Direction).eulerAngles;
    }

    private float getRemainAngle(float targetAngle)
    {
        return Mathf.DeltaAngle(mController.Movement.Rotation.eulerAngles.y, targetAngle);
    }

    private Vector3 rotateVector(Vector3 vector3, float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float newX = vector3.x * cos - vector3.z * sin;
        float newZ = vector3.x * sin + vector3.z * cos;

        return new Vector3(newX, vector3.y, newZ);
    }

}
