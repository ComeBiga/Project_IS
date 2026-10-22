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

    [SerializeField]
    private TurnAnimationData _turnAnimationData;

    [SerializeField]
    private float _blendDuration = .05f;

    private PlayerAnimation mAnimation;

    private PlayableGraph mGraph;
    private AnimatorControllerPlayable mControllerPlayables;
    private AnimationMixerPlayable mMixer;

    private float mBlendTimer = 0f;
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
        mTurnClipDic[mSelectedClipPort] = turnClip;

        GameDebug.Log($"clip Timer: {turnClip.timer}, startTime: {startTime}",
            tag: "Reset Clip");
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

    public void StartClip()
    {
        mbTurnExitStarted = false;
        mbTurnPlaybackFinished = false;
        mbNeedsControllerBlend = false;

        beginBlend(mSelectedClipPort, EClipState.BlendIn);

        TurnClip toTurnClip = getTurnClipPlayable(mToPort);
        toTurnClip.clipPlayable.SetTime(toTurnClip.timer);
        mTurnClipDic[mToPort] = toTurnClip;
    }

    public void ResetBlend()
    {
        mBlendTimer = 0f;
    }

    public void Evaluate(float deltaTime)
    {
        foreach (KeyValuePair<int, TurnClip> pair in mTurnClipDic)
        {
            TurnClip turnClip = pair.Value;
            float length = turnClip.clipPlayable.GetAnimationClip().length;
            float sampleTime = Mathf.Min(turnClip.timer, length);

            if (mbNeedsControllerBlend && pair.Key == mSelectedClipPort)
                sampleTime = Mathf.Min(sampleTime, Mathf.Max(0f, length - _blendDuration));

            turnClip.clipPlayable.SetTime(sampleTime);
        }

        mGraph.Evaluate(deltaTime);
        mAnimation.NotifyControllerEvaluated(mControllerPlayables);

        GameDebug.Log($"fromPortTime({mFromPort}): {GetTurnClipTime(mFromPort)}, toPortTime({mToPort}): {GetTurnClipTime(mToPort)}, Controller Time: {mControllerPlayables.GetTime()}",
            tag: "Evaluate");
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

    public float GetControllerTime()
    {
        return (float)mControllerPlayables.GetTime();
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayerController controller = GetComponentInParent<PlayerController>();
        mAnimation = controller.Animation;

        createGraph();
    }

    private void FixedUpdate()
    {
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

        GameDebug.Log($"Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)} / {mMixer.GetInputWeight(2)} / {mMixer.GetInputWeight(3)} / {mMixer.GetInputWeight(4)}, from/to: {mFromPort}/{mToPort}",
            tag: "Playable Mixer Weight", level: GameDebug.LogLevel.Verbose);
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
    }

    private void finished()
    {
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


        mMixer = AnimationMixerPlayable.Create(mGraph, 7);

        mGraph.Connect(mControllerPlayables, 0, mMixer, 0);

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
        newTurnClip.timer = 0f;

        mTurnClipDic.Add(portNumber, newTurnClip);

        return newTurnClip;
    }

    private TurnClip getTurnClipPlayable(int portNumber)
    {
        // return (AnimationClipPlayable)mMixer.GetInput(portNumber);
        mTurnClipDic.TryGetValue(portNumber, out TurnClip turnClip);

        return turnClip;
    }
}
