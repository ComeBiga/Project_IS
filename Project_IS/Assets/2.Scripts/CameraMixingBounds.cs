using Cinemachine;
using Cinemachine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CameraMixingBounds : MonoBehaviour
{
    public enum CameraMixingMode
    {
        Horizontal,
        Vertical,
        Square,
        Triangle,
        Distance,
    }

    [Serializable]
    public struct AdditionalCamera
    {
        public CinemachineVirtualCamera virtualCamera;
        public Vector2 BoundPosition;

        public AdditionalCamera(CinemachineVirtualCamera cam, Vector2 BoundPosition)
        {
            virtualCamera = cam;
            this.BoundPosition = BoundPosition;
        }
    }

    public struct CameraPoint
    {
        public CinemachineVirtualCamera virtualCamera;
        public int index;
        public Vector2 position;

        public CameraPoint(CinemachineVirtualCamera cam, Vector2 position, int index)
        {
            virtualCamera = cam;
            this.position = position;
            this.index = index;
        }
    }

    private struct Triangle
    {
        public CameraPoint cp1;
        public CameraPoint cp2;
        public CameraPoint cp3;

        public Triangle(CameraPoint cameraPoint1, CameraPoint cameraPoint2, CameraPoint cameraPoint3)
        {
            cp1 = cameraPoint1;
            cp2 = cameraPoint2;
            cp3 = cameraPoint3;
        }
    }

    [Header("Debug")]
    [SerializeField] private bool _drawBounds = false;
    [SerializeField] private bool _drawWeightGuide = false;

    [Header("Settings")]
    [SerializeField] private CameraMixer _cameraMixer;
    [SerializeField] private Transform _trPlayerCharacter;

    [SerializeField] private CinemachineVirtualCamera _virtualCamera1;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera2;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera3;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera4;
    [SerializeField] private CinemachineVirtualCamera[] _additionalVitualCameras;
    [SerializeField] private AdditionalCamera[] _additionalCameras;
    [SerializeField] private float _deltaThreshold = .5f;
    [SerializeField] private AnimationCurve _weightCurveX;
    [SerializeField] private AnimationCurve _weightCurveY;
    [SerializeField] private CameraMixingMode _mixingMode = CameraMixingMode.Horizontal;
    [SerializeField] private float _damping = 2f;

    private bool mbActivated = false;
    private BoxCollider mBoxCollider;
    private Bounds mBounds;
    private List<CameraPoint> mCameraPoints = new List<CameraPoint>();
    private List<Triangle> mTriangles = new List<Triangle>();
    private int mTriangleIndex = -1;

    private float[] mCameraWeights = new float[6];

    private void Start()
    {
        mBoxCollider = GetComponent<BoxCollider>();
        mBounds = mBoxCollider.bounds;

        initCameraPoints();
        initTriangles();
    }

    private void Update()
    {
        if(mbActivated)
        {
            switch(_mixingMode)
            {
                case CameraMixingMode.Horizontal:
                case CameraMixingMode.Vertical:
                    updateCameraWeights();
                    break;
                case CameraMixingMode.Square:
                    updateCameraWeightsBySquare();
                    // updateCameraWeightsByTriangle();
                    break;
                case CameraMixingMode.Triangle:
                    updateCameraWeightsByTriangle();
                    break;
                case CameraMixingMode.Distance:
                    updateCameraWeightsByDistance();
                    break;
                default:
                    updateCameraWeights();
                    break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            mbActivated = true;
            if (_additionalCameras.Length == 2)
            {
                _cameraMixer.SetCameraList(_virtualCamera1, _virtualCamera2, _virtualCamera3, _virtualCamera4, _additionalCameras[0].virtualCamera, _additionalCameras[1].virtualCamera);
            }
            else if (_additionalCameras.Length == 1)
            {
                _cameraMixer.SetCameraList(_virtualCamera1, _virtualCamera2, _virtualCamera3, _virtualCamera4, _additionalCameras[0].virtualCamera);
            }
            else if (_virtualCamera4 != null)
                _cameraMixer.SetCameraList(_virtualCamera1, _virtualCamera2, _virtualCamera3, _virtualCamera4);
            else if (_virtualCamera3 != null)
                _cameraMixer.SetCameraList(_virtualCamera1, _virtualCamera2, _virtualCamera3);
            else
                _cameraMixer.SetCameraList(_virtualCamera1, _virtualCamera2);
            updateCameraWeights();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            mbActivated = false;
        }
    }

    private void initCameraPoints()
    {
        mCameraPoints.Add(new CameraPoint(_virtualCamera1, mBounds.min, 0));
        mCameraPoints.Add(new CameraPoint(_virtualCamera2, new Vector2(mBounds.max.x, mBounds.min.y), 1));
        mCameraPoints.Add(new CameraPoint(_virtualCamera3, new Vector2(mBounds.min.x, mBounds.max.y), 2));
        mCameraPoints.Add(new CameraPoint(_virtualCamera4, new Vector2(mBounds.max.x, mBounds.max.y), 3));

        //if(_additionalCameras.Length == 1)
        //{
        //    var cp5Position = new Vector2();
        //    cp5Position.x = mBounds.size.x * _additionalCameras[0].BoundPosition.x + mBounds.min.x;
        //    cp5Position.y = mBounds.size.y * _additionalCameras[0].BoundPosition.y + mBounds.min.y;
        //    mCameraPoints.Add(new CameraPoint(_additionalCameras[0].virtualCamera, cp5Position, 4));
        //}

        for(int i = 0; i < _additionalCameras.Length; i++)
        {
            var cpPosition = new Vector2();
            cpPosition.x = mBounds.size.x * _additionalCameras[i].BoundPosition.x + mBounds.min.x;
            cpPosition.y = mBounds.size.y * _additionalCameras[i].BoundPosition.y + mBounds.min.y;
            mCameraPoints.Add(new CameraPoint(_additionalCameras[i].virtualCamera, cpPosition, mCameraPoints.Count + i));
        }
    }

    private void initTriangles()
    {
        if (_additionalCameras.Length == 0)
            return;

        if (_additionalCameras.Length == 1)
        {
            //CameraPoint cp1 = new CameraPoint(_virtualCamera1, mBounds.min, 0);
            //CameraPoint cp2 = new CameraPoint(_virtualCamera2, new Vector2(mBounds.max.x, mBounds.min.y), 1);
            //CameraPoint cp3 = new CameraPoint(_virtualCamera3, new Vector2(mBounds.min.x, mBounds.max.y), 2);
            //CameraPoint cp4 = new CameraPoint(_virtualCamera4, new Vector2(mBounds.max.x, mBounds.max.y), 3);

            //var cp5Position = new Vector2();
            //cp5Position.x = mBounds.size.x * _additionalCameras[0].BoundPosition.x + mBounds.min.x;
            //cp5Position.y = mBounds.size.y * _additionalCameras[0].BoundPosition.y + mBounds.min.y;
            //CameraPoint cp5 = new CameraPoint(_additionalCameras[0].virtualCamera, cp5Position, 4);

            // C1 C2 C5
            mTriangles.Add(new Triangle(mCameraPoints[0],
                                        mCameraPoints[1],
                                        mCameraPoints[4]));
            // C1 C3 C5
            mTriangles.Add(new Triangle(mCameraPoints[0],
                                        mCameraPoints[2],
                                        mCameraPoints[4]));
            // C2 C4 C5
            mTriangles.Add(new Triangle(mCameraPoints[1],
                                        mCameraPoints[3],
                                        mCameraPoints[4]));
            // C3 C4 C5
            mTriangles.Add(new Triangle(mCameraPoints[2],
                                        mCameraPoints[3],
                                        mCameraPoints[4]));
        }
        else if (_additionalCameras.Length == 2)
        {

        }
        else
        {
            return;
        }
    }

    private void updateCameraWeights()
    {
        float delta = 0f;

        switch (_mixingMode)
        {
            case CameraMixingMode.Horizontal:
                delta = (_trPlayerCharacter.position.x - mBounds.min.x) / mBounds.size.x;
                break;
            case CameraMixingMode.Vertical:
                delta = (_trPlayerCharacter.position.y - mBounds.min.y) / mBounds.size.y;
                break;
            default:
                break;
        }

        float camera1Weight = 0f;
        float camera2Weight = 0f;
        float camera3Weight = 0f;

        if (_virtualCamera3 != null)
        {

            if (delta < _deltaThreshold)
            {
                delta = delta / _deltaThreshold;

                float value = _weightCurveX.Evaluate(delta);

                camera1Weight = 1f - value;
                camera2Weight = value;

                camera1Weight = Mathf.Clamp01(camera1Weight);
                camera2Weight = Mathf.Clamp01(camera2Weight);

                mCameraWeights[0] = camera1Weight;
                mCameraWeights[1] = camera2Weight;

                _cameraMixer.UpdateWeights(camera1Weight, camera2Weight, camera3Weight);
            }
            else
            {
                float pivot = 1f - _deltaThreshold;
                delta = (delta - _deltaThreshold) / pivot;

                float value = _weightCurveX.Evaluate(delta);

                camera2Weight = 1f - value;
                camera3Weight = value;

                camera2Weight = Mathf.Clamp01(camera2Weight);
                camera3Weight = Mathf.Clamp01(camera3Weight);

                mCameraWeights[1] = camera2Weight;
                mCameraWeights[2] = camera3Weight;

                _cameraMixer.UpdateWeights(camera1Weight, camera2Weight, camera3Weight);
            }
        }
        else
        {
            float value = _weightCurveX.Evaluate(delta);

            camera1Weight = 1f - value;
            camera2Weight = value;

            camera1Weight = Mathf.Clamp01(camera1Weight);
            camera2Weight = Mathf.Clamp01(camera2Weight);

            mCameraWeights[0] = camera1Weight;
            mCameraWeights[1] = camera2Weight;

            _cameraMixer.UpdateWeights(camera1Weight, camera2Weight);
        }
    }

    private void updateCameraWeightsBySquare()
    {
        Vector3 playerPosition = _trPlayerCharacter.position;

        float deltaX = (playerPosition.x - mBounds.min.x) / mBounds.size.x;
        float deltaY = (playerPosition.y - mBounds.min.y) / mBounds.size.y;

        float valueX = _weightCurveX.Evaluate(deltaX);
        float valueY = _weightCurveY.Evaluate(deltaY);

        float camera1Weight = (1f - valueX) * (1f - valueY);
        float camera2Weight = valueX * (1f - valueY);
        float camera3Weight = (1f - valueX) * valueY;
        float camera4Weight = valueX * valueY;

        camera1Weight *= .2f;
        camera2Weight *= .2f;
        camera3Weight *= 1f;
        camera4Weight *= 1f;

        float weightSum = camera1Weight + camera2Weight + camera3Weight + camera4Weight;

        camera1Weight /= weightSum;
        camera2Weight /= weightSum;
        camera3Weight /= weightSum;
        camera4Weight /= weightSum;

        camera1Weight = Mathf.Clamp01(camera1Weight);
        camera2Weight = Mathf.Clamp01(camera2Weight);
        camera3Weight = Mathf.Clamp01(camera3Weight);
        camera4Weight = Mathf.Clamp01(camera4Weight);

        mCameraWeights[0] = camera1Weight;
        mCameraWeights[1] = camera2Weight;
        mCameraWeights[2] = camera3Weight;
        mCameraWeights[3] = camera4Weight;

        _cameraMixer.UpdateWeights(camera1Weight, camera2Weight, camera3Weight, camera4Weight);
    }

    private void updateCameraWeightsByTriangle()
    {
        Vector2 playerPosition = _trPlayerCharacter.position; // Q
        playerPosition.x = Mathf.Clamp(playerPosition.x, mBounds.min.x, mBounds.max.x);
        playerPosition.y = Mathf.Clamp(playerPosition.y, mBounds.min.y, mBounds.max.y);

        for (int i = 0; i < mTriangles.Count; i++)
        {
            // Q = uA + vB + wC
            // Q-A = v(B-A) + w(C-A)
            Vector2 v0 = mTriangles[i].cp2.position - mTriangles[i].cp1.position;   // B-A
            Vector2 v1 = mTriangles[i].cp3.position - mTriangles[i].cp1.position;   // C-A
            Vector2 v2 = playerPosition - mTriangles[i].cp1.position;     // Q-A

            // v2 = v * v0 + w * v2
            // dot(v2, v0) = v * dot(v0, v0) + w * dot(v1, v0)
            float d20 = Vector2.Dot(v2, v0);
            float d00 = Vector2.Dot(v0, v0);
            float d10 = Vector2.Dot(v1, v0);

            // dot(v2, v1) = v * dot(v0, v1) + w * dot(v1, v1)
            float d21 = Vector2.Dot(v2, v1);
            float d01 = Vector2.Dot(v0, v1); // d10 == d01 (교환법칙)
            float d11 = Vector2.Dot(v1, v1); 

            // 크래머 공식(Cramer's Rule)
            float determinant = d00 * d11 - d10 * d01;

            float v = (d20 * d11 - d01 * d21) / determinant;
            float w = (d00 * d21 - d20 * d10) / determinant;
            float u = 1f - v - w;

            // u + v + w = 1
            if(u >= 0f && v >= 0f && w >= 0f)
            {
                mTriangleIndex = i;

                // Q는 삼각형 내부에 존재
                _cameraMixer.UpdateWeights((mTriangles[i].cp1.index, u), 
                                            (mTriangles[i].cp2.index, v), 
                                            (mTriangles[i].cp3.index, w));

                break;
            }
        }
    }

    private void updateCameraWeightsByDistance()
    {
        Vector2 playerPosition = _trPlayerCharacter.position; // Q
        playerPosition.x = Mathf.Clamp(playerPosition.x, mBounds.min.x, mBounds.max.x);
        playerPosition.y = Mathf.Clamp(playerPosition.y, mBounds.min.y, mBounds.max.y);

        float[] weights = new float[mCameraPoints.Count];
        float sum = 0f;
        float p = 2f;

        for (int i = 0; i < mCameraPoints.Count; i++)
        {
            float distance = Vector2.Distance(playerPosition, mCameraPoints[i].position);

            if (distance < float.Epsilon)
            {
                for(int wi = 0; wi < weights.Length; wi++)
                    weights[i] = 0f;

                weights[i] = 1f;
                sum = 1f;
                break;
            }

            weights[i] = 1f / Mathf.Pow(distance, _damping);
            sum += weights[i];
        }

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] /= sum;
        }

        _cameraMixer.UpdateWeights(weights);
    }

    private void OnDrawGizmos()
    {
        if (_drawBounds)
        {
            var boxCollider = GetComponent<BoxCollider>();
            Handles.color = Color.blue;
            Vector3 center = new Vector3(transform.localScale.x * boxCollider.center.x,
                                        transform.localScale.y * boxCollider.center.y,
                                        transform.localScale.z * boxCollider.center.z);
            Vector3 size = new Vector3(transform.localScale.x * boxCollider.size.x,
                                        transform.localScale.y * boxCollider.size.y,
                                        transform.localScale.z * boxCollider.size.z);
            Handles.DrawWireCube(transform.position + center, size);
        }

        if (!Application.isPlaying)
        {
            return;
        }

        Vector3 playerPosition = _trPlayerCharacter.position;
        playerPosition.z = mBounds.min.z;

        for (int i = 0; i < mTriangles.Count; i++)
        {
            Vector3 p1 = mTriangles[i].cp1.position;
            p1.z = mBounds.min.z;
            Vector3 p2 = mTriangles[i].cp2.position;
            p2.z = mBounds.min.z;
            Vector3 p3 = mTriangles[i].cp3.position;
            p3.z = mBounds.min.z;

            Handles.color = Color.red;
            Handles.DrawPolyLine(p1, p2, p3, p1);

            if (mTriangleIndex == i)
            {
                //Vector3 playerPosition = _trPlayerCharacter.position;
                //playerPosition.z = mBounds.min.z;

                Handles.color = Color.blue;
                Handles.DrawLine(playerPosition, p1);
                Handles.DrawLine(playerPosition, p2);
                Handles.DrawLine(playerPosition, p3);
            }
        }

        if (_mixingMode == CameraMixingMode.Distance && mbActivated)
        {
            var distances = new float[mCameraPoints.Count];
            int nearestIndex = -1;
            float minDistance = float.MaxValue;

            for (int i = 0; i < mCameraPoints.Count; i++)
            {
                Vector3 cp = mCameraPoints[i].position;
                cp.z = mBounds.min.z;

                distances[i] = Vector3.Distance(cp, playerPosition);

                if (distances[i] < minDistance)
                {
                    minDistance = distances[i];
                    nearestIndex = i;
                }
            }

            for (int i = 0; i < mCameraPoints.Count; i++)
            {
                Vector3 cp = mCameraPoints[i].position;
                cp.z = mBounds.min.z;

                if (i == nearestIndex)
                {
                    Handles.color = Color.magenta;
                    Handles.DrawLine(playerPosition, cp);
                }
                else
                {
                    Handles.color = Color.gray;
                    Handles.DrawLine(playerPosition, cp);
                }
            }
        }

        if (_mixingMode == CameraMixingMode.Horizontal && mbActivated && _drawWeightGuide)
        {
            if(_virtualCamera3 == null)
            {
                // Camera 1
                Vector3 center1 = new Vector3(mBounds.min.x + (playerPosition.x - mBounds.min.x) / 2f,
                                            mBounds.center.y,
                                            mBounds.center.z);
                Vector3 size1 = new Vector3(playerPosition.x - mBounds.min.x,
                                            mBounds.size.y,
                                            mBounds.size.z);
                Color color1 = Color.magenta;
                color1.a = mCameraWeights[0] * .5f;
                Gizmos.color = color1;
                Gizmos.DrawCube(center1, size1);

                // Camera 2
                Vector3 center2 = new Vector3(playerPosition.x + (mBounds.max.x - playerPosition.x) / 2f,
                                            mBounds.center.y,
                                            mBounds.center.z);
                Vector3 size2 = new Vector3(mBounds.max.x - playerPosition.x,
                                            mBounds.size.y,
                                            mBounds.size.z);
                Color color2 = Color.magenta;
                color2.a = mCameraWeights[1] * .5f;
                Gizmos.color = color2;
                Gizmos.DrawCube(center2, size2);
            }
            else
            {
                float thresholdX = mBounds.min.x + (mBounds.size.x * _deltaThreshold);

                if (playerPosition.x < thresholdX)
                {
                    // Camera 1
                    Vector3 center1 = new Vector3(mBounds.min.x + (playerPosition.x - mBounds.min.x) / 2f,
                                                mBounds.center.y,
                                                mBounds.center.z);
                    Vector3 size1 = new Vector3(playerPosition.x - mBounds.min.x,
                                                mBounds.size.y,
                                                mBounds.size.z);
                    Color color1 = Color.magenta;
                    color1.a = mCameraWeights[0] * .5f;
                    Gizmos.color = color1;
                    Gizmos.DrawCube(center1, size1);

                    // Camera 2
                    Vector3 center2 = new Vector3(playerPosition.x + (thresholdX - playerPosition.x) / 2f,
                                                mBounds.center.y,
                                                mBounds.center.z);
                    Vector3 size2 = new Vector3(thresholdX - playerPosition.x,
                                                mBounds.size.y,
                                                mBounds.size.z);
                    Color color2 = Color.magenta;
                    color2.a = mCameraWeights[1] * .5f;
                    Gizmos.color = color2;
                    Gizmos.DrawCube(center2, size2);
                }
                else
                {
                    // Camera 2
                    Vector3 center2 = new Vector3(thresholdX + (playerPosition.x - thresholdX) / 2f,
                                                mBounds.center.y,
                                                mBounds.center.z);
                    Vector3 size2 = new Vector3(playerPosition.x - thresholdX,
                                                mBounds.size.y,
                                                mBounds.size.z);
                    Color color2 = Color.magenta;
                    color2.a = mCameraWeights[1] * .5f;
                    Gizmos.color = color2;
                    Gizmos.DrawCube(center2, size2);

                    // Camera 3
                    Vector3 center3 = new Vector3(playerPosition.x + (mBounds.max.x - playerPosition.x) / 2f,
                                                mBounds.center.y,
                                                mBounds.center.z);
                    Vector3 size3 = new Vector3(mBounds.max.x - playerPosition.x,
                                                mBounds.size.y,
                                                mBounds.size.z);
                    Color color3 = Color.magenta;
                    color3.a = mCameraWeights[2] * .5f;
                    Gizmos.color = color3;
                    Gizmos.DrawCube(center3, size3);
                }
            }
        }

        if (_mixingMode == CameraMixingMode.Vertical && mbActivated && _drawWeightGuide)
        {
            // Camera 1
            Vector3 center1 = new Vector3(mBounds.center.x,
                                        mBounds.min.y + (playerPosition.y - mBounds.min.y) / 2f,
                                        mBounds.center.z);
            Vector3 size1 = new Vector3(mBounds.size.x,
                                        playerPosition.y - mBounds.min.y,
                                        mBounds.size.z);
            Color color1 = Color.magenta;
            color1.a = mCameraWeights[0] * .5f;
            Gizmos.color = color1;
            Gizmos.DrawCube(center1, size1);

            // Camera 2
            Vector3 center2 = new Vector3(mBounds.center.x,
                                        playerPosition.y + (mBounds.max.y - playerPosition.y) / 2f,
                                        mBounds.center.z);
            Vector3 size2 = new Vector3(mBounds.size.x,
                                        mBounds.max.y - playerPosition.y,
                                        mBounds.size.z);
            Color color2 = Color.magenta;
            color2.a = mCameraWeights[1] * .5f;
            Gizmos.color = color2;
            Gizmos.DrawCube(center2, size2);
        }

        if (_mixingMode == CameraMixingMode.Square && mbActivated && _drawWeightGuide)
        {
            // Camera 1
            Vector3 center1 = new Vector3(mBounds.min.x + (playerPosition.x - mBounds.min.x) / 2f,
                                        mBounds.min.y + (playerPosition.y - mBounds.min.y) / 2f,
                                        mBounds.center.z);
            Vector3 size1 = new Vector3(playerPosition.x - mBounds.min.x,
                                        playerPosition.y - mBounds.min.y,
                                        mBounds.size.z);
            Color color1 = Color.magenta;
            color1.a = mCameraWeights[0] * .5f;
            Gizmos.color = color1;
            Gizmos.DrawCube(center1, size1);

            // Camera 2
            Vector3 center2 = new Vector3(playerPosition.x + (mBounds.max.x - playerPosition.x) / 2f,
                                        mBounds.min.y + (playerPosition.y - mBounds.min.y) / 2f,
                                        mBounds.center.z);
            Vector3 size2 = new Vector3(mBounds.max.x - playerPosition.x,
                                        playerPosition.y - mBounds.min.y,
                                        mBounds.size.z);
            Color color2 = Color.magenta;
            color2.a = mCameraWeights[1] * .5f;
            Gizmos.color = color2;
            Gizmos.DrawCube(center2, size2);

            // Camera 3
            Vector3 center3 = new Vector3(mBounds.min.x + (playerPosition.x - mBounds.min.x) / 2f,
                                        playerPosition.y + (mBounds.max.y - playerPosition.y) / 2f,
                                        mBounds.center.z);
            Vector3 size3 = new Vector3(playerPosition.x - mBounds.min.x,
                                        mBounds.max.y - playerPosition.y,
                                        mBounds.size.z);
            Color color3 = Color.magenta;
            color3.a = mCameraWeights[2] * .5f;
            Gizmos.color = color3;
            Gizmos.DrawCube(center3, size3);

            // Camera 4
            Vector3 center4 = new Vector3(playerPosition.x + (mBounds.max.x - playerPosition.x) / 2f,
                                        playerPosition.y + (mBounds.max.y - playerPosition.y) / 2f,
                                        mBounds.center.z);
            Vector3 size4 = new Vector3(mBounds.max.x - playerPosition.x,
                                        mBounds.max.y - playerPosition.y,
                                        mBounds.size.z);
            Color color4 = Color.magenta;
            color4.a = mCameraWeights[3] * .5f;
            Gizmos.color = color4;
            Gizmos.DrawCube(center4, size4);
        }
    }
}
