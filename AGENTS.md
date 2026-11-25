# AGENTS.md

Codex(구현 담당 에이전트)를 위한 규칙. 작업 전에 이 문서와 [WORKFLOW.md](WORKFLOW.md)를 읽는다.

## 역할

- **Claude:** 설계, 작업 지시서 작성(`docs/tasks/`), 코드 리뷰
- **Codex (너):** 작업 지시서대로 구현, 테스트 작성, 커밋, push
- **사용자:** Unity 에디터 작업, 실행 확인, PR 생성과 머지

작업은 항상 `docs/tasks/<이슈번호>-<이름>.md` 지시서로 시작한다. 지시서에 없는 기능을 추가하거나 범위를 넓히지 않는다. 지시서가 모호하거나 코드와 맞지 않으면 추측해서 진행하지 말고 멈춰서 질문 목록을 보고한다.

## 프로젝트

- 3D 합성(Merge) + 웨이브 디펜스 모바일 게임. 기획은 [README.md](README.md).
- Unity 6.3 LTS (6000.3.29f1), URP, C#, Input System, uGUI, Unity Test Framework (NUnit).
- 대화와 보고는 한국어로 한다. 코드 식별자는 영어, 주석은 한국어.

## 코드 구조 (반드시 지킬 것)

```text
Assets/_Project/
├── Scripts/Core/   TopWar.Core   — 게임 규칙. 순수 C#. UnityEngine 참조 금지 (noEngineReferences: true)
├── Scripts/Game/   TopWar.Game   — MonoBehaviour: 표시, 입력, UI, 연출. Core를 호출만 한다
└── Tests/EditMode/ TopWar.Core.Tests — Core의 NUnit 테스트
```

- 규칙·계산·상태 변경 로직은 **Core**에 넣는다. Game에 규칙을 넣지 않는다.
- Core는 `UnityEngine`, `Mathf`, `Debug`, `Vector3` 등을 쓰지 않는다. 필요한 수학은 `System.Math`로.
- Core 클래스의 public 동작을 바꾸거나 추가하면 **EditMode 테스트를 함께 작성·수정**한다.
- 새 폴더에 어셈블리가 필요하면 `.asmdef`를 만들고 기존 참조 방향(Game → Core, Tests → Core)을 지킨다. Core가 Game을 참조하면 안 된다.

## 코딩 컨벤션

- 네임스페이스는 폴더 경로를 따른다: `TopWar.Core.Grid`, `TopWar.Game.View` 등.
- 파일 하나에 public 타입 하나. 파일 이름 = 타입 이름.
- 상속이 필요 없는 클래스는 `sealed`.
- private 필드는 `_camelCase`. Unity 인스펙터 노출은 `[SerializeField] private` + 읽기 전용 프로퍼티. public 필드 금지.
- 상태 변경 알림은 `event Action` 사용 (예: `GridModel.Changed`). 구독하면 `OnDestroy`에서 해제한다.
- `Update`/`LateUpdate`에서 LINQ, 문자열 조합, `new` 할당, `GetComponent`, `Camera.main` 반복 호출을 피한다. 필요한 값은 캐시한다.
- UnityEngine.Object에 `??`, `?.`를 쓰지 않는다. `!= null`로 검사한다.
- 기존 코드 스타일(들여쓰기 4칸, 중괄호 줄바꿈)을 따른다.
- 밸런스 수치는 하드코딩하지 말고 `BalanceConfig`(ScriptableObject)나 Core 생성자 인자로 받는다.

## 하면 안 되는 것

- `.unity`(씬), `.prefab`, `.asset`, `ProjectSettings/` 파일을 직접 편집하지 않는다. 에디터 작업이 필요하면 보고서에 "사용자가 할 에디터 작업"으로 단계별로 적는다.
- `.meta` 파일을 새로 만들지 않는다 (Unity가 생성). 단, 파일을 **이동·이름 변경·삭제**할 때는 같은 이름의 `.meta`도 함께 옮기거나 지운다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` 등 `.gitignore` 대상은 건드리지 않는다.
- 외부 에셋, 패키지 추가(`Packages/manifest.json` 수정)는 지시서에 있을 때만 한다.
- 지시서 범위 밖의 리팩터링, 파일 이름 변경, 포맷 일괄 변경을 하지 않는다.
- README.md, WORKFLOW.md, AGENTS.md, CLAUDE.md는 지시서에 있을 때만 수정한다.

## 검증

- 이 환경에서는 Unity를 실행할 수 없다. 테스트 실행은 사용자와 CI(GameCI)가 한다.
- 대신 제출 전에 스스로 확인한다: 컴파일 오류가 날 만한 곳(누락된 using, asmdef 참조, 오타, 접근 제한자), Core에 UnityEngine 참조가 없는지, 새 public 동작에 테스트가 있는지.
- 버그 수정은 최대 3회 시도한다. 그래도 안 되면 멈추고 원인 추정과 시도한 내용을 보고한다.
- git push 실패, 권한 거부 같은 코드 밖의 문제는 재시도하지 않고 바로 보고한다.

## Git

- 지시서에 적힌 브랜치에서 작업한다. 브랜치가 없으면 최신 `develop`에서 지시서의 이름으로 만든다.
- `main`, `develop`에 직접 커밋·push하지 않는다.
- 커밋은 의미 단위로 나눈다. 메시지는 `feat: ...`, `fix: ...`, `test: ...`, `refactor: ...`, `chore: ...` 형식, 끝에 `(#이슈번호)`.
- 작업이 끝나면 작업 브랜치로 push한다. PR은 만들지 않는다 (사용자가 만든다).

## 완료 보고 형식

작업이 끝나면 아래 형식으로 보고한다.

```markdown
## 완료: <지시서 파일명>
- 브랜치 / 커밋: <브랜치>, <커밋 해시 목록>
- 변경 파일: <파일 목록과 한 줄 설명>
- 테스트: <추가·수정한 테스트>
- 사용자가 할 에디터 작업: <없음 또는 단계별 목록>
- 지시서와 다르게 한 것 / 확인 필요: <없음 또는 목록과 이유>
```
