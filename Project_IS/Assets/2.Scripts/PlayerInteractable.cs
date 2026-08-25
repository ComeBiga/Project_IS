using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerInteractable : MonoBehaviour
{
    public struct InteractedInfo
    {
        public InteractableObject interactableObject;
        public RaycastHit hitInfo;
        public float distanceToEdge;
    }

    public enum CastDirection
    {
        None = 0,
        Front = 1 << 1,
        Back = 1 << 2,
        Side = 1 << 3,
        Under = 1 << 4,

        All = ~0
    }

    public int Layer => _layer;
    public CastDirection CastedDirections => mCastedDirections;

    private Vector3 CharacterPosition => mController.Movement.Position;

    [SerializeField] private LayerMask _layer;
    [SerializeField] private float _raycastDistance = 5f;
    [SerializeField] private float _offsetY = 1f;
    [SerializeField] private float _interactableDistance = .5f;
    [SerializeField] private float _sidePassZDistance = .75f;

    private PlayerController mController;
    private float mPathZPosition;
    private CastDirection mCastedDirections = CastDirection.None;
    private Dictionary<CastDirection, InteractedInfo?> mInteractableObjectsDic = new Dictionary<CastDirection, InteractedInfo?>();

    public void Initialize(PlayerController playerController)
    {
        mController = playerController;
        mPathZPosition = mController.Movement.Position.z;

        initInteractableObjectsDic();
    }

    public void Tick()
    {
        CheckInteractableObject(out RaycastHit hit);
    }

    public bool IsDirectionCasted(CastDirection direction)
    {
        return (mCastedDirections & direction) != 0;
    }

    public bool TryGetInteractedInfo(CastDirection direction, out InteractedInfo interactedInfo)
    {
        if (IsDirectionCasted(direction))
        {
            interactedInfo = mInteractableObjectsDic[direction].Value;
            return true;
        }

        interactedInfo = new InteractedInfo();
        return false;
    }

    public CastDirection CheckInteractableObject(out RaycastHit hitInfo)
    {
        var castedDirections = CastDirection.None;

        // z가 0일 때의 위치
        Vector3 pathOrigin = CharacterPosition;
        pathOrigin.y += _offsetY;
        // pathOrigin.z = 0f;
        pathOrigin.z = mPathZPosition;

        // 현재 캐릭터의 위치
        Vector3 characterOrigin = CharacterPosition;
        characterOrigin.y += _offsetY;

        // 현재 캐릭터 발을 기준으로 한 위치
        Vector3 characterFeetOrigin = CharacterPosition;

        bool bFrontCasted = Physics.Raycast(pathOrigin,
                                        mController.Movement.DirectionToVector(),
                                        out hitInfo,
                                        _raycastDistance,
                                        _layer);

        setCastedInfo(bFrontCasted, CastDirection.Front, hitInfo);

        bool bSideCasted = Physics.Raycast(characterOrigin,
                                        Vector3.forward,
                                        out hitInfo,
                                        _raycastDistance,
                                        _layer);

        setCastedInfo(bSideCasted, CastDirection.Side, hitInfo);

        bool bUnderCasted = Physics.Raycast(characterFeetOrigin,
                                    Vector3.down,
                                    out hitInfo,
                                    _raycastDistance,
                                    _layer);

        setCastedInfo(bUnderCasted, CastDirection.Under, hitInfo);

        bool bBackCasted = Physics.Raycast(pathOrigin,
                                    PlayerMovement.DirectionToVector(mController.Movement.OppositeDirection),
                                    out hitInfo,
                                    _raycastDistance,
                                    _layer);

        setCastedInfo(bBackCasted, CastDirection.Back, hitInfo);

        return castedDirections;
    }

    private void updateInteractable(int type, RaycastHit hitInfo)
    {
        // front
        if (type == 1)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            // Bounds bounds = interactableObject.BoxCollider.bounds;
            Bounds bounds = hitInfo.collider.bounds;
            Vector3 characterPos = CharacterPosition;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            // float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);
            float distanceToEdge = Mathf.Abs(characterPos.x - hitInfo.point.x);

            // 전방의 오브젝트를 옆으로 비켜지나가는 코드
            if (interactableObject.SidePassable && characterPos.z > mPathZPosition - _sidePassZDistance)
            {
                // 가까운 모서리를 기준으로 zDistance 떨어진 점을 targetPos로 설정
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // targetPos.y = 0f;
                targetPos.y = characterPos.y;
                targetPos.z = mPathZPosition - _sidePassZDistance;

                // targetPos까지의 방향을 normalize해서 x:z 비율로 velocity.z를 계산 
                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
            }

            // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
            PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase<PlayerPushPullState>();

            // PushPull Front
            if (interactableObject.Pushable && distanceToEdge < _interactableDistance && mController.InputHandler.IsInteracting)
            {
                // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(1);
                pushPullState.SetPushPoint(hitInfo.point);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                mController.StateMachine.SwitchState<PlayerPushPullState>();
            }

            // PushPull Front (Auto Push)
            if (interactableObject.Pushable && distanceToEdge < pushPullState.FrontPushPullDistance && Mathf.Abs(mController.InputHandler.MoveInput.x) > .1f)
            {
                // mController.Animator.SetPush();
                // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(2);
                pushPullState.SetPushPoint(hitInfo.point);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                mController.StateMachine.SwitchState<PlayerPushPullState>();

            }

            // Climb Object Up
            if (interactableObject.CanClimb && distanceToEdge < _interactableDistance && mController.InputHandler.MoveInput.y > .1f)
            {
                // PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase<PlayerClimbObjectState>();
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                climbObjectState.SetHitPoint(hitInfo.point);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                mController.StateMachine.SwitchState<PlayerClimbObjectState>();
            }

            // Ladder
            if ((interactableObject.CompareTag("Ladder") || hitInfo.collider.CompareTag("LadderTop"))
                && distanceToEdge < _interactableDistance)
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                // bool bSwitched = switchToLadderState(ladderCollider);

                //if (bSwitched)
                //    return;
            }

            // Interact
            if (distanceToEdge < interactableObject.InteractionDistance && mController.InputHandler.IsInteracting)
            {
                // PlayerInteractState interactState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.Interact) as PlayerInteractState;
                PlayerInteractState interactState = mController.StateMachine.GetStateBase<PlayerInteractState>();

                interactState.SetInteractableObject(interactableObject);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.Interact);
                mController.StateMachine.SwitchState<PlayerInteractState>();
            }

        }
        // side
        else if (type == 2)
        {
            // velocity.z를 0으로 해주지 않으면 계속 z축으로 관성?이 남아있음
            Vector3 velocity = mController.Movement.Velocity;
            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);

            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();

            // PushPull Object
            if (interactableObject.Pushable && mController.InputHandler.IsInteracting)
            {
                // PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.PushPull) as PlayerPushPullState;
                PlayerPushPullState pushPullState = mController.StateMachine.GetStateBase<PlayerPushPullState>();
                pushPullState.SetPushPullObject(interactableObject as PushPullObject);
                pushPullState.SetType(0);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.PushPull);
                mController.StateMachine.SwitchState<PlayerPushPullState>();
            }

            // Climb Object Up
            if (interactableObject.CanClimb && mController.InputHandler.MoveInput.y > .1f)
            {
                // PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase<PlayerClimbObjectState>();
                climbObjectState.SetClimbObject(interactableObject, climbUp: true);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                mController.StateMachine.SwitchState<PlayerClimbObjectState>();
            }

            // Ladder
            if ((interactableObject.CompareTag("Ladder") || hitInfo.collider.CompareTag("LadderTop")))
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                // bool bSwitched = switchToLadderState(ladderCollider);

                //if (bSwitched)
                //    return;
            }
        }
        // under
        else if (type == 3)
        {
            // Climb Object Down
            if (mController.InputHandler.MoveInput.y < -.1f)
            {
                var interactableObject = hitInfo.collider.GetComponent<InteractableObject>();

                // PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase(PlayerStateMachine.EState.ClimbObject) as PlayerClimbObjectState;
                PlayerClimbObjectState climbObjectState = mController.StateMachine.GetStateBase<PlayerClimbObjectState>();
                climbObjectState.SetClimbObject(interactableObject, climbUp: false);
                // mController.StateMachine.SwitchState(PlayerStateMachine.EState.ClimbObject);
                mController.StateMachine.SwitchState<PlayerClimbObjectState>();
            }

            // Debug.Log(hitInfo.collider.name);
        }
        // back
        else if (type == 0)
        {
            var interactableObject = hitInfo.collider.GetComponentInParent<InteractableObject>();
            Bounds bounds = interactableObject.BoxCollider.bounds;
            Vector3 characterPos = CharacterPosition;

            // 현재 캐릭터 위치와 오브젝트의 가까운 모서리까지의 거리
            float distanceToMin = Mathf.Abs(characterPos.x - bounds.min.x);
            float distanceToMax = Mathf.Abs(characterPos.x - bounds.max.x);
            float distanceToEdge = Mathf.Min(distanceToMin, distanceToMax);

            // 오브젝트를 비켜지나가고 나서 z위치를 다시 0으로 맞춰주는 코드
            if (interactableObject.SidePassable && characterPos.z < mPathZPosition)
            {
                Vector3 targetPos = (characterPos.x < bounds.center.x) ? bounds.min : bounds.max;
                // 오브젝트를 감지할 수 있는 최대 거리까지 서서히 맞춰주게 함
                targetPos.x += (characterPos.x < bounds.center.x) ? -_raycastDistance : _raycastDistance;
                targetPos.y = characterPos.y;
                targetPos.z = mPathZPosition;

                Vector3 direction = targetPos - characterPos;
                Vector3 normalized = direction.normalized;

                Vector3 velocity = mController.Movement.Velocity;
                velocity.z = velocity.x * (normalized.z / normalized.x);    // velocity.x : velocity.z = normalized.x : normalized.z
                mController.Movement.SetVelocity(velocity);
            }

            // Ladder
            if ((interactableObject.CompareTag("Ladder") || hitInfo.collider.CompareTag("LadderTop"))
                && distanceToEdge < _interactableDistance)
            {
                Collider[] ladderCollider = new Collider[1];
                ladderCollider[0] = hitInfo.collider;
                //bool bSwitched = switchToLadderState(ladderCollider);

                //if (bSwitched)
                //    return;
            }
        }
        // none
        else
        {
            // velocity.z를 0으로 해주지 않으면 계속 z축으로 관성?이 남아있음
            Vector3 velocity = mController.Movement.Velocity;
            velocity.z = 0f;
            mController.Movement.SetVelocity(velocity);
        }
    }

    private void setCastedInfo(bool casted, CastDirection targetDirection, RaycastHit hitInfo)
    {
        if (casted)
        {
            mCastedDirections |= targetDirection;
            setInteractedInfo(targetDirection, hitInfo);
        }
        else
        {
            mCastedDirections &= ~targetDirection;
            clearInteractedInfo(targetDirection);
        }
    }

    private void initInteractableObjectsDic()
    {
        mInteractableObjectsDic.Add(CastDirection.Front, null);
        mInteractableObjectsDic.Add(CastDirection.Back, null);
        mInteractableObjectsDic.Add(CastDirection.Side, null);
        mInteractableObjectsDic.Add(CastDirection.Under, null);
    }

    private void setInteractedInfo(CastDirection targetDirection, RaycastHit hitInfo)
    {
        var interactedInfo = new InteractedInfo();
        interactedInfo.hitInfo = hitInfo;
        interactedInfo.interactableObject = hitInfo.collider.GetComponent<InteractableObject>();
        interactedInfo.distanceToEdge = Mathf.Abs(CharacterPosition.x - hitInfo.point.x);

        mInteractableObjectsDic[targetDirection] = interactedInfo;
    }

    private void clearInteractedInfo(CastDirection targetDirection)
    {
        mInteractableObjectsDic[targetDirection] = null;
    }
}
