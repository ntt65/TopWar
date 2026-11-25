# 대화 기록

최신 항목이 위에 온다. 작성 규칙은 [CLAUDE.md](CLAUDE.md) 참고.

---

## 2026-11-25 23:48 — #4 Phase 2.1 전투 데이터와 웨이브 생성

- **Chat ID:** 01a2132a-06cb-7440-9cc8-7dfc052bda5d
- **커밋:** `e56b3d7` (`feature/4-wave-combat`, 원격 push 완료)
- **요약:** `docs/tasks/4-1-battle-core-data.md`에 따라 적 능력치와 피해·사망 처리, 기지 내구도, 병종별 전투 프로필, 일반·보스 웨이브 생성기를 순수 C# Core로 구현했다.
  피해 처리, 공격력 성장, 보스 순환, 동일 seed 재현성을 다루는 EditMode 테스트 두 파일을 추가했다. Core의 UnityEngine 미참조와 Git 공백 검사를 확인했다.
- **결정 사항:**
  - 일반 웨이브는 웨이브 번호에 따라 적 수와 체력이 증가하고 출현 간격이 줄어든다. 병종과 레인은 seed로 재현 가능한 무작위로 배정한다.
  - 기본 설정에서 5웨이브마다 일반 적 수를 절반으로 줄이고, 가운데 레인에 마지막으로 보스를 배치한다. 보스 병종은 육군 → 공군 → 해군 순으로 순환한다.
  - 시간 진행, 이동, 공격 시뮬레이션은 4-2 범위로 둔다.
- **미결정/다음 할 일:**
  - Unity에서 새 파일의 `.meta`를 생성하고 Test Runner에서 EditMode 테스트를 실행한다. 이번 환경에서는 Unity 테스트를 실행하지 못했다.
  - 확인 후 `4-2`와 `4-3`을 순서대로 진행한다.

---

## 2026-11-25 — #3 Phase 1 프로토타입, Codex 협업 체계

- **Chat ID:** 640346c0-dcd0-4262-bc6a-86e241f914d6
- **커밋:** `feature/3-grid-merge` 브랜치 (PR → `develop`, Closes #3)
- **요약:** Unity 6.3 LTS 프로젝트를 저장소 루트에 합쳤다(빈 폴더에 Universal 3D 템플릿으로 만든 뒤 `Assets` 일부·`Packages`·`ProjectSettings`·`Library`를 이동). Phase 1(그리드, 병종별 소환, 드래그 이동·스왑·합성, 쿼터뷰 카메라, HUD)을 구현하고 에디터 Simulator에서 동작을 확인했다. 작업 방식을 WORKFLOW.md로 모으고, Codex용 AGENTS.md와 작업 지시서 양식을 만들었다.
- **결정 사항:**
  - Unity 6.3 LTS (6000.3.29f1). Android 모듈 설치됨. 코드 에디터는 VS Code.
  - 문서 역할: README = 기획, WORKFLOW.md = 작업 방식, AGENTS.md = Codex 규칙, CLAUDE.md = Claude 규칙.
  - 역할: Claude는 설계·지시서(`docs/tasks/`)·리뷰, Codex는 구현·테스트·push, 사용자는 에디터 작업·PR·머지.
  - 코드 구조: `Core`(순수 C#, 규칙) / `Game`(MonoBehaviour, 표시·입력·UI). 씬에는 `GameBootstrap` 하나만 붙이고 나머지는 코드로 생성.
  - 카메라: 그리드 폭을 화면 가로에 맞추고 그리드 아래 끝을 화면 20% 높이(버튼 위)에 고정. 위쪽 공간은 적 레인.
  - HUD는 프로토타입이라 영어 표기. 한글 폰트는 Phase 3에서 TextMeshPro로.
- **미결정/다음 할 일:**
  - Phase 1 PR 머지 후 #4 Phase 2 지시서 작성 → Codex 구현
  - GitHub 기본 브랜치 `develop`으로 변경, Unity 라이선스 시크릿 등록
  - `D:\git\TopWarUnity` 잔여 폴더 삭제

---

## 2026-11-25 — 기술 스택을 Unity 3D로 전환

- **Chat ID:** 640346c0-dcd0-4262-bc6a-86e241f914d6
- **커밋:** `chore/unity-stack` 브랜치 (PR → `develop`)
- **요약:** 실제로 움직이는 3D 게임을 목표로 Flutter + Flame에서 Unity 6 LTS(3D, URP)로 전환했다. README, CLAUDE.md, CI(GameCI), .gitignore, .gitattributes(LFS), Dependabot을 Unity 기준으로 바꿨다.
- **결정 사항:**
  - 엔진 Unity 6 LTS, C#, URP, Input System, ScriptableObject 데이터. 플랫폼은 Android/iOS (웹 제외).
  - 세로 화면 + 고정 쿼터뷰 카메라. Phase 1~3은 기본 도형으로, Phase 4에서 로우폴리 에셋으로 교체.
  - 게임 규칙은 순수 C# 클래스로 분리해 EditMode 테스트. Claude는 스크립트를, 사용자는 에디터 작업을 맡는다.
  - 교훈: GitHub에서 PR base 기본값이 `main`이라 #8이 `main`으로 머지됐다. PR을 만들 때 base를 꼭 확인하고, 기본 브랜치를 `develop`으로 바꾼다.
- **미결정/다음 할 일:**
  - GitHub 기본 브랜치를 `develop`으로 변경
  - Unity 프로젝트 생성(사용자), GitHub에 Unity 라이선스 시크릿 등록
  - 이슈 #3, #5 본문을 Unity 기준으로 수정
  - #3 Phase 1 착수

---

## 2026-11-25 — #1 README 정리 / #2 게임 설계 공백 결정

- **Chat ID:** 640346c0-dcd0-4262-bc6a-86e241f914d6
- **커밋:** `680ad92` (PR #8, `main`으로 머지됨)
- **요약:** 설계 공백 8개 항목을 결정하고 README에 반영했다. README 하단의 잘못 복사된 내용과 다른 게임명 직접 언급을 제거했다.
- **결정 사항:**
  - 전장: 적은 화면 위에서 아래 기지로 세로 레인(열마다 1개)을 따라 진격. 세로 화면.
  - 적도 육·공·해 3병종, 상성 양방향 적용. 적은 기지만 공격(아군 유닛은 죽지 않음).
  - 소환: 병종별 소환 버튼. 해군 포함 모든 병종이 모든 칸에 배치 가능.
  - 합성 최대 Lv.10.
  - 성장: 골드는 판 안에서 소환용, 보석은 스테이지 클리어 보상으로 영구 업그레이드(병종 공격력, 기지 내구도, 그리드 확장).
  - 보스 제한 시간 초과 시 보스 퇴각 + 기지 내구도 50% 감소.
  - 밸런스 초기값: 공격력 Lv마다 2배, 소환 10골드 +2씩, 기지 내구도 100, 보스 제한 60초 (Phase 4에서 조정).
- **미결정/다음 할 일:**
  - 정식 게임명 (현재 "TopWar Defense (가제)")
  - #3 Phase 1 착수

---

## 2026-11-25 21:19 — README 검토 및 대화 기록 체계 도입

- **Chat ID:** 640346c0-dcd0-4262-bc6a-86e241f914d6
- **커밋:** `chore/devops-setup` 브랜치 (PR → `develop`)
- **요약:** README.md 기획서를 검토했다. 하단(83~94줄)에 다른 AI 대화에서 잘못 복사된 내용(닫히지 않은 코드펜스, 안내문, `<ElicitationsGroup>` 태그)이 남아 있다. Claude는 세션 간 기억이 없으므로 CLAUDE.md + chat_history.md로 맥락을 이어가기로 했다.
- **결정 사항:**
  - CLAUDE.md에는 규칙, chat_history.md에는 날짜별 작업 요약을 적는다.
  - CLAUDE.md에서 `@chat_history.md`로 기록을 자동으로 불러온다.
  - 기록에는 실패·폐기한 시도를 빼고 최종 결과와 결정만 남긴다.
  - 브랜치 전략: `develop` 기본 개발, 작업마다 `feature/*` 브랜치 → push. PR 생성과 머지는 사용자가 직접 하고, Claude는 때가 되면 알린다. `main`은 릴리스용.
  - 검증은 사용자와 GitHub Actions가 맡는다. 코드 디버깅은 최대 3회, CLI·CI·승인 실패는 재시도 없이 보고한다.
  - DevOps 구성: `develop` 브랜치 생성, CI(format/analyze/test), Dependabot, PR 템플릿, Flutter용 .gitignore(기존 Unity 템플릿 교체), .gitattributes(LF).
  - 작업은 GitHub 이슈 단위로 한다. 브랜치에 이슈 번호를 넣고 PR에 `Closes #번호`를 쓴다.
- **미결정/다음 할 일:**
  - GitHub에서 `develop`·`main` 브랜치 보호 규칙 설정 (CI 통과 필수, 직접 push 금지)
  - 등록된 이슈 (진행 순서):
    - #1 docs: README 정리
    - #2 design: 게임 설계 공백 결정
    - #3 feat: Phase 1 - 그리드 및 드래그 앤 드롭 합성 프로토타입
    - #4 feat: Phase 2 - 적 웨이브, 이동 경로, 자동 전투
    - #5 feat: Phase 3 - 병종 상성, 골드 경제, HUD
    - #6 feat: Phase 4 - 이펙트, 사운드, 밸런싱
