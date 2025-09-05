using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbObjectState : PlayerStateBase
{
    [Header("Climb Up")]
    [SerializeField] private float _lerpRotationDuration = .2f;     // 이동 방향에서 애니메이션 방향으로 각도 보정해주는 시간
    [SerializeField] private float _lerpYPosDuration = .2f;         // 애니메이션 높이로 보정해주는 시간
    [SerializeField] private float _climbUpYSpeed = 2f;
    [SerializeField] private float _climbUpZSpeed = 3f;
    [SerializeField][Range(0f, 1f)] private float _climbUpZDelay = .6f;

    [Header("Climb Down")]
    [SerializeField] private float _lerpZPosDuration = .2f;         // 애니메이션 위치로 보정해주는 시간
    [SerializeField] private float _climbDownRotateSpeed = 2f;
    [SerializeField] private float _climbDownYSpeed = 1f;
    [SerializeField] private float _climbDownZSpeed = 1f;
    [SerializeField][Range(0f, 1f)] private float _climbDownEndTime = .6f;

    private Animator mAnimator;
    private PushPullObject mPushPullObject;
    private bool mbClimbing = false;                // 오르기/내리기 중인지
    private bool mbClimbUp;                         // 오르기 인지 내리기 인지

    private bool mbActiveIK = false;
    private Vector3 mTargetIKPos;
    private float mHandIKWeight = 1f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = controller.Animator.Animator;
    }

    public override void EnterState()
    {
        mController.Movement.SetVelocity(Vector3.zero);
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);
        mbClimbing = true;

        mController.Animator.onAnimationIK -= updateAnimatorIK;
        mController.Animator.onAnimationIK += updateAnimatorIK;

        if (mbClimbUp)
        {
            StartCoroutine(eClimbUp());
        }
        else
        {
            StartCoroutine(eClimbDown());
        }
    }

    public override void ExitState()
    {
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);

        // 내리기일 때 어느 방향을 보고 있었는 지 체크하는 parameter
        mController.Animator.SetHorizontal(0f);
        // 오르기 인지 내리기 인지 AnimatorController에서 체크는 아래 parameter로 한다.
        mController.Animator.SetVertical(0f);

        mController.Animator.onAnimationIK -= updateAnimatorIK;
    }

    public override void Tick()
    {

    }

    public void SetClimbObject(PushPullObject pushPullObject, bool climbUp)
    {
        mPushPullObject = pushPullObject;
        mbClimbUp = climbUp;

        // 오르기 인지 내리기 인지 AnimatorController에서 체크는 아래 parameter로 한다.
        if (mbClimbUp)
        {
            mController.Animator.SetVertical(1f);
        }
        else
        {
            mController.Animator.SetVertical(-1f);
        }

        // 내리기일 때 어느 방향을 보고 있었는 지 AnimatorController에서 체크하는 parameter
        if (mController.Movement.Direction == PlayerMovement.EDirection.Right)
        {
            mController.Animator.SetHorizontal(1f);
        }
        else
        {
            mController.Animator.SetHorizontal(-1f);
        }
    }

    // 오브젝트 오르내리기 상태를 애니메이션이 끝날 때 벗어나도록 호출해주고 있다.
    public void EndClimbObject()
    {
        mbClimbing = false;
        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }

    private IEnumerator eClimbUp()
    {
        // 애니메이션 시작할 때 y를 보정해주는 위치
        Bounds ppoBounds = mPushPullObject.BoxCollider.bounds;
        Vector3 targetPos = transform.position;
        targetPos.y = ppoBounds.max.y;

        Quaternion targetRotation = Quaternion.Euler(Vector3.zero);

        mController.Animator.SetVelocityY(0f);

        // IK Hand
        mTargetIKPos = transform.position;
        mTargetIKPos.y = ppoBounds.max.y;
        mTargetIKPos.z = ppoBounds.min.z;
        mHandIKWeight = .5f;
        // mbActiveIK = true;

        float timer = 0f;
        float lerpDuration = _lerpRotationDuration;

        Vector3 startPos = transform.position;
        // Vector3 startPos = targetPos;
        Quaternion startRot = transform.rotation;

        //while (timer < lerpDuration)
        //{
        //    float t = timer / lerpDuration;
        //    // transform.position = Vector3.Lerp(startPos, targetPos, t);

        //    transform.rotation = Quaternion.Lerp(startRot, targetRotation, t);

        //    // mHandIKWeight = t;

        //    timer += Time.deltaTime;
        //    yield return null;
        //}

        //transform.position = targetPos;
        transform.rotation = targetRotation;
        mController.Animator.SetVelocityY(1f);

        mbActiveIK = false;

        //AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

        //yield return new WaitUntil(() => animatorStateInfo.IsTag("ClimbUp"));

        //transform.position = targetPos;

        timer = 0f;

        while (mbClimbing)
        {
            //if (timer < _lerpYPosDuration)
            //{
            //    transform.position = Vector3.Lerp(transform.position, targetPos, timer / _lerpYPosDuration);
            //    // 지금 _lerpYPosDuration과 _lerpRotationDuration 값이 같기 때문에 같은 if문 안에 작성해줌
            //    // 값이 달라질거면 if문을 나눠줘야됨, 같을거면 변수를 통합해줘야됨
            //    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, timer / _lerpRotationDuration);

            //    timer += Time.deltaTime;
            //}

            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (!animatorStateInfo.IsTag("ClimbUp"))
            {
                yield return null;
                continue;
            }

            Vector3 deltaPosition = mAnimator.deltaPosition;
            deltaPosition.x = 0f;
            deltaPosition.y *= _climbUpYSpeed;
            // 캐릭터 z 위치가 0까지만 이동하게 조건을 줌
            if (animatorStateInfo.normalizedTime > _climbUpZDelay)
            {
                deltaPosition.z *= (transform.position.z < 0f) ? _climbUpZSpeed : 0f;
                mController.Movement.UpdateRotation();
            }
            else
            {
                deltaPosition.z = 0f;
            }

            transform.position += deltaPosition;

            yield return null;
        }
    }

    private IEnumerator eClimbDown()
    {
        // 애니메이션 시작할 때 z를 보정해주는 위치
        Bounds ppoBounds = mPushPullObject.BoxCollider.bounds;
        Vector3 targetPos = transform.position;
        targetPos.z = ppoBounds.min.z;

        float timer = 0f;

        // ClimbUp이랑 다르게 보정 코드가 다르게 작성돼있는데 확인해볼 필요가 있을 듯
        // ClimbUp이랑 다르게 애니메이션 태그 확인 코드를 안해줘서 이렇게 작성된 걸로 추측함
        while (timer < _lerpZPosDuration)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, timer / _lerpZPosDuration);

            timer += Time.deltaTime;
            yield return null;
        }

        float rotatedAngles = 0f;

        while (mbClimbing)
        {
            // 애니메이션 태크 확인 코드가 없어서 지금 사용되지 않음
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            // deltaPosition
            Vector3 deltaPosition = mAnimator.deltaPosition;
            deltaPosition.x = 0f;
            // 반대방향으로 배수 처리되지 않게 하기 위해 조건을 줌
            deltaPosition.y *= (deltaPosition.y < 0f) ? _climbDownYSpeed : 1f;
            deltaPosition.z *= (deltaPosition.z < 0f) ? _climbDownZSpeed : 1f;

            if (transform.position.z < -.6f)
                deltaPosition.z = 0f;

            transform.position += deltaPosition;

            // deltaRotation
            Vector3 deltaEulerAngles = mAnimator.deltaRotation.eulerAngles;
            // deltaRotation 값이 반대 회전이면 누적 각도가 의도와 다르게 쌓이는 걸 방지하기 위해 조건을 줌
            // ex) delta 각도가 358도라면 358 - 360 = -2로 계산
            deltaEulerAngles.y = (deltaEulerAngles.y < Number.DEG_180) ? deltaEulerAngles.y : deltaEulerAngles.y - Number.DEG_360;
            deltaEulerAngles.y *= _climbDownRotateSpeed;
            rotatedAngles += deltaEulerAngles.y;

            if (Mathf.Abs(rotatedAngles) < Number.DEG_90)
            {
                transform.rotation *= Quaternion.Euler(deltaEulerAngles);
            }
            else
            {
                if(animatorStateInfo.normalizedTime > _climbDownEndTime)
                {
                    mController.Movement.SetUseGravity(true);
                    mController.Movement.SetColliderActive(true);
                    mController.Movement.UpdateRotation();
                }
                else
                {
                    transform.rotation = Quaternion.Euler(Vector3.zero);
                }
            }


            yield return null;
        }
    }

    private void updateAnimatorIK()
    {
        if (!mbActiveIK)
            return;

        mAnimator.SetIKPosition(AvatarIKGoal.LeftHand, mTargetIKPos);
        mAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, mHandIKWeight);
        mAnimator.SetIKPosition(AvatarIKGoal.RightHand, mTargetIKPos);
        mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, mHandIKWeight);
    }
}
