using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterSound : MonoBehaviour
{
    public bool enableFootStep = true;
    public bool enableHandTouch = true;

    private PlayerController mController;

    public void Initialize(PlayerController playerController)
    {
        mController = playerController;

        mController.Animator.AnimationEventReceiver.onFootStepSmall += onFootStepSmall;
        mController.Animator.AnimationEventReceiver.onFootStepMedium += onFootStepMedium;
        mController.Animator.AnimationEventReceiver.onFootStepBig += onFootStepBig;
        mController.Animator.AnimationEventReceiver.onTouchHand += onTouchHand;
    }

    public void AddFootStepSmallEvent(Action action)
    {
        mController.Animator.AnimationEventReceiver.onFootStepSmall += action;
    }

    public void AddFootStepMediumEvent(Action action)
    {
        mController.Animator.AnimationEventReceiver.onFootStepMedium += action;
    }

    public void AddHandTouchEvent(Action action)
    {
        mController.Animator.AnimationEventReceiver.onTouchHand += action;
    }

    public void RemoveFootStepSmallEvent(Action action)
    {
        mController.Animator.AnimationEventReceiver.onFootStepSmall -= action;
    }

    public void RemoveFootStepMediumEvent(Action action)
    {
        mController.Animator.AnimationEventReceiver.onFootStepMedium -= action;
    }

    public void RemoveHandTouchEvent(Action action)
    {
        mController.Animator.AnimationEventReceiver.onTouchHand -= action;
    }

    public void PlayRandomClothSound()
    {
        int randomNum = UnityEngine.Random.Range(0, 3) + 1;
        float randomVolume = UnityEngine.Random.Range(.3f, .8f);

        AudioManager.instance.PlayOneShot($"Cloth{randomNum}", randomVolume);
    }

    private void Awake()
    {
        // mController = GetComponent<PlayerController>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        //mController.Animator.AnimationEventReceiver.onFootStepSmall += onFootStepSmall;
        //mController.Animator.AnimationEventReceiver.onFootStepMedium += onFootStepMedium;
        //mController.Animator.AnimationEventReceiver.onTouchHand += onTouchHand;
    }

    private void onFootStepSmall()
    {
        if(!enableFootStep)
            return;

        if (mController.Movement.Ground == null)
        {
            Debug.LogError("Ground is null. Cannot play footstep sound.");
            return;
        }

        // mController.Movement.Ground.PlayFootStepSound(volume:.5f);
        mController.Movement.Ground.PlayFootStepSound();
    }

    private void onFootStepMedium()
    {
        if(!enableFootStep)
            return;

        if (mController.Movement.Ground == null)
        {
            Debug.LogError("Ground is null. Cannot play footstep sound.");
            return;
        }

        // mController.Movement.Ground.PlayFootStepSound();
        mController.Movement.Ground.PlayFootStepBigSound(volume:.5f);
    }

    private void onFootStepBig()
    {
        if(!enableFootStep)
            return;

        if (mController.Movement.Ground == null)
        {
            Debug.LogError("Ground is null. Cannot play footstep sound.");
            return;
        }

        mController.Movement.Ground.PlayFootStepBigSound();
    }

    private void onTouchHand()
    {
        if(!enableHandTouch)
            return;

        if (mController.Movement.Ground == null)
        {
            Debug.LogError("Ground is null. Cannot play handTouch sound.");
            return;
        }

        mController.Movement.Ground.PlayHandTouchSound();
    }
}
