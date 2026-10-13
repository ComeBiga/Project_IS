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
    private EClipState mClipState = EClipState.Rest;
    private Dictionary<int, TurnClip> mTurnClipDic = new Dictionary<int, TurnClip>(5);

    public void ResetClip(float normalizedTime = 0f)
    {
        var turnClip = getTurnClipPlayable(mToPort);
        turnClip.timer = turnClip.clipPlayable.GetAnimationClip().length * normalizedTime;
        // turnClip.clipPlayable.SetTime(turnClip.timer);
        mTurnClipDic[mToPort] = turnClip;

        GameDebug.Log($"clip Timer: {turnClip.timer}, normalizedTime: {normalizedTime}",
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
        SetMixerWeight(mFromPort, mToPort, 1f);

        mFromPort = mToPort;
        mToPort = port;
        //mClipPort = port;
    }

    public void ChangeClip(PlayerTurnState.ETurnType turnType, RotationHandler.ERotationDirection rotationDirection)
    {
        switch(turnType)
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
        mClipState = EClipState.BlendIn;
        ResetBlend();

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
        //bool bBlendingToTurn = BlendToTurn();
        //bool bBlendingToController = BlendToAnimatorController();

        float controllerSetTime = (float)mControllerPlayables.GetTime() + deltaTime;
        mControllerPlayables.SetTime(controllerSetTime);

        //AnimationClipPlayable turnClip = getTurnClipPlayable();
        //float turnSetTime = turnClip.GetAnimationClip().length * normalizedTime;
        //turnClip.SetTime(turnSetTime);
        foreach(KeyValuePair<int, TurnClip> pair in mTurnClipDic)
        {
            TurnClip turnClip = pair.Value;
            turnClip.clipPlayable.SetTime(turnClip.timer);
        }

        mGraph.Evaluate(0f);

        GameDebug.Log($"fromPortTime({mFromPort}): {GetTurnClipTime(mFromPort)}, toPortTime({mToPort}): {GetTurnClipTime(mToPort)}, Controller Time: {controllerSetTime}",
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

        for(int i = 0; i <= 5; ++i)
        {
            if(mTurnClipDic.TryGetValue(i, out TurnClip turnClip))
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

    private void rest()
    {

    }

    private void blendIn()
    {
        evaluateTurnClip();

        if(!Blend(mFromPort, mToPort))
        {
            mClipState = EClipState.Playing;
            SetControllerState(AnimStateHash.stateHashes[AnimState.Run], 0, .75f);

            SetMixerWeight(mFromPort, mToPort, 1f);
        }
        //if(!BlendToTurn())
        //{
        //    mClipState = EClipState.Playing;
        //    SetControllerState(AnimStateHash.stateHashes[AnimState.Run], 0, .75f);
        //}
    }

    private void playing()
    {
        evaluateTurnClip();

        // SetMixerWeight(mFromPort, mToPort, 1f);
        float time = (float)getTurnClipPlayable(mToPort).clipPlayable.GetTime();
        float length = getTurnClipPlayable(mToPort).clipPlayable.GetAnimationClip().length;

        if (time > length - _blendDuration)
        {
            mClipState = EClipState.BlendOut;

            ChangePort(0);
            ResetBlend();
        }
    }

    private void blendOut()
    {
        evaluateTurnClip();

        if(!Blend(mFromPort, mToPort))
        {
            mClipState = EClipState.Finished;

            SetMixerWeight(mFromPort, mToPort, 1f);
        }

        //if(!BlendToAnimatorController())
        //{
        //    mClipState = EClipState.Finished;
        //}
    }

    private void finished()
    {
        // SetMixerWeight(0f);

        mClipState = EClipState.Rest;
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

        mMixer = AnimationMixerPlayable.Create(mGraph, 5);

        mGraph.Connect(mControllerPlayables, 0, mMixer, 0);

        //mGraph.Connect(mAcpIdleTurnL, 0, mMixer, 1);
        //mGraph.Connect(mAcpIdleTurnR, 0, mMixer, 2);
        //mGraph.Connect(mAcpRunTurnL, 0, mMixer, 3);
        //mGraph.Connect(mAcpRunTurnR, 0, mMixer, 4);
        registerTurnClip(_turnAnimationData.idleTurnLAnimation.sourceClip, 1);
        registerTurnClip(_turnAnimationData.idleTurnRAnimation.sourceClip, 2);
        registerTurnClip(_turnAnimationData.runTurnLAnimation.sourceClip, 3);
        registerTurnClip(_turnAnimationData.runTurnRAnimation.sourceClip, 4);

        mMixer.SetInputWeight(0, 1f);
        mMixer.SetInputWeight(1, 0f);
        mMixer.SetInputWeight(2, 0f);
        mMixer.SetInputWeight(3, 0f);
        mMixer.SetInputWeight(4, 0f);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(mGraph, "Animation", mAnimation.Animator);

        output.SetSourcePlayable(mMixer);

        // mGraph.Play();
    }

    private TurnClip registerTurnClip(AnimationClip sourceClip, int portNumber)
    {
        var newClipPlayable = AnimationClipPlayable.Create(mGraph, sourceClip);
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
