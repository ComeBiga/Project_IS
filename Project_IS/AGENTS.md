
# Project
- Engine: Unity 2022.3.61f1
- Language: C#
- Render Pipeline: URP
- Genre: 2.5D Puzzle Platformer

# Naming Conventions
- Public methods and classes use PascalCase.
- Ordinary private fields use mName format.
- [SerializeField] private fields use _name format.
- Private methods start with a lowercase letter.
- Keep the existing LogCollector and LogEntry class names.

# Coding Rules
- Follow the existing architecture and coding style.
- Use APIs compatible with Unity 2022.3.
- Do not introduce Unity 6-only APIs.
- Avoid unnecessary allocations in frequently called methods.
- Prefer minimal changes over large refactoring.

# Unity Asset Safety
- Do not manually modify .meta files.
- Do not change scenes or prefabs without approval.
- Do not modify Library, Temp, or obj folders.
- Do not install or upgrade packages without approval.

# Workflow
- Explain significant changes before implementing them.
- Summarize changed files and validation steps.
- Clearly state any tests that were not run.

# Documentation
- 복잡한 기능을 구현하거나 시스템 구조를 변경한 경우,
  작업 완료 후 문서화가 필요한지 제안한다.
- 시스템 문서는 Docs/Systems/에 작성한다.
- 작업 기록은 Docs/Changes/에 작성한다.
- 문서에는 설계 의도, 주요 동작 흐름, 변경사항,
  검증 결과, 주의사항을 포함한다.
- 기존 시스템 문서가 있다면 중복 생성하지 말고 갱신한다.
- 문서 작성 시 실제 코드와 일치하는지 확인한다.
- 사용자가 요청하지 않았다면 문서를 자동으로 생성하지 않는다.

# Documentation Language
- 모든 개발 문서는 한국어로 작성한다.
- 클래스, 함수, 변수 이름은 원문 그대로 유지한다.
- Unity API 및 기술 용어는 영어 원문을 사용한다.
- 문서 파일명은 영어로 작성한다.
- 기존 문서를 수정할 때는 해당 문서의 언어를 유지한다.

# Member Declaration Order
- 클래스 멤버는 프로퍼티, 필드, 생성자, 메서드 순서로 선언한다.
- 필드는 접근 제한자에 따라 public → protected → internal → private 순서로 선언한다.
- 메서드도 public → protected → internal → private 순서로 선언한다.
- 같은 접근 제한자를 가진 멤버는 관련 기능이나 논리적 흐름에 따라 배치한다.
- [SerializeField]가 적용된 private 필드는 public 필드 그룹 가장 아래에 포함한다.
- Unity가 호출하는 메시지 메서드는 private 메서드 그룹 가장 위에 포함한다.
- 기존 코드를 수정할 때도 이 선언 순서를 유지한다.
- 선언 순서 변경만을 위한 불필요한 전체 코드 수정은 피한다.

# Line Breaks & Spacing
- 클래스의 멤버 그룹 사이에는 빈 줄을 하나 둔다.
- 서로 다른 접근 제한자의 필드 그룹 사이에는 빈 줄을 하나 둔다.
- 같은 그룹의 필드는 빈 줄 없이 연속해서 선언한다.
- 프로퍼티와 메서드 사이에는 빈 줄을 하나 둔다.
- 각 메서드 사이에는 빈 줄을 하나 둔다.
- 메서드 내부에서는 논리적으로 구분되는 코드 블록 사이에 빈 줄을 하나 둔다.
- 조건문, 반복문 직전에는 빈 줄을 하나 둔다.
- 불필요한 연속 빈 줄은 사용하지 않는다.
- 클래스, 메서드, 조건문, 반복문의 여는 중괄호는 다음 줄에 배치한다.
- 긴 메서드 호출이나 선언은 가독성을 위해 여러 줄로 나눈다.
- 기존 코드를 수정할 때도 동일한 줄바꿈 규칙을 유지한다.

# Line Wrapping
- 한 줄은 가능한 120자를 넘지 않도록 작성한다.
- 메서드의 매개변수가 길어지면 여러 줄로 나눈다.
- 여러 줄로 나뉜 매개변수는 동일한 들여쓰기 규칙을 적용한다.
- 메서드 체이닝은 가독성이 떨어질 경우 줄을 나눈다.
- 단순한 표현식은 불필요하게 여러 줄로 나누지 않는다.

# Formatting Changes
- 코드 수정 시 기존 파일의 줄바꿈 스타일을 최대한 유지한다.
- 변경 대상과 관계없는 코드의 서식은 수정하지 않는다.
- 명시적으로 요청하지 않았다면 파일 전체를 재정렬하거나 포맷팅하지 않는다.
- 새로운 코드를 작성할 때는 위의 포맷팅 규칙을 적용한다.
