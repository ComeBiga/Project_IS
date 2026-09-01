using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraMixer : MonoBehaviour
{
    [SerializeField]
    private CameraDirector _cameraDirector;
    [SerializeField]
    private Transform _trDisabledCameras;

    private CinemachineMixingCamera mMixingCamera;
    private List<CinemachineVirtualCamera> mVirtualCameras = new List<CinemachineVirtualCamera>();

    public void ClearCameras()
    {
        for(int i = 0; i < mVirtualCameras.Count; i++)
        {
            // mVirtualCameras[i].gameObject.SetActive(false);
            mVirtualCameras[i].transform.SetParent(_trDisabledCameras);
        }

        mVirtualCameras.Clear();
    }

    public void AddCameras(params CinemachineVirtualCamera[] virtualCameras)
    {
        for(int i = 0; i < virtualCameras.Length; i++)
        {
            // virtualCameras[i].gameObject.SetActive(true);
            virtualCameras[i].transform.SetParent(transform);

            mVirtualCameras.Add(virtualCameras[i]);
        }
    }

    public void SetCameraList(params CinemachineVirtualCamera[] virtualCameras)
    {
        ClearCameras();
        AddCameras(virtualCameras);

        _cameraDirector.TurnOnCamera(mMixingCamera);
    }

    public void UpdateWeights(float camera1Weight, float camera2Weight)
    {
        mMixingCamera.SetWeight(0, camera1Weight);
        mMixingCamera.SetWeight(1, camera2Weight);
    }

    public void UpdateWeights(params float[] cameraWeights)
    {
        for(int i = 0; i < cameraWeights.Length; i++)
        {
            mMixingCamera.SetWeight(i, cameraWeights[i]);
        }
    }

    public void UpdateWeights(params (int index, float weight)[] cameraWeights)
    {
        for(int i = 0; i < mVirtualCameras.Count; i++)
        {
            mMixingCamera.SetWeight(i, 0);
        }

        for (int i = 0; i < cameraWeights.Length; i++)
        {
            mMixingCamera.SetWeight(cameraWeights[i].index, cameraWeights[i].weight);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        mMixingCamera = GetComponent<CinemachineMixingCamera>();
    }
}
