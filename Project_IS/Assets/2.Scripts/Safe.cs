using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Safe : PushPullObject
{
    private bool mbMoving = false;

    public override void StayPushPull()
    {
        if (!mbMoving)
        {
            mbMoving = true;
            AudioManager.instance.Play("SafeFriction");
        }
    }

    public override void StopPushPull()
    {
        mbMoving = false;
        AudioManager.instance.Stop("SafeFriction");
    }

    //private void Update()
    //{
    //    if(Mathf.Abs(Rigidbody.velocity.x) > .01f)
    //    {
    //        if (!mbMoving)
    //        {
    //            mbMoving = true;
    //            AudioManager.instance.Play("SafeFriction");
    //        }
    //    }
    //    else
    //    {
    //        if(mbMoving)
    //        {
    //            mbMoving = false;
    //            AudioManager.instance.Stop("SafeFriction");
    //        }
    //    }
    //}
}
