using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static PlayerMovement;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
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

    //// Animation State Name Hashes
    //private readonly int IdleLandingHash = Animator.StringToHash("IdleLanding");
    //private readonly int IdleSoftLandingHash = Animator.StringToHash("IdleSoftLanding");
    //private readonly int IdleMediumLandingHash = Animator.StringToHash("IdleMediumLanding");
    //private readonly int IdleHeavyLandingHash = Animator.StringToHash("IdleHeavyLanding");
    //private readonly int RunningLandingHash = Animator.StringToHash("RunningLanding");
    //private readonly int RunningSoftLandingHash = Animator.StringToHash("RunningSoftLanding");
    //private readonly int RunningMediumLandingHash = Animator.StringToHash("RunningMediumLanding");
    //private readonly int ClimbLedgeKneeHash = Animator.StringToHash("ClimbLedge_Knee Critical");
    //private readonly int ClimbLedgeStomachHash = Animator.StringToHash("ClimbLedge_Stomach Critical");
    //private readonly int ClimbLedgeChestHash = Animator.StringToHash("ClimbLedge_Chest Critical");
    //private readonly int ClimbLedgeOverHeadHash = Animator.StringToHash("ClimbLedge_OverHead Critical");
    //private readonly int IdleTurnLHash = Animator.StringToHash("Base Layer.Turn.IdleTurn StateMachine.IdleTurn");
    //private readonly int IdleTurnRHash = Animator.StringToHash("Base Layer.Turn.IdleTurn StateMachine.IdleTurn_R");
    //private readonly int RunTurnLHash = Animator.StringToHash("Base Layer.Turn.RunTurn.RunTurn");
    //private readonly int RunTurnRHash = Animator.StringToHash("Base Layer.Turn.RunTurn.RunTurn_R");
    //private readonly int IdleJumpHash = Animator.StringToHash("Base Layer.Jump.IdleJump");
    //private readonly int RunJumpHash = Animator.StringToHash("Base Layer.Jump.RunJump Blend Tree");
    //private readonly int LandingIdleSoftHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Idle.Landing_Idle_Soft");
    //private readonly int LandingIdleMediumHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Idle.Landing_Idle_Medium");
    //private readonly int LandingIdleHeavyHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Idle.Landing_Idle_Heavy");
    //private readonly int LandingRunningSoftHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Running.Landing_Running_Soft");
    //private readonly int LandingRunningMediumHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Running.Landing_Running_Medium");
    //private readonly int LandingRunningHeavyHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Running.Landing_Running_Heavy");
    //private readonly int FallFromRunHash = Animator.StringToHash("Base Layer.Fall-Landing.Fall_FromRun");
    //private readonly int FallFromJumpHash = Animator.StringToHash("Base Layer.Fall-Landing.Fall_FromJump");
    //private readonly int IdleToRunNameHash = Animator.StringToHash("Base Layer.Run.IdleToRun");
    //private readonly int RunNameHash = Animator.StringToHash("Base Layer.Run.Run");
    //private readonly int RunToIdleLNameHash = Animator.StringToHash("Base Layer.Run.RunToIdle_L");
    //private readonly int RunToIdleRNameHash = Animator.StringToHash("Base Layer.Run.RunToIdle_R");
    //private readonly int IdleNameHash = Animator.StringToHash("Base Layer.Idle");
    //private readonly int ClimbLedgeHangingNameHash = Animator.StringToHash("Base Layer.Climb Ledge.ClimbLedge_OverHead_Hanging");
    //private readonly int ClimbLedgeDirectlyNameHash = Animator.StringToHash("Base Layer.Climb Ledge.ClimbLedge_Directly_Critical");

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
        AnimState currentState = AnimState.Idle;
        int stateHash = -1;
        
        foreach(KeyValuePair<AnimState, int> pair in AnimStateHash.stateHashes)
        {
            // if(pair.Value == currentStateInfo.fullPathHash)
            if(pair.Value == currentAnimStateHash)
            {
                currentState = pair.Key;
                stateHash = pair.Value;
                break;
            }
        }

        if(stateHash == -1)
        {
            // Debug.LogError($"stateHash 정보를 찾을 수 없습니다! currentStateHash: {currentStateInfo.fullPathHash}");
            Debug.LogError($"stateHash 정보를 찾을 수 없습니다! currentStateHash: {currentAnimStateHash}");
            return false;
        }

        int nextStateHash = AnimStateHash.stateHashes[nextAnimState];
        mLogicalCurrentAnimStateHash = nextStateHash;

        if(_transitionTable.TryGet(currentState, nextAnimState, out TransitionTable.TransitionData transitionData))
        {
            if (transitionData.fixedDuration)
            {
                mAnimator.CrossFadeInFixedTime(nextStateHash, transitionData.duration, 0, transitionData.offset);
            }
            else
            {
                mAnimator.CrossFade(nextStateHash, transitionData.duration, 0, transitionData.offset);
            }
            
            if(transitionData.anyFrom)
                GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}] by AnyState", category: GameDebug.LogCategory.Animation);
            else
                GameDebug.Log($"Enforced Transition from [{currentState}] to [{nextAnimState}]", category: GameDebug.LogCategory.Animation);

            return true;
        }
        else
        {
            Debug.LogError($"Enforced Transition Error - stateHash의 transitionData를 찾을 수 없습니다! currentState: {currentState}({stateHash}), nextState: {nextAnimState}({nextStateHash})");
            return false;
        }
    }

    //public void CrossFadeRunToIdle(bool leftFoot)
    //{
    //    int stateHash = leftFoot ? RunToIdleLNameHash : RunToIdleRNameHash;

    //    mAnimator.CrossFadeInFixedTime(stateHash, .05f, 0, 0f);
    //}

    //public void CrossFadeTurn(bool isRunning, bool turnLeft)
    //{
    //    int stateHash = 0;

    //    if(isRunning)
    //    {
    //        stateHash = turnLeft ? RunTurnLHash : RunTurnRHash;
    //    }
    //    else
    //    {
    //        stateHash = turnLeft ? IdleTurnLHash : IdleTurnRHash;
    //    }

    //    mAnimator.CrossFadeInFixedTime(stateHash, .05f, 0, 0f);
    //}

    //public void CrossFadeJump(bool isRunning)
    //{
    //    int stateHash = isRunning ? RunJumpHash : IdleJumpHash;

    //    mAnimator.CrossFadeInFixedTime(stateHash, .05f, 0, 0f);
    //}

    //public void CrossFadeLanding(bool isRunning, PlayerLandingState.ELandingType landingType)
    //{
    //    int stateHash = 0;

    //    if (isRunning)
    //    {
    //        switch(landingType)
    //        {
    //            case PlayerLandingState.ELandingType.Soft:
    //                stateHash = LandingRunningSoftHash;
    //                break;
    //            case PlayerLandingState.ELandingType.Medium:
    //                stateHash = LandingRunningMediumHash;
    //                break;
    //            case PlayerLandingState.ELandingType.Heavy:
    //                stateHash = LandingRunningHeavyHash;
    //                break;
    //        }
    //    }
    //    else
    //    {
    //        switch (landingType)
    //        {
    //            case PlayerLandingState.ELandingType.Soft:
    //                stateHash = LandingIdleSoftHash;
    //                break;
    //            case PlayerLandingState.ELandingType.Medium:
    //                stateHash = LandingIdleMediumHash;
    //                break;
    //            case PlayerLandingState.ELandingType.Heavy:
    //                stateHash = LandingIdleHeavyHash;
    //                break;
    //        }
    //    }

    //    mAnimator.CrossFadeInFixedTime(stateHash, .05f, 0, 0f);
    //}

    //public void CrossFadeFall(bool fromJump)
    //{
    //    int stateHash = fromJump ? FallFromJumpHash : FallFromRunHash;
    //    float transitionDuration = fromJump ? .05f : .25f;

    //    mAnimator.CrossFadeInFixedTime(stateHash, transitionDuration, 0, 0f);
    //}

    //public void CrossFadeClimbLedge(bool hanging)
    //{
    //    int stateHash = hanging ? ClimbLedgeHangingNameHash : ClimbLedgeDirectlyNameHash;

    //    mAnimator.CrossFadeInFixedTime(stateHash, .2f, 0, 0f);
    //}

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

    //private Dictionary<int, string> mStateHashToName = new Dictionary<int, string>();
    //private readonly int IdleTurnStateHash = Animator.StringToHash("Base Layer.Turn.IdleTurn StateMachine.IdleTurn");
    //private readonly int IdleTurnRStateHash = Animator.StringToHash("Base Layer.Turn.IdleTurn StateMachine.IdleTurn_R");
    //private readonly int RunTurnStateHash = Animator.StringToHash("Base Layer.Turn.RunTurn.RunTurn");
    //private readonly int RunTurnRStateHash = Animator.StringToHash("Base Layer.Turn.RunTurn.RunTurn_R");
    //private readonly int RunToIdleLStateHash = Animator.StringToHash("Base Layer.Run.RunToIdle_L");
    //private readonly int RunToIdleRStateHash = Animator.StringToHash("Base Layer.Run.RunToIdle_R");
    //private readonly int RunStateHash = Animator.StringToHash("Base Layer.Run.Run");
    //private readonly int IdleToRunStateHash = Animator.StringToHash("Base Layer.Run.IdleToRun");
    //private readonly int IdleStateHash = Animator.StringToHash("Base Layer.Idle");
    //private readonly int IdleJumpStateHash = Animator.StringToHash("Base Layer.Jump.IdleJump");
    //private readonly int RunJumpStateHash = Animator.StringToHash("Base Layer.Jump.RunJump Blend Tree");
    //private readonly int FallStartStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Fall_Start");
    //private readonly int FallLoopStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Fall_Loop");
    //private readonly int FallFromRunStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Fall_FromRun");
    //private readonly int FallFromJumpStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Fall_FromJump");
    //private readonly int LandingRunningSoftStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Running.Landing_Running_Soft");
    //private readonly int LandingIdleSoftStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Idle.Landing_Idle_Soft");
    //private readonly int LandingRunningMediumStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Running.Landing_Running_Medium");
    //private readonly int LandingIdleMediumStateHash = Animator.StringToHash("Base Layer.Fall-Landing.Landing_Idle.Landing_Idle_Medium");

    private void Awake()
    {
        mAnimator = GetComponent<Animator>();
        _transitionTable.Initialize();

        //mStateHashToName.Add(IdleTurnStateHash, "IdleTurn");
        //mStateHashToName.Add(IdleTurnRStateHash, "IdleTurn_R");
        //mStateHashToName.Add(RunTurnStateHash, "RunTurn");
        //mStateHashToName.Add(RunTurnRStateHash, "RunTurn_R");
        //mStateHashToName.Add(RunToIdleLStateHash, "RunToIdle_L");
        //mStateHashToName.Add(RunToIdleRStateHash, "RunToIdle_R");
        //mStateHashToName.Add(RunStateHash, "Run");
        //mStateHashToName.Add(IdleToRunStateHash, "IdleToRun");
        //mStateHashToName.Add(IdleStateHash, "Idle");
        //mStateHashToName.Add(IdleJumpStateHash, "IdleJump");
        //mStateHashToName.Add(RunJumpStateHash, "RunJump");
        //mStateHashToName.Add(FallStartStateHash, "Fall Start");
        //mStateHashToName.Add(FallLoopStateHash, "Fall Loop");
        //mStateHashToName.Add(FallFromRunStateHash, "Fall From Run");
        //mStateHashToName.Add(FallFromJumpStateHash, "Fall From Jump");
        //mStateHashToName.Add(LandingRunningSoftStateHash, "Landing Running Soft");
        //mStateHashToName.Add(LandingIdleSoftStateHash, "Landing Idle Soft");
        //mStateHashToName.Add(LandingRunningMediumStateHash, "Landing Running Medium");
        //mStateHashToName.Add(LandingIdleMediumStateHash, "Landing Idle Medium");

    }

    private void FixedUpdate()
    {
        onAnimatorFixedUpdate?.Invoke();
    }

    private void Update()
    {
        if(_trasitionLog)
        {
            //if(mAnimator.IsInTransition(0))
            //{
            //    AnimatorTransitionInfo transitionInfo = mAnimator.GetAnimatorTransitionInfo(0);
            //    AnimatorStateInfo currentStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
            //    AnimatorStateInfo nextStateInfo = mAnimator.GetNextAnimatorStateInfo(0);
            //    string currentStateName = GetStateName(currentStateInfo.fullPathHash);
            //    string nextStateName = GetStateName(nextStateInfo.fullPathHash);
            //    // Debug.Log($"Enter transition: {transitionInfo.fullPathHash}, AnyState:{transitionInfo.anyState}, IsTurn: {currentStateInfo.IsTag("Turn")}, IsRunToIdle: {currentStateInfo.IsTag("RunToIdle")}");
            //    Debug.Log($"Enter transition: {transitionInfo.fullPathHash}, Transition Duration: {transitionInfo.duration}, Current State: {currentStateName}, Next State: {nextStateName}");
            //    Debug.Log($"TurnL: {mAnimator.GetBool(TurnLHash)}, TurnR: {mAnimator.GetBool(TurnRHash)}");
            //}
        }
    }

    //private string GetStateName(int stateHash)
    //{
    //    if (mStateHashToName.TryGetValue(stateHash, out string stateName))
    //    {
    //        return stateName;
    //    }
    //    else
    //    {
    //        return $"Unknown State: {stateHash}";
    //    }
    //}

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
                category: GameDebug.LogCategory.Animation);
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
