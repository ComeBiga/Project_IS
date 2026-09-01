using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RiceStraw : PushPullObject
{
    [SerializeField]
    private float _forceIntensity = 1f;

    [ContextMenu("Force")]
    public void Force()
    {
        var direction = Vector3.right;
        Vector3 force = direction * _forceIntensity;
        mRigidbody.AddForce(force);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            Force();
        }
    }
}
