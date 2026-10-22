# 캐릭터 Turn 재생과 회전 시스템

- 기준일: 2026-10-22
- 환경: Unity 2022.3.61f1, C#, URP
- 현재 소스를 설명하는 문서다. 변경 이력과 검증 범위는 [작업 기록](../Changes/2026-10-22_CharacterTurnCleanup.md)을 참고한다.

## 1. 개요

Turn Clip을 Playables로 재생하면서 캐릭터의 Y축 회전을 별도로 제어한다. 일반 애니메이션은 같은 Graph의 `AnimatorControllerPlayable`에서 재생하고, Mixer Weight로 Turn과 연결한다.

현재 기본 경로는 `RotationHandler.EType.Normal`이다. 이 경로에서는 `PlayerTurnState`가 제공하는 공통 시간으로 Clip 샘플 시점과 회전 진행률을 결정한다. 회전 완료, Clip 종료 구간 진입, BlendOut 완료는 서로 다른 조건이다.

## 2. 구조와 의존 관계

다음 경로는 `Assets/2.Scripts/Gameplay/Character/` 기준이다.

| 클래스 / 소스 | 책임 |
|---|---|
| `PlayerState/PlayerTurnState.cs` | Turn 종류·방향 선택, 공통 시간 진행, 입력 해제·재반전·Jump/Fall 분기, 잔여 회전 소유 |
| `Animation/TurnPlayable.cs` | Graph 생성·평가·파괴, Clip 시간 적용, Mixer Blend, Controller 전환 요청·완료 상태 제공 |
| `Animation/TurnAnimationData.cs` | 종류·방향별 Clip, 회전 완료 시간, 달리기 시작 시간, Controller 시작 위상 저장 |
| `Movement/Rotation/RotationHandler.cs` | `RotationBase` 주입, 회전 상태·방향 관리, 외부 진행률 적용, 완료 후 중복 회전 방지 |
| `Movement/Rotation/NormalRotation.cs` | 진행률에 따른 실제 회전과 목표 각도 확정 |
| `PlayerAnimation.cs` | 유효한 ControllerPlayable로 재생·Parameter·State 조회 경로 통일, Graph 평가 횟수 제공 |
| `PlayerState/PlayerMoveState.cs` | Run 준비, Blend 중 잔여 회전 계속 처리, 필요 시 RunToIdle에 인계 |
| `PlayerState/PlayerRunToIdleState.cs` | RunToIdle 재생, 잔여 회전 계속 처리, 애니메이션·회전 완료 판정 |
| `PlayerState/PlayerRunJumpState.cs`, `PlayerFallState.cs` | 기존 Turn 회전 중단, 다음 Controller 애니메이션으로 Blend |
| `PlayerMovement.cs`, `PlayerController.cs`, StateMachine | 방향·Transform 회전·이동 적용, 입력 판정, 상태 전환 |

`AnimationCurveRotation`, `RootMotionRotation`, `Offset180Rotation` 선택 경로도 남아 있다. 공통 시간과 잔여 회전 인계 설명은 Normal 경로에 해당하며 다른 전략에 동일하게 보장되지 않는다. `PlayableRotation` 클래스는 제거되었다.

### Graph 구성

`TurnPlayable.createGraph()`는 Manual Graph와 7개 입력의 `AnimationMixerPlayable`을 만든다.

| Port | 입력 |
|---|---|
| 0 | `AnimatorControllerPlayable` |
| 1 / 2 | IdleTurn L / R |
| 3 / 4 | RunTurn L / R |
| 5 / 6 | IdleTurnToIdle L / R; Clip이 있을 때만 연결 |

Mixer 출력은 `AnimationPlayableOutput`을 통해 `PlayerAnimation.Animator`에 적용된다. Controller는 `Animator.runtimeAnimatorController`로 생성된다. 따라서 Animator Controller 에셋은 계속 사용하며, 실행 상태와 Parameter의 제어 대상이 ControllerPlayable인 구조다.

## 3. 데이터와 시간 규약

`TurnAnimationData.TurnAnimation`은 현재 `struct`다.

| 필드 | 의미 |
|---|---|
| `sourceClip` | 재생할 Clip |
| `useRotationDuration`, `rotationDuration` | 명시적 180도 회전 완료 시간 사용 여부와 시간 |
| `startRunTime` | IdleTurn 입력 해제 시 IdleTurnToIdle 사용 가능 구간의 상한 |
| `controllerStartNormalizedTime` | 다음 Controller State를 시작할 정규화 시간 |
| `turnType`, `rotationDirection` | 데이터에 저장된 종류·방향 정보; 현재 선택은 `Get()`과 L/R 필드 기준 |

`GetRotationDuration()`은 `useRotationDuration`이 참이면 `rotationDuration`, 아니면 `sourceClip.length`를 반환한다. `rotationDuration`과 `_blendDuration`은 각각 회전 시간과 Mixer Blend 시간이며 대체 관계가 아니다.

대화에서 확인한 콘텐츠 전제는 회전 시간이 Clip 길이보다 짧고, 선형 회전과 Clip 자세를 콘텐츠 제작 시 맞춘다는 것이다. IdleTurnToIdle R의 회전 완료 시간 0.667초, IdleTurn 달리기 시작 약 0.883초는 사용자 제공 기준이며, 이번 문서 작업에서 실제 에셋 설정값을 검증하지 않았다.

## 4. 동작 흐름

### Turn 진입과 재진입

1. `Initialize()`에서 Rotation 객체들을 만들고 `RotationHandler.SetRotation()`으로 선택한 구현을 주입한다.
2. `EnterState()`가 인계·정지 Clip 관련 플래그를 초기화하고 `changeDirection()`을 호출한다.
3. 이동 방향을 반전하고, 현재 Transform Y각과 목표 방향 Y각의 `Mathf.DeltaAngle`로 남은 각도와 회전 방향을 구한다.
4. 남은 각도로 시작 진행률과 시작 시간을 정한다.

   ```csharp
   normalizedTime = Mathf.Clamp01(1f - Mathf.Abs(remainAngle) / Number.DEG_180);
   startTime = normalizedTime * mTurnAnimation.GetRotationDuration();
   ```

5. `ChangeClip()` → `ResetClip(startTime)` → Normal이면 `SetClipTime(mTurnTime)` → `StartClip()` 순서로 Clip과 BlendIn을 시작한다.

회전 보간 시작·목표 각도는 이동 방향에 해당하는 각도이고, 현재 Transform 자세는 남은 각도와 시작 진행률 계산에 반영된다. 반대 재입력은 `SetTurnType(mTurnType)` 후 Turn에 재진입해 같은 절차를 반복한다.

### 프레임과 회전 완료

| 호출 | 처리 |
|---|---|
| `PlayerTurnState.FixedTick()` | Normal이면 `advanceNormalRotation()` 호출 |
| `advanceNormalRotation()` | `mTurnTime`을 `Time.fixedDeltaTime`만큼 증가시키고 Clip 시간·회전 진행률 제공 |
| `RotationHandler.RotateAtNormalizedTime()` | 진행률을 Clamp하여 `NormalRotation`에 전달하고 완료 여부 기록 |
| `NormalRotation.Rotate()` | 방향에 따른 보간; 완료 시 정확한 목표 회전 적용 후 `EndRotation()` |
| `TurnPlayable.FixedUpdate()` | Blend 시간 증가; 외부 제어되지 않는 Clip timer 진행 |
| `TurnPlayable.Update()` | 재생 상태 처리·Weight 변경 후 Clip `SetTime()`과 `mGraph.Evaluate(Time.deltaTime)` |

ClipPlayable의 Speed는 0이므로 Graph 평가로 Clip 시간이 추가 진행되지 않는다. ControllerPlayable에는 같은 정지 설정을 적용하지 않고 Graph 평가의 deltaTime으로 진행시킨다. `mLastControlledClipPort`와 `mLastControlledFixedTime`은 외부에서 갱신 후 해제한 Clip이 같은 Fixed step에서 다시 증가하는 것을 막는다.

서로 다른 MonoBehaviour의 콜백 실행 순서를 이 문서에서 보장하지 않는다. Fixed 시간 갱신과 Update 평가가 나뉘므로 재생 속도·프레임 변동 검증이 필요하다.

### 정상 종료와 Controller Blend

1. `playing()`에서 선택 Clip timer가 `max(0, length - _blendDuration)`에 도달하면 `NeedsControllerBlend`를 설정한다.
2. 요청 대기 중에는 선택 Clip의 샘플 시점을 해당 경계 이하로 제한한다. timer 자체를 정지시키는 처리는 아니다.
3. Turn 상태는 `PrepareRemainingRotationTransfer()` 후 Move로 전환한다.
4. Move 진입에서 `TryPlayControllerBlend(AnimState.Run, ControllerStartNormalizedTime)`을 호출한다.
5. `PlayerAnimation.PlayForControllerBlend()`가 Controller State를 지정 위상으로 `Play()`한 뒤 `BeginControllerBlend()`가 Port 0으로 BlendOut한다. 준비 실패 시 일반 `Play()`를 시도한다.
6. `beginBlend()`는 모든 Port의 현재 Weight를 저장하고, `blendToTarget()`은 목표 Port만 1이 되도록 보간한다. 재반전 시에도 현재 Weight에서 시작한다.
7. Blend 완료 시 `IsTurnPlaybackFinished`를 설정한다. 남은 Normal 회전은 Move의 `ContinueRemainingRotation()`에서 계속 진행하며, 회전 종료 시 Clip 외부 시간 제어를 해제한다.

`PlayerMoveState.sidePass()`는 X축 이동 중 장애물을 Z축으로 피하는 기능이다. Turn 회전 처리와 별개이며 잔여 회전 정리 때문에 제거하거나 회전 로직으로 대체하지 않는다.

### 입력 해제와 RunToIdle

- Normal IdleTurn 중 `mTurnTime < startRunTime`이고 해당 방향의 정지 Clip이 유효하면 `TryStartIdleTurnToIdle()`을 사용한다.
- 정지 Clip은 현재 회전 진행률에 해당하는 위치부터 시작한다. 회전 중에는 원래 회전 시간과 정지 Clip의 회전 시간을 비례 대응시키고, 회전 완료 뒤에는 남은 Clip 시간을 1배속으로 진행한다.
- 같은 목표 방향으로 재입력하면 원래 Turn Clip의 `mTurnTime` 위치로 돌아간다. 반대 입력은 앞선 재진입 분기가 처리한다.
- 입력이 계속 없으면 끝 구간에서 Controller Idle을 준비한다. Blend 완료와 회전 완료를 모두 만족하면 Idle로 전환하며, 준비된 Idle은 `EnterWithoutAnimation()`으로 재시작을 피한다.
- Clip이 없거나 적용 구간이 아니면 잔여 회전을 인계하고 RunToIdle로 전환한다. 따라서 L Clip 미할당 시 RunToIdle 대체가 유지된다.

`PlayerRunToIdleState`는 진입 직후 이전 State를 잘못 읽지 않도록 Graph 평가 횟수 변경을 기다린다. ControllerPlayable 미사용 시에는 진입 프레임과 다른 프레임을 기다린다. 이후 `RunToIdle` Tag의 normalizedTime이 1 이상이거나 Transition 중이 아닌 Idle이면 애니메이션 완료로 본다. 완료 조건은 다음과 같다.

```csharp
bool stopCompleted = mbAnimationFinished && !mTurnState.HasRemainingRotation;
```

반대 입력 시 `stopCompleted`이면 IdleTurn, 아니면 RunTurn을 선택한다. 추가 입력이 없으면 완료 후 Idle로 전환한다.

### Jump/Fall 중단

RunJump와 Fall은 진입 시 `InterruptTurn()`으로 잔여 회전과 Clip 외부 시간 제어를 중단한다. 이후 다음 Controller 애니메이션을 준비하고 BlendOut한다. 별도 중단 전용 Blend 시간이 아니라 현재 `_blendDuration`을 사용한다.

Turn에서 Jump로 넘긴 목표 이동 방향이 있으면 `PlayerRunJumpState`는 점프 직전 실제 Y각과 목표 Y각의 `Mathf.DeltaAngle`을 저장한다. 해당 부호로 회전 방향을 정해 이전 Turn 방향을 그대로 강제하는 문제를 피한다.

## 5. Controller 제어 경로와 설계 의도

**코드에서 확인한 사실**

- Graph 생성 시 `BindControllerPlayable()`이 Animator의 Float/Int/Bool Parameter와 유효한 현재 State를 복사한다. Trigger 복사는 없다.
- 바인딩 후 `PlayerAnimation`의 재생·Parameter 설정·State/Transition 조회는 유효한 ControllerPlayable을 우선 사용한다. 바인딩은 Turn 종료 시 해제되지 않는다.
- 매 Graph 평가 후 `NotifyControllerEvaluated()`가 평가 횟수를 증가시킨다. Graph 파괴 시 Controller 바인딩도 해제한다.
- 회전 완료는 각도 기준 진행률, Blend 완료는 Mixer 시간 기준이다. 둘을 같은 종료 플래그로 취급하지 않는다.

**구조와 대화에서 해석한 설계 의도**

- 공통 시간은 재진입 위치와 실제 회전을 맞추기 위한 선택이다.
- Controller State를 먼저 준비한 뒤 Mixer를 Blend하는 구조는 Turn 끝 자세와 다음 애니메이션 위상을 연결하려는 의도다.
- State 조회와 재생의 대상을 통일하는 구조는 Animator 쪽의 오래된 Idle 정보를 읽어 RunToIdle 종료를 놓치는 문제를 피하려는 의도다.

Animator 탭에 표시되는 State만으로 실제 실행 상태를 판정하지 않는다. 진단 시 `PlayerAnimation.GetCurrentStateInfo()`, `GetTransitionInfo()`, `StateEvaluationCount`, Mixer Weight를 함께 확인한다. ControllerPlayable의 `GetTime()` 증가만으로 내부 State/Transition 진행을 입증할 수 없다.

## 6. 주의사항과 남은 정리

- `PlayerTurnState`와 `TurnPlayable`은 같은 `TurnAnimationData`를 참조해야 한다. 코드에서 참조 일치를 강제하지 않는다.
- 필수 Turn L/R Clip은 Graph 생성 시 null 검사 없이 등록한다. 정지 Clip만 선택적으로 등록한다.
- `GetRotationDuration()`은 Clip null을 방어하지 않는다. `Get()`에 `ETurnType.None`이 들어가면 기본 struct가 반환되므로 유효한 종류·데이터 설정이 필요하다.
- `_turnPlayable`의 `[FormerlySerializedAs("turnPlayable")]`는 기존 Inspector 참조를 위한 것이다. 에셋 마이그레이션 확인 없이 제거하지 않는다.
- Normal 이외 전략의 동작 동등성, 재생 속도 변경, 여러 Fixed step이 발생하는 프레임의 자세 연속성은 이번 문서 작업에서 검증하지 않았다.
- `TurnAnimationData` 최상위의 이전 데이터 필드, `InterruptToController()`, `GetTurnClipLength()` 등 정리 후보가 남아 있다. 제거 완료로 간주하지 않는다. 특히 `GetTurnClipLength()`는 Port 0 이하에 Controller의 현재 시간을 반환한다.
- `mFromPort`는 현재 로그용이다. 매 프레임 Weight 로그에는 Port 5·6이 빠져 있어 모든 입력을 보여주지 않는다.
- 이 문서는 Turn 연동 범위만 설명한다. 다른 이동·IK·상호작용 기능의 설계를 변경한 것으로 해석하지 않는다.
