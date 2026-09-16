using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TurnPlayable : MonoBehaviour
{
    public RootMotionRotationData _rotationData;

    private PlayerController mPlayerController;
    private PlayerAnimation mAnimation;
    private PlayerMovement mMovement;

    private PlayableGraph mGraph;
    private AnimatorControllerPlayable mControllerPlayables;
    private AnimationClipPlayable mTurnPlayable;
    private AnimationMixerPlayable mMixer;

    private bool mStarted = false;
    private Quaternion mStartRotation;
    private Vector3 mStartAngle;
    private Vector3 mTargetAngle;

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
        if(mStarted)
        {
            float normalizedTime = Mathf.Clamp01((float)(mTurnPlayable.GetTime() / _rotationData.sourceClip.length));

            float rotationProgress = _rotationData.normalizedRotation.Evaluate(normalizedTime);

            float angle = 180f * rotationProgress;

            mMovement.SetRotation(mStartRotation * Quaternion.Euler(0f, angle, 0f));
        }
    }

    private void createGraph()
    {
        mGraph = PlayableGraph.Create("CharacterAnimation");

        mGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        mControllerPlayables = AnimatorControllerPlayable.Create(mGraph, mAnimation.Animator.runtimeAnimatorController);

        mTurnPlayable = AnimationClipPlayable.Create(mGraph, _rotationData.sourceClip);

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

        mGraph.Play();
    }

    [ContextMenu("Start Turn")]
    public void StartTurn()
    {
        mStarted = true;

        mTurnPlayable.SetTime(0);

        mMixer.SetInputWeight(0, 0f);
        mMixer.SetInputWeight(1, 1f);

        mStartRotation = mMovement.Rotation;
        mStartAngle = mMovement.Rotation.eulerAngles;
        mTargetAngle = PlayerMovement.DirectionToEulerAngles(mMovement.OppositeDirection);
    }
}
