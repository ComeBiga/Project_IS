using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorEvent : StateMachineBehaviour
{
    [SerializeField]
    private string _stateName = "";
    [SerializeField]
    private bool _EnterEvent = false;
    [SerializeField]
    private bool _UpdateEvent = false;
    [SerializeField]
    private bool _ExitEvent = false;

    private PlayerAnimation mPlayerAnimator;

    //public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //    Debug.Log("StateMachine Entered");
    //}

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_EnterEvent)
        {
            PlayerAnimation playerAnimator = GetPlayerAnimator(animator);
            playerAnimator.EnterState(_stateName, stateInfo);
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_UpdateEvent)
        {
            PlayerAnimation playerAnimator = GetPlayerAnimator(animator);
            playerAnimator.UpdateState(_stateName, stateInfo);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_ExitEvent)
        {
            PlayerAnimation playerAnimator = GetPlayerAnimator(animator);
            playerAnimator.ExitState(_stateName, stateInfo);
        }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}


    private PlayerAnimation GetPlayerAnimator(Animator animator)
    {
        if (mPlayerAnimator == null)
        {
            mPlayerAnimator = animator.GetComponentInParent<PlayerAnimation>();
        }

        return mPlayerAnimator;
    }
}
