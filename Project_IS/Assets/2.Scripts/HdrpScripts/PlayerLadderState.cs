using PropMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class PlayerLadderState : PlayerStateBase
{
    // Idle은 현재 사용되지 않음
    private enum EClimbType { Idle, ClimbUp, ClimbDown }

    private int TopStepIndex => mStepPositions.Count - 1;

    [Header("Start Climb Up")]
    [SerializeField] private float _startHeight = .2f;
    [SerializeField] private float _distanceToCharacter = .2f;
    [SerializeField] private float _startClimbUpDuration = .2f;
    [Header("End To Platform")]
    [SerializeField] private float _endToPlatformTopTime = .6f;
    [SerializeField] private float _endToPlatformXSpeed = 2f;
    [Header("Start Climb Down")]
    [SerializeField] private float _startClimbDownXSpeed = 2f;
    [SerializeField] private float _startClimbDownYSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 2f;

    private Animator mAnimator;
    private LadderHandler mLadderHandler;

    private bool mbStartFromBottom = true;
    private bool mbLadderTop = false;
    private List<Vector3> mStepPositions;
    private int mCurrentStepIndex = 0;
    private int mMaxStepIndex;        // 매달려 있을 수 있는 가장 높은 StepIndex

    private float mStepNormalizedTime = 0f;     // 애니메이션 normalizedTime과 비교하기 위한 값
    private bool mbClimbing = false;
    private float mClimbMultiplier = 0f;        // 애니메이션 Speed Multiplier
    private EClimbType mClimbType = EClimbType.Idle;
    private bool mbIsHandDefault = true;        // Hand Default : 두 손이 같은 Step에 있는 상태
    private PlayerMovement.EDirection mPreviousDirection;
    private PlayerMovement.EDirection mLadderDirection;     // 사다리 방향 사다리 타기 종료 후 캐릭터 방향 처리 용
    private float mRotatedAngles = 0f;

    // IK
    private bool mbActiveIK = false;

    private int mLeftHandStepNum = 5;
    private int mRightHandStepNum = 5;

    // Hand IK Weight는 값 설정을 위해 SerializeField로 수정하기
    private float mLeftHandIKWeight = 1f;
    private float mRightHandIKWeight = 1f;

    private const int DISTANCE_FOOT_TO_HAND_DEFAULT = 5;
    private const int DISTANCE_FOOT_TO_HAND_STRETCH = 6;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = mController.Animator.Animator;
    }

    public override void EnterState()
    {
        mController.Movement.SetVelocity(Vector3.zero);
        mController.Movement.SetUseGravity(false);
        mController.Movement.SetColliderActive(false);
        // mController.Animator.SetLadderTop(false);        // 맨 위에서 시작 시 LadderTop과 함께 시작함

        // Top에서 시작 시 어떤 값으로 시작하는 지 확인할 필요 있음
        // => animationStateInfo.normalizedTimed은 Top, Bottom 상관없이 애니메이션 시작될 때 0부터 시작
        mStepNormalizedTime = 0f;                           
        mbClimbing = false;
        mbLadderTop = false;
        mbIsHandDefault = true;
        mRotatedAngles = 0f;

        mbActiveIK = true;

        mController.Animator.onAnimationIK -= updateAnimatorIK;
        mController.Animator.onAnimationIK += updateAnimatorIK;

        if(mbStartFromBottom)
        {
            // StartCoroutine(eClimb());
            StartCoroutine(eStartClimbUp());
        }
        else
        {
            StartCoroutine(eStartClimbDown());
        }
    }

    public override void ExitState()
    {
        mController.Movement.SetUseGravity(true);
        mController.Movement.SetColliderActive(true);
        mController.Animator.SetLadderTop(false);

        mbLadderTop = false;

        mbActiveIK = false;
        mController.Animator.onAnimationIK -= updateAnimatorIK;
    }

    public override void Tick()
    {

    }

    public bool IsInRange(LadderHandler ladderHandler)
    {
        List<Vector3> stepPositions = ladderHandler.GetStepPositions();

        int topStepIndex = stepPositions.Count - 1;
        int maxStepIndex = topStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;

        Vector3 minStepPos = stepPositions[0];
        Vector3 maxStepPos = stepPositions[maxStepIndex];

        if(transform.position.y > minStepPos.y && transform.position.y < maxStepPos.y)
        {
            return true;
        }

        return false;
    }
    
    public bool IsOverRange(LadderHandler ladderHandler)
    {
        List<Vector3> stepPositions = ladderHandler.GetStepPositions();

        int topStepIndex = stepPositions.Count - 1;
        int maxStepIndex = topStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;

        // Vector3 minStepPos = stepPositions[0];
        Vector3 maxStepPos = stepPositions[maxStepIndex];

        if(transform.position.y > maxStepPos.y)
        {
            return true;
        }

        return false;
    }

    public void SetLadder(LadderHandler ladderHandler, bool startFromBottom)
    {
        mLadderHandler = ladderHandler;
        mStepPositions = mLadderHandler.GetStepPositions();

        mLadderDirection = mLadderHandler.GetLadderDirection();
        mPreviousDirection = mController.Movement.Direction;
        mController.Movement.SetDirection(mLadderDirection);

        mbStartFromBottom = startFromBottom;

        if(startFromBottom)
        {
            // Step
            mCurrentStepIndex = 0;
            mMaxStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;

            // IK
            mLeftHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mRightHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mLeftHandIKWeight = 0f;
            mRightHandIKWeight = 0f;

            //// Start Climb Up 애니메이션 없이 시작하기 때문에 위치 즉시 설정
            //// 자연스러움을 위해서는 Lerp 처리하던지 해야함
            //Vector3 position = mController.Movement.Position;
            //if(mLadderDirection == PlayerMovement.EDirection.Right)
            //{
            //    position.x = mStepPositions[mCurrentStepIndex].x - _distanceToCharacter;
            //}
            //else
            //{
            //    position.x = mStepPositions[mCurrentStepIndex].x + _distanceToCharacter;
            //}
            //position.y = mStepPositions[mCurrentStepIndex].y;
            //transform.position = position;
            // mController.Movement.SetPosition(mStepPositions[mCurrentStepIndex].x - _distanceToCharacter, mStepPositions[mCurrentStepIndex].y, position.z);
        }
        else
        {
            // Step
            // Hand Default 상태에서 가장 높은 위치를 현재 위치로 설정하기 위해 -1을 해줌
            mCurrentStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH - 1;
            mMaxStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;

            // IK
            mLeftHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mRightHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mLeftHandIKWeight = 0f;
            mRightHandIKWeight = 0f;

            // Start Climb Down 애니메이션 후 위치 설정하기 때문에 LadderTop만 true로 줌
            mController.Animator.SetLadderTop(true);
        }
    }

    public void EndToPlatform()
    {
        mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
    }

    private IEnumerator eClimb()
    {
        // Climb Up 애니메이션이 아니면 아래를 계산하지 않음
        while(true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("ClimbUp"))
                break;

            yield return null;
        }

        // mbActiveIK = true;

        while (true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            // Idle 상태에서 키 입력이 들어올 때 처리
            if (!mbClimbing)
            {
                // 위 방향
                if (mController.InputHandler.MoveInput.y > .1f)     // 최소 Input 값을 통일하기 위해 오로지 입력만 체크하는 함수 작성?
                {
                    mbClimbing = true;
                    mbIsHandDefault = !mbIsHandDefault;
                    mClimbMultiplier = 1f;
                    mStepNormalizedTime += .5f;
                    mClimbType = EClimbType.ClimbUp;
                    mCurrentStepIndex++;

                    // Step 위치에 따른 Hand IK
                    if (mCurrentStepIndex % 2 == 0)
                    {
                        mRightHandStepNum += 2;
                        mRightHandIKWeight = 0f;    // 0부터 1까지 자연스럽게 올려주기위해 0 대입
                    }
                    else
                    {
                        mLeftHandStepNum += 2;
                        mLeftHandIKWeight = 0f;
                    }
                }
                // 아래 방향
                else if (mController.InputHandler.MoveInput.y < -.1f)
                {
                    mbClimbing = true;
                    mbIsHandDefault = !mbIsHandDefault;
                    mClimbMultiplier = -1f;
                    mStepNormalizedTime -= .5f;
                    mClimbType = EClimbType.ClimbDown;
                    mCurrentStepIndex--;

                    if (mCurrentStepIndex % 2 == 0)
                    {
                        mLeftHandStepNum -= 2;
                        mLeftHandIKWeight = 0f;
                    }
                    else
                    {
                        mRightHandStepNum -= 2;
                        mRightHandIKWeight = 0f;
                    }
                }
            }

            // Ladder Bottom
            if (mCurrentStepIndex < 0)
            {
                mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
                break;
            }

            // Ladder Top
            if (mCurrentStepIndex > mMaxStepIndex)
            {
                // Top에 도착했을 때 손 위치에 따라 처리해주는 코드인데
                // 복잡해질 거 생각하면 사다리 자체에 Step 수를 짝수든 홀수든 고정해주는 방향으로 해도될 듯
                // if (mCurrentStepNum % 2 == 0)
                if (mbIsHandDefault)
                {
                    //Vector3 topPos = transform.position;
                    //topPos.y = mStepPositions[mCurrentStepIndex].y;
                    //transform.position = topPos;
                }

                mbLadderTop = true;
                // mbActiveIK = false;
                // mController.Animator.SetLadderTop(true);

                StartCoroutine(eEndToPlatform());

                break;
            }

            // 키입력이 들어와서 Climb Up이든 Down이든 하고 있는 상태
            // 키입력 한 번에 한 스텝을 기준으로 계산
            // 코드 복잡성 때문에 크게 Climb Up, Down 분기로 나눠줘야될 듯
            if (mbClimbing)
            {
                mController.Animator.SetVertical(mClimbMultiplier);

                // 현재 위치가 다음 Step 위치가 되기 전까지 deltaPosition 처리
                if ((mClimbType == EClimbType.ClimbUp && transform.position.y < mStepPositions[mCurrentStepIndex].y)
                 || (mClimbType == EClimbType.ClimbDown && transform.position.y > mStepPositions[mCurrentStepIndex].y))
                {
                    transform.position += mController.Animator.Animator.deltaPosition;
                }

                // Hand IK Weight를 자연스럽게 0부터 1까지 계산
                if (mClimbType == EClimbType.ClimbUp)
                {
                    if (mCurrentStepIndex % 2 == 0)
                    {
                        // 한 Step이 normalizedTime으로 .5f이기 때문에 분모를 .5로 계산
                        mRightHandIKWeight = (.5f - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / .5f;
                    }
                    else
                    {
                        mLeftHandIKWeight = (.5f - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / .5f;
                    }
                }
                else if (mClimbType == EClimbType.ClimbDown)
                {
                    if (mCurrentStepIndex % 2 == 0)
                    {
                        mLeftHandIKWeight = (.5f - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / .5f;
                    }
                    else
                    {
                        mRightHandIKWeight = (.5f - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / .5f;
                    }
                }

                // normalizedTime이 한 스텝만큼 변화하면 Idle로 전환
                if ((mClimbType == EClimbType.ClimbUp && animatorStateInfo.normalizedTime > mStepNormalizedTime)
                 || (mClimbType == EClimbType.ClimbDown && animatorStateInfo.normalizedTime < mStepNormalizedTime))
                {

                    mbClimbing = false;
                    mController.Animator.SetVertical(0f);
                    // Debug.Log($"Step!! [NormalizedTime : {animatorStateInfo.normalizedTime.ToString("F1")}]");
                }
            }

            yield return null;
        }
    }

    private IEnumerator eStartClimbUp()
    {
        // Start Climb Up 애니메이션 없이 시작하기 때문에 위치 즉시 설정
        // 자연스러움을 위해서는 Lerp 처리하던지 해야함
        Vector3 targetPosition = mController.Movement.Position;

        if (mLadderDirection == PlayerMovement.EDirection.Right)
        {
            targetPosition.x = mStepPositions[mCurrentStepIndex].x - _distanceToCharacter;
        }
        else
        {
            targetPosition.x = mStepPositions[mCurrentStepIndex].x + _distanceToCharacter;
        }

        targetPosition.y = mStepPositions[mCurrentStepIndex].y;
        // transform.position = targetPosition;

        Quaternion targetRotation = mController.Movement.DirectionToRotation(mLadderDirection);
        transform.rotation = targetRotation;

        float timer = 0f;
        float lerpDuration = _startClimbUpDuration;
        Vector3 startPosition = transform.position;

        while (timer < lerpDuration)
        {
            float t = timer / lerpDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            mLeftHandIKWeight = Mathf.Lerp(0f, 1f, t);
            mRightHandIKWeight = Mathf.Lerp(0f, 1f, t);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        StartCoroutine(eClimb());
    }

    private IEnumerator eEndToPlatform()
    {
        float topYPos = mStepPositions[mCurrentStepIndex].y;

        // Top에 도착했을 때 손 위치에 따라 처리해주는 코드인데
        // 복잡해질 거 생각하면 사다리 자체에 Step 수를 짝수든 홀수든 고정해주는 방향으로 해도될 듯
        // if (mCurrentStepNum % 2 == 0)
        if (mbIsHandDefault)
        {
            while (true)
            {
                AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

                mController.Animator.SetVertical(mClimbMultiplier);

                // 현재 위치가 다음 Step 위치가 되기 전까지 deltaPosition 처리
                if (mClimbType == EClimbType.ClimbUp && transform.position.y < mStepPositions[mCurrentStepIndex].y)
                {
                    transform.position += mController.Animator.Animator.deltaPosition;
                }

                // Hand IK Weight를 자연스럽게 0부터 1까지 계산
                if (mClimbType == EClimbType.ClimbUp)
                {
                    if (mCurrentStepIndex % 2 == 0)
                    {
                        // 한 Step이 normalizedTime으로 .5f이기 때문에 분모를 .5로 계산
                        mRightHandIKWeight = (.5f - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / .5f;
                    }
                    else
                    {
                        mLeftHandIKWeight = (.5f - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / .5f;
                    }
                }

                // normalizedTime이 한 스텝만큼 변화하면 Idle로 전환
                if (mClimbType == EClimbType.ClimbUp && animatorStateInfo.normalizedTime > mStepNormalizedTime)
                {
                    mController.Animator.SetVertical(0f);
                    break;
                }

                yield return null;
            }
        }

        mbActiveIK = false;
        mController.Animator.SetLadderTop(true);

        while (mbLadderTop)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            Vector3 deltaPosition = mController.Animator.Animator.deltaPosition;
            if (animatorStateInfo.normalizedTime > _endToPlatformTopTime)
                deltaPosition.x *= (transform.position.x < mStepPositions[TopStepIndex].x + .5f) ? _endToPlatformXSpeed : 0f;
            // deltaPosition.y *= (transform.position.y < mStepPositions[TopStepIndex].y) ? 1.2f : 0f;
            deltaPosition.z = 0f;

            transform.position += deltaPosition;

            if (animatorStateInfo.normalizedTime > _endToPlatformTopTime)
            {
                Vector3 newPosition = transform.position;
                newPosition.y = Mathf.Lerp(transform.position.y, mStepPositions[TopStepIndex].y, Time.deltaTime);
                transform.position = newPosition;
            }

            yield return null;
        }
    }

    private IEnumerator eStartClimbDown()
    {
        while (true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("StartFromTop"))
                break;

            yield return null;
        }

        // LadderTop을 통해서 이미 애니메이션이 실행됐기 때문에 False 처리
        mController.Animator.SetLadderTop(false);

        while (true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (!animatorStateInfo.IsTag("StartFromTop"))
                break;

            // Start Climb Down 애니메이션 delta 계산
            // deltaPosition
            Vector3 deltaPosition = mController.Animator.Animator.deltaPosition;
            // 일정 위치까지 이동 시키기 위해서 일정 위치 전 까지는 deltaPosition을 배수 처리
            if(mLadderDirection == PlayerMovement.EDirection.Right)
            {
                deltaPosition.x *= (transform.position.x > mStepPositions[mCurrentStepIndex].x - _distanceToCharacter) ? _startClimbDownXSpeed : 0f;
            }
            else
            {
                deltaPosition.x *= (transform.position.x < mStepPositions[mCurrentStepIndex].x + _distanceToCharacter) ? _startClimbDownXSpeed : 0f;
            }
            if (animatorStateInfo.normalizedTime > .6f)
                deltaPosition.y *= (transform.position.y > mStepPositions[mCurrentStepIndex].y) ? _startClimbDownYSpeed : 0f;
            deltaPosition.z = 0f;
            transform.position += deltaPosition;

            // deltaRotation
            if(mPreviousDirection != mLadderDirection)
            {
                // 현재 방향에서 반대 방향까지 애니메이션 normalizedTime에 맞춰서 회전
                //mController.Movement.RotateTo(mPreviousDirection,
                //                            mLadderDirection,
                //                            animatorStateInfo.normalizedTime);
                Vector3 eulerAngles = mController.Animator.Animator.deltaRotation.eulerAngles;
                eulerAngles.y *= _rotationSpeed;
                mRotatedAngles += eulerAngles.y;
                if (Mathf.Abs(mRotatedAngles) < 180f)
                {
                    transform.rotation *= Quaternion.Euler(eulerAngles);
                }
                else
                {
                    transform.rotation = mController.Movement.DirectionToRotation(mLadderDirection);
                }
            }

            // IK
            mLeftHandIKWeight = animatorStateInfo.normalizedTime;
            mRightHandIKWeight = animatorStateInfo.normalizedTime;

            yield return null;
        }

        StartCoroutine(eClimb());
    }

    private void updateAnimatorIK()
    {
        if (!mbActiveIK)
            return;

        Vector3 leftHandPosition = mAnimator.GetIKPosition(AvatarIKGoal.LeftHand);
        leftHandPosition.y = mStepPositions[mLeftHandStepNum].y;
        mAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
        mAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, mLeftHandIKWeight);

        Vector3 rightHandPosition = mAnimator.GetIKPosition(AvatarIKGoal.RightHand);
        rightHandPosition.y = mStepPositions[mRightHandStepNum].y;
        mAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPosition);
        mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, mRightHandIKWeight);
    }

    private void OnDrawGizmosSelected()
    {
        //if (mController.StateMachine.CurrentState != PlayerStateMachine.EState.Ladder)
        //{
        //    return;
        //}

        // 현재 Step과 가장 높은 손의 위치를 표시
        //Gizmos.DrawWireSphere(mStepPositions[mCurrentStepNum], .1f);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireSphere(mStepPositions[mCurrentStepNum + 6], .1f);

        //if(mStepPositions != null && mStepPositions.Count > 0)
        //{
        //    Gizmos.color = Color.blue;
        //    Vector3 pos = mStepPositions[mStepPositions.Count - 1];
        //    pos.x = pos.x - _distanceToCharacter;
        //    Gizmos.DrawWireSphere(pos, .1f);
        //    pos.x = pos.x + _distanceToCharacter;
        //    Gizmos.DrawWireSphere(pos, .1f);
        //    pos.x = pos.x + _distanceToCharacter;
        //    Gizmos.DrawWireSphere(pos, .1f);
        //}

        //Gizmos.DrawWireSphere(transform.position, .3f);
    }
}
