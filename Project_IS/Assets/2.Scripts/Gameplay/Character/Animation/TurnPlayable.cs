using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TurnPlayable : MonoBehaviour
{
    public AnimatorControllerPlayable ControllerPlayable => mControllerPlayables;
    public bool StartExit => mStartExit;

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

    private AnimationClipPlayable mAcpIdleTurnR;
    private AnimationClipPlayable mAcpRunTurnR;
    private AnimationClipPlayable mAcpIdleTurnToIdleR;

    private bool mStarted = false;
    private Quaternion mStartRotation;
    private Vector3 mStartAngle;
    private Vector3 mTargetAngle;
    private float mGraphTimer = 0f;
    private float mLastGraphTime = 0f;
    private float mClipTimer = 0f;
    private bool mStartExit = false;

    public void ChangeClip(AnimationClipPlayable animationClipPlayable)
    {
        mGraph.Disconnect(mMixer, 1);
        mGraph.Connect(animationClipPlayable, 0, mMixer, 1);
    }

    public void ChangeClip(PlayerTurnState.ETurnType turnType, RotationHandler.ERotationDirection rotationDirection)
    {
        switch(turnType)
        {
            case PlayerTurnState.ETurnType.Idle:
                ChangeClip(mAcpIdleTurnR);
                break;
            case PlayerTurnState.ETurnType.Run:
                ChangeClip(mAcpRunTurnR);
                break;
            default:
                Debug.LogWarning($"No clip found for turn type: {turnType}");
                break;
        }
    }

    public void SetMixerWeight(float weight)
    {
        weight = Mathf.Clamp01(weight);

        mMixer.SetInputWeight(0, 1 - weight);
        mMixer.SetInputWeight(1, weight);
    }

    public void StartClip()
    {
        mClipTimer = 0f;
        mMixer.GetInput(1).SetTime(0f);
    }

    public bool BlendToTurn()
    {
        float turnTime = (float)mTurnPlayable.GetTime();
        float t = turnTime / _blendDuration;
        float blendWeight = Mathf.Clamp01(t);

        float clampedWeight = Mathf.Clamp01(blendWeight);

        mMixer.SetInputWeight(0, 1 - clampedWeight);
        mMixer.SetInputWeight(1, clampedWeight);

        return !(turnTime > _blendDuration);
    }

    public bool BlendToAnimatorController()
    {
        float turnTime = (float)mTurnPlayable.GetTime();
        float t = (turnTime - (_rotationData.sourceClip.length - _blendDuration)) / _blendDuration;
        float blendWeight = Mathf.Clamp01(t);

        float clampedWeight = Mathf.Clamp01(blendWeight);

        mMixer.SetInputWeight(0, clampedWeight);
        mMixer.SetInputWeight(1, 1 - clampedWeight);

        return !(t > 1f);
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
        // if(mStarted)
        {
            //BlendToTurn();

            //float normalizedTime = Mathf.Clamp01((float)(mTurnPlayable.GetTime() / _rotationData.sourceClip.length));

            //float rotationProgress = _rotationData.normalizedRotation.Evaluate(normalizedTime);

            //float angle = 180f * rotationProgress;

            //Quaternion newRotation = mStartRotation * Quaternion.Euler(0f, angle, 0f);
            //mMovement.SetRotation(newRotation);

            // mGraph.Evaluate(Time.fixedDeltaTime);
            mClipTimer += Time.fixedDeltaTime;

            // Debug.Log($"New Rotation: {newRotation.eulerAngles}, Turn Clip Timer: {mTurnPlayable.GetTime()}, Mixer Weight: {mMixer.GetInputWeight(0)} / {mMixer.GetInputWeight(1)}, PlayState: {mTurnPlayable.GetPlayState()}");
        }

        // mGraph.Evaluate(Time.fixedDeltaTime);
    }

    private void Update()
    {
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
        mGraph.Evaluate(0f);
        mLastGraphTime = mGraphTimer;

        Debug.Log((float)mControllerPlayables.GetTime());

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            StartClip();
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SetMixerWeight(1f);
        }

        if(Input.GetKeyDown(KeyCode.Alpha9))
        {
            ChangeClip(mAcpIdleTurnR);
        }

        if(Input.GetKeyDown(KeyCode.Alpha0))
        {
            ChangeClip(mAcpRunTurnR);
        }
    }

    private void createGraph()
    {
        mGraph = PlayableGraph.Create("CharacterAnimation");

        mGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        mControllerPlayables = AnimatorControllerPlayable.Create(mGraph, mAnimation.Animator.runtimeAnimatorController);

        mTurnPlayable = AnimationClipPlayable.Create(mGraph, _rotationData.sourceClip);
        mAcpIdleTurnR = AnimationClipPlayable.Create(mGraph, _turnAnimationData.idleTurnRAnimation.sourceClip);
        mAcpRunTurnR = AnimationClipPlayable.Create(mGraph, _turnAnimationData.runTurnRAnimation.sourceClip);
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

        mMixer = AnimationMixerPlayable.Create(mGraph, 2);

        mGraph.Connect(mControllerPlayables, 0, mMixer, 0);

        mGraph.Connect(mTurnPlayable, 0, mMixer, 1);

        mMixer.SetInputWeight(0, 1f);
        mMixer.SetInputWeight(1, 0f);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(mGraph, "Animation", mAnimation.Animator);

        output.SetSourcePlayable(mMixer);

        // mGraph.Play();
    }

    [ContextMenu("Start Turn")]
    public void StartTurn()
    {
        mStarted = true;
        mClipTimer = 0f;

        // mControllerPlayables.Play(AnimStateHash.stateHashes[AnimState.Run]);
        mTurnPlayable.SetTime(mClipTimer);

        //mMixer.SetInputWeight(0, 0f);
        //mMixer.SetInputWeight(1, 1f);

        mStartRotation = mMovement.Rotation;
        mStartAngle = mMovement.Rotation.eulerAngles;
        mTargetAngle = PlayerMovement.DirectionToEulerAngles(mMovement.OppositeDirection);
    }
}
