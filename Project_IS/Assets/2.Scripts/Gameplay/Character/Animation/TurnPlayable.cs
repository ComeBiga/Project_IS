using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TurnPlayable : MonoBehaviour
{
    public struct TurnClip
    {
        public AnimationClipPlayable clipPlayable;
        public int portNumber;
        public float timer;
    }

    public enum EClipState
    {
        Rest,
        BlendIn,
        Playing,
        BlendOut,
        Finished
    }

    public AnimatorControllerPlayable ControllerPlayable => mControllerPlayables;
    public bool StartExit => mStartExit;
    public float BlendDuration => _blendDuration;
    public EClipState ClipState => mClipState;
    public bool IsTurnExitStarted => mbTurnExitStarted;
    public bool IsTurnPlaybackFinished => mbTurnPlaybackFinished;
    public bool NeedsControllerBlend => mbNeedsControllerBlend;
    public float ControllerStartNormalizedTime => mControllerStartNormalizedTime;

    public bool HasTurnOutput   // TurnPlayable이 ControllerPlayable로 Blend 될 수 있는지 판단
    {
        get
        {
            if (!mGraph.IsValid())
                return false;

            if (mbNeedsControllerBlend || mClipState == EClipState.BlendIn || mClipState == EClipState.Playing)
                return true;

            for (int i = 1; i < mMixer.GetInputCount(); ++i)
            {
                if (mMixer.GetInputWeight(i) > 0f)
                    return true;
            }

            return false;
        }
    }

    public RootMotionRotationData _rotationData;

    [SerializeField]
    private TurnAnimationData _turnAnimationData;

    [SerializeField]
    private float _blendDuration = .05f;

    private PlayerController mPlayerController;
    private PlayerAnimation mAnimation;
    private PlayerMovement mMovement;

    private PlayableGraph mGraph;
    private AnimatorControllerPlayable mControllerPlayables;
    private AnimationClipPlayable mTurnPlayable;
    private AnimationMixerPlayable mMixer;

    private AnimationClipPlayable mAcpIdleTurnL;
    private AnimationClipPlayable mAcpIdleTurnR;
    private AnimationClipPlayable mAcpRunTurnL;
    private AnimationClipPlayable mAcpRunTurnR;
    private AnimationClipPlayable mAcpIdleTurnToIdleR;

    private bool mStarted = false;
    private bool mbBlending = false;
    private Quaternion mStartRotation;
    private Vector3 mStartAngle;
    private Vector3 mTargetAngle;
    private float mGraphTimer = 0f;
    private float mLastGraphTime = 0f;
    private float mBlendTimer = 0f;
    private float mClipTimer = 0f;
    private bool mStartExit = false;
    [Obsolete] private int mClipPort = 1;
    private int mFromPort = 0;
    private int mToPort = 0;
    private int mSelectedClipPort = 1;
    private readonly float[] mBlendStartWeights = new float[7];
    private bool mbTurnExitStarted;
    private bool mbTurnPlaybackFinished;
    private bool mbNeedsControllerBlend;
    private float mControllerStartNormalizedTime;
    private EClipState mClipState = EClipState.Rest;
    private int mControlledClipPort = -1;   // TurnPlayable 외부에서 제어되고있는 Port
    private int mLastControlledClipPort = -1;
    private double mLastControlledFixedTime = double.NegativeInfinity;
    private Dictionary<int, TurnClip> mTurnClipDic = new Dictionary<int, TurnClip>(5);

    public void SetClipTime(float time)
    {
        if (mControlledClipPort < 0)
            mControlledClipPort = mSelectedClipPort;

        var turnClip = getTurnClipPlayable(mControlledClipPort);
        turnClip.timer = time;
        mTurnClipDic[mControlledClipPort] = turnClip;
        mLastControlledClipPort = mControlledClipPort;
        mLastControlledFixedTime = Time.fixedTimeAsDouble;
    }

    /// <summary>
    /// TurnPlayable 외부 제어 해제
    /// </summary>
    public void ReleaseClipTime()
    {
        mControlledClipPort = -1;
    }

    /// <summary>
    /// 현재 클립의 타이머를 리셋
    /// </summary>
    /// <param name="startTime"></param>
    public void ResetClip(float startTime = 0f)
    {
        ReleaseClipTime();

        var turnClip = getTurnClipPlayable(mSelectedClipPort);
        turnClip.timer = startTime;
        // turnClip.clipPlayable.SetTime(turnClip.timer);
        mTurnClipDic[mSelectedClipPort] = turnClip;

        GameDebug.Log($"clip Timer: {turnClip.timer}, startTime: {startTime}",
            tag: "Reset Clip");
    }

    [Obsolete]
    public void ChangeClip(AnimationClipPlayable animationClipPlayable)
    {
        mGraph.Disconnect(mMixer, 1);
        mGraph.Connect(animationClipPlayable, 0, mMixer, 1);
    }

    public void ChangePort(int port)
    {
        mSelectedClipPort = port;
    }

    /// <summary>
    /// ControllerPlayable로 전환
    /// </summary>
    public void InterruptToController()
    {
        ReleaseClipTime();
        BeginControllerBlend();
    }

    public void BeginControllerBlend()
    {
        mbNeedsControllerBlend = false;

        if (!mGraph.IsValid() || mClipState == EClipState.BlendOut)
            return;

        if (HasTurnOutput)
        {
            mbTurnExitStarted = true;
            beginBlend(0, EClipState.BlendOut);
        }
        else
        {
            mClipState = EClipState.Rest;
        }
    }

    public bool TryStartIdleTurnToIdle(
        RotationHandler.ERotationDirection direction,
        float normalizedRotation,
        out float rotationDuration)
    {
        bool isLeft = direction == RotationHandler.ERotationDirection.Left;
        int port = isLeft ? 5 : 6;

        TurnAnimationData.TurnAnimation animation = isLeft ? _turnAnimationData.idleTurnToIdleLAnimation :
            _turnAnimationData.idleTurnToIdleRAnimation;
        rotationDuration = 0f;

        if (animation.sourceClip == null || !mTurnClipDic.ContainsKey(port))
            return false;

        rotationDuration = animation.GetRotationDuration();
        if (rotationDuration <= 0f)
            return false;

        mSelectedClipPort = port;
        mControllerStartNormalizedTime = animation.controllerStartNormalizedTime;
        ResetClip(Mathf.Clamp01(normalizedRotation) * rotationDuration);
        StartClip();

        return true;
    }

    public void ChangeClip(PlayerTurnState.ETurnType turnType, RotationHandler.ERotationDirection rotationDirection)
    {
        mControllerStartNormalizedTime = _turnAnimationData.Get(turnType, rotationDirection).controllerStartNormalizedTime;

        switch (turnType)
        {
            case PlayerTurnState.ETurnType.Idle:
                ChangePort(rotationDirection == RotationHandler.ERotationDirection.Left ? 1 : 2);
                // ChangeClip(rotationDirection == RotationHandler.ERotationDirection.Left ? mAcpIdleTurnL : mAcpIdleTurnR);
                break;
            case PlayerTurnState.ETurnType.Run:
                ChangePort(rotationDirection == RotationHandler.ERotationDirection.Left ? 3 : 4);
                // ChangeClip(rotationDirection == RotationHandler.ERotationDirection.Left ? mAcpRunTurnL : mAcpRunTurnR);
                break;
            default:
                Debug.LogWarning($"No clip found for turn type: {turnType}");
                break;
        }
    }

    [Obsolete]
    public void SetMixerWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);

        mMixer.SetInputWeight(0, 1 - weight);
        mMixer.SetInputWeight(mClipPort, weight);
    }

    public void SetMixerWeight(int fromPort, int toPort, float weight)
    {
        mMixer.SetInputWeight(fromPort, 1 - weight);
        mMixer.SetInputWeight(toPort, weight);
    }

    public void StartClip()
    {
        mbTurnExitStarted = false;
        mbTurnPlaybackFinished = false;
        mbNeedsControllerBlend = false;

        beginBlend(mSelectedClipPort, EClipState.BlendIn);

        mStarted = true;

        // mClipTimer = 0f;

        TurnClip toTurnClip = getTurnClipPlayable(mToPort);
        toTurnClip.clipPlayable.SetTime(toTurnClip.timer);
        mTurnClipDic[mToPort] = toTurnClip;
    }

    [Obsolete]
    public bool BlendToTurn()
    {
        float turnTime = (float)getTurnClipPlayable(mToPort).clipPlayable.GetTime();
        float t = turnTime / _blendDuration;
        float blendWeight = Mathf.Clamp01(t);

        float clampedWeight = Mathf.Clamp01(blendWeight);

        bool isBlending = !(turnTime < 0.01f || turnTime > _blendDuration);

        if (isBlending)
        {
            //mMixer.SetInputWeight(0, 1 - clampedWeight);
            //mMixer.SetInputWeight(mClipPort, clampedWeight);
            SetMixerWeight(0, mToPort, clampedWeight);

            GameDebug.Log($"t: {t}, blendWeight: {blendWeight}, clampedWeight: {clampedWeight}, Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)}, isBlending: {isBlending}",
                tag: "BlendToTurn");
        }

        return isBlending;
    }

    [Obsolete]
    public bool BlendToAnimatorController()
    {
        float turnTime = (float)getTurnClipPlayable(mFromPort).clipPlayable.GetTime();
        float t = (turnTime - (GetTurnClipLength(mFromPort) - _blendDuration)) / _blendDuration;
        float blendWeight = Mathf.Clamp01(t);

        float clampedWeight = Mathf.Clamp01(blendWeight);

        bool isBlending = !(t < 0.01f || t > 1f);

        if (isBlending)
        {
            //mMixer.SetInputWeight(0, clampedWeight);
            //mMixer.SetInputWeight(mClipPort, 1 - clampedWeight);
            SetMixerWeight(0, mToPort, clampedWeight);
         
            GameDebug.Log($"t: {t}, blendWeight: {blendWeight}, clampedWeight: {clampedWeight}, Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)}, isBlending: {isBlending}",
            tag: "BlendToAnimatorController");
        }

        return isBlending;
    }

    //public bool Blend(int fromPort, int toPort)
    //{
    //    if(fromPort == 0)
    //    {
    //        float t = (float)getTurnClipPlayable(toPort).clipPlayable.GetTime() / _blendDuration;
    //        Blend(fromPort, toPort, t);
    //    }
    //    else if(toPort == 0)
    //    {
    //        float turnTime = (float)getTurnClipPlayable(fromPort).clipPlayable.GetTime();
    //        float t = (turnTime - (GetTurnClipLength() - _blendDuration)) / _blendDuration;
    //        Blend(fromPort, toPort, t);
    //    }
    //    else
    //    {
    //    }
    //}

    public bool Blend(int fromPort, int toPort)
    {
        float t = mBlendTimer / _blendDuration;
        float clampedWeight = Mathf.Clamp01(t);

        if (fromPort == toPort)
            clampedWeight = 1f;

        SetMixerWeight(fromPort, toPort, clampedWeight);

        return !(t > 1f);
    }

    public void ResetBlend()
    {
        mBlendTimer = 0f;
    }

    public float EvaluateAngle()
    {
        float normalizedTime = Mathf.Clamp01((float)(mTurnPlayable.GetTime() / _rotationData.sourceClip.length));

        float rotationProgress = _rotationData.normalizedRotation.Evaluate(normalizedTime);

        float angle = 180f * rotationProgress;

        return angle;
    }

    public Vector3 EvaluatePosition()
    {
        float normalizedTime = Mathf.Clamp01((float)(mTurnPlayable.GetTime() / _rotationData.sourceClip.length));

        float evaluatedX = _rotationData.moveXCurve.Evaluate(normalizedTime);
        float evaluatedZ = _rotationData.moveZCurve.Evaluate(normalizedTime);

        Debug.Log(evaluatedX);

        return new Vector3(evaluatedX, 0f, evaluatedZ);
    }

    public void Evaluate(float deltaTime)
    {
        var currentState = mControllerPlayables.GetCurrentAnimatorStateInfo(0);
        GameDebug.Log($"Before Evaluate - DeltaTime: {deltaTime}, ControllerPlayable Time: {mControllerPlayables.GetTime()}",
            tag: "Evaluate Validation");
        GameDebug.Log($"Before Evaluate - Current State: {AnimStateNameLookUp.names[currentState.fullPathHash]}, normalizedTime: {currentState.normalizedTime}, length: {currentState.length}, speed: {currentState.speed}, speedMultiplier: {currentState.speedMultiplier}",
            tag: "Evaluate Validation");

        var transitionState = mControllerPlayables.GetAnimatorTransitionInfo(0);
        var nextState = mControllerPlayables.GetNextAnimatorStateInfo(0);
        if (mControllerPlayables.IsInTransition(0))
            GameDebug.Log($"Before Evaluate - IsInTransition: {mControllerPlayables.IsInTransition(0)}, Next State: {AnimStateNameLookUp.names[nextState.fullPathHash]}, normalizedTime: {nextState.normalizedTime}, transition normalizedTime: {transitionState.normalizedTime}, duration: {transitionState.duration}, durationUnit: {transitionState.durationUnit}",
                tag: "Evaluate Validation");

        GameDebug.Log($"Before Evaluate - Mixer Port 0 Weight: {mMixer.GetInputWeight(0)}, clip State: {mClipState}",
            tag: "Evaluate Validation");

        //bool bBlendingToTurn = BlendToTurn();
        //bool bBlendingToController = BlendToAnimatorController();

        //AnimationClipPlayable turnClip = getTurnClipPlayable();
        //float turnSetTime = turnClip.GetAnimationClip().length * normalizedTime;
        //turnClip.SetTime(turnSetTime);
        foreach (KeyValuePair<int, TurnClip> pair in mTurnClipDic)
        {
            TurnClip turnClip = pair.Value;
            float length = turnClip.clipPlayable.GetAnimationClip().length;
            float sampleTime = Mathf.Min(turnClip.timer, length);

            if (mbNeedsControllerBlend && pair.Key == mSelectedClipPort)
                sampleTime = Mathf.Min(sampleTime, Mathf.Max(0f, length - _blendDuration));

            turnClip.clipPlayable.SetTime(sampleTime);

            GameDebug.Log($"Before Evaluate - Clip port: {pair.Key}, Clip Set Time: {turnClip.timer}",
                tag: "Evaluate Validation");
        }

        mGraph.Evaluate(deltaTime);
        mAnimation.NotifyControllerEvaluated(mControllerPlayables);

        currentState = mControllerPlayables.GetCurrentAnimatorStateInfo(0);
        GameDebug.Log($"After Evaluate - DeltaTime: {deltaTime}, ControllerPlayable Time: {mControllerPlayables.GetTime()}",
            tag: "Evaluate Validation");
        GameDebug.Log($"After Evaluate - Current State: {AnimStateNameLookUp.names[currentState.fullPathHash]}, normalizedTime: {currentState.normalizedTime}, length: {currentState.length}, speed: {currentState.speed}, speedMultiplier: {currentState.speedMultiplier}",
            tag: "Evaluate Validation");

        transitionState = mControllerPlayables.GetAnimatorTransitionInfo(0);
        nextState = mControllerPlayables.GetNextAnimatorStateInfo(0);
        if (mControllerPlayables.IsInTransition(0))
            GameDebug.Log($"After Evaluate - IsInTransition: {mControllerPlayables.IsInTransition(0)}, Next State: {AnimStateNameLookUp.names[nextState.fullPathHash]}, normalizedTime: {nextState.normalizedTime}, transition normalizedTime: {transitionState.normalizedTime}, duration: {transitionState.duration}, durationUnit: {transitionState.durationUnit}",
                tag: "Evaluate Validation");

        GameDebug.Log($"After Evaluate - Mixer Port 0 Weight: {mMixer.GetInputWeight(0)}, clip State: {mClipState}",
            tag: "Evaluate Validation");

        foreach (KeyValuePair<int, TurnClip> pair in mTurnClipDic)
        {
            TurnClip turnClip = pair.Value;

            GameDebug.Log($"After Evaluate - Clip port: {pair.Key}, Clip timer: {turnClip.clipPlayable.GetTime()}",
                tag: "Evaluate Validation");
        }

        GameDebug.Log($"fromPortTime({mFromPort}): {GetTurnClipTime(mFromPort)}, toPortTime({mToPort}): {GetTurnClipTime(mToPort)}, Controller Time: {mControllerPlayables.GetTime()}",
            tag: "Evaluate");
    }

    public void SetControllerState(int stateHash, int layer, float normalizedTime)
    {
        AnimatorStateInfo lastStateInfo = mControllerPlayables.GetCurrentAnimatorStateInfo(layer);
        mControllerPlayables.Play(stateHash, layer, normalizedTime);

        AnimatorStateInfo currentStateInfo = mControllerPlayables.GetCurrentAnimatorStateInfo(layer);

        GameDebug.Log($"Last State: {AnimStateNameLookUp.names[lastStateInfo.fullPathHash]}, To State: {AnimStateNameLookUp.names[stateHash]}",
            tag: "SetControllerState");
    }

    public float GetTurnClipTime(int portNumber)
    {
        if (portNumber <= 0)
            return GetControllerTime();

        return (float)getTurnClipPlayable(portNumber).clipPlayable.GetTime();
    }

    public float GetTurnClipLength(int portNumber)
    {
        if (portNumber <= 0)
            return GetControllerTime();

        return (float)getTurnClipPlayable(portNumber).clipPlayable.GetAnimationClip().length;
    }

    public float GetTurnClipNormalizedTime(int portNumber)
    {
        if (portNumber <= 0)
            return GetControllerTime();

        return Mathf.Clamp01((float)(getTurnClipPlayable(portNumber).clipPlayable.GetTime() / _rotationData.sourceClip.length));
    }

    public float GetControllerTime()
    {
        return (float)mControllerPlayables.GetTime();
    }

    // Start is called before the first frame update
    void Start()
    {
        mPlayerController = GetComponentInParent<PlayerController>();
        mAnimation = mPlayerController.Animation;
        mMovement = mPlayerController.Movement;

        createGraph();
    }

    private void FixedUpdate()
    {
        mGraphTimer += Time.fixedDeltaTime;
        mBlendTimer += Time.fixedDeltaTime;

        for (int i = 0; i < mMixer.GetInputCount(); ++i)
        {
            // A released clip may already have received its time in this fixed step.
            bool updatedThisStep = i == mLastControlledClipPort &&
                mLastControlledFixedTime == Time.fixedTimeAsDouble;

            if (i != mControlledClipPort && !updatedThisStep && mTurnClipDic.TryGetValue(i, out TurnClip turnClip))
            {
                turnClip.timer += Time.fixedDeltaTime;
                mTurnClipDic[i] = turnClip;
            }
        }

        //if(mTurnClipDic.TryGetValue(mFromPort, out TurnClip fromClip))
        //{
        //    fromClip.timer += Time.fixedDeltaTime;
        //    mTurnClipDic[mFromPort] = fromClip;
        //}

        //if (mTurnClipDic.TryGetValue(mToPort, out TurnClip toClip))
        //{
        //    toClip.timer += Time.fixedDeltaTime;
        //    mTurnClipDic[mToPort] = toClip;
        //}

        // if(mStarted)
        {
            //BlendToTurn();

            //float normalizedTime = Mathf.Clamp01((float)(mTurnPlayable.GetTime() / _rotationData.sourceClip.length));

            //float rotationProgress = _rotationData.normalizedRotation.Evaluate(normalizedTime);

            //float angle = 180f * rotationProgress;

            //Quaternion newRotation = mStartRotation * Quaternion.Euler(0f, angle, 0f);
            //mMovement.SetRotation(newRotation);

            // mGraph.Evaluate(Time.fixedDeltaTime);
            // mClipTimer += Time.fixedDeltaTime;

            // Debug.Log($"New Rotation: {newRotation.eulerAngles}, Turn Clip Timer: {mTurnPlayable.GetTime()}, Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)}, PlayState: {mTurnPlayable.GetPlayState()}");
        }

        // mGraph.Evaluate(Time.fixedDeltaTime);
    }

    private void Update()
    {
        switch(mClipState)
        {
            case EClipState.Rest:
                rest();
                break;
            case EClipState.BlendIn:
                blendIn();
                break;
            case EClipState.Playing:
                playing();
                break;
            case EClipState.BlendOut:
                blendOut();
                break;
            case EClipState.Finished:
                finished();
                break;
        }

        //if (mStarted)
        //{
        //    GameDebug.Log($"Turn Clip Timer: {mClipTimer}, Turn Clip Length: {GetTurnClipLength()}",
        //        tag: "Update");

        //    float normalizedTime = mClipTimer / GetTurnClipLength();
        //    Evaluate(normalizedTime, Time.deltaTime);

        //    if((float)getTurnClipPlayable().GetTime() < _blendDuration)
        //    {
        //        BlendToTurn();
        //    }

        //    if ((float)getTurnClipPlayable().GetTime() > GetTurnClipLength() - _blendDuration)
        //    {
        //        BlendToAnimatorController();
        //    }

        //    if(mClipTimer > GetTurnClipLength())
        //    {
        //        mStarted = false;
        //        // mStartExit = true;

        //        GameDebug.Log($"Turn Clip Finished. Starting Exit.",
        //            tag: "Update");
        //    }
        //}

        GameDebug.Log($"Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)} / {mMixer.GetInputWeight(2)} / {mMixer.GetInputWeight(3)} / {mMixer.GetInputWeight(4)}, from/to: {mFromPort}/{mToPort}",
            tag: "Playable Mixer Weight", level: GameDebug.LogLevel.Verbose);


        //if (mStarted)
        //{
        //    if (mTurnTime > _rotationData.sourceClip.length - _blendDuration)
        //        mStartExit = true;

        //    // mControllerPlayables.SetTime((float)mControllerPlayables.GetTime() + Time.deltaTime);
        //    mTurnPlayable.SetTime(mTurnTime);
        //    mGraph.Evaluate(0f);

        //    Debug.Log($"Turn Clip Timer: {mTurnPlayable.GetTime()}, Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)}");

        //    if(mTurnTime > _rotationData.sourceClip.length)
        //    {
        //        mStarted = false;
        //    }

        //    // Debug.Log(mControllerPlayables.GetTime());
        //}
        //else
        //{
        //    mGraph.Evaluate(Time.deltaTime);
        //}

        float graphDeltaTime = mGraphTimer - mLastGraphTime;

        // mControllerPlayables.SetTime((float)mControllerPlayables.GetTime() + graphDeltaTime);
        // mMixer.GetInput(1).SetTime(mClipTimer);

        // mGraph.Evaluate(graphDeltaTime);
        // mGraph.Evaluate(0f);
        mLastGraphTime = mGraphTimer;

        // Debug.Log((float)mControllerPlayables.GetTime());

        //if (Input.GetKeyDown(KeyCode.Alpha7))
        //{
        //    StartClip();
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha8))
        //{
        //    SetMixerWeight(1f);
        //}

        //if(Input.GetKeyDown(KeyCode.Alpha9))
        //{
        //    ChangeClip(mAcpIdleTurnR);
        //}

        //if(Input.GetKeyDown(KeyCode.Alpha0))
        //{
        //    ChangeClip(mAcpRunTurnR);
        //}
    }

    private void OnDestroy()
    {
        if (mAnimation != null)
            mAnimation.UnbindControllerPlayable(mControllerPlayables);

        if (mGraph.IsValid())
            mGraph.Destroy();
    }

    private void beginBlend(int targetPort, EClipState state)
    {
        for (int i = 0; i < mBlendStartWeights.Length; ++i)
            mBlendStartWeights[i] = mMixer.GetInputWeight(i);

        mFromPort = mToPort;
        mToPort = targetPort;
        mClipState = state;

        ResetBlend();
    }

    private bool blendToTarget()
    {
        float t = _blendDuration > 0f ? Mathf.Clamp01(mBlendTimer / _blendDuration) : 1f;

        for (int i = 0; i < mBlendStartWeights.Length; ++i)
        {
            float targetWeight = i == mToPort ? 1f : 0f;
            mMixer.SetInputWeight(i, Mathf.Lerp(mBlendStartWeights[i], targetWeight, t));
        }

        return t < 1f;
    }

    private void rest()
    {
        evaluateTurnClip();

        var currentState = mControllerPlayables.GetCurrentAnimatorStateInfo(0);
        GameDebug.Log($"Rest - ControllerPlayable Time: {mControllerPlayables.GetTime()}",
            tag: "Evaluate Validation");
        GameDebug.Log($"Rest - Current State: {AnimStateNameLookUp.names[currentState.fullPathHash]}, normalizedTime: {currentState.normalizedTime}, length: {currentState.length}, speed: {currentState.speed}, speedMultiplier: {currentState.speedMultiplier}",
            tag: "Evaluate Validation");
    }

    private void blendIn()
    {
        bool bBlending = blendToTarget();

        if (!bBlending)
            mClipState = EClipState.Playing;

        evaluateTurnClip();
    }

    private void playing()
    {
        TurnClip clip = getTurnClipPlayable(mSelectedClipPort);
        float length = clip.clipPlayable.GetAnimationClip().length;

        if (clip.timer >= Mathf.Max(0f, length - _blendDuration))
            mbNeedsControllerBlend = true;

        evaluateTurnClip();
    }

    private void blendOut()
    {
        bool bBlending = blendToTarget();

        if (!bBlending)
        {
            mClipState = EClipState.Finished;
            mbTurnPlaybackFinished = true;
        }

        evaluateTurnClip();

        //if(!BlendToAnimatorController())
        //{
        //    mClipState = EClipState.Finished;
        //}
    }

    private void finished()
    {
        // SetMixerWeight(0f);

        mClipState = EClipState.Rest;
        evaluateTurnClip();
    }

    private void evaluateTurnClip()
    {
        // float normalizedTime = mClipTimer / GetTurnClipLength();
        Evaluate(Time.deltaTime);
    }

    private void createGraph()
    {
        mGraph = PlayableGraph.Create("CharacterAnimation");

        mGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        mControllerPlayables = AnimatorControllerPlayable.Create(mGraph, mAnimation.Animator.runtimeAnimatorController);

        //mTurnPlayable = AnimationClipPlayable.Create(mGraph, _rotationData.sourceClip);
        //mAcpIdleTurnL = AnimationClipPlayable.Create(mGraph, _turnAnimationData.idleTurnLAnimation.sourceClip);
        //mAcpIdleTurnR = AnimationClipPlayable.Create(mGraph, _turnAnimationData.idleTurnRAnimation.sourceClip);
        //mAcpRunTurnL = AnimationClipPlayable.Create(mGraph, _turnAnimationData.runTurnLAnimation.sourceClip);
        //mAcpRunTurnR = AnimationClipPlayable.Create(mGraph, _turnAnimationData.runTurnRAnimation.sourceClip);
        // mAcpIdleTurnR = AnimationClipPlayable.Create(mGraph, _turnAnimationData.idleTurnRAnimation.sourceClip);

        //var animationClips = mAnimator.runtimeAnimatorController.animationClips;
        //AnimationClip turnAnimationClip = null;

        //foreach(AnimationClip clip in animationClips)
        //{
        //    if(clip.name == "Male_Idle_Turn_R")
        //    {
        //        turnAnimationClip = clip;
        //        mTurnPlayable = AnimationClipPlayable.Create(mGraph, clip);
        //        break;
        //    }
        //}

        mMixer = AnimationMixerPlayable.Create(mGraph, 7);

        mGraph.Connect(mControllerPlayables, 0, mMixer, 0);

        //mGraph.Connect(mAcpIdleTurnL, 0, mMixer, 1);
        //mGraph.Connect(mAcpIdleTurnR, 0, mMixer, 2);
        //mGraph.Connect(mAcpRunTurnL, 0, mMixer, 3);
        //mGraph.Connect(mAcpRunTurnR, 0, mMixer, 4);
        registerTurnClip(_turnAnimationData.idleTurnLAnimation.sourceClip, 1);
        registerTurnClip(_turnAnimationData.idleTurnRAnimation.sourceClip, 2);
        registerTurnClip(_turnAnimationData.runTurnLAnimation.sourceClip, 3);
        registerTurnClip(_turnAnimationData.runTurnRAnimation.sourceClip, 4);

        if (_turnAnimationData.idleTurnToIdleLAnimation.sourceClip != null)
            registerTurnClip(_turnAnimationData.idleTurnToIdleLAnimation.sourceClip, 5);

        if (_turnAnimationData.idleTurnToIdleRAnimation.sourceClip != null)
            registerTurnClip(_turnAnimationData.idleTurnToIdleRAnimation.sourceClip, 6);

        mMixer.SetInputWeight(0, 1f);
        mMixer.SetInputWeight(1, 0f);
        mMixer.SetInputWeight(2, 0f);
        mMixer.SetInputWeight(3, 0f);
        mMixer.SetInputWeight(4, 0f);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(mGraph, "Animation", mAnimation.Animator);

        output.SetSourcePlayable(mMixer);
        mAnimation.BindControllerPlayable(mControllerPlayables);

        // mGraph.Play();
    }

    private TurnClip registerTurnClip(AnimationClip sourceClip, int portNumber)
    {
        var newClipPlayable = AnimationClipPlayable.Create(mGraph, sourceClip);

        // Clip time is supplied explicitly; only the controller advances with the graph.
        newClipPlayable.SetSpeed(0d);
        mGraph.Connect(newClipPlayable, 0, mMixer, portNumber);

        var newTurnClip = new TurnClip();
        newTurnClip.clipPlayable = newClipPlayable;
        newTurnClip.portNumber = portNumber;
        newTurnClip.timer = 0f;

        mTurnClipDic.Add(portNumber, newTurnClip);

        return newTurnClip;
    }

    public TurnClip getTurnClipPlayable(int portNumber)
    {
        // return (AnimationClipPlayable)mMixer.GetInput(portNumber);
        mTurnClipDic.TryGetValue(portNumber, out TurnClip turnClip);

        return turnClip;
    }

    //[ContextMenu("Start Turn")]
    //public void StartTurn()
    //{
    //    mStarted = true;
    //    mClipTimer = 0f;

    //    // mControllerPlayables.Play(AnimStateHash.stateHashes[AnimState.Run]);
    //    mTurnPlayable.SetTime(mClipTimer);

    //    //mMixer.SetInputWeight(0, 0f);
    //    //mMixer.SetInputWeight(1, 1f);

    //    mStartRotation = mMovement.Rotation;
    //    mStartAngle = mMovement.Rotation.eulerAngles;
    //    mTargetAngle = PlayerMovement.DirectionToEulerAngles(mMovement.OppositeDirection);
    //}
}
