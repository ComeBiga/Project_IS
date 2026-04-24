using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    public Animator Animator => mAnimator;
    public AnimationEventReceiver AnimationEventReceiver => _animationEventReceiver;

    public event Action onAnimationIK = null;

    [SerializeField] private AnimationEventReceiver _animationEventReceiver;

    // Animator Parameter Hashes
    private readonly int StateHash = Animator.StringToHash("State");
    private readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private readonly int VerticalHash = Animator.StringToHash("Vertical");
    private readonly int IsLeftFootHash = Animator.StringToHash("IsLeftFoot");
    private readonly int MoveInputXTappedHash = Animator.StringToHash("MoveInputXTapped");
    private readonly int MoveInputXPressedHash = Animator.StringToHash("MoveInputXPressed");
    private readonly int MoveInputXHeldHash = Animator.StringToHash("MoveInputXHeld");
    private readonly int MoveInputYTappedHash = Animator.StringToHash("MoveInputYTapped");
    private readonly int MoveInputYPressedHash = Animator.StringToHash("MoveInputYPressed");
    private readonly int MoveInputYHeldHash = Animator.StringToHash("MoveInputYHeld");
    private readonly int InputXRawHash = Animator.StringToHash("InputXRaw");
    private readonly int InputXMagnitudeHash = Animator.StringToHash("InputXMagnitude");
    private readonly int InputYMagnitudeHash = Animator.StringToHash("InputYMagnitude");
    private readonly int JumpHash = Animator.StringToHash("Jump");
    private readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int VelocityYHash = Animator.StringToHash("VelocityY");
    private readonly int LandingHash = Animator.StringToHash("Landing");
    private readonly int HeavyLandingHash = Animator.StringToHash("HeavyLanding");
    private readonly int TurnLHash = Animator.StringToHash("TurnL");
    private readonly int TurnRHash = Animator.StringToHash("TurnR");
    private readonly int LadderTopHash = Animator.StringToHash("LadderTop");
    private readonly int IndexHash = Animator.StringToHash("Index");
    private readonly int ClimbObjectHash = Animator.StringToHash("ClimbObject");
    private readonly int ClimbLedgeHash = Animator.StringToHash("ClimbLedge");
    private readonly int ClimbLadderHash = Animator.StringToHash("ClimbLadder");
    private readonly int PushHash = Animator.StringToHash("Push");

    // Animation State Name Hashes
    private readonly int IdleLandingHash = Animator.StringToHash("IdleLanding");
    private readonly int IdleSoftLandingHash = Animator.StringToHash("IdleSoftLanding");
    private readonly int IdleMediumLandingHash = Animator.StringToHash("IdleMediumLanding");
    private readonly int IdleHeavyLandingHash = Animator.StringToHash("IdleHeavyLanding");
    private readonly int RunningLandingHash = Animator.StringToHash("RunningLanding");
    private readonly int RunningSoftLandingHash = Animator.StringToHash("RunningSoftLanding");
    private readonly int RunningMediumLandingHash = Animator.StringToHash("RunningMediumLanding");

    private Animator mAnimator;

    public void SetState(int value)
    {
        mAnimator.SetInteger(StateHash, value);
    }

    public void SetHorizontal(float value)
    {
        mAnimator.SetFloat(HorizontalHash, value);
    }

    public void SetVertical(float value)
    {
        mAnimator.SetFloat(VerticalHash, value);
    }

    public void SetIsLeftFoot(bool value)
    {
        mAnimator.SetBool(IsLeftFootHash, value);
    }

    public void SetMoveInputXTapped(bool value)
    {
        mAnimator.SetBool(MoveInputXTappedHash, value);
    }

    public void SetMoveInputXPressed(bool value)
    {
        mAnimator.SetBool(MoveInputXPressedHash, value);
    }

    public void SetMoveInputXHeld(bool value)
    {
        mAnimator.SetBool(MoveInputXHeldHash, value);
    }

    public void SetMoveInputYTapped(bool value)
    {
        mAnimator.SetBool(MoveInputYTappedHash, value);
    }

    public void SetMoveInputYPressed(bool value)
    {
        mAnimator.SetBool(MoveInputYPressedHash, value);
    }

    public void SetMoveInputYHeld(bool value)
    {
        mAnimator.SetBool(MoveInputYHeldHash, value);
    }

    public void SetInputXMagnitude(float value)
    {
        mAnimator.SetFloat(InputXMagnitudeHash, value);
    }

    public void SetInputXRaw(float value)
    {
        mAnimator.SetFloat(InputXRawHash, value);
    }

    public void SetInputYMagnitude(float value)
    {
        mAnimator.SetFloat(InputYMagnitudeHash, value);
    }

    public void SetVelocityY(float value)
    {
        mAnimator.SetFloat(VelocityYHash, value);
    }

    public void TurnL(bool value)
    {
        mAnimator.SetBool(TurnLHash, value);
    }
    
    public void TurnR(bool value)
    {
        mAnimator.SetBool(TurnRHash, value);
    }

    public void SetIsGrounded(bool value)
    {
        mAnimator.SetBool(IsGroundedHash, value);
    }

    public void SetJump()
    {
        mAnimator.SetTrigger(JumpHash);
    }

    //public void SetLanding()
    //{
    //    mAnimator.SetTrigger(LandingHash);
    //}

    public void SetLanding(bool value)
    {
        mAnimator.SetBool(LandingHash, value);
    }

    //public void ResetLanding()
    //{
    //    mAnimator.ResetTrigger(LandingHash);
    //}

    public void SetHeavyLanding()
    {
        mAnimator.SetTrigger(HeavyLandingHash);
    }

    public void SetLadderTop(bool value)
    {
        mAnimator.SetBool(LadderTopHash, value);
    }

    public void SetIndex(int value)
    {
        mAnimator.SetInteger(IndexHash, value);
    }

    public void SetClimbObject()
    {
        mAnimator.SetTrigger(ClimbObjectHash);
    }

    public void SetClimbLedge()
    {
        mAnimator.SetTrigger(ClimbLedgeHash);
    }

    public void SetClimbLadder()
    {
        mAnimator.SetTrigger(ClimbLadderHash);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="normalizedTime"></param>
    /// <param name="landingType">0:soft, 1:mideum, 2:heavy</param>
    public void PlayIdleLanding(int landingType, float normalizedTime)
    {
        switch(landingType)
        {
            case 0:
                mAnimator.Play(IdleSoftLandingHash, 0, normalizedTime);
                break;
            case 1:
                mAnimator.Play(IdleMediumLandingHash, 0, normalizedTime);
                break;
            case 2:
                mAnimator.Play(IdleHeavyLandingHash, 0, normalizedTime);
                break;
            default:
                mAnimator.Play(IdleSoftLandingHash, 0, normalizedTime);
                break;
        }
    }

    public void CrossFadeIdleLanding(int landingType, float normalizedTrasitionDuration)
    {
        switch(landingType)
        {
            case 0:
                mAnimator.CrossFade(IdleSoftLandingHash, normalizedTrasitionDuration, 0);
                break;
            case 1:
                mAnimator.CrossFade(IdleMediumLandingHash, normalizedTrasitionDuration, 0);
                break;
            case 2:
                mAnimator.CrossFade(IdleHeavyLandingHash, normalizedTrasitionDuration, 0);
                break;
            default:
                mAnimator.CrossFade(IdleSoftLandingHash, normalizedTrasitionDuration, 0);
                break;
        }
    }

    public void PlayRunningLanding(float normalizedTime)
    {
        mAnimator.Play(RunningLandingHash, 0, normalizedTime);
    }

    public void CrossFadeRunningLanding(float normalizedTransitionDuration)
    {
        mAnimator.CrossFade(RunningLandingHash, normalizedTransitionDuration, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="landingType">0:soft, 1:medium</param>
    /// <param name="normalizedTransitionDuration"></param>
    public void CrossFadeRunningLanding(int landingType, float normalizedTransitionDuration)
    {
        switch(landingType)
        {
            case 0:
                mAnimator.CrossFade(RunningSoftLandingHash, normalizedTransitionDuration, 0);
                return;
            case 1:
                mAnimator.CrossFade(RunningMediumLandingHash, normalizedTransitionDuration, 0);
                return;
            default:
                mAnimator.CrossFade(RunningSoftLandingHash, normalizedTransitionDuration, 0);
                return;
        }

    }

    public void SetPush()
    {
        mAnimator.SetTrigger(PushHash);
    }

    private void Awake()
    {
        mAnimator = GetComponent<Animator>();
    }

    // 이 함수의 유무에 따라 Animator가 어떻게 달라지는 지 확인 필요
    // 이 함수가 없으면 RootMotion이 직접 계산 되는 것 같음
    // 계산에 문제가 없도록 남겨둘 필요가 있음
    private void OnAnimatorMove()
    {

    }

    private void OnAnimatorIK(int layerIndex)
    {
        onAnimationIK?.Invoke();
    }

    //private void FootStepR()
    //{
    //    // Debug.Log("Right Foot Step");
    //}

    //private void FootStepL()
    //{
    //    // Debug.Log("Left Foot Step");
    //}
}
