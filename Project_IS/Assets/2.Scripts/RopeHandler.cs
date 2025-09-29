using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RopeVerlet))]
public class RopeHandler : MonoBehaviour
{
    [SerializeField] private PlayerRopeClimbState _ropeClimbState;

    private RopeVerlet mRopeVerlet;
    private List<RopeVerlet.GrabPoint> mGrabPoints = new List<RopeVerlet.GrabPoint>();

    private static int CLIMB_INDEX_DISTANCE = 2;

    public void StartClimb(int jointIndex)
    {
        mRopeVerlet.SetJointIndex(jointIndex);

        for(int i = 0; i < mGrabPoints.Count; i++)
        {
            mRopeVerlet.AddGrabPoint(jointIndex + mGrabPoints[i].segmentIndex,
                                    mGrabPoints[i].transform, 
                                    mGrabPoints[i].humanBodyBone);
        }

        mRopeVerlet.SetPendulum();
    }

    public void EndClimb()
    {
        mRopeVerlet.ClearGrabPoints();
        mRopeVerlet.SetNormal();

        mGrabPoints.Clear();
    }

    public void ClimbUpByJointIndex()
    {
        mRopeVerlet.AddJointIndex(-CLIMB_INDEX_DISTANCE);
    }

    public void ClimbDownByJointIndex() 
    {
        mRopeVerlet.AddJointIndex(CLIMB_INDEX_DISTANCE);
    }

    public void AddGrabPoint(int indexFromJoint, Transform pointTransform, HumanBodyBones humanBodyBone)
    {
        var newGrabPoint = new RopeVerlet.GrabPoint(pointTransform, indexFromJoint, humanBodyBone);
        mGrabPoints.Add(newGrabPoint);

        //mRopeVerlet.AddGrabPoint(-1, pointTransform, humanBodyBone);
    }

    public RopeVerlet.GrabPoint GetGrabPoint(HumanBodyBones humanBodyBone)
    {
        return mRopeVerlet.GetGrabPoint(humanBodyBone);
    }

    public void SetGrabPoint(RopeVerlet.GrabPoint grabPoint)
    {
        mRopeVerlet.SetGrabPoint(grabPoint);
    }

    public void AddListenerOnAfterSimulateSegments(System.Action action)
    {
        mRopeVerlet.onAfterSimulateSegments += action;
    }

    public void RemoveListenerOnAfterSimulateSegments(System.Action action)
    {
        mRopeVerlet.onAfterSimulateSegments -= action;
    }

    public Transform GetJointPointTransform()
    {
        return mRopeVerlet.JointPoint;
    }

    public bool CouldClimbUp()
    {
        // Up
        if(mRopeVerlet.ValidateSegmentIndex(mRopeVerlet.JointPointIndex - CLIMB_INDEX_DISTANCE))
            return true;

        return false;
    }

    public bool CouldClimbDown()
    {
        // Down
        RopeVerlet.GrabPoint lastGrabPoint = mRopeVerlet.GrabPoints[mRopeVerlet.GrabPoints.Count - 1];

        if (mRopeVerlet.ValidateSegmentIndex(lastGrabPoint.segmentIndex + CLIMB_INDEX_DISTANCE))
            return true;

        return false;
    }

    public bool ValidateSegmentIndex(int segmentIndex)
    {
        return mRopeVerlet.ValidateSegmentIndex(segmentIndex);
    }

    public void SwingLeft()
    {
        mRopeVerlet.SwingLeft();
    }

    public void SwingRight()
    {
        mRopeVerlet.SwingRight();
    }

    public void StopSwing()
    {
        mRopeVerlet.StopSwing();
    }

    // Start is called before the first frame update
    void Start()
    {
        mRopeVerlet = GetComponent<RopeVerlet>();

        mRopeVerlet.onCollision += onRopeCollision;
    }

    private void onRopeCollision(int segmentIndex, Collider[] colliders)
    {
        _ropeClimbState.NotifyRopeCollision(segmentIndex, this);
    }
}
