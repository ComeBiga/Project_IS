using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerMovement;

[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    public PlayerMovement Movement => mMovement;
    public PlayerInputHandler InputHandler => mInputHandler;
    public PlayerStateMachine StateMachine => mStateMachine;
    public PlayerAnimation Animation => mAnimation;
    public PlayerCharacterSound CharacterSound => mCharacterSound;
    public PlayerInteractable Interactable => mInteractable;

    private PlayerAnimation mAnimation;
    private PlayerInputHandler mInputHandler;
    private PlayerMovement mMovement;
    private PlayerStateMachine mStateMachine;
    private PlayerInteractable mInteractable;
    private PlayerCharacterSound mCharacterSound;

    public bool CheckOppositeInputX()
    {
        bool bOppositePressed = InputHandler.MoveInputXOppositePressed;
        InputHandler.ResetMoveInputXOppositePressed();

        if (bOppositePressed)
        {
            return true;
        }

        EDirection InputXDirection = PlayerMovement.MoveInputXToDirection(InputHandler.MoveInput.x);

        if (Mathf.Abs(InputHandler.MoveInput.x) > .001f && InputXDirection == Movement.OppositeDirection)
        {
            return true;
        }

        return false;
    }

    private void Awake()
    {
        mAnimation = GetComponentInChildren<PlayerAnimation>();
        mInputHandler = GetComponent<PlayerInputHandler>();
        mMovement = GetComponent<PlayerMovement>();

        mStateMachine = GetComponent<PlayerStateMachine>();
        mInteractable = GetComponent<PlayerInteractable>();
        mCharacterSound = GetComponent<PlayerCharacterSound>();
    }

    private void Start()
    {
        mInputHandler.Initialize();
        mMovement.Initialize();
        mStateMachine.Initialize();
        mInteractable.Initialize(this);
        mCharacterSound.Initialize(this);

        StartCoroutine(eLateFixedUpdate());
    }

    private void FixedUpdate()
    {
        mMovement.FixedTick();

        mStateMachine.CurrentStateBase.FixedTick();
    }

    private void OnAnimatorMove()
    {
        mStateMachine.CurrentStateBase.AnimatorMoveTick();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        mStateMachine.CurrentStateBase.AnimatorIKTick();
    }

    private IEnumerator eLateFixedUpdate()
    {
        while(true)
        {
            yield return new WaitForFixedUpdate();

            mMovement.LateFixedTick();

            mStateMachine.CurrentStateBase.LateFixedTick();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        mStateMachine.CurrentStateBase.Tick();

        mMovement.Tick();

        mStateMachine.UpdateStandbyStates();

        mInteractable.Tick();
    }
}
