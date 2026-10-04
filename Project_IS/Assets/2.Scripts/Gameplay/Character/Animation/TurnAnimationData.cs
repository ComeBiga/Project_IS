using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Animation/Turn Animation Data")]
public class TurnAnimationData : ScriptableObject
{
    [System.Serializable]
    public struct TurnAnimation
    {
        public AnimationClip sourceClip;
        public PlayerTurnState.ETurnType turnType;
        public RotationHandler.ERotationDirection rotationDirection;
        public bool useRotationDuration;
        public float rotationDuration;

        public float GetDuration()
        {
            return useRotationDuration ? rotationDuration : sourceClip.length;
        }
    }

    public AnimationClip sourceClip;
    public PlayerTurnState.ETurnType turnType;
    public bool useRotationDuration = false;
    public float rotationDuration;

    public TurnAnimation idleTurnLAnimation;
    public TurnAnimation idleTurnRAnimation;
    public TurnAnimation runTurnLAnimation;
    public TurnAnimation runTurnRAnimation;

    public TurnAnimation Get(PlayerTurnState.ETurnType turnType, RotationHandler.ERotationDirection rotationDirection)
    {
        switch(turnType)
        {
            case PlayerTurnState.ETurnType.Idle:
                return rotationDirection == RotationHandler.ERotationDirection.Left ? idleTurnLAnimation : idleTurnRAnimation;
            case PlayerTurnState.ETurnType.Run:
                return rotationDirection == RotationHandler.ERotationDirection.Left ? runTurnLAnimation : runTurnRAnimation;
            default:
                break;
        }

        return new TurnAnimation();
    }


}
