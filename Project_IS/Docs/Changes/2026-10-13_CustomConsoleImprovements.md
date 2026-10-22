# Custom Console 개선 작업 기록

- 작업일: 2026-10-13
- 시스템 설명: [Custom Console 시스템](../Systems/CustomConsole.md).
- 기준: 이번 대화의 수정 기록과 현재 소스. 문서 작성 시 Git diff에는 Custom Console C# 변경이 남아 있지 않았다. 따라서 diff만으로 작업 전후를 입증하지 않으며, 대화 기록과 소스가 일치하는 항목만 기록한다.
- AGENTS.md, Visual Scripting 및 게임 플레이 파일의 다른 작업은 이번 변경에 포함하지 않는다.

## 1. 목적

로그 호출 함수와 소스 위치를 확인하기 쉽게 만들고, 로그 증가 중 가독성을 높인다. 향후 트리 보기를 위해 내부 그룹 구조를 보존하면서 평면 표시를 제공한다. 새 로그의 화면 반영을 매 Editor 업데이트에서 0.2초 간격의 배치 처리로 변경한다.

## 2. 추가·수정·삭제된 기능

| 항목 | 작업 전 | 이번 변경 |
|---|---|---|
| 호출 함수 | 세 번째 스택 줄을 공백으로 분리해 첫 두 요소 연결 | 세 번째 줄의 함수명·인자 목록 보존, 위치 문자열 제거, 짧거나 빈 스택 방어 |
| 소스 열기 | Call Function 일반 텍스트 | 같은 스택 줄의 경로·줄 저장, 셀 왼쪽 클릭으로 선택 및 파일 열기 |
| 표시 행 | Frame/ExecutionOrder 그룹과 로그 | 전체 트리는 유지하고 mRows에는 수집 순서의 로그만 추가 |
| 그룹 정보 | 별도 그룹 행 | 직전 표시 로그와 비교하여 Frame/ExecutionOrder 변경 시 출력 |
| 렌더링 | 그룹별 기본 RowGUI와 로그 셀 | 평면 직접 그리기 경로 추가, Foldout 숨김, 기존 아이콘·색상·구분선 재사용 |
| 선택·스크롤 | 기존 로그 선택 및 부모 탐색 | 이전 선택 참조 해제, 평면은 표시 로그 직접 이동, 트리는 부모 탐색 유지 |
| Clear | 트리 및 기존 목록 초기화 | mLogItems도 초기화, 선택 이벤트 전달, 스크롤/갱신 시각 초기화 |
| 갱신 | 대기 로그를 매 Editor 업데이트 반영 | Editor 시간으로 0.2초 간격 일괄 반영 |

클래스와 파일은 삭제하지 않았다. 기존 그룹 행은 평면 표시에서 제외되지만 트리에 남아 있다. 높이 고정, 표시 일시정지, 자동 따라가기 토글, 보기 선택 UI는 구현하지 않았다.

**기존부터 있던 기능:** Unity 로그 이벤트 수집, Frame/Order/ID 기록, 대괄호 메시지 제거, 문자열 기반 executionOrder 분류, 종류별 카운트와 필터, 대기 목록, 하단 근처 자동 스크롤, 상세 StackTrace. 시스템 전체를 이번 작업에서 새로 만든 것으로 해석하지 않는다.

## 3. 주요 변경 파일

모든 경로는 프로젝트 루트 기준이다.

| 파일 | 주요 코드와 변경 |
|---|---|
| Assets/8.Tools/CustomConsole/LogCollector.cs | getCallMethod에 out 경로·줄 번호 추가, HandleLog에서 함께 저장 |
| Assets/8.Tools/CustomConsole/LogEntry.cs | CallFilePath, CallLineNumber 추가 |
| Assets/8.Tools/CustomConsole/LogTreeViewItem.cs | ShowFrame, ShowExecutionOrder 추가 |
| Assets/8.Tools/CustomConsole/ConsoleTreeView.cs | mLogItems/mUseTreeView, addFlatRow/rebuildFlatRows/rebuildTreeRows 분리, RowGUI/drawLogCell, openCallFunction, 선택·Clear·스크롤 보완 |
| Assets/8.Tools/CustomConsole/Editor/CustomConsoleWindow.cs | LOG_REFRESH_INTERVAL=0.2, mNextLogRefreshTime, OnEnable/onEditorUpdate/Clear 처리 |

FrameTreeViewItem.cs, ExecutionOrderTreeViewItem.cs, CustomConsoleStyles.cs는 이번 작업에서 변경하지 않았다. 씬, 프리팹, 패키지와 .meta도 변경하지 않았다.

## 4. 기존 동작에 미치는 영향

- 현재 mUseTreeView=false이며 그룹 행 없이 로그만 표시한다. 실행 단계로 묶인 순서 대신 수집 순서를 유지한다.
- 첫 표시 로그 및 프레임 변경 시 두 값, 같은 프레임의 단계 변경 시 Execution Order만 표시한다. 필터 변경 후에도 표시 로그끼리 다시 비교한다.
- Call Function은 함수 선언이 아니라 세 번째 스택 항목의 실행 위치로 이동을 요청한다. 위치가 없으면 텍스트만 표시한다.
- 새로운 로그와 카운트는 배치 시점에 반영된다. 수집은 즉시이며 선택과 필터는 즉시 처리한다.
- Clear는 즉시 실행되고 필터 enable은 유지한다. 로그 ID는 재사용하지 않는다.
- 숨겨진 로그로 ScrollToLog를 호출하면 평면 보기에서는 이동하지 않는다.

**확인된 의도:** 트리 구조 보존, 평면 로그의 반복 정보 축소, 래퍼 다음 실제 호출자 표시, 표시 반영 빈도 완화.

**설계 해석:** 별도 수집 순서 목록과 항목 참조 공유는 그룹 트리를 유지하면서 순서를 보존한다. 파싱은 수집 시 수행하여 렌더링 중 반복 분석을 피한다. 실제 성능 이득은 측정하지 않았다.

## 5. 수행한 검증

- 작업 당시 및 문서 갱신 시 관련 C# 소스를 읽고 필드와 호출 경로를 확인했다.
- Git status/diff를 확인하여 다른 작업을 구분했다. 현재 소스 diff가 없다는 사실은 Unity 테스트 통과를 의미하지 않는다.
- 작업 당시 git diff --check에서 앞선 변경은 오류가 보고되지 않았고, 갱신 간격 변경 후 기존 주석의 trailing whitespace 2곳이 보고되었다. 이 공백은 수정하지 않았다.
- 문서 링크·구성과 기존 기록의 날짜 포함 파일명 전환을 확인했다.
- 이번 문서 작업은 문서만 작성·갱신한다. 기존 C# 및 프로젝트 설정을 변경하지 않는다.

## 6. 미검증 사항과 남은 문제

Unity 컴파일, 자동 테스트, 수동 Editor/Play Mode 테스트, 외부 편집기 줄 이동, 실제 갱신 간격과 대량 로그 성능은 실행 검증하지 않았다.

향후 검증 항목:

- 첫 로그, Frame/실행 단계 변경, None 및 필터로 첫 로그를 숨기는 경우.
- 파일 위치 없는 스택, CRLF, 짧은 스택, 프로젝트 절대 경로 및 실제 클릭.
- 선택 해제, 필터로 선택 로그 숨김, Clear 직후 로그, 자동 스크롤 및 키보드 탐색.
- Play Mode 정지 중 갱신과 대량 유입 시 배치 비용.

기존 미해결 문제는 Update 부분 문자열 분류, OnLogAdded(null) Clear 전달, 대괄호 데이터 제거 및 무제한 로그 보관이다. 현재 구현에는 세 번째 줄 고정, 외부 파일 fallback 부재, 미구현 보기 전환, Reload 후 트리/목록 복원 부족, 숨겨진 마지막 로그에 대한 자동 스크롤 제한이 있다. 세부 제약과 수정 주의사항은 시스템 문서에 유지한다.

이번 변경은 표시 구조와 데이터 흐름을 바꾼 작업이므로 별도 작업 기록을 유지한다. 이전 CustomConsoleImprovements.md는 이 파일로 이름을 변경하고 내용을 갱신하여 중복 기록을 만들지 않았다.