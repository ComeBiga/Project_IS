# Custom Console 시스템

- 기준: 2026-10-13 현재 소스, Unity 2022.3.61f1.
- 작업 이력: [Custom Console 개선 기록](../Changes/2026-10-13_CustomConsoleImprovements.md).
- 아래 동작은 소스에서 확인한 사실이다. 의도에 관한 해석은 별도로 표시한다. Unity 실행 검증은 수행하지 않았다.

## 1. 개요

Unity 로그를 수집하여 EditorWindow에서 Frame, Execution Order, Message, Call Function 열로 표시한다. 현재는 1행 1로그 평면 보기이며, 내부 트리와 트리 렌더링 경로는 보존한다. 종류별 필터, 상세 StackTrace, 소스 위치 열기, 하단 자동 스크롤, Clear를 제공한다. 새 로그의 화면 반영은 0.2초 간격으로 묶는다.

## 2. 구조

다음 경로는 프로젝트 루트 기준이다.

| 클래스 | 소스 경로 | 책임 |
|---|---|---|
| LogCollector | Assets/8.Tools/CustomConsole/LogCollector.cs | Application.logMessageReceived 구독, LogEntry 생성·보관, OnLogAdded 전달 |
| LogEntry / ExecutionOrder | Assets/8.Tools/CustomConsole/LogEntry.cs | 원본 로그 및 콜백 분류 데이터 |
| CustomConsoleWindow | Assets/8.Tools/CustomConsole/Editor/CustomConsoleWindow.cs | 창 생명주기, 대기 로그, 갱신 시간, 헤더·툴바·상세 영역 |
| ConsoleTreeView | Assets/8.Tools/CustomConsole/ConsoleTreeView.cs | 트리와 표시 목록, 필터·렌더링·선택·스크롤·파일 열기 |
| FrameTreeViewItem | Assets/8.Tools/CustomConsole/FrameTreeViewItem.cs | 프레임 그룹과 실행 단계 항목 검색 |
| ExecutionOrderTreeViewItem | Assets/8.Tools/CustomConsole/ExecutionOrderTreeViewItem.cs | 실행 단계 그룹과 자식 로그 |
| LogTreeViewItem | Assets/8.Tools/CustomConsole/LogTreeViewItem.cs | LogEntry 참조, VisibleIndex, ShowFrame, ShowExecutionOrder |
| CustomConsoleStyles | Assets/8.Tools/CustomConsole/CustomConsoleStyles.cs | 배경·선택·구분선 색상과 크기 |

### 저장 구조와 표시 구조

- LogCollector.Logs: 수집 순서의 LogEntry.
- rootItem: Root → Frame → ExecutionOrder → Log. None 로그는 Frame에 직접 연결한다.
- mLogItems: 트리에 붙인 동일한 LogTreeViewItem 참조를 수집 순서로 보관한다.
- mRows: 현재 표시할 항목 참조. 평면 보기에서는 필터를 통과한 로그만 포함한다.

Root ID는 0, 로그 ID는 양수, 그룹 ID는 음수다. depth는 Root -1, Frame 0, ExecutionOrder 1, Log 2이며 None 로그도 depth 2를 유지한다. 행 높이는 20이다.

mUseTreeView는 private bool이고 현재 false다. 전환 UI/API는 없다. 트리 표시 경로를 보존한 상태이며 사용자가 두 보기를 전환할 수 있는 구현은 아니다.

ExecutionOrder는 Script Execution Order 숫자가 아니라 StackTrace의 문자열로 분류한 콜백 종류다. LogEntry.Order는 프레임 내 수집 순번이며 현재 열에 표시하지 않는다.

## 3. 동작 흐름

### 초기화와 수집

1. OnEnable에서 TreeViewState와 ConsoleTreeView를 생성한다.
2. TreeView 생성자는 Reload로 Root를 준비하고 카운트를 초기화한 뒤 기존 Logs를 AddLogEntry로 반영한다.
3. 창은 헤더와 선택 이벤트를 연결한다. 대기 목록을 비우고 다음 갱신 시간을 설정한 뒤 OnLogAdded와 EditorApplication.update를 구독한다. OnDisable에서 구독을 해제한다.
4. HandleLog는 Time.frameCount를 기록하고 프레임이 바뀌면 Order를 0으로 되돌린다.
5. 메시지의 대괄호 부분을 제거하고, 함수·위치·실행 단계를 파싱해 LogEntry를 Logs에 저장한다.
6. OnLogAdded → 창의 onLogAdded → mPendingLogEntries 순으로 전달한다.

getCallMethod는 첫 두 줄을 건너뛰고 세 번째 줄만 사용한다. 빈 스택이나 세 줄 미만이면 함수명은 빈 문자열, 위치는 null/0이다. 해당 줄의 ` (at ` 앞부분을 함수명으로 사용하며, 마지막 colon과 닫는 괄호 사이에서 양수 줄 번호를 추출한다. 경로의 backslash를 slash로 변환한다. 위치가 없는 줄은 함수명만 저장한다.

### 화면 반영

```text
EditorApplication.update → onEditorUpdate
→ 대기 로그가 없거나 갱신 시각 전이면 반환
→ 다음 시각 = 현재 Editor 시각 + LOG_REFRESH_INTERVAL(0.2)
→ 추가 전 IsNearBottom 확인
→ 대기 로그별 AddLogEntry → 대기 목록 비움
→ 하단 근처였다면 마지막 배치 로그로 ScrollToLog → Repaint
```

EditorApplication.timeSinceStartup으로 간격을 계산한다. 고정 주기 타이머가 아니므로 Editor가 바쁘면 지연된다. 유휴 후 갱신 시각이 지났다면 첫 로그는 바로 반영된다. 선택·필터·크기 변경 등 다른 Repaint까지 제한하는 것은 아니다.

AddLogEntry는 트리에 로그를 붙이고 카운트와 mLogItems를 갱신한다. 평면에서는 addFlatRow로 증분 추가하고, 트리에서는 rebuildTreeRows로 전체 표시 목록을 재구성한다. 새 그룹은 펼침 상태로 기록한다.

필터 변경은 SetLogTypeEnable → RefreshRows → rebuildRows로 처리한다. Error/Exception/Assert는 Error 설정과 카운트를 공유한다. 카운트는 필터에 관계없는 반영 로그 개수다.

### 평면 렌더링

addFlatRow는 필터를 통과한 직전 표시 로그와 비교한다.

| 조건 | ShowFrame | ShowExecutionOrder |
|---|---|---|
| 첫 표시 로그 또는 Frame 변경 | true | true |
| 같은 Frame에서 executionOrder 변경 | false | true |
| 두 값 동일 | false | false |

VisibleIndex는 추가 전 mRows.Count이다. rebuildFlatRows도 같은 함수를 사용하므로 필터 변경 시 표시 속성과 배경 순번이 다시 계산된다. None도 표시 조건을 만족하면 None 문자열을 출력한다.

OnGUI → DrawLogList → TreeView.OnGUI → RowGUI → drawCell/drawLogCell로 그린다. 평면 경로는 base.RowGUI 없이 셀을 직접 그려 들여쓰기를 사용하지 않는다. 열 enum은 유지하고 GetColumn으로 실제 열 번호를 구한다. Foldout 열은 SetColumnHeader에서 숨기고 allowToggleVisibility를 false로 설정한다.

Message에는 종류별 아이콘을 표시한다. VisibleIndex 홀짝으로 배경을 교대하며 선택색을 적용한다. ShowFrame 행에는 프레임 구분선, 그 외 ShowExecutionOrder 행에는 실행 단계 구분선을 적용한다. 첫 표시 행도 프레임 구분선 대상이다.

### 파일 열기와 선택

Call Function 셀에 경로와 양수 줄 번호가 있으면 링크 커서를 표시한다. 왼쪽 MouseDown에서 SetSelection(...FireSelectionChanged) → openCallFunction → Event.Use 순으로 처리한다.

프로젝트 내부 절대 경로는 프로젝트 접두사를 제거한 뒤 AssetDatabase.LoadAssetAtPath<MonoScript>로 읽는다. 로드에 성공하면 AssetDatabase.OpenAsset(script, line)을 호출한다. 여는 줄은 함수 선언이 아니라 세 번째 스택 항목의 실행 위치다.

SelectionChanged는 이전 선택 참조를 비우고 로그 또는 null을 LogSelected로 전달한다. 창은 상세 스크롤을 초기화하고 다시 그린다. DrawDetail은 TryGetSelectedLogEntry로 TreeView에서 현재 로그를 가져온다.

평면 ScrollToLog는 mRows에서 ID를 찾아 FrameItem을 호출하며 숨겨진 로그는 무시한다. 트리 경로는 로그가 표시되지 않으면 부모를 재귀 탐색한다.

### Clear

툴바 Clear는 Collector → 대기 목록 → TreeView → 창 선택을 비우고 갱신 시각을 재설정한다. TreeView는 그룹 상태, 두 목록, 펼침/선택 상태와 스크롤 및 카운트를 초기화하고 다시 그린다. 필터 enable은 유지한다. Collector는 Frame/Order를 초기화하지만 다음 로그 ID는 초기화하지 않는다.

## 4. 설계 의도와 제약조건

**대화에서 확인된 의도:** 향후 트리 보기 지원을 위해 전체 트리를 보존하며 현재는 수집 순서의 평면 목록을 제공한다. 첫 두 스택 항목이 Debug.Log/GameDebug.Log라는 사용자의 로그 구조를 기준으로 세 번째 항목을 선택한다. 수집과 표시 반영 주기를 분리한다.

**코드 구조에 대한 해석:** mLogItems는 실행 단계별 트리 순회와 무관하게 원래 수집 순서를 보존한다. 동일 항목 참조를 공유하여 로그 객체 복제를 피한다. 필터 적용 후 비교는 숨겨진 로그 때문에 첫 표시 행의 값이 빠지는 것을 막는다. 수집 시 위치를 파싱하여 셀을 그릴 때마다 StackTrace를 분석하지 않는다. 성능 개선 정도는 측정하지 않았다.

유지해야 할 조건:

- 평면 mRows에는 LogTreeViewItem만 추가한다. 마지막 행을 해당 타입으로 캐스팅한다.
- 평면 표시를 부모 펼침 상태에 종속시키지 않는다.
- Column enum과 헤더 배열 순서를 맞추고 visible index와 실제 열 번호를 구분한다.
- Clear는 저장·대기·표시 목록을 함께 처리한다.
- Unity 2022.3 호환 API와 기존 IMGUI 구조를 유지한다. 렌더링 경로에서 불필요한 할당을 추가하지 않는다.
- C# 파일의 기존 인코딩을 확인하고 보존한다.

## 5. 의존 관계

- UnityEngine: Application 로그 이벤트, LogType, Time으로 수집 데이터를 만든다.
- UnityEditor / IMGUI.Controls: EditorWindow, TreeViewState, TreeView, MultiColumnHeader, EditorApplication 및 AssetDatabase를 사용한다.
- Assets/2.Scripts/Systems/Debug/GameDebug.cs: GameDebug.Log가 설정 필터를 통과한 메시지에 접두사를 붙여 Debug.Log를 호출한다. Collector는 GameDebug를 직접 참조하지 않고 Unity 이벤트로 수집한다. GameDebug에서 차단된 로그는 이 경로로 전달되지 않는다.
- 외부 코드 편집기: AssetDatabase.OpenAsset을 통해 열기를 요청한다. 편집기별 실제 줄 이동은 미검증이다.
- URP 및 게임 플레이 클래스에 대한 직접 의존은 이 도구 코드에 없다.

## 6. 주의사항

### 기존 문제

- getExecutionOrder는 Update ()를 먼저 Contains 검사하므로 FixedUpdate/LateUpdate를 Update로 분류할 수 있다. LateUpdate/LateFixedUpdate 별도 분기가 없다.
- Collector.Clear는 OnLogAdded(null)을 보낸다. 현재 버튼은 즉시 대기 목록을 비우지만 외부 Clear나 다른 열린 창에는 null이 남아 AddLogEntry에서 예외가 날 수 있다.
- removeTags는 모든 대괄호 내용을 제거한다. 일반 메시지 데이터도 제거될 수 있다.
- 저장 개수 제한이 없으며 이전 직접 그리기 코드와 미사용 상태가 남아 있다. DrawLogList의 return 아래 이전 구현은 실행되지 않는다.
- 일부 UnityEditor 참조 파일이 Editor 폴더 밖에 있다. Player 빌드 호환성은 미검증이다.

### 현재 구현의 제한과 수정 주의

- 모든 로그에서 세 번째 줄을 고정 선택한다. 래퍼를 거치지 않은 로그/예외는 원하는 호출자와 다를 수 있다.
- 위치 파싱은 Unity의 (at 경로:줄) 형식에 의존한다. MonoScript 로드 실패는 별도 표시 없이 끝나며 프로젝트 외부 파일 fallback은 없다.
- 중간으로 스크롤하면 화면 첫 행의 Frame/ExecutionOrder가 공란일 수 있다.
- 필터로 숨겨진 선택 로그의 상세가 남을 수 있다. 마지막 배치 로그가 숨겨졌다면 자동 스크롤도 이동하지 않는다.
- 평면 항목의 parent/depth가 유지되므로 키보드 탐색과 FrameItem의 영향은 Unity에서 확인해야 한다.
- Reload는 Root만 다시 만들고 mLogItems/현재 프레임을 복원하지 않는다. 생성 이후 표시 재구성에는 RefreshRows를 사용한다.
- 보기 전환 구현 시 헤더 표시/토글 허용, 행 재구성, 선택·스크롤을 함께 처리해야 한다. 트리 경로는 추가마다 전체 재구성하므로 성능 검토가 필요하다.
- 배치 처리량 제한이 없다. 0.2초 간격은 대량 로그에서 한 번의 처리 비용을 줄여 주지 않는다.
- mAutoScroll은 실제 조건에 연결되지 않는다. 현재 조건은 추가 전 IsNearBottom이며 토글 UI는 없다.

Unity 컴파일·실행·성능 검증은 수행하지 않았다. 권장 검증 항목은 작업 기록에 정리했다.