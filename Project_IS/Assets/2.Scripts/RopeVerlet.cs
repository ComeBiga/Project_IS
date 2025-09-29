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
        public HumanBodyBones humanBodyBone;
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
    public Transform JointPoint => _JointPoint;
    public int JointPointIndex => _JointPointIndex;

    [Header("Debug")]
    [SerializeField] private bool _debugSegments = true;
    [SerializeField] private bool _showPendulumInfo = false;

    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 50;
    [SerializeField] private float _ropeSegmentLength = 0.225f;

    [Header("Grab Points")]
    [SerializeField] private List<GrabPoint> _grabPoints = new List<GrabPoint>();
    // [SerializeField] private GrabPoint[] _grabPoints;
    [SerializeField] private Transform _JointPoint;
    [SerializeField] private int _JointPointIndex = 9;
    [SerializeField] private int _swingIndex = 9;
    [SerializeField] private float _swingForce = 10f;
    [SerializeField] private float _swingAddForce = 10f;
    [SerializeField] private float _swingAccelerate = .2f;
    [SerializeField] private float _swingAccMultiplier = 1f;
    [SerializeField] private float _outForce = .1f;
    [SerializeField] private float _outForceTime = 1f;
    [SerializeField] private float _forceDamping = .1f;
    [SerializeField] private AnimationSwinger _animationSwinger;

    [Header("Physics")]
    [SerializeField] private Vector3 _gravityForce = new Vector3(0f, -2f, 0f);
    [SerializeField] private float _dampingFactor = 0.98f;
    [SerializeField] private bool _onlyTrigger = true;
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
    private float mDeltaForce = 0f;
    private float mDeltaOutForce = 0f;
    private float mRemainAccel = 0f;
    private float mRemainOutForce = 0f;
    private bool mbSwingForce = false;
    private Vector3 mDirForce;
    private Vector3 mDirAngle;
    private Vector3 mOriginForce;
    private Vector3 mMinPoint;
    private Vector3 mMaxPoint;
    private Vector3 mFinalForce;
    private float mTimer = 0f;
    private float mSinFromAngle = 0f;
    private float mDeltaTorque = 0f;

    private float theta = 0f;
    private float omega = 0f;

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

        //for (int i = 0; i < _grabPoints.Length; i++)
        //{
        //    _grabPoints[i].segmentIndex += amount;
        //}
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

        //mRopeStartPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mRopeStartPoint = transform.position;
        // mRopeStartPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mRopeStartPoint.z = 0f;

        for (int i = 0; i < _numOfRopeSegments; i++)
        {
            mRopeSegments.Add(new RopeSegment(mRopeStartPoint));
            mRopeStartPoint.y -= _ropeSegmentLength;
        }

        _JointPoint.position = mRopeSegments[_JointPointIndex].currentPosition;
    }

    private void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Y))
        //{
        //    mDeltaForce = _swingForce;
        //    mDeltaOutForce = _swingForce;
        //    mRemainAccel = _swingAddForce * mSinFromAngle * _swingAccMultiplier;
        //    mRemainOutForce = _outForce * mSinFromAngle;
        //    // mbSwingForce = true;
        //    mDeltaTorque = _swingTorque;

        //    mTimer = 0f;
        //}
        //if(Input.GetKeyDown(KeyCode.T))
        //{
        //    mDeltaForce = -_swingForce;
        //    mDeltaOutForce = _swingForce;
        //    mRemainAccel = _swingAddForce * mSinFromAngle * _swingAccMultiplier;
        //    mRemainOutForce = _outForce * mSinFromAngle;
        //    // mbSwingForce = true;
        //    mDeltaTorque = -_swingTorque;

        //    mTimer = 0f;
        //}

        //if (Input.GetKeyDown(KeyCode.U))
        //{
        //    AddIndex(-1);
        //}
        //if (Input.GetKeyDown(KeyCode.J))
        //{
        //    AddIndex(1);
        //}

        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    mMinPoint = Vector3.zero;
        //    mMaxPoint = Vector3.zero;
        //}

        //mTimer += Time.deltaTime;

        //if(mTimer > .5f)
        //{
        //    mbSwingForce = false;
        //}

        DrawRope();
    }

    private void FixedUpdate()
    {
        // Simulate();
        if (!_usePendulum)
        {
            if(!mbEnterNormal)
            {
                mbEnterNormal = true;
                mbEnterPendulum = false;
                // Debug.Log("Enter Normal");
            }
            // theta = 0f;
            // omega = 0f;
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

        //if (_animationSwinger.target != null)
        //{
        //    _animationSwinger.UpdatePos();
        //    // Debug.Log("Rope Update");

        //}

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

            for (int j = 0; j < _grabPoints.Count; j++)
            {
                if (i == _grabPoints[j].segmentIndex)
                {
                    if (!_grabPoints[j].active)
                        break;

                    //segment.currentPosition = _grabPoints[j].transform.position;
                    //mRopeSegments[i] = segment;
                    bGrab = true;
                    continue;
                }
            }

            if (bGrab)
                continue;

            Vector3 velocity = (segment.currentPosition - segment.oldPosition) * _dampingFactor;

            segment.oldPosition = segment.currentPosition;

            // if (i < _grabPoints[0].segmentIndex || i > _grabPoints[_grabPoints.Length - 1].segmentIndex)
            segment.currentPosition += velocity;
            segment.currentPosition += _gravityForce * Time.fixedDeltaTime;

            Vector3 dirForAngle = (segment.currentPosition - mRopeSegments[0].currentPosition).normalized;
            dirForAngle.z = 0f;
            float angle = Vector3.Angle(Vector3.down, dirForAngle);
            float sinFromAngle = Mathf.Sin(angle * Mathf.Deg2Rad);
            mSinFromAngle = sinFromAngle;

            //theta = angle * Mathf.Deg2Rad;

            //if(dirForAngle.x < 0f)
            //{
            //    theta = -theta;
            //    angle = -angle;
            //}

            //float distanceToFirst = (mRopeSegments[_JointPointIndex].currentPosition - mRopeSegments[0].currentPosition).magnitude;
            //float sinTheta = Mathf.Sin(theta);
            ////float alpha = -(_pendulumGravity / distanceToFirst) * sinTheta - _pendulumDamping * omega;
            //float alpha = -(_gravityForce.y / distanceToFirst) * sinTheta - _dampingFactor * omega;
            //omega += alpha * Time.fixedDeltaTime;
            //omega = Mathf.Clamp(omega, -_maxOmegaMagnitude, _maxOmegaMagnitude);
            //Debug.Log($"[0] Theta: {theta}({angle}), sin(theta):{sinTheta}, alpha:{alpha}, Omega: {omega}, L: {distanceToFirst}");

            // segment.currentPosition += dirForAngle * _outForce * Time.fixedDeltaTime;

            // Swinging
            if (i == _swingIndex)
            {
                Vector3 dirToForce = (Vector3.down - dirForAngle).normalized;

                if (dirForAngle.x > 0f)
                {
                    dirToForce = new Vector3(-dirForAngle.y, dirForAngle.x, 0f);
                }
                else if(dirForAngle.x < 0f)
                {
                    dirToForce = new Vector3(-dirForAngle.y, dirForAngle.x, 0f);
                }
                else
                {
                    dirToForce = Vector3.right;
                }

                // Debug.Log($"{dirForAngle}, {dirToForce}");

                mDirForce = dirToForce;
                mDirAngle = dirForAngle;
                mOriginForce = segment.currentPosition;
                // Vector3 swingForce = Vector3.zero;

                if (mbSwingForce)
                {
                    // mbSwingForce = false;
                    float mDeltaAccel = Mathf.Lerp(0f, mRemainAccel, _swingAccelerate);
                    mRemainAccel -= mDeltaAccel;

                    if (mDeltaForce > 0f)
                    {
                        mDeltaForce += mDeltaAccel;
                    }
                    else
                    {
                        mDeltaForce -= mDeltaAccel;
                    }

                    segment.currentPosition += dirToForce * mDeltaForce * Time.fixedDeltaTime;
                    segment.currentPosition += dirForAngle * Mathf.Abs(mDeltaForce) * Time.fixedDeltaTime;
                    mDeltaForce = Mathf.Lerp(mDeltaForce, 0f, _forceDamping);

                    //float deltaOutForce = Mathf.Lerp(0f, mRemainOutForce, _swingAccelerate);
                    //mRemainOutForce -= deltaOutForce;

                    //mDeltaOutForce += deltaOutForce;

                    // if(mTimer < _outForceTime)
                    // segment.currentPosition += dirForAngle * mDeltaOutForce * Time.fixedDeltaTime;

                    // Gizmos
                    // swingForce = dirToForce * mDeltaForce;
                }

                // Gizmos
                if (segment.currentPosition.x < mMinPoint.x)
                {
                    mMinPoint = segment.currentPosition;
                    mMinPoint.z = 0f;
                }

                if (segment.currentPosition.x > mMaxPoint.x)
                {
                    mMaxPoint = segment.currentPosition;
                    mMaxPoint.z = 0f;
                }

                mFinalForce = segment.currentPosition - segment.oldPosition;
            }

            if (i == _JointPointIndex)
            {
                _JointPoint.position = segment.currentPosition;
                // Debug.Log(_JointPoint.position);
            }

            mRopeSegments[i] = segment;
        }

        onAfterSimulateSegments?.Invoke();

        if (_animationSwinger.target != null)
        {
            _animationSwinger.UpdatePos();
            // Debug.Log("Rope Update");
        }

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
                    continue;
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

        _JointPoint.position = segment.currentPosition;
        Vector3 dirToFirst = (mRopeSegments[0].currentPosition - segment.currentPosition).normalized;
        _JointPoint.up = dirToFirst;

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

        // Character To JointPoint
        if (_animationSwinger.target != null)
        {
            _animationSwinger.UpdatePos();
            // Debug.Log("Rope Update");
        }

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
                    continue;
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

        //// Joint Point is fixed
        //RopeSegment jointSegment = mRopeSegments[_JointPointIndex];
        //Vector3 jointPos = firstSegment.currentPosition;
        //jointPos.y = transform.position.y - _ropeSegmentLength * _JointPointIndex;
        //jointSegment.currentPosition = jointPos;
        //mRopeSegments[_JointPointIndex] = jointSegment;

        //// first to joint point
        //Vector3 dirFirstToJoint = (mRopeSegments[_JointPointIndex].currentPosition - mRopeSegments[0].currentPosition).normalized;

        //for (int i = 1; i < _JointPointIndex; ++i)
        //{
        //    RopeSegment currentSegment = mRopeSegments[i];

        //    currentSegment.currentPosition = mRopeSegments[0].currentPosition + dirFirstToJoint * _ropeSegmentLength * i;

        //    mRopeSegments[i] = currentSegment;
        //}

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
                    // 拱府利牢 面倒 贸府 内靛
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

                // 拱府利牢 面倒 贸府 内靛
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
        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(mOriginForce, mDirForce * 3f);

        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(mOriginForce, mDirAngle * 3f);

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

        Gizmos.DrawRay(mOriginForce, mFinalForce * 3f);
    }
}
