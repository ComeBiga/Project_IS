using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public bool SidePassable => _sidePassable;
    public bool Pushable => _pushable;
    public bool CanClimb => _canClimb;
    public BoxCollider BoxCollider => mBoxCollider;

    [SerializeField] protected bool _sidePassable = false;
    [SerializeField] protected bool _pushable = false;
    [SerializeField] protected bool _canClimb = false;

    protected BoxCollider mBoxCollider;

    protected virtual void Start()
    {
        mBoxCollider = GetComponent<BoxCollider>();
    }
}
