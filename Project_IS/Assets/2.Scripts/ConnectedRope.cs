using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RopeVerlet;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

[RequireComponent(typeof(LineRenderer))]
public class ConnectedRope : MonoBehaviour
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

    [SerializeField] private Transform _trConntectedPoint;

    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 50;               // 총 Segment 개수
    [SerializeField] private float _ropeSegmentLength = 0.225f;         // Segment 당 길이

    [Header("Physics")]
    [SerializeField] private Vector3 _gravityForce = new Vector3(0f, -2f, 0f);
    [SerializeField] private float _dampingFactor = 0.98f;
    [SerializeField] private bool _onlyTrigger = true;              // true이면 물리적인 충돌을 체크하지 않음
    [SerializeField] private LayerMask _collisionLayerMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.1f;
    [SerializeField] private float _correctionClampAmount = 0.1f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 50;
    [SerializeField] private float _constraintIntensity = 0.5f;

    [Header("Optimizations")]
    [SerializeField] private int _collisionSegmentInterval = 2;

    private LineRenderer mLineRenderer;
    private List<RopeSegment> mRopeSegments = new List<RopeSegment>();

    private Vector3 mRopeStartPoint;

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
    }

    private void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < _numOfConstraintRuns; i++)
        {
            ApplyConstraints();

            if (i % _collisionSegmentInterval == 0)
                HandleCollisions();
        }
    }

    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_numOfRopeSegments];
        for (int i = 0; i < _numOfRopeSegments; i++)
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

            Vector3 velocity = (segment.currentPosition - segment.oldPosition) * _dampingFactor;

            segment.oldPosition = segment.currentPosition;
            segment.currentPosition += velocity;
            segment.currentPosition += _gravityForce * Time.fixedDeltaTime;

            mRopeSegments[i] = segment;
        }
    }

    private void ApplyConstraints()
    {
        // First segment is fixed
        RopeSegment firstSegment = mRopeSegments[0];
        firstSegment.currentPosition = transform.position;
        mRopeSegments[0] = firstSegment;

        // Last segment is connected to the target point
        RopeSegment lastSegment = mRopeSegments[_numOfRopeSegments - 1];
        lastSegment.currentPosition = _trConntectedPoint.position;
        mRopeSegments[_numOfRopeSegments - 1] = lastSegment;

        int index = 0;

        for (int i = index; i < _numOfRopeSegments - 1; i++)
        {
            RopeSegment currentSegment = mRopeSegments[i];
            RopeSegment nextSegment = mRopeSegments[i + 1];

            float dist = (currentSegment.currentPosition - nextSegment.currentPosition).magnitude;
            float difference = dist - _ropeSegmentLength;

            Vector3 changeDir = (currentSegment.currentPosition - nextSegment.currentPosition).normalized;
            Vector3 changeVector = changeDir * difference;

            if (i != index)
            {
                currentSegment.currentPosition -= changeVector * _constraintIntensity;
                mRopeSegments[i] = currentSegment;
                nextSegment.currentPosition += changeVector * _constraintIntensity;
                mRopeSegments[i + 1] = nextSegment;
            }
            // Last segment is fixed, only move the second to last one
            else if (i == _numOfRopeSegments - 2)
            {
                currentSegment.currentPosition -= changeVector * _constraintIntensity;
                mRopeSegments[i] = currentSegment;
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

            if (!_onlyTrigger)
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
        }
    }
}
