using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get ; private set; }
    public bool JumpPressed { get; private set; }
    public bool IsInteracting { get; private set; }
    public bool DownPressed { get; private set; }

    public void ResetJump()
    {
        JumpPressed = false;
    }

    public void ResetDown()
    {
        DownPressed = false;
    }

    // Update is called once per frame
    void Update()
    {
        MoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        JumpPressed = Input.GetButtonDown("Jump");
        IsInteracting = Input.GetButton("Fire1");

        if(Input.GetAxis("Vertical") < -.01f)
        {
            DownPressed = true;
        }
    }
}
