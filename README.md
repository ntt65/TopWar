# TopWar Defense (가제)

**그리드 기반 유닛 합성(Merge)** 과 **웨이브 디펜스(Wave Defense)** 를 결합한 3D 전략 디펜스 게임 프로젝트입니다.

---

## 1. 게임 개요 (Game Overview)

* **장르:** 3D 합성(Merge) + 웨이브 디펜스(Wave Defense)
* **플랫폼:** Mobile (Android / iOS)
* **화면:** 세로(Portrait) 모드, 위에서 비스듬히 내려다보는 고정 쿼터뷰 카메라
* **그래픽:** 3D 로우폴리 유닛이 전장에서 실제로 움직이고, 사격하고, 합성 연출을 보여준다.
* **핵심 재미 요소:**
  1. 제한된 그리드 공간 내에서 하위 유닛을 합성하여 상위 병기로 진화시키는 쾌감
  2. 병종 간 상성(육군·해군·공군)을 고려한 전략적 진형 배치
  3. 점진적으로 강해지는 적 웨이브를 방어하며 기지를 확장하는 성장 루프

---

## 2. 세계관 및 시나리오 (Scenario & Lore)

* **배경 설정:** 자원 고갈 이후, 남은 에너지 코어(Energy Core)가 매장된 중앙 섬(Central Island)을 차지하기 위해 다국적 용병단과 정체불명의 기계 군단(Dark Legion)이 충돌한다.
* **플레이어 역할:** 전초기지 사령관(Commander). 한정된 보급품으로 기초 병력을 소집하고, 현장 병합(Field Merge) 기술을 통해 최정예 기갑·항공·함대 전력을 구축하여 방어선을 사수해야 한다.
* **진행 흐름:**
  * **Chapter 1 (해안 방어선):** 상륙하는 적 보병 및 경장갑 차량 저지
  * **Chapter 2 (폭풍의 해협):** 해상 전함 및 공중 폭격기 편대 방어
  * **Chapter 3 (코어 요새):** 거대 보스 병기(Boss Unit) 공략 및 최종 방어선 구축

---

## 3. 핵심 게임 루프 (Core Gameplay Loop)

```text
[스테이지 진행]
  [골드 소모: 병종 선택 후 Lv.1 유닛 소환]
         │
         ▼
  [동일 병종·레벨 드래그 & 합성 (Lv.N + Lv.N -> Lv.N+1)]
         │
         ▼
  [그리드 배치 및 상성 기반 자동 교전]
         │
         ▼
  [적 웨이브 격퇴 및 골드 획득] ──(반복)──▶ [스테이지 클리어: 보석 획득]

[스테이지 사이]
  [보석으로 영구 업그레이드: 병종 공격력, 기지 내구도, 그리드 확장]
```

---

## 4. 주요 시스템 명세 (Core Systems)

### ① 전장 구조 (Battlefield)

```text
   ▼    ▼    ▼    ▼    ▼     ← 적 스폰 (열마다 레인 1개)
   │    │    │    │    │
 ┌────┬────┬────┬────┬────┐
 │    │    │    │    │    │
 ├────┼────┼────┼────┼────┤
 │    │  유닛 그리드 (5×5)  │   ← 아군 방어진
 ├────┼────┼────┼────┼────┤
 │    │    │    │    │    │
 └────┴────┴────┴────┴────┘
 [         기   지         ]   ← 적이 도달하면 내구도 감소
```

* 적은 화면 위쪽에서 아래쪽 기지를 향해 **레인(세로 열)** 을 따라 진격한다. 레인 수는 그리드 열 수와 같다.
* 적은 그리드의 아군 유닛을 공격하지 않고 **기지만 공격**한다. 기지에 도달한 적은 사라지며 기지 내구도를 깎는다.
* 아군 유닛은 사거리 안의 적을 자동으로 공격한다.

### ② 그리드 및 합성 시스템 (Grid & Merge)

* **전장 규격:** 기본 $5 \times 5$ 타일 그리드 (영구 업그레이드로 $6 \times 6$, 최대 $7 \times 7$ 까지 확장)
* **배치 제약:** 모든 병종을 모든 칸에 배치할 수 있다 (지형 제약 없음).
* **합성 규칙:** 동일한 병종(Type)과 레벨(Level)을 가진 두 유닛을 겹치면 상위 레벨($N + 1$) 유닛 1기로 즉시 진화
* **최대 레벨:** Lv.10
* **전술 재배치:** 전투 중에도 유닛 위치를 자유롭게 이동하거나 스왑(Swap)할 수 있다.

### ③ 소환 시스템 (Summon)

* **병종별 소환 버튼:** 육군·공군·해군 버튼이 따로 있고, 누르면 해당 병종 Lv.1 유닛이 빈 칸에 생성된다.
* 빈 칸이 없으면 소환할 수 없다.
* 소환할 때마다 비용이 증가한다 (초기값은 아래 밸런스 표 참고).

### ④ 병종 및 상성 시스템 (Unit Types & Counter)

* **육군 (Army):** 높은 체력과 안정적인 지상 화력 / **공군에게 강함**
* **공군 (Air Force):** 빠른 공격 속도와 광역 타격 / **해군에게 강함**
* **해군 (Navy):** 긴 사거리와 강력한 단일 폭딜 / **육군에게 강함**
* **적도 3병종:** 적 유닛도 육군·공군·해군으로 나뉘며 같은 상성표를 따른다.
* **상성 보정:** 우위 병종 공격 시 데미지 **+25%**, 열위 병종 공격 시 **-15%** 적용

### ⑤ 웨이브 및 보스 시스템 (Wave & Boss)

* **일반 웨이브:** 정해진 레인을 따라 다수의 적 유닛이 순차적으로 진격
* **보스 웨이브 (매 5 Wave 단위):** 높은 체력과 특수 패턴(방어막 생성, 소환 등)을 가진 거대 보스 등장
* **보스 제한 시간:** 시간 안에 격퇴하지 못하면 보스는 퇴각하고 **기지 내구도가 50% 감소**한다.
* **패배 조건:** 기지 내구도가 0이 되면 패배

### ⑥ 재화 및 성장 시스템 (Economy & Progression)

| 재화 | 획득 | 사용 | 유지 |
| --- | --- | --- | --- |
| 골드 | 적 처치, 웨이브 클리어 | 유닛 소환 | 스테이지 안에서만 |
| 보석 | 스테이지 클리어 | 영구 업그레이드 | 계속 유지 |

* **영구 업그레이드 (스테이지 사이):**
  * 병종별 공격력 강화 (육군 / 공군 / 해군)
  * 기지 최대 내구도 증가
  * 그리드 확장 ($5 \times 5$ → $6 \times 6$ → $7 \times 7$)

### ⑦ 밸런스 초기값 (Phase 4에서 조정)

| 항목 | 초기값 |
| --- | --- |
| 레벨별 공격력 | $\text{Lv.1 공격력} \times 2^{(N-1)}$ (합성 = 공격력 2배) |
| 소환 비용 | 10골드에서 시작, 소환할 때마다 +2골드 |
| 기지 내구도 | 100 |
| 보스 제한 시간 | 60초 |

---

## 5. 기술 스택 (Tech Stack)

* **Engine:** Unity 6.3 LTS (6000.3.29f1), 3D 프로젝트
* **Language:** C#
* **Rendering:** URP (Universal Render Pipeline), 모바일 최적화
* **Input:** Input System 패키지 (터치 드래그 → 레이캐스트로 그리드 칸 선택)
* **Data:** ScriptableObject (유닛 스탯 테이블, 웨이브 데이터, 밸런스 값)
* **Test:** Unity Test Framework (EditMode: 합성·상성·경제 로직, PlayMode: 전투 흐름)
* **CI:** GitHub Actions + GameCI (`game-ci/unity-test-runner`)
* **아트:** Phase 1~3은 기본 도형(큐브·캡슐)으로 프로토타입, Phase 4에서 로우폴리 3D 에셋으로 교체
* **대용량 파일:** Git LFS (모델, 텍스처, 오디오)

### 게임 로직 구조 원칙

* 합성·상성·경제·웨이브 같은 **게임 규칙은 MonoBehaviour와 분리된 순수 C# 클래스**로 작성해 EditMode 테스트로 검증한다.
* MonoBehaviour는 화면 표시, 입력, 연출만 담당하고 규칙 클래스를 호출한다.

---

## 6. 개발 환경 (Development Setup)

### 필요한 도구

* **Unity Hub** + **Unity 6.3 LTS (6000.3.29f1)**
  * 모듈: Android Build Support, OpenJDK, Android SDK & NDK Tools
  * iOS 빌드는 Mac + iOS Build Support 필요
* **코드 에디터:** VS Code + Unity 확장 (`VisualStudioToolsForUnity.vstuc`)
  * Unity → Edit → Preferences → External Tools → External Script Editor를 Visual Studio Code로 설정
* **Git LFS:** 모델·텍스처·오디오 등 대용량 파일 관리 (Git for Windows에 포함)

### 저장소 구조

```text
TopWar/
├── Assets/
│   └── _Project/            ← 이 게임의 모든 리소스와 코드
│       ├── Scripts/
│       │   └── Core/        ← 게임 규칙 (순수 C#, UnityEngine 미사용)
│       └── Tests/
│           └── EditMode/    ← 규칙 단위 테스트
├── Packages/                ← 패키지 목록 (manifest.json)
├── ProjectSettings/         ← 프로젝트 설정, Unity 버전
├── .github/                 ← CI 워크플로우, PR 템플릿, Dependabot
├── CLAUDE.md                ← Claude 작업 규칙
└── chat_history.md          ← 작업 기록
```

### Git에 올리는 것 / 올리지 않는 것

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

Unreal과 비교하면 `Library/`는 `DerivedDataCache/`와 `Intermediate/`, `Logs/`와 `UserSettings/`는 `Saved/`에 해당한다.

### Git LFS 대상

`.fbx`, `.obj`, `.blend`, `.png`, `.jpg`, `.tga`, `.psd`, `.exr`, `.hdr`, `.wav`, `.mp3`, `.ogg`, `.ttf`, `.otf`, `.mp4` ([.gitattributes](.gitattributes) 참고)

### 브랜치 및 작업 흐름

* `main`: 릴리스용 / `develop`: 기본 개발 브랜치
* 이슈마다 `develop`에서 브랜치 생성: `feature/<이슈번호>-<설명>`, `fix/...`, `chore/...`
* 커밋 → push → `develop` 대상 PR (본문에 `Closes #이슈번호`) → CI 통과 확인 → 머지

### CI

* PR과 push 때 GitHub Actions + GameCI로 EditMode·PlayMode 테스트를 실행한다.
* 저장소 Secrets에 `UNITY_LICENSE`(`C:\ProgramData\Unity\Unity_lic.ulf` 내용), `UNITY_EMAIL`, `UNITY_PASSWORD`가 있어야 한다. 없으면 테스트를 건너뛴다.

---

## 7. 개발 마일스톤 (Roadmap)

* [ ] **Phase 1:** Unity 프로젝트 생성, 3D $5 \times 5$ 그리드 및 터치 드래그 합성 프로토타입 구현 (#3)
* [ ] **Phase 2:** 적 웨이브 스폰, 레인 이동, 자동 사격 및 투사체 충돌 로직 구현 (#4)
* [ ] **Phase 3:** 병종 상성 연산, 골드·보석 경제, 소환/업그레이드 HUD 연동 (#5)
* [ ] **Phase 4:** 로우폴리 3D 에셋 교체, 합성·전투 파티클 이펙트, 사운드 및 밸런싱 튜닝 (#6)
