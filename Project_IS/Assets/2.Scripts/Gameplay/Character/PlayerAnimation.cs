using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static PlayerMovement;

[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    public Animator Animator => mAnimator;
    public AnimationEventReceiver AnimationEventReceiver => _animationEventReceiver;

    public event Action onAnimatorFixedUpdate = null;
    public event Action onAnimatorMove = null;
    public event Action onAnimatorIK = null;
    public event Action onAnimatorStateChanged = null;
    public event Action<bool> onEndTransition = null;
    public event Action<string, AnimatorStateInfo> onEnterState = null;
    public event Action<string, AnimatorStateInfo> onUpdateState = null;
    public event Action<string, AnimatorStateInfo> onExitState = null;

    [SerializeField] private AnimationEventReceiver _animationEventReceiver;
    [SerializeField] private bool _trasitionLog = false;
    [SerializeField] private TransitionTable _transitionTable;

    private int mCurrentAnimatorStateHash = -1;
    private int mLogicalCurrentAnimStateHash = -1;
    private bool mbWasInTransition = false;
    private bool mbSelfTransition = false;

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
    private readonly int FallHash = Animator.StringToHash("Fall");
    private readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int VelocityYHash = Animator.StringToHash("VelocityY");
    private readonly int LandingHash = Animator.StringToHash("Landing");
    private readonly int HeavyLandingHash = Animator.StringToHash("HeavyLanding");
    private readonly int TurnHash = Animator.StringToHash("Turn");
    private readonly int TurnTriggerHash = Animator.StringToHash("Turn Trigger");
    private readonly int TurnLHash = Animator.StringToHash("TurnL");
    private readonly int TurnRHash = Animator.StringToHash("TurnR");
    private readonly int LadderTopHash = Animator.StringToHash("LadderTop");
    private readonly int IndexHash = Animator.StringToHash("Index");
    private readonly int ClimbObjectHash = Animator.StringToHash("ClimbObject");
    private readonly int ClimbLedgeHash = Animator.StringToHash("ClimbLedge");
    private readonly int ClimbLadderHash = Animator.StringToHash("ClimbLadder");
    private readonly int PushHash = Animator.StringToHash("Push");
    private readonly int FootPositionHash = Animator.StringToHash("FootPosition");
    private readonly int InputXHash = Animator.StringToHash("InputX");
    private readonly int FrontWallHash = Animator.StringToHash("FrontWall");
    private readonly int RunningHash = Animator.StringToHash("Running");
    private readonly int IdleToRunHash = Animator.StringToHash("IdleToRun");
    private readonly int RunToIdleHash = Animator.StringToHash("RunToIdle");
    private readonly int ActivateHash = Animator.StringToHash("Activate");
    private readonly int MotionTimeHash = Animator.StringToHash("MotionTime");
    private readonly int MultiplierHash = Animator.StringToHash("Multiplier");

    private Animator mAnimator;

    public void SetMultiplier(float value)
    {
        mAnimator.SetFloat(MultiplierHash, value);
    }

    public void SetMotionTime(float value)
    {
        mAnimator.SetFloat(MotionTimeHash, value);
    }

    public float GetVertical()
    {
        return mAnimator.GetFloat(VerticalHash);
    }

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

        // GameDebug.Log($"Set Vertical: {value}", tag: "Ladder LookBack");
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

    public void SetInputX(bool value)
    {
        mAnimator.SetBool(InputXHash, value);
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

    public void SetTurn(bool value)
    {
        mAnimator.SetBool(TurnHash, value);
    }

    public void SetTurnTrigger()
    {
        mAnimator.SetTrigger(TurnTriggerHash);
    }

    public void ResetTurnTrigger()
    {
        mAnimator.ResetTrigger(TurnTriggerHash);
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

    public void SetJump(bool value)
    {
        mAnimator.SetBool(JumpHash, value);
    }

    public void SetFall(bool value)
    {
        mAnimator.SetBool(FallHash, value);
    }

    public void ResetFall()
    {
        mAnimator.ResetTrigger(FallHash);
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

    public void SetFootPosition(int value)
    {
        mAnimator.SetInteger(FootPositionHash, value);
    }

    public void SetFrontWall(bool value)
    {
        mAnimator.SetBool(FrontWallHash, value);
    }

    public void SetRunning(bool value)
    {
        mAnimator.SetBool(RunningHash, value);
    }

    public void SetIdleToRun(bool value)
    {
        mAnimator.SetBool(IdleToRunHash, value);
    }

    public void SetRunToIdle(bool value)
    {
        mAnimator.SetBool(RunToIdleHash, value);
    }

    public void SetActivate()
    {
        mAnimator.SetTrigger(ActivateHash);
    }

    public bool Play(AnimState nextAnimState)
    {
        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
        int currentAnimStateHash = (mLogicalCurrentAnimStateHash == -1) ? currentStateInfo.fullPathHash : mLogicalCurrentAnimStateHash;

        if (!TryGetAnimState(currentAnimStateHash, out AnimState currentState))
            return false;
        // AnimState currentState = AnimState.Idle;
        //int stateHash = -1;

        //foreach(KeyValuePair<AnimState, int> pair in AnimStateHash.stateHashes)
        //{
        //    // if(pair.Value == currentStateInfo.fullPathHash)
        //    if(pair.Value == currentAnimStateHash)
        //    {
        //        currentState = pair.Key;
        //        stateHash = pair.Value;
        //        break;
        //    }
        //}

        //if(stateHash == -1)
        //{
        //    // Debug.LogError($"stateHash 정보를 찾을 수 없습니다! currentStateHash: {currentStateInfo.fullPathHash}");
        //    Debug.LogError($"stateHash 정보를 찾을 수 없습니다! currentStateHash: {currentAnimStateHash}");
        //    return false;
        //}

        int nextStateHash = AnimStateHash.stateHashes[nextAnimState];
        mLogicalCurrentAnimStateHash = nextStateHash;

        if(_transitionTable.TryGet(currentState, nextAnimState, out TransitionTable.TransitionData transitionData))
        {
            CrossFade(nextStateHash, transitionData.fixedDuration, transitionData.duration, transitionData.offset);
            //if (transitionData.fixedDuration)
            //{
            //    mAnimator.CrossFadeInFixedTime(nextStateHash, transitionData.duration, 0, transitionData.offset);
            //}
            //else
            //{
            //    mAnimator.CrossFade(nextStateHash, transitionData.duration, 0, transitionData.offset);
            //}
            
            if(transitionData.anyFrom)
                GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}] by AnyState", tag: "Animation Play", category: GameDebug.LogCategory.Animation);
            else
                GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}]", tag: "Animation Play", category: GameDebug.LogCategory.Animation);

            return true;
        }
        else
        {
            int stateHash = AnimStateHash.stateHashes[currentState];
            Debug.LogError($"Enforced Transition Error - stateHash의 transitionData를 찾을 수 없습니다! currentState: {currentState}({stateHash}), nextState: {nextAnimState}({nextStateHash})");
            return false;
        }
    }

    public bool Play(AnimState nextAnimState, float offset)
    {
        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
        int currentAnimStateHash = (mLogicalCurrentAnimStateHash == -1) ? currentStateInfo.fullPathHash : mLogicalCurrentAnimStateHash;

        if (!TryGetAnimState(currentAnimStateHash, out AnimState currentState))
            return false;

        int nextStateHash = AnimStateHash.stateHashes[nextAnimState];
        mLogicalCurrentAnimStateHash = nextStateHash;

        if(_transitionTable.TryGet(currentState, nextAnimState, out TransitionTable.TransitionData transitionData))
        {
            CrossFade(nextStateHash, transitionData.fixedDuration, transitionData.duration, offset);
            
            if(transitionData.anyFrom)
                GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}] by AnyState", tag: "Animation Play", category: GameDebug.LogCategory.Animation);
            else
                GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}]", tag: "Animation Play", category: GameDebug.LogCategory.Animation);

            return true;
        }
        else
        {
            int stateHash = AnimStateHash.stateHashes[currentState];
            Debug.LogError($"Enforced Transition Error - stateHash의 transitionData를 찾을 수 없습니다! currentState: {currentState}({stateHash}), nextState: {nextAnimState}({nextStateHash})");
            return false;
        }
    }

    public bool Play(AnimState nextAnimState, bool fixedTime, float duration, float offset)
    {
        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
        int currentAnimStateHash = (mLogicalCurrentAnimStateHash == -1) ? currentStateInfo.fullPathHash : mLogicalCurrentAnimStateHash;

        AnimState currentState = AnimState.Idle;
        int stateHash = -1;

        foreach (KeyValuePair<AnimState, int> pair in AnimStateHash.stateHashes)
        {
            if (pair.Value == currentAnimStateHash)
            {
                currentState = pair.Key;
                stateHash = pair.Value;
                break;
            }
        }

        if (stateHash == -1)
        {
            Debug.LogError($"stateHash 정보를 찾을 수 없습니다! currentStateHash: {currentAnimStateHash}");
            return false;
        }

        int nextStateHash = AnimStateHash.stateHashes[nextAnimState];
        mLogicalCurrentAnimStateHash = nextStateHash;
        
        CrossFade(nextStateHash, fixedTime, duration, offset);
        //if (fixedTime)
        //{
        //    mAnimator.CrossFadeInFixedTime(nextStateHash, duration, 0, offset);
        //}
        //else
        //{
        //    mAnimator.CrossFade(nextStateHash, duration, 0, offset);
        //}

        GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}]", tag: "Animation Play", category: GameDebug.LogCategory.Animation);

        return true;
    }

    public void CrossFade(int nextStateHash, bool fixedTime, float duration, float offset)
    {
        if (fixedTime)
        {
            mAnimator.CrossFadeInFixedTime(nextStateHash, duration, 0, offset);
        }
        else
        {
            mAnimator.CrossFade(nextStateHash, duration, 0, offset);
        }
    }

    public bool ValidateAnimStateHash(int animStateHash)
    {
        // AnimState currentState = AnimState.Idle;
        int stateHash = -1;

        foreach (KeyValuePair<AnimState, int> pair in AnimStateHash.stateHashes)
        {
            if (pair.Value == animStateHash)
            {
                // currentState = pair.Key;
                stateHash = pair.Value;

                return true;
            }
        }

        Debug.LogError($"stateHash 정보를 찾을 수 없습니다! StateHash: {animStateHash}");
        return false;
    }

    public bool TryGetAnimState(int animStateHash, out AnimState animState)
    {
        foreach (KeyValuePair<AnimState, int> pair in AnimStateHash.stateHashes)
        {
            if (pair.Value == animStateHash)
            {
                animState = pair.Key;
                return true;
            }
        }

        Debug.LogError($"stateHash 정보를 찾을 수 없습니다! StateHash: {animStateHash}");

        animState = AnimState.Idle;
        return false;
    }

    public void EnterState(string stateName, AnimatorStateInfo animatorStateInfo)
    {
        onEnterState?.Invoke(stateName, animatorStateInfo);
    }

    public void UpdateState(string stateName, AnimatorStateInfo animatorStateInfo)
    {
        onUpdateState?.Invoke(stateName, animatorStateInfo);
    }

    public void ExitState(string stateName, AnimatorStateInfo animatorStateInfo)
    {
        onExitState?.Invoke(stateName, animatorStateInfo);
    }

    private void Awake()
    {
        mAnimator = GetComponent<Animator>();
        _transitionTable.Initialize();

    }

    private void FixedUpdate()
    {
        onAnimatorFixedUpdate?.Invoke();
    }

    // 이 함수의 유무에 따라 Animator가 어떻게 달라지는 지 확인 필요
    // 이 함수가 없으면 RootMotion이 직접 계산 되는 것 같음
    // 계산에 문제가 없도록 남겨둘 필요가 있음
    private void OnAnimatorMove()
    {
        onAnimatorMove?.Invoke();

        if(mAnimator.IsInTransition(0))
        {
            AnimatorTransitionInfo transitionInfo = mAnimator.GetAnimatorTransitionInfo(0);
            AnimatorStateInfo nextStateInfo = mAnimator.GetNextAnimatorStateInfo(0);

            AnimStateNameLookUp.names.TryGetValue(mCurrentAnimatorStateHash, out string currentStateName);
            AnimStateNameLookUp.names.TryGetValue(nextStateInfo.fullPathHash, out string nextStateName);

            if (mCurrentAnimatorStateHash == nextStateInfo.fullPathHash)
                mbSelfTransition = true;

            mbWasInTransition = true;

            GameDebug.Log($"Animator In Transition from [{currentStateName}] to [{nextStateName}], transition normalized time: {transitionInfo.normalizedTime}, duration: {transitionInfo.duration}",
                tag: "Animation Transition", category: GameDebug.LogCategory.Animation, level: GameDebug.LogLevel.Verbose);
        }
        else
        {
            if (mbWasInTransition)
            {
                onEndTransition?.Invoke(mbSelfTransition);

                mbWasInTransition = false;
                mbSelfTransition = false;
            }
        }

        AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

        if(mCurrentAnimatorStateHash != currentStateInfo.fullPathHash)
        {
            int lastStateHash = mCurrentAnimatorStateHash;
            mCurrentAnimatorStateHash = currentStateInfo.fullPathHash;

            onAnimatorStateChanged?.Invoke();

            AnimStateNameLookUp.names.TryGetValue(lastStateHash, out string lastStateName);
            AnimStateNameLookUp.names.TryGetValue(mCurrentAnimatorStateHash, out string currentStateName);

            GameDebug.Log($"Animator State Changed from [{lastStateName}] to [{currentStateName}], Normalized Time: {currentStateInfo.normalizedTime}", 
                tag: "Animation State Changed", category: GameDebug.LogCategory.Animation);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        onAnimatorIK?.Invoke();
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
