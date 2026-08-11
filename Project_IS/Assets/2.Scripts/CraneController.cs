using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraneController : InteractableObject
{
    [SerializeField]
    private Transform _trCrane;
    [SerializeField]
    private float _craneMinY;
    [SerializeField]
    private float _craneMaxY;
    [SerializeField]
    private float _moveSpeed = 1f;
    [SerializeField]
    private float _characterDistance = .6f;
    [SerializeField]
    private float _lerpSpeed = 5f;
    [SerializeField]
    private Transform _trLever;
    [SerializeField]
    private Vector3 _leverPositionOffset;
    [Range(0f, 1f)]
    [SerializeField]
    private float _leverIKWeight;

    private Animator mAnimator;
    private Animator mPlayerAnimator;
    private bool mbCraneMoving = false;

    private readonly int LeverStateHash = Animator.StringToHash("LeverState");

    public override void Enter(PlayerController playerController)
    {
        mPlayerAnimator = playerController.Animator.Animator;

        playerController.Animator.onAnimationIK -= updateAnimatorIK;
        playerController.Animator.onAnimationIK += updateAnimatorIK;
    }

    public override void Exit(PlayerController playerController)
    {
        setCraneMoving(false);

        playerController.Animator.onAnimationIK -= updateAnimatorIK;
    }

    override public void Tick(PlayerController playerController)
    {
        if(!playerController.InputHandler.IsInteracting)
        {
            // playerController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
            playerController.StateMachine.SwitchState<PlayerMoveState>();

            return;
        }

        Vector3 targetPosition = playerController.transform.position;
        targetPosition.x = transform.position.x + _characterDistance;
        playerController.transform.position = Vector3.Lerp(playerController.transform.position, targetPosition, Time.deltaTime * _lerpSpeed);

        float inputYMagnitude = Mathf.Abs(playerController.InputHandler.MoveInput.y);
        playerController.Animator.SetInputYMagnitude(inputYMagnitude);
        playerController.Animator.SetVertical(playerController.InputHandler.MoveInput.y);

        // if(playerController.InputHandler.MoveInput.y > 0.01f)
        if(playerController.InputHandler.MoveInputRaw.y > .01f)
        {
            mAnimator.SetInteger(LeverStateHash, 1);
            _trCrane.position += _moveSpeed * Vector3.up * Time.deltaTime;

            if(_trCrane.position.y > _craneMaxY)
            {
                _trCrane.position = new Vector3(_trCrane.position.x, _craneMaxY, _trCrane.position.z);
                setCraneMoving(false);
            }
            else
            {
                setCraneMoving(true);
            }
        }
        else if(playerController.InputHandler.MoveInputRaw.y < -0.01f)
        {
            mAnimator.SetInteger(LeverStateHash, 2);
            _trCrane.position += _moveSpeed * Vector3.down * Time.deltaTime;

            if(_trCrane.position.y < _craneMinY)
            {
                _trCrane.position = new Vector3(_trCrane.position.x, _craneMinY, _trCrane.position.z);
                setCraneMoving(false);
            }
            else
            {
                setCraneMoving(true);
            }
        }
        else
        {
            mAnimator.SetInteger(LeverStateHash, 0);
            setCraneMoving(false);
        }
    }

    protected override void Start()
    {
        base.Start();

        mAnimator = GetComponent<Animator>();
    }

    private void setCraneMoving(bool value)
    {
        if(value == true)
        {
            if(mbCraneMoving == false)
            {
                // Start Moving Crane
                AudioManager.instance.PlayOneShot("CraneLiftStart");
                AudioManager.instance.PlayOneShot("CraneLever");
                AudioManager.instance.Play("CraneLift");
            }
        }
        else
        {
            if(mbCraneMoving == true)
            {
                // Stop Moving Crane
                AudioManager.instance.PlayOneShot("CraneLiftStop");
                AudioManager.instance.Stop("CraneLift");
            }
        }

        mbCraneMoving = value;
    }

    private void updateAnimatorIK()
    {
        Vector3 targetPosition = _trLever.position;
        targetPosition += _leverPositionOffset;
        mPlayerAnimator.SetIKPosition(AvatarIKGoal.RightHand, targetPosition);
        mPlayerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, _leverIKWeight);
    }
}
