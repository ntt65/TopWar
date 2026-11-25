# 대화 기록

최신 항목이 위에 온다. 작성 규칙은 [CLAUDE.md](CLAUDE.md) 참고.

---

## 2026-11-25 — #1 README 정리 / #2 게임 설계 공백 결정

- **Chat ID:** 640346c0-dcd0-4262-bc6a-86e241f914d6
- **커밋:** `docs/1-readme-design` 브랜치 (PR → `develop`, Closes #1 #2)
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
