using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RopeVerlet : MonoBehaviour
{
    public struct RopeSegment
    {
        public Vector3 currentPosition;
        public Vector3 oldPosition;

        public RopeSegment(Vector3 pos)
        {
            currentPosition = pos;
            oldPosition = pos;
        }
    }

    [System.Serializable]
    public struct GrabPoint
    {
        public Transform transform;
        public int segmentIndex;
        public HumanBodyBones humanBodyBone;        // 캐릭터가 Grab하는 부위
        public bool active;

        public GrabPoint(Transform t, int index, HumanBodyBones humanBodyBone)
        {
            transform = t;
            segmentIndex = index;
            this.humanBodyBone = humanBodyBone;
            active = true;
        }
    }

    public event System.Action<int, Collider[]> onCollision;
    public event System.Action onAfterSimulateSegments = null;

    public List<GrabPoint> GrabPoints => _grabPoints;
    public Transform JointPoint => _trJointPoint;
    public int JointPointIndex => _JointPointIndex;

    [Header("Debug")]
    [SerializeField] private bool _debugSegments = true;
    [SerializeField] private bool _showPendulumInfo = false;

    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 50;               // 총 Segment 개수
    [SerializeField] private float _ropeSegmentLength = 0.225f;         // Segment 당 길이

    [Header("Grab Points")]
    [SerializeField] private List<GrabPoint> _grabPoints = new List<GrabPoint>();
    [SerializeField] private Transform _trJointPoint;                     // jointPoint의 transform .._trJointPoint로 변수명 수정하기
    [SerializeField] private int _JointPointIndex = 9;

    [Header("Physics")]
    [SerializeField] private Vector3 _gravityForce = new Vector3(0f, -2f, 0f);
    [SerializeField] private float _dampingFactor = 0.98f;
    [SerializeField] private bool _onlyTrigger = true;              // true이면 물리적인 충돌을 체크하지 않음
    [SerializeField] private LayerMask _collisionLayerMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.1f;
    [SerializeField] private float _correctionClampAmount = 0.1f;

    [Header("Pendulum")]
    [SerializeField] private bool _usePendulum = true;
    [SerializeField] private float _pendulumGravity = 9.8f;
    [SerializeField] private float _pendulumDamping = 0.98f;
    [SerializeField] private float _swingTorque = 5f;
    [SerializeField] private float _swingTorqueDamping = .03f;
    [SerializeField] private float _maxOmegaMagnitude = .8f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 50;
    [SerializeField] private float _constraintIntensity = 0.5f;

    [Header("Optimizations")]
    [SerializeField] private int _collisionSegmentInterval = 2;

    private LineRenderer mLineRenderer;
    private List<RopeSegment> mRopeSegments = new List<RopeSegment>();

    private Vector3 mRopeStartPoint;

    // 아래는 디버그 용도로 사용된 변수들
    private Vector3 mMinPoint;          // 로프가 얼마나 스윙되는 지 시각적으로 보기위한 변수
                                        // 관련 코드가 일반 모드에서만 작성이 돼있어서 Pendulum용으로 새로 작성하던지 제거하면 될 듯
    private Vector3 mMaxPoint;

    // 각속도로 계산하는 방식에 사용되는 변수들
    private float mDeltaTorque = 0f;                    // 주어진 토크
    private float theta = 0f;                           // 각도
    private float omega = 0f;                           // 각가속도

    // 각 모드로 전환될 때 한 번만 실행되는 코드를 위해서 선언
    private bool mbEnterNormal = false;
    private bool mbEnterPendulum = false;

    public Vector3 GetSegmentPosition(int index)
    {
        if(index < 0 || index >= mRopeSegments.Count)
            return Vector3.zero;

        return mRopeSegments[index].currentPosition;
    }

    public void SetJointIndex(int index)
    {
        _JointPointIndex = index;
    }

    public void AddJointIndex(int amount)
    {
        _JointPointIndex += amount;
    }

    public void AddGrabPoint(int segmentIndex, Transform pointTransform, HumanBodyBones humanBodyBone)
    {
        var newGrabPoint = new GrabPoint(pointTransform, segmentIndex, humanBodyBone);

        _grabPoints.Add(newGrabPoint);
    }

    public GrabPoint GetGrabPoint(HumanBodyBones humanBodyBone)
    {
        for(int i = 0; i < _grabPoints.Count; i++)
        {
            if(_grabPoints[i].humanBodyBone == humanBodyBone)
            {
                return _grabPoints[i];
            }
        }

        return new GrabPoint();
    }

    public void SetGrabPoint(GrabPoint grabPoint)
    {
        for (int i = 0; i < _grabPoints.Count; i++)
        {
            if (_grabPoints[i].humanBodyBone == grabPoint.humanBodyBone)
            {
                _grabPoints[i] = grabPoint;
                return;
            }
        }
    }

    public void ClearGrabPoints()
    {
        _grabPoints.Clear();
    }

    public void SetNormal()
    {
        _usePendulum = false;
    }

    public void SetPendulum()
    {
        _usePendulum = true;
    }

    public bool ValidateSegmentIndex(int segmentIndex)
    {
        if(segmentIndex >= 0 && segmentIndex < _numOfRopeSegments)
            return true;

        return false;
    }

    public void SwingLeft()
    {
        mDeltaTorque = -_swingTorque;
    }

    public void SwingRight()
    {
        mDeltaTorque = _swingTorque;
    }

    public void StopSwing()
    {
        mDeltaTorque = 0f;
    }

    private void Awake()
    {
        mLineRenderer = GetComponent<LineRenderer>();
        mLineRenderer.positionCount = _numOfRopeSegments;

        mRopeStartPoint = transform.position;
        mRopeStartPoint.z = 0f;

        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            mRopeSegments.Add(new RopeSegment(mRopeStartPoint));
            mRopeStartPoint.y -= _ropeSegmentLength;
        }

        _trJointPoint.position = mRopeSegments[_JointPointIndex].currentPosition;
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    mMinPoint = Vector3.zero;
        //    mMaxPoint = Vector3.zero;
        //}

        DrawRope();
    }

    private void FixedUpdate()
    {
        if (!_usePendulum)
        {
            if(!mbEnterNormal)
            {
                mbEnterNormal = true;
                mbEnterPendulum = false;
                // Debug.Log("Enter Normal");
            }

            Simulate();
        }
        else
        {
            if(!mbEnterPendulum)
            {
                mbEnterPendulum = true;
                mbEnterNormal = false;
                // Debug.Log("Enter Pendulum");
            }

            SimulatePendulum();
        }

        for (int i = 0; i < _numOfConstraintRuns; i++)
        {
            ApplyConstraints();

            if (i % _collisionSegmentInterval == 0)
                HandleCollisions();
        }

        // 일반 모드에서 단진자 모드로 전환될 때 자연스럽게 전환시키기 위해서
        // 일반 모드에서도 각속도와 각을 계산(일반 모드에서 각속도와 각이 사용되지는 않음)
        if(!_usePendulum)
        {
            Vector3 dirForAngle = (mRopeSegments[_JointPointIndex].currentPosition - mRopeSegments[0].currentPosition).normalized;
            dirForAngle.z = 0f;
            float angle = Vector3.Angle(Vector3.down, dirForAngle);
            theta = angle * Mathf.Deg2Rad;

            if (dirForAngle.x < 0f)
            {
                theta = -theta;
                angle = -angle;
            }

            float distanceToFirst = (mRopeSegments[_JointPointIndex].currentPosition - mRopeSegments[0].currentPosition).magnitude;
            float sinTheta = Mathf.Sin(theta);
            //float alpha = -(_pendulumGravity / distanceToFirst) * sinTheta - _pendulumDamping * omega;
            float alpha = -(_gravityForce.y / distanceToFirst) * sinTheta - _dampingFactor * omega;
            omega += alpha * Time.fixedDeltaTime;
            omega = Mathf.Clamp(omega, -_maxOmegaMagnitude, _maxOmegaMagnitude);
            // Debug.Log($"[0] Theta: {theta}({angle}), sin(theta):{sinTheta}, alpha:{alpha}, Omega: {omega}, L: {distanceToFirst}");
        }
    }

    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_numOfRopeSegments];
        for(int i = 0; i < _numOfRopeSegments; i++)
        {
            ropePositions[i] = mRopeSegments[i].currentPosition;
        }

        mLineRenderer.SetPositions(ropePositions);
    }

    private void Simulate()
    {
        for (int i = 0; i < mRopeSegments.Count; i++)
        {
            RopeSegment segment = mRopeSegments[i];

            bool bGrab = false;

            // Grab되는 Point들은 위치를 계산하지 않음
            for (int j = 0; j < _grabPoints.Count; j++)
            {
                if (i == _grabPoints[j].segmentIndex)
                {
                    if (!_grabPoints[j].active)
                        break;

                    bGrab = true;
                    break;
                }
            }

            if (bGrab)
                continue;

            Vector3 velocity = (segment.currentPosition - segment.oldPosition) * _dampingFactor;

            segment.oldPosition = segment.currentPosition;
            segment.currentPosition += velocity;
            segment.currentPosition += _gravityForce * Time.fixedDeltaTime;

            if (i == _JointPointIndex)
            {
                _trJointPoint.position = segment.currentPosition;
            }

            mRopeSegments[i] = segment;
        }

        onAfterSimulateSegments?.Invoke();

        // Grab되는 Segment들을 해당 위치로 고정
        for (int i = 0; i < mRopeSegments.Count; i++)
        {
            RopeSegment segment = mRopeSegments[i];

            for (int j = 0; j < _grabPoints.Count; j++)
            {
                if (i == _grabPoints[j].segmentIndex)
                {
                    if (!_grabPoints[j].active)
                        break;

                    segment.currentPosition = _grabPoints[j].transform.position;
                    mRopeSegments[i] = segment;
                    break;
                }
            }
        }
    }

    private void SimulatePendulum()
    {
        // First segment is fixed
        RopeSegment segment = mRopeSegments[0];

        Vector3 velocity = (segment.currentPosition - segment.oldPosition) * _dampingFactor;

        segment.oldPosition = segment.currentPosition;
        segment.currentPosition += velocity;
        segment.currentPosition += _gravityForce * Time.fixedDeltaTime;

        mRopeSegments[0] = segment;

        // Joint Point
        segment = mRopeSegments[_JointPointIndex];

        segment.oldPosition = segment.currentPosition;

        float distanceToFirst = (segment.currentPosition - mRopeSegments[0].currentPosition).magnitude;
        float tau = mDeltaTorque;
        mDeltaTorque = Mathf.Lerp(mDeltaTorque, 0f, _swingTorqueDamping);

        float sinTheta = Mathf.Sin(theta);
        
        // 각가속도
        float alpha = -(_pendulumGravity / distanceToFirst) * sinTheta
                      + (tau / distanceToFirst * distanceToFirst) - _pendulumDamping * omega;

        omega += alpha * Time.fixedDeltaTime;
        omega = Mathf.Clamp(omega, -_maxOmegaMagnitude, _maxOmegaMagnitude);
        theta += omega * Time.fixedDeltaTime;

        if(_showPendulumInfo)
            Debug.Log($"[1] theta:{theta}({theta * Mathf.Rad2Deg}), sin(theta):{sinTheta}, alpha:{alpha}, omega:{omega}, L: {distanceToFirst}");

        Vector3 p = mRopeSegments[0].currentPosition + new Vector3(Mathf.Sin(theta), -Mathf.Cos(theta), 0f) * distanceToFirst;
        segment.currentPosition = p;

        mRopeSegments[_JointPointIndex] = segment;

        _trJointPoint.position = segment.currentPosition;
        Vector3 dirToFirst = (mRopeSegments[0].currentPosition - segment.currentPosition).normalized;
        _trJointPoint.up = dirToFirst;

        // First to Joint Point
        Vector3 dirFirstToJoint = (mRopeSegments[_JointPointIndex].currentPosition - mRopeSegments[0].currentPosition).normalized;

        for (int i = 1; i < _JointPointIndex; i++)
        {
            segment = mRopeSegments[i];

            segment.oldPosition = segment.currentPosition;
            segment.currentPosition = mRopeSegments[0].currentPosition + dirFirstToJoint * _ropeSegmentLength * i;

            mRopeSegments[i] = segment;
        }

        // Joint Point to End
        for(int i = _JointPointIndex + 1; i < mRopeSegments.Count; i++)
        {
            segment = mRopeSegments[i];

            velocity = (segment.currentPosition - segment.oldPosition) * _dampingFactor;

            segment.oldPosition = segment.currentPosition;
            segment.currentPosition += velocity;
            segment.currentPosition += _gravityForce * Time.fixedDeltaTime;

            mRopeSegments[i] = segment;
        }

        onAfterSimulateSegments?.Invoke();

        // Grab Point
        for (int i = _JointPointIndex + 1; i < mRopeSegments.Count; i++)
        {
            segment = mRopeSegments[i];

            for (int j = 0; j < _grabPoints.Count; j++)
            {
                if (i == _grabPoints[j].segmentIndex)
                {
                    if (!_grabPoints[j].active)
                        break;

                    segment.currentPosition = _grabPoints[j].transform.position;
                    mRopeSegments[i] = segment;
                    break;
                }
            }
        }
    }

    private void ApplyConstraints()
    {
        // First segment is fixed
        RopeSegment firstSegment = mRopeSegments[0];
        firstSegment.currentPosition = transform.position;
        mRopeSegments[0] = firstSegment;

        // Constraint를 0부터 하는 지 Joint Segment부터 하는 지에 따라 결과가 다른 거 같다
        // 나중에 테스트 해보고 나은 결과로 수정하면 될 거 같다
        //int index = _JointPointIndex;
        int index = 0;

        for(int i = index; i < _numOfRopeSegments - 1; i++)
        {
            RopeSegment currentSegment = mRopeSegments[i];
            RopeSegment nextSegment = mRopeSegments[i + 1];

            float dist = (currentSegment.currentPosition - nextSegment.currentPosition).magnitude;
            float difference = dist - _ropeSegmentLength;

            Vector3 changeDir = (currentSegment.currentPosition - nextSegment.currentPosition).normalized;
            Vector3 changeVector = changeDir * difference;

            if (i != index)
            {
                if (_grabPoints.Count > 0)
                {
                    bool bIsGrabPoint = false;

                    for (int j = 0; j < _grabPoints.Count; j++)
                    {
                        // Grab되는 Segment를 기준으로 바로 위나 아래 Segment 쪽으로 당겨지거나 밀리지 않게 계산
                        if (i == _grabPoints[j].segmentIndex)
                        {
                            if (!_grabPoints[j].active)
                                break;

                            nextSegment.currentPosition += changeVector * _constraintIntensity;
                            mRopeSegments[i + 1] = nextSegment;
                            bIsGrabPoint = true;
                            break;
                        }
                        else if (i + 1 == _grabPoints[j].segmentIndex)
                        {
                            if (!_grabPoints[j].active)
                                break;

                            currentSegment.currentPosition -= changeVector * .1f;
                            mRopeSegments[i] = currentSegment;
                            bIsGrabPoint = true;
                            break;
                        }
                    }

                    if (!bIsGrabPoint)
                    {
                        currentSegment.currentPosition -= changeVector * _constraintIntensity;
                        mRopeSegments[i] = currentSegment;
                        nextSegment.currentPosition += changeVector * _constraintIntensity;
                        mRopeSegments[i + 1] = nextSegment;
                    }
                }
                else
                {
                    //if (i == _swingIndex)
                    //{
                    //    nextSegment.currentPosition += changeVector;
                    //    mRopeSegments[i + 1] = nextSegment;
                    //    continue;
                    //}

                    currentSegment.currentPosition -= changeVector * _constraintIntensity;
                    mRopeSegments[i] = currentSegment;
                    nextSegment.currentPosition += changeVector * _constraintIntensity;
                    mRopeSegments[i + 1] = nextSegment;
                }
            }
            else
            {
                // First segment is fixed, only move the second one
                nextSegment.currentPosition += changeVector;
                mRopeSegments[i + 1] = nextSegment;
            }
        }
    }

    private void HandleCollisions()
    {
        for (int i = 1; i < mRopeSegments.Count; i++)
        {
            RopeSegment segment = mRopeSegments[i];
            Vector3 velocity = segment.currentPosition - segment.oldPosition;
            Collider[] colliders = Physics.OverlapSphere(segment.currentPosition, _collisionRadius, _collisionLayerMask);

            if(!_onlyTrigger)
            {
                foreach (Collider collider in colliders)
                {
                    // 물리적인 충돌 처리 코드
                    Vector3 closestPoint = collider.ClosestPoint(segment.currentPosition);
                    float distance = Vector3.Distance(closestPoint, segment.currentPosition);

                    if (distance < _collisionRadius)
                    {
                        Vector3 normal = (segment.currentPosition - closestPoint).normalized;

                        if (normal == Vector3.zero)
                        {
                            normal = (segment.currentPosition - collider.transform.position).normalized;
                        }

                        float depth = _collisionRadius - distance;
                        segment.currentPosition += normal * depth;

                        velocity = Vector3.Reflect(velocity, normal) * _bounceFactor;
                    }
                }

                // 물리적인 충돌 처리 코드
                segment.oldPosition = segment.currentPosition - velocity;// * _correctionClampAmount;
                mRopeSegments[i] = segment;
            }

            if (colliders.Length > 0)
            {
                onCollision?.Invoke(i, colliders);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_debugSegments && mRopeSegments.Count > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(mRopeSegments[0].currentPosition, .1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(mRopeSegments[_JointPointIndex].currentPosition, .1f);

            for (int i = 0; i < _JointPointIndex; ++i)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(mRopeSegments[i].currentPosition, .05f);
            }

            for (int i = _JointPointIndex + 1; i < _numOfRopeSegments; ++i)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(mRopeSegments[i].currentPosition, .05f);
            }
        }

        //Gizmos.color = Color.green;
        //Gizmos.DrawSphere(mMinPoint, .2f);
        //Gizmos.DrawSphere(mMaxPoint, .2f);
    }
}
