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
    public PlayerAnimator Animator => _animator;
    public PlayerCharacterSound CharacterSound => mCharacterSound;

    [SerializeField] private PlayerAnimator _animator;

    private PlayerInputHandler mInputHandler;
    private PlayerMovement mMovement;
    private PlayerStateMachine mStateMachine;
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
        mInputHandler = GetComponent<PlayerInputHandler>();
        mMovement = GetComponent<PlayerMovement>();

        mStateMachine = GetComponent<PlayerStateMachine>();
        mCharacterSound = GetComponent<PlayerCharacterSound>();
    }

    private void Start()
    {
        mMovement.Initialize();
        mStateMachine.Initialize();
        mCharacterSound.Initialize(this);

        StartCoroutine(eLateFixedUpdate());
    }

    private void FixedUpdate()
    {
        mMovement.FixedTick();

        mStateMachine.CurrentStateBase.FixedTick();
    }

    private IEnumerator eLateFixedUpdate()
    {
        while(true)
        {
            mMovement.LateFixedTick();

            yield return new WaitForFixedUpdate();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        mStateMachine.CurrentStateBase.Tick();

        mMovement.Tick();
    }
}
