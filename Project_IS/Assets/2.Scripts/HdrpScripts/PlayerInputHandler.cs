using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get ; private set; }
    public Vector2 MoveInputRaw { get ; private set; }
    public bool JumpPressed { get; private set; }
    public bool IsInteracting { get; private set; }
    public bool DownPressed { get; private set; }
    public bool MoveInputXTapped { get; private set; }
    public bool MoveInputXPressed { get; private set; }
    public bool MoveInputXHeld { get; private set; }
    public bool MoveInputYTapped { get; private set; }
    public bool MoveInputYPressed { get; private set; }
    public bool MoveInputYHeld { get; private set; }
    public bool MoveInputXOppositePressed { get; private set; }
    public float AxisSensitivity => _axisSensitivity;

    [SerializeField] private float _axisSensitivity = 0.1f;
    [SerializeField] private float _axisDeadZone = 0.3f;        // CopilotÀÌ ÃßÃµÇØÁà¼­ ÀÏ´Ü ³öµÒ
    [SerializeField] private float _heldThreshold = 0.15f;

    private float mInputXDuration = 0f;
    private float mInputYDuration = 0f;

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

    public void ResetMoveInputXOppositePressed()
    {
        MoveInputXOppositePressed = false;
        // Debug.Log("OppositePressed False");
    }

    public void SetMoveInput(Vector2 value)
    {
        MoveInput = value;
    }

    public Vector2 GetInputMagnitude()
    {
        return new Vector2(Mathf.Abs(MoveInput.x), Mathf.Abs(MoveInput.y));
    }

    public Vector2 GetInputRawMagnitude()
    {
        return new Vector2(Mathf.Abs(MoveInputRaw.x), Mathf.Abs(MoveInputRaw.y));
    }

    // Update is called once per frame
    void Update()
    {
        var newMoveInput = MoveInput;
        newMoveInput.y = Input.GetAxis("Vertical");

        Vector2 newMoveInputRaw = MoveInputRaw;
        newMoveInputRaw.y = Input.GetAxisRaw("Vertical");

        if (Input.GetAxisRaw("Horizontal") > .99f)
        {
            if (newMoveInput.x < 0f)
            // if (newMoveInput.x < 0f && MoveInputXOppositePressed == false)
            {
                newMoveInput.x = 0f;
                MoveInputXOppositePressed = true;

                // Debug.Log("OppositePressed True");
            }

            newMoveInput.x += Time.deltaTime * _axisSensitivity;
            newMoveInputRaw.x = 1f;
        }
        else if (Input.GetAxisRaw("Horizontal") < -.99f)
        {
            if (newMoveInput.x > 0f)
            // (newMoveInput.x > 0f && MoveInputXOppositePressed == false)
            {
                newMoveInput.x = 0f;
                MoveInputXOppositePressed = true;

                // Debug.Log("OppositePressed True");
            }

            newMoveInput.x -= Time.deltaTime * _axisSensitivity;
            newMoveInputRaw.x = -1f;
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

            newMoveInputRaw.x = 0f;
        }

        newMoveInput.x = Mathf.Clamp(newMoveInput.x, -1f, 1f);
        MoveInput = newMoveInput;
        // MoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        MoveInputRaw = newMoveInputRaw;

        JumpPressed = Input.GetButtonDown("Jump");
        IsInteracting = Input.GetButton("Fire1");

        if(Input.GetAxis("Vertical") < -.01f)
        {
            DownPressed = true;
        }

        calculateInput();

        // Debug.Log($"[{Time.frameCount}] Horizontal: {Input.GetAxis("Horizontal")}, MoveInput: {MoveInput}, MoveInputRaw: {MoveInputRaw}");
        GameDebug.Log($"MoveInput: {MoveInput}", 
                        tag: "MoveInput", 
                        category: GameDebug.LogCategory.Input, 
                        level: GameDebug.LogLevel.Verbose);
    }

    private void calculateInput()
    {
        // X Axis
        if ((MoveInput.x > .01f || MoveInput.x < -.01f)
            && (MoveInputRaw.x > .01f || MoveInputRaw.x < -.01f))
        {
            if(mInputXDuration < .001f)
            {
                MoveInputXTapped = true;
            }
            else if (mInputXDuration > .001f && mInputXDuration < _heldThreshold)
            {
                MoveInputXTapped = false;
                MoveInputXPressed = true;
                MoveInputXHeld = false;

                //MoveInputXOppositePressed = false;
                //Debug.Log("OppositePressed False");
            }
            else
            {
                MoveInputXTapped = false;
                MoveInputXPressed = false;
                MoveInputXHeld = true;
            }

            mInputXDuration += Time.deltaTime;
        }
        else
        {
            mInputXDuration = 0f;
            MoveInputXTapped = false;
            MoveInputXPressed = false;
            MoveInputXHeld = false;
        }

        // Y Axis
        if ((MoveInput.y > .01f || MoveInput.y < -.01f)
            && (MoveInputRaw.y > .01f || MoveInputRaw.y < -.01f))
        {
            if(mInputYDuration < .001f)
            {
                MoveInputYTapped = true;
            }
            else if (mInputYDuration > .001f && mInputYDuration < _heldThreshold)
            {
                MoveInputYTapped = false;
                MoveInputYPressed = true;
                MoveInputYHeld = false;
            }
            else
            {
                MoveInputYTapped = false;
                MoveInputYPressed = false;
                MoveInputYHeld = true;
            }

            mInputYDuration += Time.deltaTime;
        }
        else
        {
            mInputYDuration = 0f;
            MoveInputYTapped = false;
            MoveInputYPressed = false;
            MoveInputYHeld = false;
        }
    }
}
