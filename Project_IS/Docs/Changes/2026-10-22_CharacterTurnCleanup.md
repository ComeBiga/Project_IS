# 캐릭터 Turn 시간 제어·Controller Blend 및 정리 기록

- 기록일: 2026-10-22
- 시스템 문서: [캐릭터 Turn 재생과 회전](../Systems/CharacterTurn.md)
- 기준: 현재 소스, 대화의 요청·실행 기록, HEAD `0e1848bd`, 문서 작성 시점의 Git Diff.

## 1. 범위와 목적

대화에서 진행한 Turn Clip·회전 시간 동기화, 일반 Controller 애니메이션과의 연결, 중단·입력 해제 처리를 기록한다. 최근에는 사용하지 않는 `PlayableRotation` 경로를 제거하고 serialized 필드 이름을 규칙에 맞췄다.

문서 작성 시점에 시간 동기화와 Controller Blend의 핵심 구현은 이미 HEAD에 포함되어 있다. 따라서 현재 Diff의 모든 삭제를 새 기능 구현으로 보거나, Diff에 없는 기능을 미구현으로 보지 않는다. 단계별 과거 Diff 전체를 재구성한 기록은 아니며, 대화 내용과 현재 소스가 일치하는 범위에서 설명한다.

Visual Scripting 생성물과 `ProjectSettings/VisualScriptingSettings.asset`, Custom Console 작업은 이번 Turn 변경 범위에서 제외한다. 이번 문서 요청으로는 문서 두 개만 추가하며 C#·Scene·Prefab·프로젝트 설정을 변경하지 않는다.

## 2. 기존 동작과 대화에서 변경한 동작

| 구분 | 기존 기반 / 문제 | 대화에서 진행하고 현재 코드로 확인한 결과 |
|---|---|---|
| Turn 실행 | TurnPlayable 호출과 RotationHandler 전략 주입 구조가 존재 | 기존 구조를 유지하면서 Normal 경로의 공통 시간을 `PlayerTurnState`에서 관리 |
| 재진입 | 남은 각도로 시작 진행률 계산 | Clip 시작 시간을 전체 Clip 길이가 아닌 `GetRotationDuration()`에 대응 |
| 시간 진행 | Clip과 회전의 개별 시간·평가 시점 불일치 우려 | FixedTick에서 공통 시간 제공, Clip Speed 0과 명시적 `SetTime()`, Controller는 Graph deltaTime으로 평가 |
| 회전 종료 | Clip 종료·Blend 종료와 구별 필요 | 정확한 목표 각도 적용과 완료 기록, 잔여 회전 인계 |
| 일반 애니메이션 연결 | Turn 종료 시 다음 자세·위상 준비 필요 | 다음 Controller State를 `PlayForControllerBlend()`로 준비한 뒤 Mixer Port 0으로 Blend |
| 입력 해제 | 비활성 조건문으로 남은 Idle 전환 처리 | IdleTurnToIdle 선택, 미할당·후반 구간의 RunToIdle 대체, 애니메이션과 회전의 완료 대기 |
| Jump/Fall | 중단과 Turn 재진입의 처리가 혼재할 가능성 | `InterruptTurn()`과 다음 애니메이션 Blend; Jump 목표 방향의 DeltaAngle 사용 |
| State 조회 | ControllerPlayable은 RunToIdle인데 Animator 조회는 Idle인 문제 보고 | `PlayerAnimation`이 재생과 조회를 같은 활성 ControllerPlayable로 전달 |
| 정지 후 재반전 | RunToIdle에 남아 RunTurn으로 진입하는 문제 보고 | 평가 이후 완료 판정, 완료와 잔여 회전 여부에 따른 IdleTurn/RunTurn 선택 |
| 데이터 | IdleTurnToIdle 관련 개별 필드와 이전 Duration 메서드 | L/R `TurnAnimation` 필드, `startRunTime`, `GetRotationDuration()` 사용 |

위 표의 문제와 해결 의도는 대화 기록에 근거한다. 현재 코드는 시스템 문서의 호출 흐름과 일치하지만, 모든 경계 상황의 실행 성공을 의미하지 않는다.

## 3. 최근 정리의 출처 구분

### Codex가 직접 적용한 변경

- `Movement/Rotation/PlayableRotation.cs` 삭제.
- `PlayerTurnState`의 관련 필드·생성·선택 분기 제거.
- `RotationHandler`의 관련 필드·`InitPlayableRotation()`·선택 분기 제거. `EType.Playable`을 제거하고 나머지를 0~3으로 명시하여 기존 값 유지.
- `TurnPlayable`의 전용 `StartExit`, Curve 평가, 구형 Blend·Weight API, 관련 필드 및 종속된 normalizedTime 조회 제거.
- `PlayerTurnState.turnPlayable`을 `_turnPlayable`로 변경하고 모든 사용처 수정. `[FormerlySerializedAs("turnPlayable")]`로 기존 직렬화 참조 연결.

### 사용자 정리 또는 출처를 별도로 봐야 하는 Diff

- `PlayerRunToIdleState`의 `mbObservedRunToIdle`, 진단 로그, 단순 base 호출만 하던 `Initialize()` 제거는 Codex의 위 제거 작업 전부터 존재했다.
- `PlayerTurnState`의 이전 Duration·TimeOffset 필드와 `LogControllerPlayable()` 제거도 작업 시작 시 이미 존재했다.
- 문서 작성 전 최신 소스에서 `mRemainAngle` 지역 변수화, `getCurrentDirectionEulerAngles()` 이름 변경, 중복 로그 정리, `ChangeClip(AnimationClipPlayable)` 제거가 추가로 확인된다. 위 Codex 적용분과 구분한다.
- `.meta` 삭제가 현재 Diff에 보인다. Codex의 클래스 제거 당시에는 `.cs`만 삭제하고 `.meta`는 남겼으므로, 이후 삭제 주체는 이 기록에서 단정하지 않는다.

### 제안에 머물러 있는 사항

`TurnAnimationData` 최상위 이전 필드 제거, `InterruptToController()`·`GetTurnClipLength()` 정리, 내부 전용 API의 private 전환, `mFromPort`와 매 프레임 로그 정리는 완료로 기록하지 않는다. 문서 작성 시 현재 소스에 남아 있다.

## 4. 주요 소스와 영향

경로는 `Assets/2.Scripts/Gameplay/Character/` 기준이다.

| 소스 | 현재 담당 변경 |
|---|---|
| `Animation/TurnPlayable.cs` | Clip 수동 시간·Controller Graph 평가, 현재 Weight에서 Blend, Controller 요청과 완료 플래그, 구형 전용 API 제거 |
| `PlayerState/PlayerTurnState.cs` | 공통 시간, 정지 Clip 분기, 잔여 회전 인계·중단, `_turnPlayable` 이름 변경 |
| `Animation/TurnAnimationData.cs` | L/R 정지 Clip과 시간·Controller 위상 데이터 |
| `Movement/Rotation/RotationHandler.cs`, `NormalRotation.cs`, `RotationBase.cs` | 외부 진행률·회전 완료 처리; Handler에서 PlayableRotation 경로 제거 |
| `PlayerAnimation.cs` | Controller 바인딩, Parameter·재생·조회 전달, 평가 횟수 |
| `PlayerState/PlayerMoveState.cs`, `PlayerRunToIdleState.cs` | 다음 State 준비, 잔여 회전 유지와 정지 완료 판정 |
| `PlayerState/PlayerRunJumpState.cs`, `PlayerFallState.cs` | Turn 중단과 Controller Blend; Jump DeltaAngle 방향 결정 |
| `Movement/Rotation/PlayableRotation.cs` | 삭제된 이전 전략 |

정상 Normal Turn 경로는 유지한다. ControllerPlayable 바인딩은 Turn 중에만 사용되는 것이 아니므로, 일반 애니메이션의 재생과 조회에도 영향을 준다. Animator 직접 State 조회로 되돌리면 재생 대상과 판정 대상이 달라질 수 있다. `sidePass()`의 장애물 회피 기능은 별개이며 이 정리 대상으로 취급하지 않는다.

## 5. 검증 결과

| 검증 | 결과와 범위 |
|---|---|
| 이전 사용자 실행 확인 | Controller 제어 경로 수정 후 플레이가 정상 동작한다고 보고. 재생 속도·재반전은 시각적으로 정상으로 보였지만 확정적 검증은 아니라고 보고 |
| PlayableRotation 제거 당시 참조 검색 | `Assets/2.Scripts`에서 삭제한 클래스·전용 API의 잔여 참조 없음 |
| 제거 당시 직렬화 검색 | `_rotationType: 4`인 Scene·Prefab·asset 검색 결과 없음; 런타임 생성·외부 데이터까지 보장하지 않음 |
| 제거 직후 소스 컴파일 | Unity 참조 DLL을 사용한 Roslyn 컴파일: 오류 0, 경고 234. 삭제된 소스를 임시 응답 파일에서 제외하고 OS 임시 폴더에 출력. Unity Editor 컴파일·빌드와 동일한 검증은 아님 |
| `_turnPlayable` 이름 변경 | 이전 이름은 `FormerlySerializedAs` 문자열에만 남는 것을 확인. 해당 단계에서 컴파일 재실행은 하지 않음 |
| 각 변경 당시 `git diff --check` | 통과 |
| 이번 문서 작업 | 현재 소스·Diff·기존 문서 목록 대조. 실행 테스트와 컴파일을 새로 수행하지 않음 |

이전 컴파일 결과는 이후 사용자 정리까지 포함한 최신 소스의 검증 결과로 확대하지 않는다.

## 6. 미검증 사항과 남은 문제

- Unity Play Mode 회귀 테스트, 프로젝트 빌드, Inspector 참조 마이그레이션을 이번 문서 작업에서 수행하지 않았다.
- 좌우 Turn·입력 해제·정지 Clip 미할당 대체·Jump/Fall·재반전을 다양한 프레임과 재생 속도에서 확인해야 한다.
- 두 컴포넌트의 `TurnAnimationData` 참조 일치와 Clip별 수치 설정은 콘텐츠 제약이다. 이번 문서 작성에서 실제 Scene·Prefab 설정을 검증하지 않았다.
- Animator 탭의 표시와 ControllerPlayable 실행 상태가 다를 수 있다. 진단은 `PlayerAnimation` 조회 결과와 Mixer Weight를 기준으로 한다.
- Normal 이외 전략의 동작 동등성은 검증하지 않았다.
- 이전 필드·미사용 API·로그 정리와 입력 데이터 방어는 남은 유지보수 항목이다. 세부 제한사항은 시스템 문서에 기록했다.
