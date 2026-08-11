using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLandingState : PlayerStateBase
{
    public enum ELandingType { Soft, Medium, Heavy };
    public enum EMoveType { Idle, Run };

    [Header("Landing Duration")]
    [SerializeField] private float _IdleSoftDuration = .2f;
    [SerializeField] private float _RunSoftDuration = .2f;
    [SerializeField] private float _IdleMediumDuration = .2f;
    [SerializeField] private float _RunMediumDuration = .2f;
    [SerializeField] private float _HeavyDuration = .2f;

    [Header("Critical Duration")]
    [SerializeField] private float _IdleSoftCriticalDuration = .1f;
    [SerializeField] private float _RunSoftCriticalDuration = .1f;
    [SerializeField] private float _IdleMediumCriticalDuration = .1f;
    [SerializeField] private float _RunMediumCriticalDuration = .1f;
    [SerializeField] private float _HeavyCriticalDuration = .1f;

    [Header("Landing X Velocity")]
    [SerializeField] private float _IdleSoftXVelocity = 0f;
    [SerializeField] private float _RunSoftXVelocity = 2f;
    [SerializeField] private float _IdleMediumXVelocity = 0f;
    [SerializeField] private float _RunMediumXVelocity = 1f;
    [SerializeField] private float _HeavyXVelocity = 0f;

    private ELandingType mLandingType;
    private EMoveType mMoveType;
    private float mLandingDuration = float.MaxValue;
    private float mCriticalDuration = float.MaxValue;
    private float mLandingXVelocity = 0f;
    private Vector2 mLandingMoveInput = Vector2.zero;
    private float mTimer = 0f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
    }

    public override void EnterState()
    {
        if (mController.InputHandler.GetInputRawMagnitude().x > .1f)
        {
            mMoveType = EMoveType.Run;
        }
        else
        {
            mMoveType = EMoveType.Idle;

            mController.Movement.SetVelocity(Vector3.zero);
        }

        int landingType = -1;

        switch(mLandingType)
        {
            case ELandingType.Soft:
                landingType = 0;
                if(mMoveType == EMoveType.Idle)
                {
                    mLandingDuration = _IdleSoftDuration;
                    mCriticalDuration = _IdleSoftCriticalDuration;
                    mLandingXVelocity = _IdleSoftXVelocity;
                }
                else
                {
                    mLandingDuration = _RunSoftDuration;
                    mCriticalDuration = _RunSoftCriticalDuration;
                    mLandingXVelocity = _RunSoftXVelocity;
                }
                break;
            case ELandingType.Medium:
                landingType = 1;
                if(mMoveType == EMoveType.Idle)
                {
                    mLandingDuration = _IdleMediumDuration;
                    mCriticalDuration = _IdleMediumCriticalDuration;
                    mLandingXVelocity = _IdleMediumXVelocity;
                }
                else
                {
                    mLandingDuration = _RunMediumDuration;
                    mCriticalDuration = _RunMediumCriticalDuration;
                    mLandingXVelocity = _RunMediumXVelocity;
                }
                break;
            case ELandingType.Heavy:
                landingType = 2;
                mLandingDuration = _HeavyDuration;
                mCriticalDuration = _HeavyCriticalDuration;
                mLandingXVelocity = _HeavyXVelocity;
                break;
        }

        //mController.Animator.SetInputX(mMoveType == EMoveType.Run ? true : false);
        //mController.Animator.SetIndex(landingType);
        // mController.Animator.SetLanding(true);
        // mController.Animator.CrossFadeLanding(mMoveType == EMoveType.Run ? true : false, mLandingType);

        if(mMoveType == EMoveType.Idle)
        {
            switch(mLandingType)
            {
                case ELandingType.Soft:
                    mController.Animator.Play(AnimState.Landing_Idle_Soft);
                    break;
                case ELandingType.Medium:
                    mController.Animator.Play(AnimState.Landing_Idle_Medium);
                    break;
                case ELandingType.Heavy:
                    mController.Animator.Play(AnimState.Landing_Idle_Heavy);
                    break;
            }
        }
        else
        {
            switch(mLandingType)
            {
                case ELandingType.Soft:
                    mController.Animator.Play(AnimState.Landing_Running_Soft);
                    break;
                case ELandingType.Medium:
                    mController.Animator.Play(AnimState.Landing_Running_Medium);
                    break;
                case ELandingType.Heavy:
                    mController.Animator.Play(AnimState.Landing_Running_Heavy);
                    break;
            }
        }

        mLandingMoveInput.x = mLandingXVelocity / mController.Movement.MoveSpeed * mController.Movement.DirectionToVector().x;

        mTimer = 0f;
    }

    public override void ExitState()
    {
        // mController.Animator.SetIndex(0);
        mController.Animator.SetLanding(false);
    }

    public override void FixedTick()
    {

    }

    public override void Tick()
    {
        if(mMoveType == EMoveType.Run)
        {
            // mController.Movement.Move(mController.Movement.DirectionToVector());
            landingMove();
        }

        // Critical Duration
        if (mTimer < mCriticalDuration)
        {
            mTimer += Time.deltaTime;
            return;
        }

        // Landing Duration
        if (mTimer < mLandingDuration)
        {
            // To Fall

            // To Jump
            if(mController.InputHandler.JumpPressed)
            {
                mController.InputHandler.ResetJump();
                mController.StateMachine.SwitchState<PlayerRunJumpState>();
                return;
            }

            // To Turn
            if(mController.CheckOppositeInputX())
            {
                mController.StateMachine.SwitchState<PlayerTurnState>((turnState) =>
                {
                    var turnType = (mMoveType == EMoveType.Idle) ? PlayerTurnState.ETurnType.Idle : PlayerTurnState.ETurnType.Run;
                    turnState.SetTurnType(turnType);
                });

                return;
            }

            if(mMoveType == EMoveType.Idle)
            {
                // To IdleToRun
                if(mController.InputHandler.GetInputRawMagnitude().x > .1f)
                {
                    mController.StateMachine.SwitchState<PlayerIdleToRunState>();

                    return;
                }
            }
            else
            {
                // To RunToIdle
                if(mController.InputHandler.GetInputRawMagnitude().x < .1f)
                {
                    mController.StateMachine.SwitchState<PlayerRunToIdleState>();

                    return;
                }
            }

            mTimer += Time.deltaTime;
            return;
        }


        if(mMoveType == EMoveType.Idle)
        {
            // To Idle
            mController.StateMachine.SwitchState<PlayerIdleState>();
            return;
        }
        else
        {
            // To Move
            mController.StateMachine.SwitchState<PlayerMoveState>();
            return;
        }
    }

    public void SetLandingType(ELandingType type)
    {
        mLandingType = type;
    }

    private void landingMove()
    {
        mController.Movement.Move(mLandingMoveInput);
        mLandingMoveInput.x += Time.deltaTime * mController.InputHandler.AxisSensitivity * mController.Movement.DirectionToVector().x;
        mLandingMoveInput.x = Mathf.Clamp(mLandingMoveInput.x, -1f, 1f);

        // DebugUtility.FrameCountLog($"LandingMoveInput.x: {mLandingMoveInput.x}, DirectionToVector: {mController.Movement.DirectionToVector()}");
    }

    private void criticalUpdate()
    {

    }


}
