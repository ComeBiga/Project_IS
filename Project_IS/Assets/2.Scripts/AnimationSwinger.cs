using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSwinger : MonoBehaviour
{
    public Transform target;

    [SerializeField] private Vector3 _posOffset;
    [SerializeField] private float _startDistanceFromJoint = .225f;
    [SerializeField] private RopeVerlet _ropeVerlet;
    [SerializeField] private float _waitTime = 1f;
    [SerializeField] private float _climbDistance = .57f;

    private Animator mAnimator;
    private Rigidbody mRigidbody;
    private bool mbReverse = false;

    private bool mbClimb = false;
    private bool mbIsClimbUp = false;
    private float mGoalNormalizedTime = 0f;
    private Vector3 mDeltaDistance = Vector3.zero;
    private float mDistanceFromJoint;
    private float mStretchDistance;
    private bool mbDistanceLerped = false;
    private RopeVerlet.GrabPoint mLastGrabPoint_RH;
    private RopeVerlet.GrabPoint mLastGrabPoint_LH;
    private RopeVerlet.GrabPoint mLastGrabPoint_RT;

    private static int MultiplierHash = Animator.StringToHash("Direction");

    public void UpdatePos()
    {
        if (target == null)
            return;

        Vector3 handPos = mAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position;

        Vector3 dirToCenter = target.up.normalized;
        Vector3 handTarget = target.position - dirToCenter * mDistanceFromJoint;
        //Vector3 toTarget = target.position - (handPos + _posOffset);
        Vector3 toTarget = handTarget - handPos;

        transform.position += toTarget;
        transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, target.up), target.up);
        // Debug.Log($"transform.right: {transform.right}, target.up: {target.up}, cross: {Vector3.Cross(transform.right, target.up)}");
    }

    private void Start()
    {
        mAnimator = GetComponent<Animator>();
        // mAnimator.SetFloat(MultiplierHash, 1f);

        // StartCoroutine(eSwingForward());
        mRigidbody = GetComponent<Rigidbody>();

        mDistanceFromJoint = _startDistanceFromJoint;
    }

    private void Update()
    {
        if(!mbClimb)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (Input.GetKey(KeyCode.U))
            {
                mbClimb = true;
                mbIsClimbUp = true;
                mGoalNormalizedTime += 1f;
                mAnimator.SetFloat(MultiplierHash, 1f);
                mDeltaDistance = Vector3.zero;

                mLastGrabPoint_RH = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                mLastGrabPoint_LH = _ropeVerlet.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                mLastGrabPoint_RT = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightToes);
                _ropeVerlet.AddJointIndex(-2);
                mDistanceFromJoint += _climbDistance;
                mStretchDistance = mDistanceFromJoint; 
                Debug.Log($"{mStretchDistance}, normalizedTime: {animatorStateInfo.normalizedTime}");
            }

            if(Input.GetKey(KeyCode.J))
            {
                mbClimb = true;
                mbIsClimbUp = false;
                mGoalNormalizedTime -= 1f;
                mAnimator.SetFloat(MultiplierHash, -1f);
                mDeltaDistance = Vector3.zero;

                mLastGrabPoint_RH = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                mLastGrabPoint_LH = _ropeVerlet.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                mLastGrabPoint_RT = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightToes);
                _ropeVerlet.AddJointIndex(2);
                mDistanceFromJoint -= _climbDistance;
                mStretchDistance = mDistanceFromJoint;
                Debug.Log($"{mStretchDistance}, normalizedTime: {animatorStateInfo.normalizedTime}");
            }
        }

        if(mbClimb)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
            // Debug.Log(animatorStateInfo.normalizedTime);

            float normalizedTime = animatorStateInfo.normalizedTime - (int)animatorStateInfo.normalizedTime;

            if(animatorStateInfo.normalizedTime < 0f)
            {
                normalizedTime = 1f + normalizedTime;
            }

            if(mbIsClimbUp)
            {
                if (normalizedTime > .8f && normalizedTime < .95f)
                {
                    mbDistanceLerped = true;
                    mDeltaDistance += mAnimator.deltaPosition;
                    float t = (normalizedTime - .8f) / .18f;
                    mDistanceFromJoint = Mathf.Lerp(mStretchDistance, _startDistanceFromJoint, t);
                    // Debug.Log(normalizedTime - .8f);

                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = false;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                }
                else if (mbDistanceLerped && normalizedTime > .95f)
                {
                    mDistanceFromJoint = _startDistanceFromJoint;

                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RH.segmentIndex - 2;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                }
                else if (normalizedTime > .78f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_LH.segmentIndex - 2;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (normalizedTime > .6f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = false;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (normalizedTime > .4f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RT.segmentIndex - 2;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if(normalizedTime > .1f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = false;
                    // _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
            }
            else
            {
                if (mbDistanceLerped && normalizedTime < .1f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RT.segmentIndex + 2;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .4f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightToes);
                    grabPoint.active = false;
                    // _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .6f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_LH.segmentIndex + 2;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .78f)
                {
                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.LeftMiddleProximal);
                    grabPoint.active = false;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                    // Debug.Log(normalizedTime);
                }
                else if (mbDistanceLerped && normalizedTime < .8f)
                {
                    mDistanceFromJoint = _startDistanceFromJoint;

                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = true;
                    grabPoint.segmentIndex = mLastGrabPoint_RH.segmentIndex + 2;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                }
                else if (normalizedTime > .8f && normalizedTime < .95f)
                {
                    mbDistanceLerped = true;
                    mDeltaDistance += mAnimator.deltaPosition;
                    float t = (normalizedTime - .8f) / .18f;
                    mDistanceFromJoint = Mathf.Lerp(mStretchDistance, _startDistanceFromJoint, 1 - t);
                    // Debug.Log(normalizedTime - .8f);

                    RopeVerlet.GrabPoint grabPoint = _ropeVerlet.GetGrabPoint(HumanBodyBones.RightMiddleProximal);
                    grabPoint.active = false;
                    _ropeVerlet.SetGrabPoint(grabPoint);
                }
            }


            if ((mbIsClimbUp && animatorStateInfo.normalizedTime > mGoalNormalizedTime)
                || (!mbIsClimbUp && animatorStateInfo.normalizedTime < mGoalNormalizedTime))
            {
                mbClimb = false;
                mAnimator.SetFloat(MultiplierHash, 0f);

                mbDistanceLerped = false;
                mDistanceFromJoint = _startDistanceFromJoint;
                // Debug.Log(mDeltaDistance);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //var animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

        //if(!mbReverse)
        //{
        //    if(animatorStateInfo.normalizedTime > .99f)
        //    {
        //        mAnimator.SetFloat(MultiplierHash, -1f);
        //        mbReverse = true;
        //    }
        //}
        //else
        //{
        //    if(animatorStateInfo.normalizedTime < .01f)
        //    {
        //        mAnimator.SetFloat(MultiplierHash, 1f);
        //        mbReverse = false;
        //    }
        //}
    }

    private IEnumerator eSwingForward()
    {
        while (true)
        {
            var animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.normalizedTime > .9f)
            {
                mAnimator.SetFloat(MultiplierHash, 0f);

                yield return new WaitForSeconds(_waitTime);
                break;
            }

            yield return null;
        }

        mAnimator.SetFloat(MultiplierHash, -1f);
        mbReverse = true;

        StartCoroutine(eSwingBackward());
    }

    private IEnumerator eSwingBackward()
    {
        while (true)
        {
            var animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.normalizedTime < .1f)
            {
                mAnimator.SetFloat(MultiplierHash, 0f);

                yield return new WaitForSeconds(_waitTime);
                break;
            }

            yield return null;
        }

        mAnimator.SetFloat(MultiplierHash, 1f);
        mbReverse = false;

        StartCoroutine(eSwingForward());
    }

    private void OnAnimatorMove()
    {
        
    }

    private void OnAnimatorIK(int layerIndex)
    {
        
    }
}
