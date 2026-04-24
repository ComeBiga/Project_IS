using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public bool SidePassable => _sidePassable;
    public bool Pushable => _pushable;
    public bool CanClimb => _canClimb;
    public float InteractionDistance => _interactionDistance;
    public BoxCollider BoxCollider => mBoxCollider;

    [SerializeField] protected bool _sidePassable = false;
    [SerializeField] protected bool _pushable = false;
    [SerializeField] protected bool _canClimb = false;
    [SerializeField] protected float _interactionDistance = .5f;

    protected BoxCollider mBoxCollider;

    public virtual void Enter(PlayerController playerController)
    {
    }

    public virtual void Exit(PlayerController playerController)
    {
    }

    public virtual void Tick(PlayerController playerController)
    {

    }

    protected virtual void Start()
    {
        mBoxCollider = GetComponent<BoxCollider>();
    }
}
