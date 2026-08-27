using PropMaker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class PlayerLadderState : PlayerStateBase
{
    public struct LadderInfo
    {
        public Collider collider;
        public LadderHandler handler;
        public bool startFromBottom;
        public LadderPart part;
    }

    public enum LadderPart { Bottom, Middle, Top };

    // Idle은 현재 사용되지 않음
    private enum EClimbType { Idle, ClimbUp, ClimbDown }

    [Header("Interactable")]
    [SerializeField] private float _interactableDistance = .5f;

    [Header("Start Climb Up")]
    [SerializeField] private float _startHeight = .2f;
    [SerializeField] private float _distanceToCharacter = .2f;
    [SerializeField] private float _startClimbUpDuration = .2f;
    [Header("End To Ground")]
    [SerializeField] private float _endToGroundDuration = .2f;
    [Header("End To Platform")]
    [SerializeField] private float _endToPlatformTopTime = .6f;
    [SerializeField] private float _endToPlatformXSpeed = 2f;
    [Header("Start Climb Down")]
    [SerializeField] private float _startClimbDownStepNormalizedTime = .5f;
    [SerializeField] private float _startClimbDownXSpeed = 2f;
    [SerializeField] private float _startClimbDownYSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 2f;

    private int TopStepIndex => mStepPositions.Count - 1;
    private bool IsTop => mCurrentStepIndex > mMaxStepIndex;
    private bool IsBottom => mCurrentStepIndex < 0;

    private Animator mAnimator;
    private LadderHandler mLadderHandler;

    private LadderPart mStartPart;
    private bool mbStartFromBottom = true;
    private bool mbLadderTop = false;
    private List<Vector3> mStepPositions;
    private int mCurrentStepIndex = 0;
    private int mLastStepIndex = -1;
    private int mMaxStepIndex;        // 매달려 있을 수 있는 가장 높은 StepIndex

    private bool mbClimbLoop = false;
    private float mStepNormalizedTime = 0f;     // 애니메이션 normalizedTime과 비교하기 위한 값
    private bool mbClimbing = false;
    private float mClimbMultiplier = 0f;        // 애니메이션 Speed Multiplier
    private EClimbType mClimbType = EClimbType.Idle;
    private bool mbIsHandDefault = true;        // Hand Default : 두 손이 같은 Step에 있는 상태
    private PlayerMovement.EDirection mPreviousDirection;
    private PlayerMovement.EDirection mLadderDirection;     // 사다리 방향 사다리 타기 종료 후 캐릭터 방향 처리 용
    private float mRotatedAngles = 0f;
    private bool mbIsSameDirectionStart;
    private float mStartMoveInputX = 0f;
    private bool mbPressedLookBack = false;
    private bool mbIsLookBack = false;
    private float mClimbUpMotionTime = 0f;

    // IK
    private bool mbActiveIK = false;

    private int mLeftHandStepNum = 5;
    private int mRightHandStepNum = 5;

    // Hand IK Weight는 값 설정을 위해 SerializeField로 수정하기
    private float mLeftHandIKWeight = 1f;
    private float mRightHandIKWeight = 1f;

    private const float CLIMB_ANIMATION_LENGTH = .5f;
    private const float STEP_NORMALIZED_TIME = .5f;
    private const int DISTANCE_FOOT_TO_HAND_DEFAULT = 5;
    // private const int DISTANCE_FOOT_TO_HAND_STRETCH = 6;
    private const int DISTANCE_FOOT_TO_HAND_STRETCH = 5;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);

        mAnimator = mController.Animator.Animator;
    }

    public override void EnterState()
    {
        mController.Movement.SetVelocity(Vector3.zero);
        mController.Movement.SetUseGravity(false);
        // mController.Movement.SetColliderActive(false);
        mController.Movement.SetColliderTrigger(true);      // 사다리 타는 중 카메라 Bounds 체크를 위해
        // mController.Animator.SetLadderTop(false);        // 맨 위에서 시작 시 LadderTop과 함께 시작함

        mbClimbLoop = true;
        // Top에서 시작 시 어떤 값으로 시작하는 지 확인할 필요 있음
        // => animationStateInfo.normalizedTimed은 Top, Bottom 상관없이 애니메이션 시작될 때 0부터 시작
        // mStepNormalizedTime = 0f;                           
        mbClimbing = false;
        mbLadderTop = false;
        // mbIsHandDefault = true;
        mRotatedAngles = 0f;
        mStartMoveInputX = 0f;
        mbIsLookBack = false;

        // mbActiveIK = true;

        // mController.Animator.SetClimbLadder();
        mController.Animator.SetInputXMagnitude(0f);
        mController.Animator.SetVertical(0f);
        mController.Animator.SetMotionTime(mClimbUpMotionTime);

        mController.Animator.onAnimatorIK -= updateAnimatorIK;
        mController.Animator.onAnimatorIK += updateAnimatorIK;

        mController.CharacterSound.enableFootStep = false;
        mController.CharacterSound.enableHandTouch = false;
        mController.CharacterSound.AddFootStepMediumEvent(playFootStepSound);
        mController.CharacterSound.AddHandTouchEvent(playHandTouchSound);

        // if (mbStartFromBottom)
        if(mStartPart == LadderPart.Bottom)
        {
            playFootStepSound();

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
        // mController.Movement.SetColliderActive(true);
        mController.Movement.SetColliderTrigger(false);
        mController.Animator.SetLadderTop(false);

        mbClimbLoop = false;
        mbLadderTop = false;

        mbActiveIK = false;
        mController.Animator.onAnimatorIK -= updateAnimatorIK;
        mController.Animator.SetVertical(0f);

        mController.CharacterSound.enableFootStep = true;
        mController.CharacterSound.enableHandTouch = true;
        mController.CharacterSound.RemoveFootStepMediumEvent(playFootStepSound);
        mController.CharacterSound.RemoveHandTouchEvent(playHandTouchSound);
    }

    public override void Tick()
    {
        if (mbLadderTop)
            return;
        
        if(mbPressedLookBack && mController.InputHandler.JumpPressed)
        {
            mbClimbLoop = false;

            mController.Animator.SetMultiplier(1f);
            mController.Movement.SetDirection(mMovement.OppositeDirection);
            // mController.Movement.SetRotationToCurrentDirection();
            // mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
            mController.StateMachine.SwitchState<PlayerRunJumpState>((state) =>
            {
                state.SetRotationDuration(.05f);
                state.SetRotationCW(mbIsHandDefault ? false : true);
                state.EnterWithoutAnimation();
                StartCoroutine(eLookBackToJump());
                // mController.Animator.Play(mbIsHandDefault ? AnimState.Ladder_Look_Back_To_Jump_L : AnimState.Ladder_Look_Back_To_Jump_R);
            });

            mController.InputHandler.ResetJump();
        }
        //if(mLadderDirection == PlayerMovement.EDirection.Right)
        //{
        //    if (mController.InputHandler.MoveInput.x < 0.1f)
        //    {
        //        // mRightHandIKWeight = 0f;

        //        if(mController.InputHandler.JumpPressed)
        //        {
        //            mbClimbLoop = false;

        //            mController.Movement.SetDirection(PlayerMovement.EDirection.Left);
        //            // mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
        //            mController.StateMachine.SwitchState<PlayerRunJumpState>();

        //            mController.InputHandler.ResetJump();
        //        }
        //    }
        //}
        //else if(mLadderDirection == PlayerMovement.EDirection.Left)
        //{
        //    if (mController.InputHandler.MoveInput.x > 0.1f)
        //    {
        //        if (mController.InputHandler.JumpPressed)
        //        {
        //            mbClimbLoop = false;

        //            mController.Movement.SetDirection(PlayerMovement.EDirection.Right);
        //            // mController.StateMachine.SwitchState(PlayerStateMachine.EState.RunJump);
        //            mController.StateMachine.SwitchState<PlayerRunJumpState>();

        //            mController.InputHandler.ResetJump();
        //        }
        //    }
        //}

        // mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));
        // mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInputRaw.x));
    }

    public bool CheckLadder(out LadderInfo ladderInfo)
    {
        ladderInfo = new LadderInfo();

        if (mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Front, out PlayerInteractable.InteractedInfo interactedInfo))
        {
            Collider ladderCollider = interactedInfo.hitInfo.collider;
            ladderInfo.collider = ladderCollider;

            if (interactedInfo.distanceToEdge < _interactableDistance)
            {

                // Top Start
                if (ladderCollider.CompareTag("LadderTop"))
                {
                    ladderInfo.handler = ladderCollider.GetComponentInParent<LadderHandler>();
                    ladderInfo.part = LadderPart.Top;
                    return true;
                }

                // Bottom Start
                if (interactedInfo.interactableObject.CompareTag("Ladder"))
                {
                    ladderInfo.handler = ladderCollider.GetComponent<LadderHandler>();
                    ladderInfo.part = LadderPart.Bottom;
                    return true;
                }
            }
        }

        if (mInteractable.TryGetInteractedInfo(PlayerInteractable.CastDirection.Back, out PlayerInteractable.InteractedInfo backInteractedInfo))
        {
            Collider ladderCollider = backInteractedInfo.hitInfo.collider;
            ladderInfo.collider = ladderCollider;

            if (backInteractedInfo.distanceToEdge < _interactableDistance)
            {

                // Top Start
                if (ladderCollider.CompareTag("LadderTop"))
                {
                    ladderInfo.handler = ladderCollider.GetComponentInParent<LadderHandler>();
                    ladderInfo.part = LadderPart.Top;
                    return true;
                }
            }
        }

        return false;
    }

    public bool SwitchToLadderState(Collider[] ladderColliders)
    {
        GameDebug.Log($"In switchToLadderState(), ladderColliders.Length: {ladderColliders.Length}",
            tag: "switchToLadderState");

        foreach (Collider ladderCollider in ladderColliders)
        {
            // Bottom
            // Todo: InputHandler.IsUpPressed() 정의하기
            if (mController.InputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Up))
            {
                if (ladderCollider.tag == "LadderTop")
                    continue;

                // PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase<PlayerLadderState>();
                LadderHandler ladderHandler = ladderCollider.GetComponent<LadderHandler>();

                // Top에서 위 키 입력했을 때 사다리 타는 걸 방지하기 위함
                if (ladderStateBase.IsOverRange(ladderHandler))
                    continue;

                ladderStateBase.SetLadder(ladderHandler, startFromBottom: true);

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                mController.StateMachine.SwitchState<PlayerLadderState>();
                return true;
            }
            // Top
            else if (mController.InputHandler.MoveInput.y < -.1f)
            {
                if (ladderCollider.tag != "LadderTop")
                    continue;

                // PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Ladder) as PlayerLadderState;
                PlayerLadderState ladderStateBase = mController.StateMachine.GetStateBase<PlayerLadderState>();
                LadderHandler ladderHandler = ladderCollider.GetComponentInParent<LadderHandler>();
                ladderStateBase.SetLadder(ladderHandler, startFromBottom: false);

                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Ladder);
                mController.StateMachine.SwitchState<PlayerLadderState>();
                return true;
            }
        }

        return false;
    }


    public bool IsInRange(LadderHandler ladderHandler)
    {
        List<Vector3> stepPositions = ladderHandler.GetStepPositions();

        int topStepIndex = stepPositions.Count - 1;
        int maxStepIndex = topStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;

        Vector3 minStepPos = stepPositions[0];
        Vector3 maxStepPos = stepPositions[maxStepIndex];

        if(mCharacterPosition.y > minStepPos.y && mCharacterPosition.y < maxStepPos.y)
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

        if(mCharacterPosition.y > maxStepPos.y)
        {
            return true;
        }

        return false;
    }

    public bool IsValidStartInMiddle(LadderInfo ladderInfo)
    {
        float ladderMinY = ladderInfo.handler.BoxCollider.bounds.min.y;
        List<Vector3> stepPositions = ladderInfo.handler.GetStepPositions();
        int maxStepIndex = stepPositions.Count - 1 - DISTANCE_FOOT_TO_HAND_STRETCH;
        Vector3 maxStepPosition = stepPositions[maxStepIndex];

        GameDebug.Log($"Character Position Y: {mCharacterPosition.y}, Ladder Min Position Y: {ladderMinY}",
            tag: "IsValidStartInMiddle");

        // if (mCharacterPosition.y < stepPositions[0].y)
        if (mCharacterPosition.y < ladderMinY || mCharacterPosition.y > maxStepPosition.y)
            return false;

        return true;

        //// Step
        //for (int i = 0; i < stepPositions.Count; i++)
        //{
        //    if (mCharacterPosition.y < stepPositions[i].y)
        //    {
        //        mCurrentStepIndex = i - 1;

        //        if (mCurrentStepIndex < 0)
        //            return false;

        //        break;
        //    }
        //}
    }

    public void SetLadder(LadderInfo ladderInfo)
    {
        SetLadder(ladderInfo.handler, ladderInfo.part == LadderPart.Bottom ? true : false);
    }

    public void SetLadder(LadderHandler ladderHandler, bool startFromBottom)
    {
        mLadderHandler = ladderHandler;
        mStepPositions = mLadderHandler.GetStepPositions();

        // direction
        mLadderDirection = mLadderHandler.GetLadderDirection();
        mPreviousDirection = mController.Movement.Direction;
        mbIsSameDirectionStart = mLadderDirection == mController.Movement.Direction;

        if(mLadderDirection != PlayerMovement.EDirection.Forward)
            mController.Movement.SetDirection(mLadderDirection);

        mStartPart = startFromBottom ? LadderPart.Bottom : LadderPart.Top;
        mbStartFromBottom = startFromBottom;

        if(startFromBottom)
        {
            // Step
            mCurrentStepIndex = 0;
            mMaxStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;
            mbIsHandDefault = true;

            // Animation
            mStepNormalizedTime = 0f;
            mClimbUpMotionTime = 0f;

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
            //// Hand Default 상태에서 가장 높은 위치를 현재 위치로 설정하기 위해 -1을 해줌
            // mCurrentStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH - 1;
            mCurrentStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;
            mMaxStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;
            mbIsHandDefault = false;

            // Animation
            mStepNormalizedTime = _startClimbDownStepNormalizedTime;
            mClimbUpMotionTime = _startClimbDownStepNormalizedTime;

            // IK
            mLeftHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mRightHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mLeftHandIKWeight = 0f;
            mRightHandIKWeight = 0f;

            // Start Climb Down 애니메이션 후 위치 설정하기 때문에 LadderTop만 true로 줌
            mController.Animator.SetLadderTop(true);
        }
    }

    public bool SetLadderInMiddle(LadderInfo ladderInfo)
    {
        return SetLadderInMiddle(ladderInfo.handler);
    }

    public bool SetLadderInMiddle(LadderHandler ladderHandler)
    {
        mLadderHandler = ladderHandler;
        mStepPositions = mLadderHandler.GetStepPositions();

        // direction
        mLadderDirection = mLadderHandler.GetLadderDirection();
        mPreviousDirection = mController.Movement.Direction;
        mbIsSameDirectionStart = mLadderDirection == mController.Movement.Direction;

        if (mLadderDirection != PlayerMovement.EDirection.Forward)
            mController.Movement.SetDirection(mLadderDirection);

        mStartPart = LadderPart.Bottom;
        mbStartFromBottom = true;

        // Step
        for (int i = 0; i < mStepPositions.Count; i++)
        {
            if(mCharacterPosition.y < mStepPositions[i].y)
            {
                mCurrentStepIndex = i - 1;

                if (mCurrentStepIndex < 0)
                    mCurrentStepIndex = 0;
                    // return false;

                break;
            }
        }

        // mCurrentStepIndex = 0;
        mMaxStepIndex = TopStepIndex - DISTANCE_FOOT_TO_HAND_STRETCH;
        mbIsHandDefault = true;

        // Animation
        mStepNormalizedTime = 0f;
        mClimbUpMotionTime = 0f;

        // IK
        if (mCurrentStepIndex % 2 == 0)
        {
            mLeftHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
            mRightHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT;
        }
        else
        {
            mLeftHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT + 1;
            mRightHandStepNum = mCurrentStepIndex + DISTANCE_FOOT_TO_HAND_DEFAULT - 1;
        }

        mLeftHandIKWeight = 0f;
        mRightHandIKWeight = 0f;

        return true;
    }

    [Obsolete]
    public void EndToPlatform()
    {
        if(!mbLadderTop)
            return;

        mbLadderTop = false;

        // PlayerMoveState moveState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Move) as PlayerMoveState;
        PlayerMoveState moveState = mController.StateMachine.GetStateBase<PlayerMoveState>();

        if (Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
        {
            moveState.EnterToRun(mStartMoveInputX);
        }
        else
        {
            // moveState.EnterToIdle();
        }

        // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
        mController.StateMachine.SwitchState<PlayerMoveState>();
    }

    private void playFootStepSound(float volume)
    {
        AudioManager.instance.PlayOneShot("LadderFootStep", volume);
    }

    private void playFootStepSound()
    {
        AudioManager.instance.PlayOneShot("LadderFootStep");
    }

    private void playHandTouchSound()
    {
        AudioManager.instance.PlayOneShot("HandTouchWood", .1f);
    }

    private IEnumerator eClimb()
    {
        GameDebug.Log($"eClimb() called", tag: "LadderState Call");

        // Climb Up 애니메이션이 아니면 아래를 계산하지 않음
        while(true)
        {
            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (animatorStateInfo.IsTag("ClimbUp"))
                break;

            yield return null;
        }

        //// mbActiveIK = true;
        //if (mCurrentStepIndex % 2 == 1)
        //{
        //    //mbClimbing = true;
        //    //mbIsHandDefault = !mbIsHandDefault;
        //    //mClimbMultiplier = 1f;
        //    mStepNormalizedTime += .5f;
        //    //mClimbType = EClimbType.ClimbUp;
        //    mAnimator.Play(mAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, .5f);
        //}

        while (mbClimbLoop)
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
                    mStepNormalizedTime += STEP_NORMALIZED_TIME;
                    mClimbType = EClimbType.ClimbUp;
                    mLastStepIndex = mCurrentStepIndex;
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
                    mStepNormalizedTime -= STEP_NORMALIZED_TIME;
                    mClimbType = EClimbType.ClimbDown;
                    mLastStepIndex = mCurrentStepIndex;
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

                GameDebug.Log($"currentStepIndex: {mCurrentStepIndex}, LeftHandStepNum: {mLeftHandStepNum}, RightHandStepNum: {mRightHandStepNum}, stepNormalizedTime: {mStepNormalizedTime}",
                    tag: "LadderStepNum");

                // 사다리 반대 보기
                if (mLadderDirection == PlayerMovement.EDirection.Right)
                {
                    if (mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Up) || mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Down) || !mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Left))
                    {
                        if (mbIsLookBack)
                        {
                            mbIsLookBack = false;
                            mController.Animator.SetVertical(0f);
                            mController.Animator.Play(AnimState.Ladder_ClimbUp);

                            GameDebug.Log($"Look Back UnPressed", tag: "Ladder LookBack");
                        }
                    }
                    else if(mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Left))
                    {
                        if (!mbIsLookBack)
                        {
                            mbIsLookBack = true;
                            mController.Animator.SetVertical(1f);
                            mController.Animator.Play(AnimState.Ladder_Look_Back_L);

                            GameDebug.Log($"Look Back Pressed", tag: "Ladder LookBack");
                        }

                        // GameDebug.Log($"Current Anim State: {AnimStateNameLookUp.names[animatorStateInfo.fullPathHash]}, normalizedTime: {animatorStateInfo.normalizedTime}", tag: "Ladder LookBack");

                    }
                    //if (mController.InputHandler.MoveInputRaw.x < -.1f)
                    //{
                    //    // mRightHandIKWeight = 0f;
                    //    mLeftHandIKWeight = 0f;
                    //    // mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInputRaw.x));
                    //    mController.Animator.SetInputXMagnitude(1f);
                    //}
                    //else
                    //{
                    //    mController.Animator.SetInputXMagnitude(0f);
                    //}
                }
                else if (mLadderDirection == PlayerMovement.EDirection.Left)
                {
                    if (mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Right))
                    {
                        if (!mbIsLookBack)
                        {
                            mbIsLookBack = true;
                            mController.Animator.SetVertical(1f);
                            mController.Animator.Play(AnimState.Ladder_Look_Back_L);
                        }
                    }
                    else
                    {
                        if (mbIsLookBack)
                        {
                            mbIsLookBack = false;
                            mController.Animator.Play(AnimState.Ladder_ClimbUp);
                        }
                    }
                    //if (mController.InputHandler.MoveInputRaw.x > .1f)
                    //{
                    //    // mRightHandIKWeight = 0f;
                    //    mLeftHandIKWeight = 0f;
                    //    // mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInputRaw.x));
                    //    mController.Animator.SetInputXMagnitude(1f);
                    //}
                    //else
                    //{
                    //    mController.Animator.SetInputXMagnitude(0f);
                    //}
                }

                //if (mController.InputHandler.MoveInput.x < 0.1f)
                //{
                //    // mRightHandIKWeight = 0f;
                //    mLeftHandIKWeight = 0f;
                //}
            }

            // Ladder Bottom
            if (mCurrentStepIndex < 0)
            {
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
                StartCoroutine(eEndToGround());
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

                mController.Movement.SetGround(mLadderHandler.TopGround);
                //StartCoroutine(eEndToPlatform());

                mStateMachine.SwitchState<PlayerClimbLedgeState>((state) =>
                {
                    Bounds bounds = mLadderHandler.TopGround.GetComponent<BoxCollider>().bounds;
                    float distanceToMinLedgePointX = Mathf.Abs(mCharacterPosition.x - bounds.min.x);
                    float distanceToMaxLedgePointX = Mathf.Abs(mCharacterPosition.x - bounds.max.x);
                    // float distanceToNearestLedgePointX = Mathf.Min(distanceToMaxLedgePointX, distanceToMinLedgePointX);
                    float nearestLedgePointX = (distanceToMinLedgePointX < distanceToMaxLedgePointX) ? bounds.min.x : bounds.max.x;
                    Vector3 nearestLedgePoint = new Vector3(nearestLedgePointX, bounds.max.y, mCharacterPosition.z);

                    var climbLedgeInfo = new PlayerClimbLedgeState.ClimbLedgeInfo();
                    climbLedgeInfo.ledgeBounds = bounds;
                    climbLedgeInfo.checkIndex = 2;
                    climbLedgeInfo.nearestLedgePoint = nearestLedgePoint;

                    state.SetInfo(climbLedgeInfo);
                });

                break;
            }

            // 키입력이 들어와서 Climb Up이든 Down이든 하고 있는 상태
            // 키입력 한 번에 한 스텝을 기준으로 계산
            // 코드 복잡성 때문에 크게 Climb Up, Down 분기로 나눠줘야될 듯
            if (mbClimbing)
            {
                mController.Animator.SetInputXMagnitude(0f);
                mController.Animator.SetVertical(mClimbMultiplier);

                //mClimbUpMotionTime += mClimbMultiplier * Time.fixedDeltaTime;
                //mController.Animator.SetMotionTime(mClimbUpMotionTime);

                // 현재 위치가 다음 Step 위치가 되기 전까지 deltaPosition 처리
                if ((mClimbType == EClimbType.ClimbUp && mCharacterPosition.y < mStepPositions[mCurrentStepIndex].y)
                 || (mClimbType == EClimbType.ClimbDown && mCharacterPosition.y > mStepPositions[mCurrentStepIndex].y))
                {
                    // transform.position += mController.Animator.Animator.deltaPosition;
                    // mController.Movement.AddPosition(mController.Animator.Animator.deltaPosition);

                    float clampedNormalizedTime = 0f;
                    float t = 0f;

                    if(mClimbType == EClimbType.ClimbUp)
                    {
                        clampedNormalizedTime = mStepNormalizedTime - animatorStateInfo.normalizedTime;
                        // clampedNormalizedTime = mStepNormalizedTime - mClimbUpMotionTime;
                        t = 1 - clampedNormalizedTime / STEP_NORMALIZED_TIME;

                    }
                    else if(mClimbType == EClimbType.ClimbDown)
                    {
                        clampedNormalizedTime = animatorStateInfo.normalizedTime - mStepNormalizedTime;
                        // clampedNormalizedTime = mClimbUpMotionTime - mStepNormalizedTime;
                        t = 1 - clampedNormalizedTime / STEP_NORMALIZED_TIME;
                    }

                    float lerpedY = Mathf.Lerp(mStepPositions[mLastStepIndex].y, mStepPositions[mCurrentStepIndex].y, t);

                    Vector3 newPosition = mCharacterPosition;
                    newPosition.y = lerpedY;
                    mMovement.SetPosition(newPosition);

                    GameDebug.Log($"NormalizedTime: {animatorStateInfo.normalizedTime}/{mStepNormalizedTime}, clampedNormalizedTime: {clampedNormalizedTime}, t: {t}, lerpedY: {lerpedY}, StepPosY: {mStepPositions[mLastStepIndex].y}>{mStepPositions[mCurrentStepIndex].y}",
                        tag: "Ladder Climbing");
                }

                float stepGap = mStepPositions[1].y - mStepPositions[0].y;
                GameDebug.Log($"Climb Multiplier: {mClimbMultiplier}, deltaPosition: {mController.Animator.Animator.deltaPosition}, StepGap: {stepGap.ToString("G9")}",
                    tag: "Ladder Climbing");

                // Hand IK Weight를 자연스럽게 0부터 1까지 계산
                if (mClimbType == EClimbType.ClimbUp)
                {
                    if (mCurrentStepIndex % 2 == 0)
                    {
                        // 한 Step이 normalizedTime으로 .5f이기 때문에 분모를 .5로 계산
                        mRightHandIKWeight = (STEP_NORMALIZED_TIME - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / STEP_NORMALIZED_TIME;
                    }
                    else
                    {
                        mLeftHandIKWeight = (STEP_NORMALIZED_TIME - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / STEP_NORMALIZED_TIME;
                    }
                }
                else if (mClimbType == EClimbType.ClimbDown)
                {
                    if (mCurrentStepIndex % 2 == 0)
                    {
                        mLeftHandIKWeight = (STEP_NORMALIZED_TIME - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / STEP_NORMALIZED_TIME;
                    }
                    else
                    {
                        mRightHandIKWeight = (STEP_NORMALIZED_TIME - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / STEP_NORMALIZED_TIME;
                    }
                }

                // normalizedTime이 한 스텝만큼 변화하면 Idle로 전환
                if ((mClimbType == EClimbType.ClimbUp && animatorStateInfo.normalizedTime > mStepNormalizedTime)
                 || (mClimbType == EClimbType.ClimbDown && animatorStateInfo.normalizedTime < mStepNormalizedTime))
                //if ((mClimbType == EClimbType.ClimbUp && mClimbUpMotionTime > mStepNormalizedTime)
                // || (mClimbType == EClimbType.ClimbDown && mClimbUpMotionTime < mStepNormalizedTime))
                {
                    mbClimbing = false;
                    mController.Animator.SetVertical(0f);
                    // Debug.Log($"Step!! [NormalizedTime : {animatorStateInfo.normalizedTime.ToString("F1")}]");
                }
            }

            yield return null;
        }
    }

    private void climb()
    {
        StartCoroutine(eReadyToClimb());
        StartCoroutine(eClimbInFixedUpdate());
    }

    private IEnumerator eReadyToClimb()
    {
        while(mbClimbLoop)
        {
            if (mbClimbing || mbIsLookBack)
            {
                yield return null;
                continue;
            }

            if(mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Up))
            {
                mbClimbing = true;
                mbIsHandDefault = !mbIsHandDefault;
                mClimbMultiplier = 1f;
                mStepNormalizedTime += STEP_NORMALIZED_TIME;
                mClimbType = EClimbType.ClimbUp;
                mLastStepIndex = mCurrentStepIndex;
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
            else if(mInputHandler.IsKeyPressed(PlayerInputHandler.PressKey.Down))
            {
                mbClimbing = true;
                mbIsHandDefault = !mbIsHandDefault;
                mClimbMultiplier = -1f;
                mStepNormalizedTime -= STEP_NORMALIZED_TIME;
                mClimbType = EClimbType.ClimbDown;
                mLastStepIndex = mCurrentStepIndex;
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

            if(IsBottom)
            {
                mbClimbing = false;
                StartCoroutine(eEndToGround());
                break;
            }

            if(IsTop)
            {
                mbLadderTop = true;

                mController.Movement.SetGround(mLadderHandler.TopGround);

                mStateMachine.SwitchState<PlayerClimbLedgeState>((state) =>
                {
                    Bounds bounds = mLadderHandler.TopGround.GetComponent<BoxCollider>().bounds;
                    float distanceToMinLedgePointX = Mathf.Abs(mCharacterPosition.x - bounds.min.x);
                    float distanceToMaxLedgePointX = Mathf.Abs(mCharacterPosition.x - bounds.max.x);
                    // float distanceToNearestLedgePointX = Mathf.Min(distanceToMaxLedgePointX, distanceToMinLedgePointX);
                    float nearestLedgePointX = (distanceToMinLedgePointX < distanceToMaxLedgePointX) ? bounds.min.x : bounds.max.x;
                    Vector3 nearestLedgePoint = new Vector3(nearestLedgePointX, bounds.max.y, mCharacterPosition.z);

                    var climbLedgeInfo = new PlayerClimbLedgeState.ClimbLedgeInfo();
                    climbLedgeInfo.ledgeBounds = bounds;
                    climbLedgeInfo.checkIndex = 2;
                    climbLedgeInfo.nearestLedgePoint = nearestLedgePoint;

                    state.SetInfo(climbLedgeInfo);
                    // state.ClimbWithoutInput();
                });

                break;
            }

            lookBack();

            yield return null;
        }
    }

    private void lookBack()
    {
        // 사다리 반대 보기
        if (mLadderDirection == PlayerMovement.EDirection.Right)
        {
            if(mInputHandler.PressedKeys == PlayerInputHandler.PressKey.Left)
            {
                if (!mbPressedLookBack)
                {
                    mbPressedLookBack = true;
                    StartCoroutine(eLookBack());
                    //mController.Animator.SetMultiplier(1f);
                    //mController.Animator.Play(AnimState.Ladder_Look_Back_L);

                    GameDebug.Log($"Look Back Pressed", tag: "Ladder LookBack");
                }
            }
            else
            {
                if (mbPressedLookBack)
                {
                    mbPressedLookBack = false;
                    StartCoroutine(eLookFront());
                    // mController.Animator.Play(AnimState.Ladder_ClimbUp);

                    GameDebug.Log($"Look Back UnPressed", tag: "Ladder LookBack");
                }
            }
        }
        else if (mLadderDirection == PlayerMovement.EDirection.Left)
        {
            if(mInputHandler.PressedKeys == PlayerInputHandler.PressKey.Right)
            {
                if (!mbIsLookBack)
                {
                    mbIsLookBack = true;
                    mController.Animator.Play(AnimState.Ladder_Look_Back_L);
                }
            }
            else
            {
                if (mbIsLookBack)
                {
                    mbIsLookBack = false;
                    mController.Animator.Play(AnimState.Ladder_ClimbUp);
                }
            }
        }
    }

    private IEnumerator eLookBack()
    {
        mbIsLookBack = true;
        mController.Animator.SetMultiplier(1f);

        if(mbIsHandDefault)
            mController.Animator.Play(AnimState.Ladder_Look_Back_L);
        else
            mController.Animator.Play(AnimState.Ladder_Look_Back_R);

        while (mbClimbLoop)
        {
            var stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsTag("LookBack"))
            {
                yield return null;
                continue;
            }

            if (!(mInputHandler.PressedKeys == PlayerInputHandler.PressKey.Left))
            {
                mbPressedLookBack = false;
                StartCoroutine(eLookFront());
                yield break;
            }

            if (stateInfo.normalizedTime > 1f)
            {
                mController.Animator.SetMultiplier(0f);
            }

            yield return null;
        }
    }

    private IEnumerator eLookFront()
    {
        mController.Animator.SetMultiplier(-1f);

        while (mbClimbLoop)
        {
            var stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if(stateInfo.normalizedTime < 0f)
            {
                mController.Animator.Play(AnimState.Ladder_ClimbUp);
                mbIsLookBack = false;
                break;
            }

            yield return null;
        }

    }

    private IEnumerator eClimbInFixedUpdate()
    {
        while(mbClimbLoop)
        {
            if(!mbClimbing)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            mClimbUpMotionTime += mClimbMultiplier * (1f / CLIMB_ANIMATION_LENGTH) * Time.fixedDeltaTime; // 부호 * 변화속도 * 시간
            mController.Animator.SetMotionTime(mClimbUpMotionTime);

            if(mClimbType == EClimbType.ClimbUp)
            {
                // 현재 위치가 다음 Step 위치가 되기 전까지 deltaPosition 처리
                if (mCharacterPosition.y < mStepPositions[mCurrentStepIndex].y)
                {
                    float clampedNormalizedTime = 0f;
                    float t = 0f;

                    clampedNormalizedTime = mStepNormalizedTime - mClimbUpMotionTime;
                    t = 1 - clampedNormalizedTime / STEP_NORMALIZED_TIME;

                    float lerpedY = Mathf.Lerp(mStepPositions[mLastStepIndex].y, mStepPositions[mCurrentStepIndex].y, t);

                    Vector3 newPosition = mCharacterPosition;
                    newPosition.y = lerpedY;
                    mMovement.SetPosition(newPosition);

                    GameDebug.Log($"NormalizedTime: {animatorStateInfo.normalizedTime}/{mStepNormalizedTime}, clampedNormalizedTime: {clampedNormalizedTime}, t: {t}, lerpedY: {lerpedY}, StepPosY: {mStepPositions[mLastStepIndex].y}>{mStepPositions[mCurrentStepIndex].y}",
                        tag: "Ladder Climbing");
                }

                // Hand IK Weight를 자연스럽게 0부터 1까지 계산
                if (mCurrentStepIndex % 2 == 0)
                {
                    // 한 Step이 normalizedTime으로 .5f이기 때문에 분모를 .5로 계산
                    mRightHandIKWeight = (STEP_NORMALIZED_TIME - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / STEP_NORMALIZED_TIME;
                }
                else
                {
                    mLeftHandIKWeight = (STEP_NORMALIZED_TIME - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / STEP_NORMALIZED_TIME;
                }

                // normalizedTime이 한 스텝만큼 변화하면 Idle로 전환
                if (mClimbUpMotionTime > mStepNormalizedTime)
                {
                    mbClimbing = false;
                }
            }
            else if(mClimbType == EClimbType.ClimbDown)
            {
                // 현재 위치가 다음 Step 위치가 되기 전까지 deltaPosition 처리
                if (mCharacterPosition.y > mStepPositions[mCurrentStepIndex].y)
                {
                    float clampedNormalizedTime = 0f;
                    float t = 0f;

                    clampedNormalizedTime = mClimbUpMotionTime - mStepNormalizedTime;
                    t = 1 - clampedNormalizedTime / STEP_NORMALIZED_TIME;

                    float lerpedY = Mathf.Lerp(mStepPositions[mLastStepIndex].y, mStepPositions[mCurrentStepIndex].y, t);

                    Vector3 newPosition = mCharacterPosition;
                    newPosition.y = lerpedY;
                    mMovement.SetPosition(newPosition);

                    GameDebug.Log($"NormalizedTime: {animatorStateInfo.normalizedTime}/{mStepNormalizedTime}, clampedNormalizedTime: {clampedNormalizedTime}, t: {t}, lerpedY: {lerpedY}, StepPosY: {mStepPositions[mLastStepIndex].y}>{mStepPositions[mCurrentStepIndex].y}",
                        tag: "Ladder Climbing");
                }

                // Hand IK Weight를 자연스럽게 0부터 1까지 계산
                if (mCurrentStepIndex % 2 == 0)
                {
                    mLeftHandIKWeight = (STEP_NORMALIZED_TIME - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / STEP_NORMALIZED_TIME;
                }
                else
                {
                    mRightHandIKWeight = (STEP_NORMALIZED_TIME - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / STEP_NORMALIZED_TIME;
                }

                // normalizedTime이 한 스텝만큼 변화하면 Idle로 전환
                if (mClimbUpMotionTime < mStepNormalizedTime)
                {
                    mbClimbing = false;
                }
            }

            //// 현재 위치가 다음 Step 위치가 되기 전까지 deltaPosition 처리
            //if ((mClimbType == EClimbType.ClimbUp && mCharacterPosition.y < mStepPositions[mCurrentStepIndex].y)
            // || (mClimbType == EClimbType.ClimbDown && mCharacterPosition.y > mStepPositions[mCurrentStepIndex].y))
            //{
            //    float clampedNormalizedTime = 0f;
            //    float t = 0f;

            //    if (mClimbType == EClimbType.ClimbUp)
            //    {
            //        clampedNormalizedTime = mStepNormalizedTime - mClimbUpMotionTime;
            //        t = 1 - clampedNormalizedTime / STEP_NORMALIZED_TIME;

            //    }
            //    else if (mClimbType == EClimbType.ClimbDown)
            //    {
            //        clampedNormalizedTime = mClimbUpMotionTime - mStepNormalizedTime;
            //        t = 1 - clampedNormalizedTime / STEP_NORMALIZED_TIME;
            //    }

            //    float lerpedY = Mathf.Lerp(mStepPositions[mLastStepIndex].y, mStepPositions[mCurrentStepIndex].y, t);

            //    Vector3 newPosition = mCharacterPosition;
            //    newPosition.y = lerpedY;
            //    mMovement.SetPosition(newPosition);

            //    GameDebug.Log($"NormalizedTime: {animatorStateInfo.normalizedTime}/{mStepNormalizedTime}, clampedNormalizedTime: {clampedNormalizedTime}, t: {t}, lerpedY: {lerpedY}, StepPosY: {mStepPositions[mLastStepIndex].y}>{mStepPositions[mCurrentStepIndex].y}",
            //        tag: "Ladder Climbing");
            //}

            float stepGap = mStepPositions[1].y - mStepPositions[0].y;
            GameDebug.Log($"Climb Multiplier: {mClimbMultiplier}, deltaPosition: {mController.Animator.Animator.deltaPosition}, StepGap: {stepGap.ToString("G9")}",
                tag: "Ladder Climbing");

            //// Hand IK Weight를 자연스럽게 0부터 1까지 계산
            //if (mClimbType == EClimbType.ClimbUp)
            //{
            //    if (mCurrentStepIndex % 2 == 0)
            //    {
            //        // 한 Step이 normalizedTime으로 .5f이기 때문에 분모를 .5로 계산
            //        mRightHandIKWeight = (STEP_NORMALIZED_TIME - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / STEP_NORMALIZED_TIME;
            //    }
            //    else
            //    {
            //        mLeftHandIKWeight = (STEP_NORMALIZED_TIME - (mStepNormalizedTime - animatorStateInfo.normalizedTime)) / STEP_NORMALIZED_TIME;
            //    }
            //}
            //else if (mClimbType == EClimbType.ClimbDown)
            //{
            //    if (mCurrentStepIndex % 2 == 0)
            //    {
            //        mLeftHandIKWeight = (STEP_NORMALIZED_TIME - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / STEP_NORMALIZED_TIME;
            //    }
            //    else
            //    {
            //        mRightHandIKWeight = (STEP_NORMALIZED_TIME - (animatorStateInfo.normalizedTime - mStepNormalizedTime)) / STEP_NORMALIZED_TIME;
            //    }
            //}

            //// normalizedTime이 한 스텝만큼 변화하면 Idle로 전환
            //if ((mClimbType == EClimbType.ClimbUp && mClimbUpMotionTime > mStepNormalizedTime)
            // || (mClimbType == EClimbType.ClimbDown && mClimbUpMotionTime < mStepNormalizedTime))
            //{
            //    mbClimbing = false;
            //}

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator eStartClimbUp()
    {
        GameDebug.Log($"eStartClimbUp() called", tag: "LadderState Call");

        mController.Animator.Play(AnimState.Ladder_ClimbUp);

        // Start Climb Up 애니메이션 없이 시작하기 때문에 위치 즉시 설정
        // 자연스러움을 위해서는 Lerp 처리하던지 해야함
        Vector3 targetPosition = mController.Movement.Position;

        if (mLadderDirection == PlayerMovement.EDirection.Right)
        {
            targetPosition.x = mStepPositions[mCurrentStepIndex].x - _distanceToCharacter;
        }
        else if (mLadderDirection == PlayerMovement.EDirection.Left)
        {
            targetPosition.x = mStepPositions[mCurrentStepIndex].x + _distanceToCharacter;
        }
        else if (mLadderDirection == PlayerMovement.EDirection.Forward)
        {
            targetPosition.x = mStepPositions[mCurrentStepIndex].x;
        }

        targetPosition.y = mStepPositions[mCurrentStepIndex].y;
        // transform.position = targetPosition;

        if(mLadderHandler.SidePassable)
        {
            targetPosition.z = mLadderHandler.transform.position.z;
        }

        Quaternion targetRotation = mController.Movement.DirectionToRotation(mLadderDirection);
        transform.rotation = targetRotation;

        float timer = 0f;
        float lerpDuration = _startClimbUpDuration;
        Vector3 startPosition = mCharacterPosition;

        while (timer < lerpDuration)
        {
            float t = timer / lerpDuration;
            // transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            mController.Movement.SetPosition(Vector3.Lerp(startPosition, targetPosition, t));

            mLeftHandIKWeight = Mathf.Lerp(0f, 1f, t);
            mRightHandIKWeight = Mathf.Lerp(0f, 1f, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // transform.position = targetPosition;
        mController.Movement.SetPosition(targetPosition);

        // StartCoroutine(eClimb());
        climb();
    }

    private IEnumerator eEndToGround()
    {
        mController.Animator.Play(AnimState.Idle);

        Vector3 startPos = mCharacterPosition;
        Vector3 targetPos = startPos;
        // targetPos.y = mLadderHandler.Bottom.y;
        targetPos.y = mLadderHandler.BottomGround.transform.position.y;

        float timer = 0f;
        float duration = _endToGroundDuration;

        while (timer < duration)
        {
            Vector3 currentPos = mCharacterPosition;
            currentPos.y = Mathf.Lerp(startPos.y, targetPos.y, timer / duration);
            mController.Movement.SetPosition(currentPos);

            timer += Time.deltaTime;
            yield return null;
        }

        mController.Movement.SetPosition(targetPos);

        // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Move);
        mController.StateMachine.SwitchState<PlayerIdleState>((state) =>
        {
            state.EnterWithoutAnimation();
        });
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
                if (mClimbType == EClimbType.ClimbUp && mCharacterPosition.y < mStepPositions[mCurrentStepIndex].y)
                {
                    // transform.position += mController.Animator.Animator.deltaPosition;
                    mController.Movement.AddPosition(mController.Animator.Animator.deltaPosition);
                }

                GameDebug.Log($"ClimbType: {mClimbType}, posY: {mCharacterPosition.y}, stepPositionY: {mStepPositions[mCurrentStepIndex].y}, deltaPosition: {mController.Animator.Animator.deltaPosition}",
                    tag: "EndToPlatform ClimbUp", level: GameDebug.LogLevel.Verbose);

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

            if(mLadderDirection == PlayerMovement.EDirection.Forward)
            {
                if (animatorStateInfo.normalizedTime > _endToPlatformTopTime)
                    deltaPosition.z *= (mCharacterPosition.z < mStepPositions[TopStepIndex].z + .5f) ? _endToPlatformXSpeed : 0f;
                // deltaPosition.y *= (transform.position.y < mStepPositions[TopStepIndex].y) ? 1.2f : 0f;
                deltaPosition.x = 0f;
            }
            else
            {
                if (animatorStateInfo.normalizedTime > _endToPlatformTopTime)
                    deltaPosition.x *= (mCharacterPosition.x < mStepPositions[TopStepIndex].x + .5f) ? _endToPlatformXSpeed : 0f;
                // deltaPosition.y *= (transform.position.y < mStepPositions[TopStepIndex].y) ? 1.2f : 0f;
                deltaPosition.z = 0f;
            }

            // transform.position += deltaPosition;
            mController.Movement.AddPosition(deltaPosition);

            if (animatorStateInfo.normalizedTime > _endToPlatformTopTime)
            {
                Vector3 newPosition = mCharacterPosition;
                newPosition.y = Mathf.Lerp(mCharacterPosition.y, mStepPositions[TopStepIndex].y, Time.deltaTime);
                // mController.Movement.SetPosition(newPosition);
            }

            mController.Animator.SetInputXMagnitude(Mathf.Abs(mController.InputHandler.MoveInput.x));

            // mStartMoveInputX = deltaPosition.x / (Time.deltaTime * mController.Movement.MoveSpeed);
            float decimalTime = animatorStateInfo.normalizedTime - (int)animatorStateInfo.normalizedTime;

            GameDebug.Log($"CurrentPositionY: {mCharacterPosition.y}, TopStepPositionY: {mStepPositions[TopStepIndex].y}, normalizedTime: {animatorStateInfo.normalizedTime}",
                tag: "EndToPlatform Loop");

            //if (decimalTime > .8f)
            //{
            //    EndToPlatform();
            //    yield break;
            //}

            yield return null;
        }
    }

    private IEnumerator eStartClimbDown()
    {
        mController.Animator.Play(AnimState.Ladder_Start_From_Top);

        //while (true)
        //{
        //    AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

        //    if (animatorStateInfo.IsTag("StartFromTop"))
        //        break;

        //    yield return null;
        //}

        // LadderTop을 통해서 이미 애니메이션이 실행됐기 때문에 False 처리
        mController.Animator.SetLadderTop(false);

        var moveDirection = (mLadderDirection == PlayerMovement.EDirection.Left) ? PlayerMovement.EDirection.Right : PlayerMovement.EDirection.Left;

        float timer = 0f;
        float turnDuration = .5f;
        float startAngle = mMovement.DirectionToRotation(mPreviousDirection).eulerAngles.y;
        float targetAngle = mMovement.DirectionToRotation(mLadderDirection).eulerAngles.y;
        Vector3 ladderPosition = mLadderHandler.transform.position;

        float firstMoveXDuration = .5f;
        float firstStartX = mCharacterPosition.x;
        // float firstTargetX = ladderPosition.x + PlayerMovement.DirectionToVector(mPreviousDirection).x * .05f;
        float firstTargetX = ladderPosition.x + PlayerMovement.DirectionToVector(moveDirection).x * .05f;

        float secondMoveXDuration = .25f;
        float secondStartX = firstTargetX;
        // float secondTargetX = ladderPosition.x + PlayerMovement.DirectionToVector(mPreviousDirection).x * _distanceToCharacter;
        float secondTargetX = ladderPosition.x + PlayerMovement.DirectionToVector(moveDirection).x * _distanceToCharacter;

        float moveYStartTime = .75f;
        float moveYDuration = .5f;
        float startY = mCharacterPosition.y;
        // float targetY = mCharacterPosition.y - 1.75f;
        float targetY = mStepPositions[mCurrentStepIndex].y;

        float animDuration = 1.25f;

        while (true)
        {
            //AnimatorStateInfo animatorStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            //if (!animatorStateInfo.IsTag("StartFromTop"))
            //    break;

            Vector3 lerpedPos = mCharacterPosition;

            if(timer > animDuration)
            {
                mMovement.SetRotation(Quaternion.Euler(0f, targetAngle, 0f));

                lerpedPos.x = secondTargetX;
                lerpedPos.y = targetY;
                mMovement.SetPosition(lerpedPos);

                break;
            }

            timer += Time.deltaTime;

            if(timer < turnDuration)
            {
                float lerpedAngle = Mathf.Lerp(startAngle, targetAngle, timer / turnDuration);
                mMovement.SetRotation(Quaternion.Euler(0f, lerpedAngle, 0f));
            }

            if (timer < firstMoveXDuration)
            {
                float lerpedX = Mathf.Lerp(firstStartX, firstTargetX, timer / firstMoveXDuration);
                lerpedPos.x = lerpedX;
            }

            if(timer > firstMoveXDuration && timer < firstMoveXDuration + secondMoveXDuration)
            {
                float lerpedX = Mathf.Lerp(secondStartX, secondTargetX, (timer - firstMoveXDuration) / secondMoveXDuration);
                lerpedPos.x = lerpedX;
            }

            if(timer > moveYStartTime && timer < moveYStartTime + moveYDuration)
            {
                float lerpedY = Mathf.Lerp(startY, targetY, (timer - moveYStartTime) / moveYDuration);
                lerpedPos.y = lerpedY;
            }

            mMovement.SetPosition(lerpedPos);

            GameDebug.Log($"Angle Start/Target: {startAngle}/{targetAngle}, X Start/Target: {secondStartX}/{secondTargetX}, Y Start/Target: {startY}/{targetY}, Current Angle/Position: {mCharacterRotation.eulerAngles.y}/{mCharacterPosition}",
                tag: "Ladder StartFromTop");

            //// Start Climb Down 애니메이션 delta 계산
            //// deltaPosition
            //Vector3 deltaPosition = mController.Animator.Animator.deltaPosition;
            //// 일정 위치까지 이동 시키기 위해서 일정 위치 전 까지는 deltaPosition을 배수 처리
            //// x
            //if(mLadderDirection == PlayerMovement.EDirection.Right)
            //{
            //    deltaPosition.x *= (mCharacterPosition.x > mStepPositions[mCurrentStepIndex].x - _distanceToCharacter) ? _startClimbDownXSpeed : 0f;

            //    if (mbIsSameDirectionStart)
            //        deltaPosition.x *= -1f;
            //}
            //else
            //{
            //    deltaPosition.x *= (mCharacterPosition.x < mStepPositions[mCurrentStepIndex].x + _distanceToCharacter) ? _startClimbDownXSpeed : 0f;

            //    if (mbIsSameDirectionStart)
            //        deltaPosition.x *= -1f;
            //}

            //// y
            //if (animatorStateInfo.normalizedTime > .6f)
            //    deltaPosition.y *= (mCharacterPosition.y > mStepPositions[mCurrentStepIndex].y) ? _startClimbDownYSpeed : 0f;

            //// z
            //deltaPosition.z = 0f;
            //// transform.position += deltaPosition;
            //mController.Movement.AddPosition(deltaPosition);

            // deltaRotation
            //if(mPreviousDirection != mLadderDirection)
            //{
            //    // 현재 방향에서 반대 방향까지 애니메이션 normalizedTime에 맞춰서 회전
            //    //mController.Movement.RotateTo(mPreviousDirection,
            //    //                            mLadderDirection,
            //    //                            animatorStateInfo.normalizedTime);
            //    Vector3 eulerAngles = mController.Animator.Animator.deltaRotation.eulerAngles;
            //    eulerAngles.y *= _rotationSpeed;
            //    mRotatedAngles += eulerAngles.y;
            //    if (Mathf.Abs(mRotatedAngles) < 180f)
            //    {
            //        transform.rotation *= Quaternion.Euler(eulerAngles);
            //    }
            //    else
            //    {
            //        transform.rotation = mController.Movement.DirectionToRotation(mLadderDirection);
            //    }
            //}

            //// IK
            //mLeftHandIKWeight = animatorStateInfo.normalizedTime;
            //mRightHandIKWeight = animatorStateInfo.normalizedTime;

            yield return null;
        }

        GameDebug.Log($"StartFromTop End");

        mController.Animator.Play(AnimState.Ladder_ClimbUp);


        // StartCoroutine(eClimb());
        climb();
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

    private IEnumerator eLookBackToJump()
    {
        mController.Animator.Play(mbIsHandDefault ? AnimState.Ladder_Look_Back_To_Jump_L : AnimState.Ladder_Look_Back_To_Jump_R);

        while (true)
        {
            yield return new WaitForFixedUpdate();

            var stateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);

            if(stateInfo.normalizedTime > .99f)
            {
                mController.Animator.Play(AnimState.RunJump_Blend_Tree);
                break;
            }
        }
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
