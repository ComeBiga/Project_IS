using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FridgePushDowner : MonoBehaviour
{
    [SerializeField]
    private GameObject _goTopGround;
    [SerializeField]
    private PhysicMaterial _pmNoneFriction;
    [SerializeField]
    private BoxCollider _boxCollider;

    private bool mbIsPushedDown = false;

    // Update is called once per frame
    void Update()
    {
        if (mbIsPushedDown)
        {
            return;
        }

        if(transform.rotation.eulerAngles.z < 5f)
        {
            mbIsPushedDown = true;
            _boxCollider.material = _pmNoneFriction;
            _goTopGround.SetActive(true);

            Debug.Log("Fell Down");
        }
    }
}
