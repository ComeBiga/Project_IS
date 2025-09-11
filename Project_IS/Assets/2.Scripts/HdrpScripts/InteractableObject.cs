using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public bool SidePassable => _sidePassable;
    public bool Pushable => _pushable;
    public bool CanClimb => _canClimb;
    public BoxCollider BoxCollider => mBoxCollider;

    [SerializeField] private bool _sidePassable = false;
    [SerializeField] private bool _pushable = false;
    [SerializeField] private bool _canClimb = false;

    protected BoxCollider mBoxCollider;

    protected virtual void Start()
    {
        mBoxCollider = GetComponent<BoxCollider>();
    }
}
