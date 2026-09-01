using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [System.Serializable]
    public struct CameraMixingInfo
    {
        public CinemachineVirtualCamera dollyCamera;
        public float startPosition;
        public float endPosition;
    }

    [SerializeField]
    private CinemachineMixingCamera _mixingCamera;
    [SerializeField]
    private Transform _trWaitingLines;
    [SerializeField]
    private CinemachineVirtualCamera _dollyCamera;
    [SerializeField]
    private float _startMixingPosition = .75f;
    [SerializeField]
    private float _endMixingPosition = 1f;
    [SerializeField]
    private Transform _trPlayerCharacter;

    [SerializeField]
    private CameraMixingInfo[] _cameraMixingInfos;

    private CinemachineTrackedDolly mTrackedDolly;
    private int mCameraIndex = 0;
    private int mWeightCount = 3;
    private int mWeightIndex = 0;
    private bool mbCirculated = false;

    // Start is called before the first frame update
    void Start()
    {
        mTrackedDolly = _dollyCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(mTrackedDolly.m_PathPosition);

        //if(mTrackedDolly.m_PathPosition >= _startMixingPosition && mTrackedDolly.m_PathPosition <= _endMixingPosition)
        //{
        //    float t = (mTrackedDolly.m_PathPosition - _startMixingPosition) / (_endMixingPosition - _startMixingPosition);
        //    _mixingCamera.SetWeight(0, 1f - t);
        //    _mixingCamera.SetWeight(1, t);
        //}

        // 현재 구간
        if(_trPlayerCharacter.position.x >= _cameraMixingInfos[mCameraIndex].startPosition 
            && _trPlayerCharacter.position.x < _cameraMixingInfos[mCameraIndex].endPosition)
        {
            int nextWeightIndex = (mWeightIndex + 1) % mWeightCount;
            _mixingCamera.SetWeight(mWeightIndex, 1f);
            _mixingCamera.SetWeight(nextWeightIndex, 0f);

            if(!mbCirculated)
            {
                mbCirculated = true;
                int cameraToWaitIndex = mCameraIndex + 2;
                int lastWeightIndex = (mWeightIndex - 1) % mWeightCount;

                if(mCameraIndex - 1 >= 0)
                {
                    if(cameraToWaitIndex < _cameraMixingInfos.Length)
                    {
                        CameraMixingInfo cameraMixingInfo = _cameraMixingInfos[cameraToWaitIndex];
                        cameraMixingInfo.dollyCamera.transform.SetParent(_trWaitingLines);
                        cameraMixingInfo.dollyCamera.gameObject.SetActive(false);
                    }

                    CameraMixingInfo lastCameraMixingInfo = _cameraMixingInfos[mCameraIndex - 1];
                    lastCameraMixingInfo.dollyCamera.transform.SetParent(transform);
                    lastCameraMixingInfo.dollyCamera.transform.SetSiblingIndex(lastWeightIndex);
                    lastCameraMixingInfo.dollyCamera.gameObject.SetActive(true);
                }
            }
        }
        // 이전 구간으로 전환
        else if(_trPlayerCharacter.position.x < _cameraMixingInfos[mCameraIndex].startPosition)
        {
            --mCameraIndex;

            mWeightIndex = (mWeightIndex - 1) < 0 ? mWeightCount - 1 : mWeightIndex - 1;
            mbCirculated = false;
        }
        // 블렌딩 구간
        else if(_trPlayerCharacter.position.x >= _cameraMixingInfos[mCameraIndex].endPosition
            && _trPlayerCharacter.position.x < _cameraMixingInfos[mCameraIndex + 1].startPosition)
        {
            float t = (_trPlayerCharacter.position.x - _cameraMixingInfos[mCameraIndex].endPosition) / (_cameraMixingInfos[mCameraIndex + 1].startPosition - _cameraMixingInfos[mCameraIndex].endPosition);
            int nextWeightIndex = (mWeightIndex + 1) % mWeightCount;
            _mixingCamera.SetWeight(mWeightIndex, 1f - t);
            _mixingCamera.SetWeight(nextWeightIndex, t);
        }
        // 다음 구간으로 전환
        else if(_trPlayerCharacter.position.x >= _cameraMixingInfos[mCameraIndex + 1].startPosition
            && _trPlayerCharacter.position.x < _cameraMixingInfos[mCameraIndex + 1].endPosition)
        {
            int nextWeightIndex = (mWeightIndex + 1) % mWeightCount;
            _mixingCamera.SetWeight(mWeightIndex, 0f);
            _mixingCamera.SetWeight(nextWeightIndex, 1f);
            int cameraToWaitIndex = mCameraIndex - 1;
            ++mCameraIndex;

            mWeightIndex = (mWeightIndex + 1) % mWeightCount;
            nextWeightIndex = (mWeightIndex + 1) % mWeightCount;

            if (mCameraIndex + 1 < _cameraMixingInfos.Length)
            {
                if(cameraToWaitIndex >= 0)
                {
                    CameraMixingInfo cameraMixingInfo = _cameraMixingInfos[cameraToWaitIndex];
                    cameraMixingInfo.dollyCamera.transform.SetParent(_trWaitingLines);
                    cameraMixingInfo.dollyCamera.gameObject.SetActive(false);
                }

                CameraMixingInfo nextCameraMixingInfo = _cameraMixingInfos[mCameraIndex + 1];
                nextCameraMixingInfo.dollyCamera.transform.SetParent(transform);
                nextCameraMixingInfo.dollyCamera.transform.SetSiblingIndex(nextWeightIndex);
                nextCameraMixingInfo.dollyCamera.gameObject.SetActive(true);
            }
        }
    }
}
