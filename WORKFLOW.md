# WORKFLOW (v2)

개발 환경, 저장소 규칙, 작업 흐름을 정리한 문서. 게임 기획은 [README.md](README.md), Claude 전용 규칙은 [CLAUDE.md](CLAUDE.md) 참고.

> **v2:** 게임을 연속 좌표 러너 슈터로 재설계하면서 작업 방식도 바꿨다. 핵심은 **"플레이할 수 있는 상태를 작게, 자주"**. 지시서 → 외부 구현 → 리뷰를 거치던 v1 방식은 조작감·화면이 중요한 이 게임에 너무 느렸다.

---

## 1. 개발 환경

* **Unity Hub** + **Unity 6.3 LTS (6000.3.29f1)**, 템플릿 Universal 3D (URP)
  * Android 빌드: Android Build Support, OpenJDK, Android SDK & NDK Tools 모듈
  * iOS 빌드는 Mac + iOS Build Support 필요
* **코드 에디터:** VS Code + Unity 확장 (`VisualStudioToolsForUnity.vstuc`)
* **Git LFS:** 모델·텍스처·오디오 (Git for Windows에 포함)
* **모바일 화면 테스트:** Window → General → Device Simulator
* **키보드 테스트:** 에디터 Play 중 `←→` / `A D` 로 좌우 이동, 마우스 드래그로 터치 대신

---

## 2. 저장소 구조

```text
TopWar/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/            ← 게임 규칙 (순수 C#, UnityEngine 미사용) — TopWar.Core.asmdef
│   │   │   │   └── Runner/      ← v2 러너 규칙
│   │   │   └── Game/            ← 화면·입력·연출 (MonoBehaviour) — TopWar.Game.asmdef
│   │   ├── Art/                 ← 직접 만든 머티리얼·프리팹
│   │   └── Tests/EditMode/      ← 규칙 단위 테스트
│   └── ThirdParty/              ← 외부 무료 에셋 (팩마다 폴더 + 라이선스 파일)
├── docs/
│   ├── reference/               ← 1단계: 사용자가 준 참고 장면 정리
│   ├── PLAN.md                  ← 2단계: 전체 계획서 (마일스톤, 작업 목록, 완료 기준)
│   ├── design/                  ← 3단계: 시스템별 상세 설계서
│   └── tasks/                   ← Codex에 맡길 때만 쓰는 작업 지시서
├── README.md                    ← 게임 기획서
├── WORKFLOW.md                  ← 이 문서
├── CLAUDE.md / AGENTS.md        ← Claude / Codex 규칙
└── chat_history.md              ← 작업 기록
```

### 코드 구조 원칙

* 이동·사격·충돌·보상 같은 **게임 규칙은 `Core`에 순수 C#**. `Tick(deltaTime)`으로 진행하는 시뮬레이션.
* `Game`은 `Core`를 호출해 그리고, 입력을 넘기고, 연출만 한다.
* v1 코드는 모두 지웠다 (2026-11-26). 필요하면 `develop` 브랜치 기록에서 찾는다.

---

## 3. Git에 올리는 것 / 올리지 않는 것

| 구분 | 폴더·파일 | 이유 |
| --- | --- | --- |
| **커밋** | `Assets/`, `Packages/`, `ProjectSettings/` | 프로젝트 원본 |
| **커밋** | 모든 `.meta` 파일 | GUID·임포트 설정. 빠지면 참조가 깨짐 |
| 제외 | `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/` | 캐시·임시·개인 설정 |
| 제외 | `Build/`, `Builds/`, `*.apk`, `*.aab` | 빌드 결과물 |
| 제외 | `.vs/`, `.vscode/`, `*.sln`, `*.csproj` | IDE 자동 생성 |
| 제외 | `*.keystore`, `*.jks`, `.env` | 서명 키, 비밀 정보 |

**Git LFS 대상:** `.fbx`, `.obj`, `.blend`, `.png`, `.jpg`, `.tga`, `.psd`, `.exr`, `.hdr`, `.wav`, `.mp3`, `.ogg`, `.ttf`, `.otf`, `.mp4` ([.gitattributes](.gitattributes))

**외부 에셋:** 라이선스가 CC0 또는 상업적 사용 가능인 것만 쓴다. `Assets/ThirdParty/<팩 이름>/`에 넣고 라이선스 파일을 함께 커밋한다.

---

## 4. 작업 흐름

### 브랜치

* `main`: 릴리스용. 직접 커밋하지 않는다.
* `develop`: v1 프로토타입(그리드 디펜스)까지 머지된 상태.
* **`v2`: 재설계 개발 브랜치.** Claude가 여기에 직접 작게 커밋·push한다. 사용자는 이 브랜치를 받아 바로 Play로 확인한다.
* 마일스톤(README 6장의 R1~R5)이 끝나면 `v2` → `develop` PR을 만든다. v2가 안정되면 `develop`을 v2 기준으로 이어간다.
* 실험이 크거나 되돌릴 가능성이 높으면 `v2/<설명>` 브랜치를 따로 만들고 확인 후 `v2`에 머지한다.

### 문서 단계 (구현 전에 반드시)

구현은 아래 세 문서가 있고 **사용자가 확인한 뒤**에 시작한다. 문서 없이 코드부터 쓰지 않는다.

| 단계 | 문서 | 내용 | 사용자 확인 |
| --- | --- | --- | --- |
| 1. 참고 자료 | `docs/reference/<주제>.md` | 사용자가 준 스크린샷·영상 분석을 장면 요소 목록으로 정리. 각 요소를 "넣음 / 우리식으로 바꿈 / 안 넣음(이유)"으로 분류 | 빠진 요소가 없는지 |
| 2. 계획서 | `docs/PLAN.md` | 마일스톤(R1~R5)별 목표, 작업 목록(체크박스), 순서와 의존 관계, 완료 기준(Play로 확인할 장면), 위험 요소 | 범위·순서 |
| 3. 상세 설계 | `docs/design/<시스템>.md` | 시스템별 데이터 구조, 클래스·메서드, 상태와 규칙, 수치 초기값, 화면 표현, 테스트 항목, 참고 자료 요소와의 대응 | 규칙·수치 |

* 계획서와 설계서는 살아 있는 문서다. 구현하면서 바뀐 결정은 바로 문서에 반영하고, 계획서 체크박스를 갱신한다.
* 새 참고 자료가 오면 1단계부터 다시 반영한다 (reference → PLAN → design 순서로 영향 확인).

### 구현 한 바퀴 (짧게, 자주)

```text
[설계서의 작업 하나] → [Claude: 구현 → push] → [사용자: git pull, 에디터 Play, 스크린샷·느낌 전달]
   → [Claude: 수정, 설계서·계획서 갱신] → 다음 작업
```

1. **설계서 기준으로 구현:** 구현 전후로 참고 자료 요소 목록과 설계서를 대조한다. "무엇을 보고 만들었는지"가 남아야 방향이 다시 어긋나지 않는다.
2. **한 번에 하나의 눈에 보이는 변화:** 커밋 하나가 Play로 확인할 수 있는 변화 하나가 되게 한다 (예: "병사 무리가 지휘관을 따라옴", "캡슐을 쏘면 병사 합류").
3. **보이는 품질도 처음부터:** 기본 도형이라도 색·조명·그림자·비율을 맞춰 게임처럼 보이게 한다. 에셋 교체를 마지막까지 미루지 않는다.
4. **확인은 사용자 에디터에서:** Claude는 Unity를 실행할 수 없다. 사용자가 Play 결과(스크린샷, 콘솔 에러, 느낌)를 알려주면 바로 고친다.
5. **테스트:** `Core` 규칙에는 EditMode 테스트를 붙인다. 단, 화면에서 동작을 먼저 확인하고, 규칙이 정해진 뒤에 테스트를 보강해도 된다.

### 이슈와 커밋

* 이슈는 마일스톤 단위(R1~R5)로 만든다. 제목 `feat: R1 ...`.
* 커밋 메시지: `feat: ...`, `fix: ...`, `test: ...`, `refactor: ...`, `chore: ...`, `docs: ...` (끝에 `(#이슈번호)`)
* PR 본문에는 `Closes #이슈번호`.

### Codex (선택)

* 화면과 상관없는 규칙 로직이나 테스트 보강처럼 **범위가 분명한 일**만 `docs/tasks/`에 지시서를 써서 맡긴다 ([TEMPLATE.md](docs/tasks/TEMPLATE.md), 규칙은 [AGENTS.md](AGENTS.md)).
* Codex 작업은 `v2`에서 하고, Claude가 리뷰한다.

### 역할 분담

| 작업 | 담당 |
| --- | --- |
| 참고 자료 정리, 계획서, 상세 설계, 구현, 커밋·push | Claude |
| 참고 자료 제공, 계획서·설계서 확인 | 사용자 |
| 규칙 로직·테스트 보강 (맡길 때만) | Codex |
| Play 확인, 스크린샷·피드백, 에셋 다운로드·임포트 | 사용자 |
| Unity 에디터 작업 (씬, 설정) | 사용자 (Claude가 단계별 안내) |
| 이슈 생성, PR 생성, 머지 | 사용자 |

---

## 5. CI

* `.github/workflows/ci.yml`: `develop`·`main` 대상 PR과 push에서 GameCI로 EditMode·PlayMode 테스트를 실행한다.
* Unity 라이선스 시크릿이 없으면 테스트를 건너뛴다 (실패하지 않음). 그래서 지금은 **에디터 Test Runner 결과가 기준**이다.
* 시크릿 등록 (Settings → Secrets and variables → Actions): `UNITY_LICENSE` (`C:\ProgramData\Unity\Unity_lic.ulf` 내용), `UNITY_EMAIL`, `UNITY_PASSWORD`
* `.github/dependabot.yml`: GitHub Actions 업데이트 PR을 매월 `develop`으로 연다.
