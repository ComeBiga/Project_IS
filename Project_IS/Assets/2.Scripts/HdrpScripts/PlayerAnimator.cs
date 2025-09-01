using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    public Animator Animator => mAnimator;

    public event Action onAnimationIK = null;

    private readonly int StateHash = Animator.StringToHash("State");
    private readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private readonly int VerticalHash = Animator.StringToHash("Vertical");
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

    public void SetInputXMagnitude(float value)
    {
        mAnimator.SetFloat(InputXMagnitudeHash, value);
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

    public void SetLanding()
    {
        mAnimator.SetTrigger(LandingHash);
    }

    public void ResetLanding()
    {
        mAnimator.ResetTrigger(LandingHash);
    }

    public void SetHeavyLanding()
    {
        mAnimator.SetTrigger(HeavyLandingHash);
    }

    public void SetLadderTop(bool value)
    {
        mAnimator.SetBool(LadderTopHash, value);
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
}
