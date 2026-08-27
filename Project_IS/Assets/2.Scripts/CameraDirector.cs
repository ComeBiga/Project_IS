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

    private void Awake()
    {
        GameObject goPlayerCharacter = PlayerCharacterUtility.FindActivePlayerCharacterObject();
        var virtualCameras = GetComponentsInChildren<CinemachineVirtualCameraBase>();

        foreach(CinemachineVirtualCameraBase virtualCamera in virtualCameras)
        {
            if(virtualCamera.Follow != null)
                virtualCamera.Follow = goPlayerCharacter.transform;

            if(virtualCamera.LookAt != null)
                virtualCamera.LookAt = goPlayerCharacter.transform;
        }

        var mixingBoundsArray = GetComponentsInChildren<CameraMixingBounds>();

        foreach(CameraMixingBounds mixingBounds in mixingBoundsArray)
        {
            mixingBounds.SetPlayerCharacter(goPlayerCharacter.transform);
        }
    }
}
