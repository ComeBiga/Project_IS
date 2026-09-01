using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScenario : MonoBehaviour
{
    public Transform _trPlayer;
    public CinemachineVirtualCamera _firstCamera;
    public CinemachineVirtualCamera _secondCamera;
    public CinemachineVirtualCamera _thirdCamera;
    //public CinemachineVirtualCamera _fourthCamera;
    //public CinemachineVirtualCamera _fifthCamera;
    public float _transitionXPos = 28f;
    //public float _transitionXPos_AfterObstacle = 38f;
    //public float _transitionXPos_Ladder = 62f;
    public float _transitionYPos_Ladder = 10f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_trPlayer.position.y > _transitionYPos_Ladder)
        {
            _thirdCamera.gameObject.SetActive(true);
            _secondCamera.gameObject.SetActive(false);
        }
        ////else if(_trPlayer.position.x > _transitionXPos_Ladder)
        ////{
        ////    _fourthCamera.gameObject.SetActive(true);
        ////    _thirdCamera.gameObject.SetActive(false);
        ////}
        //else if(_trPlayer.position.x > _transitionXPos_AfterObstacle)
        //{
        //    _thirdCamera.gameObject.SetActive(true);
        //    _secondCamera.gameObject.SetActive(false);
        //}
        else if (_trPlayer.position.x > _transitionXPos)
        {
            _firstCamera.gameObject.SetActive(false);
            _secondCamera.gameObject.SetActive(true);
        }
        else
        {
            _firstCamera.gameObject.SetActive(true);
            _secondCamera.gameObject.SetActive(false);
        }
    }
}
