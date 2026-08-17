using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerStateMachine : MonoBehaviour
{
    [Obsolete]
    public enum EState { Idle = 0, Move, IdleJump, RunJump, Ladder, 
                        PushPull = 5, ClimbObject, Fall, ClimbLedge, ClimbRope, 
                        Slope = 10, Die, 
                        Interact = 100, }

    public EState CurrentState => mCurrentState;
    public PlayerStateBase CurrentStateBase => mCurrentStateBase;

    [Obsolete]
    [SerializeField] private List<PlayerStateBase> _states = new List<PlayerStateBase>();

    private PlayerController mController;
    private EState mCurrentState;
    [SerializeField] private PlayerStateBase mCurrentStateBase;
    [Obsolete]
    private Dictionary<EState, PlayerStateBase> mStateDic = new Dictionary<EState, PlayerStateBase>(20);
    private Dictionary<Type, PlayerStateBase> mStateDicByType = new Dictionary<Type, PlayerStateBase>(20);

    public void Initialize()
    {
        //foreach (var state in _states)
        //{
        //    ResisterState(state);
        //}
        resisterStates();

        // SwitchState(PlayerStateMachine.EState.Move);
        SwitchState<PlayerIdleState>();
    }

    [Obsolete("Use GetStateBase<PlayerStateBase>() instead.")]
    public PlayerStateBase GetStateBase(EState state)
    {
        return mStateDic[state];
    }

    public T GetStateBase<T>() where T : PlayerStateBase
    {
        Type type = typeof(T);

        if (mStateDicByType.ContainsKey(type))
        {
            return mStateDicByType[type] as T;
        }

        return null;
    }

    [Obsolete]
    public void ResisterState(PlayerStateBase state)
    {
        state.Initialize(mController);
        mStateDic.Add(state.key, state);
    }

    [Obsolete("Use SwitchState<PlayerStateBase>() instead.")]
    public PlayerStateBase SwitchState(EState state)
    {
        mCurrentStateBase?.ExitState();
        mCurrentStateBase = mStateDic[state];
        mCurrentState = state;
        mCurrentStateBase.EnterState();

        mController.Animator.SetState((int)state);

        return mCurrentStateBase;
    }

    public void SwitchState(PlayerStateBase nextState)
    {
        var lastState = mCurrentStateBase;

        mCurrentStateBase?.ExitState();
        mCurrentStateBase = nextState;
        mCurrentStateBase.EnterState();

        GameDebug.Log($"SwitchState from: {lastState?.GetType().Name}, to: {nextState.GetType().Name}", category: GameDebug.LogCategory.State);
    }

    public void SwitchState<T>(Action<T> onBeforeEnter = null) where T : PlayerStateBase
    {
        T nextState = GetStateBase<T>();

        onBeforeEnter?.Invoke(nextState);

        SwitchState(nextState);
    }

    public void UpdateStandbyStates()
    {
        foreach(KeyValuePair<Type, PlayerStateBase> pair in mStateDicByType)
        {
            if (pair.Key == mCurrentStateBase.GetType())
                continue;

            PlayerStateBase standbyState = pair.Value;
            standbyState.Standby();
        }
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
            resisterStateByType(state);
        }
    }

    private void resisterStateByType(PlayerStateBase state)
    {
        state.Initialize(mController);
        mStateDicByType.Add(state.GetType(), state);
    }
}
