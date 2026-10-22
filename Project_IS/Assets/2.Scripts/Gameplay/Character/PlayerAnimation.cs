using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Animations.Rigging;
using static PlayerMovement;

[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    public Animator Animator => mAnimator;
    public bool UsesControllerPlayable => mControllerPlayable.IsValid();
    public uint StateEvaluationCount => mStateEvaluationCount;
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
    private AnimatorControllerPlayable mControllerPlayable;
    private uint mStateEvaluationCount;

    public void BindControllerPlayable(AnimatorControllerPlayable controller)
    {
        if (!controller.IsValid())
            return;

        // Copy settings made before the manual graph was created.
        foreach (AnimatorControllerParameter parameter in mAnimator.parameters)
        {
            int hash = parameter.nameHash;

            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    controller.SetFloat(hash, mAnimator.GetFloat(hash));
                    break;
                case AnimatorControllerParameterType.Int:
                    controller.SetInteger(hash, mAnimator.GetInteger(hash));
                    break;
                case AnimatorControllerParameterType.Bool:
                    controller.SetBool(hash, mAnimator.GetBool(hash));
                    break;
            }
        }

        for (int layer = 0; layer < mAnimator.layerCount; ++layer)
        {
            AnimatorStateInfo state = mAnimator.GetCurrentAnimatorStateInfo(layer);

            if (state.fullPathHash != 0 && controller.HasState(layer, state.fullPathHash))
                controller.Play(state.fullPathHash, layer, state.normalizedTime);
        }

        mControllerPlayable = controller;
        mStateEvaluationCount = 0;
        mLogicalCurrentAnimStateHash = -1;
        mCurrentAnimatorStateHash = -1;
        mbWasInTransition = false;
        mbSelfTransition = false;
    }

    public void UnbindControllerPlayable(AnimatorControllerPlayable controller)
    {
        if (mControllerPlayable.GetHandle() != controller.GetHandle())
            return;

        mControllerPlayable = default;
        mLogicalCurrentAnimStateHash = -1;
        mCurrentAnimatorStateHash = -1;
        mbWasInTransition = false;
        mbSelfTransition = false;
    }

    public void NotifyControllerEvaluated(AnimatorControllerPlayable controller)
    {
        if (UsesControllerPlayable && mControllerPlayable.GetHandle() == controller.GetHandle())
            ++mStateEvaluationCount;
    }

    public AnimatorStateInfo GetCurrentStateInfo(int layer)
    {
        return UsesControllerPlayable ? mControllerPlayable.GetCurrentAnimatorStateInfo(layer) :
            mAnimator.GetCurrentAnimatorStateInfo(layer);
    }

    public AnimatorStateInfo GetNextStateInfo(int layer)
    {
        return UsesControllerPlayable ? mControllerPlayable.GetNextAnimatorStateInfo(layer) :
            mAnimator.GetNextAnimatorStateInfo(layer);
    }

    public AnimatorTransitionInfo GetTransitionInfo(int layer)
    {
        return UsesControllerPlayable ? mControllerPlayable.GetAnimatorTransitionInfo(layer) :
            mAnimator.GetAnimatorTransitionInfo(layer);
    }

    public bool IsInTransition(int layer)
    {
        return UsesControllerPlayable ? mControllerPlayable.IsInTransition(layer) : mAnimator.IsInTransition(layer);
    }

    public float GetFloat(int hash)
    {
        return UsesControllerPlayable ? mControllerPlayable.GetFloat(hash) : mAnimator.GetFloat(hash);
    }

    public float GetFloat(string name)
    {
        return GetFloat(Animator.StringToHash(name));
    }

    public void SetMultiplier(float value)
    {
        setFloat(MultiplierHash, value);
    }

    public void SetMotionTime(float value)
    {
        setFloat(MotionTimeHash, value);
    }

    public float GetVertical()
    {
        return GetFloat(VerticalHash);
    }

    public void SetState(int value)
    {
        setInteger(StateHash, value);
    }

    public void SetHorizontal(float value)
    {
        setFloat(HorizontalHash, value);
    }

    public void SetVertical(float value)
    {
        setFloat(VerticalHash, value);

        // GameDebug.Log($"Set Vertical: {value}", tag: "Ladder LookBack");
    }

    public void SetIsLeftFoot(bool value)
    {
        setBool(IsLeftFootHash, value);
    }

    public void SetMoveInputXTapped(bool value)
    {
        setBool(MoveInputXTappedHash, value);
    }

    public void SetMoveInputXPressed(bool value)
    {
        setBool(MoveInputXPressedHash, value);
    }

    public void SetMoveInputXHeld(bool value)
    {
        setBool(MoveInputXHeldHash, value);
    }

    public void SetMoveInputYTapped(bool value)
    {
        setBool(MoveInputYTappedHash, value);
    }

    public void SetMoveInputYPressed(bool value)
    {
        setBool(MoveInputYPressedHash, value);
    }

    public void SetMoveInputYHeld(bool value)
    {
        setBool(MoveInputYHeldHash, value);
    }

    public void SetInputXMagnitude(float value)
    {
        setFloat(InputXMagnitudeHash, value);
    }

    public void SetInputX(bool value)
    {
        setBool(InputXHash, value);
    }

    public void SetInputXRaw(float value)
    {
        setFloat(InputXRawHash, value);
    }

    public void SetInputYMagnitude(float value)
    {
        setFloat(InputYMagnitudeHash, value);
    }

    public void SetVelocityY(float value)
    {
        setFloat(VelocityYHash, value);
    }

    public void SetTurn(bool value)
    {
        setBool(TurnHash, value);
    }

    public void SetTurnTrigger()
    {
        setTrigger(TurnTriggerHash);
    }

    public void ResetTurnTrigger()
    {
        resetTrigger(TurnTriggerHash);
    }

    public void TurnL(bool value)
    {
        setBool(TurnLHash, value);
    }
    
    public void TurnR(bool value)
    {
        setBool(TurnRHash, value);
    }

    public void SetIsGrounded(bool value)
    {
        setBool(IsGroundedHash, value);
    }

    public void SetJump(bool value)
    {
        setBool(JumpHash, value);
    }

    public void SetFall(bool value)
    {
        setBool(FallHash, value);
    }

    public void ResetFall()
    {
        resetTrigger(FallHash);
    }

    //public void SetLanding()
    //{
    //    mAnimator.SetTrigger(LandingHash);
    //}

    public void SetLanding(bool value)
    {
        setBool(LandingHash, value);
    }

    //public void ResetLanding()
    //{
    //    mAnimator.ResetTrigger(LandingHash);
    //}

    public void SetHeavyLanding()
    {
        setTrigger(HeavyLandingHash);
    }

    public void SetLadderTop(bool value)
    {
        setBool(LadderTopHash, value);
    }

    public void SetIndex(int value)
    {
        setInteger(IndexHash, value);
    }

    public void SetClimbObject()
    {
        setTrigger(ClimbObjectHash);
    }

    public void SetClimbLedge()
    {
        setTrigger(ClimbLedgeHash);
    }

    public void SetClimbLadder()
    {
        setTrigger(ClimbLadderHash);
    }

    public void SetFootPosition(int value)
    {
        setInteger(FootPositionHash, value);
    }

    public void SetFrontWall(bool value)
    {
        setBool(FrontWallHash, value);
    }

    public void SetRunning(bool value)
    {
        setBool(RunningHash, value);
    }

    public void SetIdleToRun(bool value)
    {
        setBool(IdleToRunHash, value);
    }

    public void SetRunToIdle(bool value)
    {
        setBool(RunToIdleHash, value);
    }

    public void SetActivate()
    {
        setTrigger(ActivateHash);
    }

    /// <summary>
    /// ControllerPlayable 전환 용 Play 메서드
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="state"></param>
    /// <param name="normalizedTime"></param>
    /// <returns></returns>
    public bool PlayForControllerBlend(
        AnimatorControllerPlayable controller,
        AnimState state,
        float normalizedTime)
    {
        if (!controller.IsValid() || !AnimStateHash.stateHashes.TryGetValue(state, out int stateHash) ||
            !controller.HasState(0, stateHash))
        {
            Debug.LogError($"Cannot prepare Controller Blend state: {state}");
            return false;
        }

        controller.Play(stateHash, 0, Mathf.Clamp01(normalizedTime));
        mLogicalCurrentAnimStateHash = stateHash;

        GameDebug.Log($"Controller Blend Target: {state}, normalizedTime: {Mathf.Clamp01(normalizedTime)}",
            tag: "Controller Blend");

        return true;
    }

    public bool Play(AnimState nextAnimState)
    {
        AnimatorStateInfo currentStateInfo = GetCurrentStateInfo(0);
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
        //    // Debug.LogError($"stateHash ������ ã�� �� �����ϴ�! currentStateHash: {currentStateInfo.fullPathHash}");
        //    Debug.LogError($"stateHash ������ ã�� �� �����ϴ�! currentStateHash: {currentAnimStateHash}");
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
            Debug.LogError($"Enforced Transition Error - stateHash�� transitionData�� ã�� �� �����ϴ�! currentState: {currentState}({stateHash}), nextState: {nextAnimState}({nextStateHash})");
            return false;
        }
    }

    public bool Play(AnimState nextAnimState, float offset)
    {
        AnimatorStateInfo currentStateInfo = GetCurrentStateInfo(0);
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
            Debug.LogError($"Enforced Transition Error - stateHash�� transitionData�� ã�� �� �����ϴ�! currentState: {currentState}({stateHash}), nextState: {nextAnimState}({nextStateHash})");
            return false;
        }
    }

    public bool Play(AnimState nextAnimState, bool fixedTime, float duration, float offset)
    {
        AnimatorStateInfo currentStateInfo = GetCurrentStateInfo(0);
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
            Debug.LogError($"stateHash ������ ã�� �� �����ϴ�! currentStateHash: {currentAnimStateHash}");
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
            if (UsesControllerPlayable)
                mControllerPlayable.CrossFadeInFixedTime(nextStateHash, duration, 0, offset);
            else
                mAnimator.CrossFadeInFixedTime(nextStateHash, duration, 0, offset);
        }
        else
        {
            if (UsesControllerPlayable)
                mControllerPlayable.CrossFade(nextStateHash, duration, 0, offset);
            else
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

        Debug.LogError($"stateHash ������ ã�� �� �����ϴ�! StateHash: {animStateHash}");
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

        Debug.LogError($"stateHash ������ ã�� �� �����ϴ�! StateHash: {animStateHash}");

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

    private void Start()
    {
        // mAnimator.runtimeAnimatorController.animationClips
    }

    private void FixedUpdate()
    {
        onAnimatorFixedUpdate?.Invoke();
    }

    // �� �Լ��� ������ ���� Animator�� ��� �޶����� �� Ȯ�� �ʿ�
    // �� �Լ��� ������ RootMotion�� ���� ��� �Ǵ� �� ����
    // ��꿡 ������ ������ ���ܵ� �ʿ䰡 ����
    private void OnAnimatorMove()
    {
        onAnimatorMove?.Invoke();

        if (IsInTransition(0))
        {
            AnimatorTransitionInfo transitionInfo = GetTransitionInfo(0);
            AnimatorStateInfo nextStateInfo = GetNextStateInfo(0);

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

        AnimatorStateInfo currentStateInfo = GetCurrentStateInfo(0);

        if (!IsInTransition(0) && currentStateInfo.fullPathHash != 0)
            mLogicalCurrentAnimStateHash = currentStateInfo.fullPathHash;

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
    private void setFloat(int hash, float value)
    {
        if (UsesControllerPlayable)
            mControllerPlayable.SetFloat(hash, value);
        else
            mAnimator.SetFloat(hash, value);
    }

    private void setBool(int hash, bool value)
    {
        if (UsesControllerPlayable)
            mControllerPlayable.SetBool(hash, value);
        else
            mAnimator.SetBool(hash, value);
    }

    private void setInteger(int hash, int value)
    {
        if (UsesControllerPlayable)
            mControllerPlayable.SetInteger(hash, value);
        else
            mAnimator.SetInteger(hash, value);
    }

    private void setTrigger(int hash)
    {
        if (UsesControllerPlayable)
            mControllerPlayable.SetTrigger(hash);
        else
            mAnimator.SetTrigger(hash);
    }

    private void resetTrigger(int hash)
    {
        if (UsesControllerPlayable)
            mControllerPlayable.ResetTrigger(hash);
        else
            mAnimator.ResetTrigger(hash);
    }

}
