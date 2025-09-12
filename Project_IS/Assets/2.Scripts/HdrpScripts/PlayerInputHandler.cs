using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get ; private set; }
    public bool JumpPressed { get; private set; }
    public bool IsInteracting { get; private set; }
    public bool DownPressed { get; private set; }

    [SerializeField] private float _axisSensitivity = 0.1f;
    [SerializeField] private float _axisDeadZone = 0.3f;        // CopilotÀÌ ÃßÃµÇØÁà¼­ ÀÏ´Ü ³öµÒ

    public void ResetJump()
    {
        JumpPressed = false;
    }

    public void ResetDown()
    {
        DownPressed = false;
    }

    public void ResetMoveInput()
    {
        MoveInput = Vector2.zero;
        // Input.ResetInputAxes();
    }

    // Update is called once per frame
    void Update()
    {
        var newMoveInput = MoveInput;
        newMoveInput.y = Input.GetAxis("Vertical");

        if (Input.GetAxisRaw("Horizontal") > .99f)
        {
            if(newMoveInput.x < 0f)
                newMoveInput.x = 0f;

            newMoveInput.x += Time.deltaTime * _axisSensitivity;
        }
        else if (Input.GetAxisRaw("Horizontal") < -.99f)
        {
            if (newMoveInput.x > 0f)
                newMoveInput.x = 0f;
            newMoveInput.x -= Time.deltaTime * _axisSensitivity;
        }
        else
        {
            if (newMoveInput.x > 0f)
            {
                newMoveInput.x -= Time.deltaTime * _axisSensitivity;
                if (newMoveInput.x < 0f)
                    newMoveInput.x = 0f;
            }
            else if (newMoveInput.x < 0f)
            {
                newMoveInput.x += Time.deltaTime * _axisSensitivity;
                if (newMoveInput.x > 0f)
                    newMoveInput.x = 0f;
            }
        }

        newMoveInput.x = Mathf.Clamp(newMoveInput.x, -1f, 1f);
        MoveInput = newMoveInput;
        // MoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));


        JumpPressed = Input.GetButtonDown("Jump");
        IsInteracting = Input.GetButton("Fire1");

        if(Input.GetAxis("Vertical") < -.01f)
        {
            DownPressed = true;
        }
    }
}
