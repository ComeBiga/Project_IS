using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameSystem : MonoBehaviour
{
    [SerializeField]
    private bool _fixedFrameRate = true;
    [SerializeField]
    private int _targetFrameRate = 60;
    [SerializeField]
    private bool _updateTimeScale = false;
    [SerializeField]
    private float _timeScale = 1f;
    [SerializeField]
    private bool _useTimeScalePreset = false;
    [SerializeField]
    private List<float> _timeScalePreset;

    [Header("CheckPoint")]
    [SerializeField]
    private Transform _trPlayerCharacter;
    [SerializeField]
    private Vector3 _checkPoint = Vector3.zero;

    [Header("Player Character")]
    [SerializeField]
    private GameObject _goPlayerCharacter;
    [SerializeField]
    private CinemachineVirtualCamera _virtualCamera;
    [SerializeField]
    private bool _cameraAutoFollow = false;

    // Start is called before the first frame update
    void Start()
    {
        if(_fixedFrameRate)
            Application.targetFrameRate = _targetFrameRate;

        Time.timeScale = _timeScale;

        _goPlayerCharacter = FindActivePlayerCharacterObject();

        if(_cameraAutoFollow)
            _virtualCamera.Follow = _goPlayerCharacter.transform;
    }

    private void Update()
    {
        if (_updateTimeScale)
            Time.timeScale = _timeScale;

        if(_useTimeScalePreset)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Time.timeScale = _timeScalePreset[0];
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Time.timeScale = _timeScalePreset[1];
            }

            if(Input.GetKeyDown(KeyCode.Alpha3))
            {
                Time.timeScale = _timeScalePreset[2];
            }

            if(Input.GetKeyDown(KeyCode.Alpha4))
            {
                Time.timeScale = _timeScalePreset[3];
            }

            if(Input.GetKeyDown(KeyCode.Alpha5))
            {
                Time.timeScale = _timeScalePreset[4];
            }
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            _checkPoint = _goPlayerCharacter.transform.position;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Vector3 position = _goPlayerCharacter.transform.position;
            position.x = _checkPoint.x;
            position.y = _checkPoint.y;
            _goPlayerCharacter.transform.position = position;
        }
    }

    // private float mLastXPos = 0f;

    private void LateUpdate()
    {
        //float deltaXPos = _virtualCamera.transform.position.x - mLastXPos;

        //Debug.Log($"cameraPosition: {_virtualCamera.transform.position}, deltaXPos: {deltaXPos}");

        //mLastXPos = _virtualCamera.transform.position.x;
    }

    private GameObject FindActivePlayerCharacterObject()
    {
        GameObject[] playerCharacterObjects = GameObject.FindGameObjectsWithTag("Player");

        for(int i = 0;  i < playerCharacterObjects.Length; i++)
        {
            if (playerCharacterObjects[i].activeSelf)
                return playerCharacterObjects[i];
        }

        return null;
    }

#if UNITY_EDITOR

    [MenuItem("Tools/Select Player Character #p")]
    private static void SelectPlayerCharacter()
    {
        GameObject target = GameObject.FindWithTag("Player");
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        // EditorUtility.FocusProjectWindow();
    }
#endif
}
