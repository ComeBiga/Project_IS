using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraneBody : PushPullObject
{
    private bool mbMoving = false;

    public override void StayPushPull()
    {
        if (!mbMoving)
        {
            mbMoving = true;
            AudioManager.instance.Play("CraneBodyCreak");
        }
    }

    public override void StopPushPull()
    {
        mbMoving = false;
        AudioManager.instance.Stop("CraneBodyCreak");
    }
}
