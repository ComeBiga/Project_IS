using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    private CinemachineVirtualCameraBase mCurrentCamera;

    public void TurnOnCamera(CinemachineVirtualCameraBase virtualCameraBase)
    {
        if (mCurrentCamera != null)
            mCurrentCamera.Priority = 10;

        virtualCameraBase.Priority = 100;
        mCurrentCamera = virtualCameraBase;
    }
}
