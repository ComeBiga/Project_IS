using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CranePillar : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            if (collision.collider.GetComponent<PushPullObject>() != null)
            {
                AudioManager.instance.PlayOneShot("CraneBodyImpact");
            }
        }
    }
}
