# WORKFLOW

개발 환경, 저장소 규칙, 작업 흐름을 정리한 문서. 게임 기획은 [README.md](README.md), Claude 전용 규칙은 [CLAUDE.md](CLAUDE.md) 참고.

---

## 1. 개발 환경

* **Unity Hub** + **Unity 6.3 LTS (6000.3.29f1)**
  * 프로젝트 템플릿: Universal 3D (URP)
  * Android 빌드 시: Android Build Support, OpenJDK, Android SDK & NDK Tools 모듈 (Hub → 설치 → ⚙ → 모듈 추가)
  * iOS 빌드는 Mac + iOS Build Support 필요
* **코드 에디터:** VS Code + Unity 확장 (`VisualStudioToolsForUnity.vstuc`)
  * Unity → Edit → Preferences → External Tools → External Script Editor를 Visual Studio Code로 설정
* **Git LFS:** 모델·텍스처·오디오 등 대용량 파일 관리 (Git for Windows에 포함)
* **모바일 화면 테스트:** Unity → Window → General → Device Simulator

---

## 2. 저장소 구조

```text
TopWar/
├── Assets/
│   └── _Project/                ← 이 게임의 모든 리소스와 코드
│       ├── Scripts/
│       │   ├── Core/            ← 게임 규칙 (순수 C#, UnityEngine 미사용) — TopWar.Core.asmdef
│       │   └── Game/            ← 화면·입력·UI (MonoBehaviour) — TopWar.Game.asmdef
│       └── Tests/
│           └── EditMode/        ← 규칙 단위 테스트 — TopWar.Core.Tests.asmdef
├── Packages/                    ← 패키지 목록 (manifest.json)
├── ProjectSettings/             ← 프로젝트 설정, Unity 버전
├── .github/                     ← CI 워크플로우, PR 템플릿, Dependabot
├── README.md                    ← 게임 기획서
├── WORKFLOW.md                  ← 이 문서
├── CLAUDE.md                    ← Claude 작업 규칙
└── chat_history.md              ← 작업 기록
```

### 코드 구조 원칙

* 합성·상성·경제·웨이브 같은 **게임 규칙은 `Core`에 순수 C#으로** 작성하고 EditMode 테스트를 붙인다.
* `Game`의 MonoBehaviour는 표시, 입력, 연출만 담당하고 `Core`를 호출한다.

---

## 3. Git에 올리는 것 / 올리지 않는 것

| 구분 | 폴더·파일 | 이유 |
| --- | --- | --- |
| **커밋** | `Assets/`, `Packages/`, `ProjectSettings/` | 프로젝트 원본 |
| **커밋** | 모든 `.meta` 파일 | 에셋 GUID와 임포트 설정. 빠지면 참조가 깨짐 |
| 제외 | `Library/` | 임포트 캐시. 가장 크고 Unity가 자동 재생성 |
| 제외 | `Temp/`, `Obj/`, `Logs/` | 임시 파일, 컴파일 중간물, 로그 |
| 제외 | `UserSettings/` | 개인 에디터 레이아웃·설정 |
| 제외 | `Build/`, `Builds/`, `*.apk`, `*.aab` | 빌드 결과물 |
| 제외 | `.vs/`, `*.sln`, `*.csproj` | IDE가 자동 생성 |
| 제외 | `*.keystore`, `*.jks`, `.env` | 서명 키, 비밀 정보 |

Unreal과 비교하면 `Library/`는 `DerivedDataCache/`·`Intermediate/`, `Logs/`·`UserSettings/`는 `Saved/`에 해당한다.

**Git LFS 대상:** `.fbx`, `.obj`, `.blend`, `.png`, `.jpg`, `.tga`, `.psd`, `.exr`, `.hdr`, `.wav`, `.mp3`, `.ogg`, `.ttf`, `.otf`, `.mp4` ([.gitattributes](.gitattributes))

---

## 4. 작업 흐름

### 브랜치

* `main`: 릴리스용. 직접 커밋하지 않는다. `develop`이 안정되면 PR로 머지한다.
* `develop`: 기본 개발 브랜치. 직접 커밋하지 않고 작업 브랜치의 PR만 받는다.
* 작업 브랜치는 최신 `develop`에서 만든다. 이슈 하나에 브랜치 하나.
  * 기능: `feature/<이슈번호>-<설명>` (예: `feature/3-grid-merge`)
  * 버그 수정: `fix/<이슈번호>-<설명>`
  * 문서·설정: `chore/<설명>`, `docs/<이슈번호>-<설명>`

### 한 작업의 흐름

```text
[이슈 생성] → [develop에서 브랜치] → [구현·커밋·push] → [PR → develop] → [CI 통과] → [머지] → [이슈 자동 종료]
```

1. **이슈:** 할 일을 이슈로 만든다. 제목 형식 `feat: ...`, `fix: ...`, `docs: ...`, `design: ...`
2. **브랜치:** 최신 `develop`을 pull하고 이슈 번호를 넣은 브랜치를 만든다.
3. **커밋·push:** 작업 단위로 커밋한다. 메시지 형식 `feat: ...`, `fix: ...`, `chore: ...`, `docs: ...`
4. **PR:** base를 **`develop`** 으로 확인한다 (릴리스만 `main`). 본문에 `Closes #이슈번호`.
5. **머지:** CI 통과를 확인하고 머지한다. 작업 브랜치는 삭제한다.

### 역할 분담

| 작업 | 담당 |
| --- | --- |
| 이슈 생성, PR 생성, 머지 | 사용자 |
| 이슈·PR 제목과 본문 초안 | Claude |
| C# 코드, 테스트, 문서 작성, 커밋, push | Claude |
| Unity 에디터 작업 (씬, 프리팹, 설정) | 사용자 (Claude가 단계별 안내) |
| 동작 확인 | 사용자 + CI |

---

## 5. CI

* `.github/workflows/ci.yml`: `develop`·`main` 대상 PR과 push에서 GameCI로 EditMode·PlayMode 테스트를 실행한다.
* Unity 프로젝트나 라이선스 시크릿이 없으면 테스트를 건너뛴다 (실패하지 않음).
* 저장소 Settings → Secrets and variables → Actions에 등록:
  * `UNITY_LICENSE`: `C:\ProgramData\Unity\Unity_lic.ulf` 파일 내용 전체
  * `UNITY_EMAIL`, `UNITY_PASSWORD`: Unity 계정
* `.github/dependabot.yml`: GitHub Actions 업데이트 PR을 매월 `develop`으로 연다.
