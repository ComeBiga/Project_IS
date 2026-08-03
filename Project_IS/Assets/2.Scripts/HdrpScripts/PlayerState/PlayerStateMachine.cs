using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerStateMachine : MonoBehaviour
{
    public enum EState { Idle = 0, Move, IdleJump, RunJump, Ladder, 
                        PushPull = 5, ClimbObject, Fall, ClimbLedge, ClimbRope, 
                        Slope = 10, Die, 
                        Interact = 100, }

    public EState CurrentState => mCurrentState;
    public PlayerStateBase CurrentStateBase => mCurrentStateBase;

    [SerializeField] private List<PlayerStateBase> _states = new List<PlayerStateBase>();

    private PlayerController mController;
    private EState mCurrentState;
    private PlayerStateBase mCurrentStateBase;
    private Dictionary<EState, PlayerStateBase> mStateDic = new Dictionary<EState, PlayerStateBase>(20);
    private Dictionary<Type, PlayerStateBase> mStateTypeDic = new Dictionary<Type, PlayerStateBase>(20);

    public void Initialize()
    {
        foreach (var state in _states)
        {
            ResisterState(state);
        }

        SwitchState(PlayerStateMachine.EState.Move);
    }

    public PlayerStateBase GetStateBase(EState state)
    {
        return mStateDic[state];
    }

    public void ResisterState(PlayerStateBase state)
    {
        state.Initialize(mController);
        mStateDic.Add(state.key, state);
    }
    
    public PlayerStateBase SwitchState(EState state)
    {
        mCurrentStateBase?.ExitState();
        mCurrentStateBase = mStateDic[state];
        mCurrentState = state;
        mCurrentStateBase.EnterState();

        mController.Animator.SetState((int)state);

        return mCurrentStateBase;
    }

    private void Awake()
    {
        mController = GetComponent<PlayerController>();
    }

    private void resisterStates()
    {
        PlayerStateBase[] states = GetComponentsInChildren<PlayerStateBase>();

        foreach (var state in states)
        {
            ResisterState(state);
        }
    }

    private void resisterStateByType(PlayerStateBase state)
    {

    }
}
