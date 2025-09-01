using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler), typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    public PlayerMovement Movement => mMovement;
    public PlayerInputHandler InputHandler => mInputHandler;
    public PlayerStateMachine StateMachine => mStateMachine;
    public PlayerAnimator Animator => _animator;

    [SerializeField] private PlayerAnimator _animator;

    private PlayerInputHandler mInputHandler;
    private PlayerMovement mMovement;
    private PlayerStateMachine mStateMachine;

    private void Awake()
    {
        mInputHandler = GetComponent<PlayerInputHandler>();
        mMovement = GetComponent<PlayerMovement>();

        mStateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        mMovement.Initialize();
        mStateMachine.Initialize();
    }

    // Update is called once per frame
    private void Update()
    {
        mStateMachine.CurrentStateBase.Tick();

        mMovement.Tick();
    }
}
