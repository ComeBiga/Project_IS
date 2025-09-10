using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private bool _sidePassable = false;
    public bool SidePassable => _sidePassable;

    protected BoxCollider mBoxCollider;
    public BoxCollider BoxCollider => mBoxCollider;

    protected virtual void Start()
    {
        mBoxCollider = GetComponent<BoxCollider>();
    }
}
