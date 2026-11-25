# CLAUDE.md

## 프로젝트

- 2D 합성(Merge) + 웨이브 디펜스 게임. 상세 기획은 [README.md](README.md) 참고.
- 스택: Flutter (Dart) + Flame, Riverpod, JSON 데이터 테이블.
- 사용자와는 한국어로 대화한다.
- 검증은 사용자와 GitHub Actions가 맡는다. 불필요한 확인·탐색 명령은 하지 않고, 작업에 꼭 필요한 CLI만 실행한다.
- 코드 버그 디버깅은 최대 3회까지 시도한다. 3회 안에 해결되지 않으면 멈추고 원인과 시도 내용을 보고한다.
- CLI 실패, CI 실패, 권한·승인 거부처럼 코드 밖의 문제는 재시도하지 않는다. 한 번 실패하면 바로 멈추고, 무엇이 막혔는지와 사용자가 할 일을 짧게 보고한다.

## 브랜치 전략

- `main`: 릴리스용. 직접 커밋하지 않는다. `develop`이 안정되면 PR로 머지한다.
- `develop`: 기본 개발 브랜치. 직접 커밋하지 않고 feature 브랜치의 PR만 받는다.
- 작업 브랜치는 `develop`에서 만든다. 작업 하나에 브랜치 하나.
  - 기능: `feature/<짧은-설명>` (예: `feature/grid-merge`)
  - 버그 수정: `fix/<짧은-설명>`
  - 문서·설정: `chore/<짧은-설명>`
- 작업이 끝나면: 커밋 → push → `develop` 대상 PR 생성 → 사용자 확인 후 squash 머지 → 작업 브랜치 삭제.
- PR 머지는 사용자가 승인한 뒤에만 한다.

## CI / DevOps

- `.github/workflows/ci.yml`: `develop`·`main` 대상 PR/push에서 `dart format` 검사 → `flutter analyze` → `flutter test` 실행. `pubspec.yaml`이 없으면 건너뛴다.
- `.github/dependabot.yml`: GitHub Actions·pub 의존성 업데이트 PR을 매월 `develop`으로 연다.
- `.gitattributes`: 줄바꿈을 LF로 통일한다. 커밋 전 `dart format .`을 실행한다.

## 대화 기록 규칙

이전 작업 맥락은 아래 파일에 있다. 세션을 시작하면 먼저 읽는다.

@chat_history.md

세션 중 의미 있는 작업이나 결정이 있었다면, 세션을 마무리할 때(또는 사용자가 "기록해줘"라고 할 때) `chat_history.md` **맨 위**에 항목을 추가한다.

- 대화 전문을 옮기지 말고 **요약**만 적는다.
- 실패한 시도, 되돌린 작업, 버린 접근은 적지 않는다. **최종 결과와 확정된 결정만** 정리해서 남긴다.
  - 예외: "X 방식은 안 된다"처럼 다음에 같은 실수를 막는 교훈은 결정 사항에 한 줄로 남긴다.
- 같은 세션에서 다시 기록하면 새 항목을 만들지 말고 기존 항목을 갱신한다.
- Chat ID는 세션 ID(UUID)를 쓴다. 알 수 없으면 `unknown`으로 적는다.
- 커밋 해시는 이 세션에서 만든 커밋들. 커밋이 없으면 `없음 (HEAD: <짧은 해시>)`으로 적는다.

항목 형식:

```markdown
## <YYYY-MM-DD HH:MM> — <제목>
- **Chat ID:** <세션 ID>
- **커밋:** <해시 목록 또는 "없음 (HEAD: xxxxxxx)">
- **요약:** <무엇을 했는지 2~4줄>
- **결정 사항:** <확정된 것>
- **미결정/다음 할 일:** <남은 것>
```
