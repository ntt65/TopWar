# 대화 기록

최신 항목이 위에 온다. 작성 규칙은 [CLAUDE.md](CLAUDE.md) 참고.

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
- **미결정/다음 할 일:**
  - GitHub에서 `develop`·`main` 브랜치 보호 규칙 설정 (CI 통과 필수, 직접 push 금지)
  - README 하단의 잘못 복사된 내용 정리
  - 설계 공백 결정: 그리드↔레인 관계, 적 병종 유무와 적의 공격 대상, 해군 배치(물 타일?), 합성 최대 레벨·소환 비용·병종 선택 방식, 기지 확장·연구 시스템 명세
  - 실제 게임명 "Top War" 언급 정리 (출시 시 고유한 이름 권장)
  - Phase 1: 5×5 그리드 + 드래그 앤 드롭 합성 프로토타입
