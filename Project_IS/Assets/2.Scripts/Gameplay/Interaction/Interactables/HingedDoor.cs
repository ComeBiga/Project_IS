using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class HingedDoor : InteractableObject
{
    [SerializeField] private Transform _trPivot;
    [SerializeField] private float _lerpDistance = .39238f;
    [SerializeField] private float _lerpDuration = .2f;

    private PlayerInteractable.InteractedInfo mInteractedInfo;
    private Animator mDoorAnimator;
    private int mDoorPushAnimStateHash = AnimStateHash.stateHashes[AnimState.Door_Push];
    private bool mbLerp = false;
    private float mLerpTimer = 0f;
    private Vector3 mLerpStartPosition;

    public override void Enter(PlayerController playerController)
    {
        initLerp(playerController);

        playerController.Movement.SetVelocity(Vector3.zero);
        playerController.Animation.Play(AnimState.Door_Push);

        mBoxCollider.enabled = false;
    }

    public override void Exit(PlayerController playerController)
    {
        playerController.InputHandler.SetMoveInput(Vector2.zero);
    }

    public override void FixedTick(PlayerController playerController)
    {
        if(mbLerp)
        {
            float targetX = mInteractedInfo.hitInfo.point.x - _lerpDistance;
            Vector3 targetPos = playerController.Movement.Position;
            targetPos.x = targetX;

            if (mLerpTimer > _lerpDuration)
            {
                mbLerp = false;
                mLerpTimer = 0f;
                
                playerController.Movement.SetPosition(targetPos);
                openDoor(playerController);

                return;
            }

            float lerpedX = Mathf.Lerp(mLerpStartPosition.x, targetX, mLerpTimer / _lerpDuration);
            Vector3 lerpedPos = playerController.Movement.Position;
            lerpedPos.x = lerpedX;
            playerController.Movement.SetPosition(lerpedPos);

            mLerpTimer += Time.fixedDeltaTime;

            return;
        }

        AnimatorStateInfo currentStateInfo = playerController.Animation.Animator.GetCurrentAnimatorStateInfo(0);

        if(currentStateInfo.IsName(AnimStateNameLookUp.names[mDoorPushAnimStateHash]))
        {
            if(currentStateInfo.normalizedTime > .99f)
            {
                playerController.StateMachine.SwitchState<PlayerIdleState>();

                return;
            }

            Vector3 deltaPosition = playerController.Animation.Animator.deltaPosition;
            deltaPosition.z = 0f;
            playerController.Movement.AddPosition(deltaPosition);
        }
        // playerController.Movement.Move(playerController.InputHandler.MoveInput, .3f);
    }

    protected override void Start()
    {
        base.Start();

        mDoorAnimator = GetComponentInChildren<Animator>();
    }

    protected override bool check(PlayerController playerController, PlayerInteractable.InteractedInfo interactedInfo)
    {
        mInteractedInfo = interactedInfo;

        if (interactedInfo.distanceToEdge < playerController.Interactable.InteractableDistance)
            return true;

        return false;
    }

    private void initLerp(PlayerController playerController)
    {
        mbLerp = true;
        mLerpTimer = 0f;

        mLerpStartPosition = playerController.Movement.Position;
    }

    private void openDoor(PlayerController playerController)
    {
        //playerController.Movement.SetVelocity(Vector3.zero);
        //playerController.Animation.Play(AnimState.Door_Push);

        mDoorAnimator.SetFloat("Multiplier", 1f);
        // mBoxCollider.enabled = false;
    }
}
