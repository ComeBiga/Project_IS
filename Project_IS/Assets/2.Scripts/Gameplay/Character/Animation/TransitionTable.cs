using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Animation/Transition Table", fileName = "TransitionTable")]
public class TransitionTable : ScriptableObject
{
    [System.Serializable]
    public class TransitionData
    {
        public string name;

        public bool anyFrom;
        public AnimState from;
        public AnimState to;

        public bool fixedDuration;
        public float duration;

        [Range(0f, 1f)]
        public float offset;
    }

    public readonly struct TransitionKey
    {
        public readonly AnimState From;
        public readonly AnimState To;

        public TransitionKey(AnimState from, AnimState to)
        {
            From = from;
            To = to;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionKey other && From == other.From && To == other.To;
        }
    }

    public IReadOnlyList<TransitionData> Transitions => transitions;

    [SerializeField]
    private List<TransitionData> transitions;

    private Dictionary<TransitionKey, TransitionData> fromToTransitions;
    private Dictionary<AnimState, TransitionData> anyFromTransitions;

    public void SetTransitions(List<TransitionData> newTransitions)
    {
        transitions = newTransitions;
    }

    public void Initialize()
    {
        fromToTransitions = new();
        anyFromTransitions = new();

        foreach (TransitionData transitionData in transitions)
        {
            if(transitionData.anyFrom)
            {
                anyFromTransitions[transitionData.to] = transitionData;
            }
            else
            {
                var key = new TransitionKey(transitionData.from, transitionData.to);

                fromToTransitions[key] = transitionData;    
            }
        }
    }

    public bool TryGet(AnimState from, AnimState to, out TransitionData transition)
    {
        if(fromToTransitions.TryGetValue(new TransitionKey(from, to), out transition))
        {
            return true;
        }

        return anyFromTransitions.TryGetValue(to, out transition);
    }
}