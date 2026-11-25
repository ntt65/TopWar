# 13-4-runner-flow: 러너 스테이지 흐름과 보상 (Core + Game)

- **이슈:** #13
- **브랜치:** `feature/13-runner-stage`
- **작성:** Claude / 2026-11-26
- **순서:** 러너 지시서 4개 중 마지막. **13-3이 끝난 뒤** 진행한다. medium으로 충분.

## 목표

3의 배수 스테이지를 로비의 `Start Stage` 버튼으로 시작하면 러너가 나오고, 끝나면 보석을 받고 진행이 저장된다. 개발용 `Runner` 버튼은 **연습 모드**(보상·기록 없음)로 남긴다.

## 배경

- 기획: [README.md](../../README.md) ⑦ 러너 보너스 스테이지.
- 13-3에서 `GameBootstrap.StartRunner`가 설정을 직접 만들고, 결과 화면에 `Waves 0/0`, 보석 0을 띄우는 임시 처리를 했다. 이를 정식으로 바꾼다.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Scripts/Core/Progression/StageRules.cs` | 수정 | 러너 스테이지 판정, 보상, 설정 생성 |
| `Tests/EditMode/ProgressionTests.cs` | 수정 | 위 테스트 추가 |
| `Scripts/Game/GameBootstrap.cs` | 수정 | 스테이지 분기, 러너 결과 처리 |
| `Scripts/Game/UI/ResultScreen.cs` | 수정 | 상세 줄을 문자열로 받기 |
| `Scripts/Game/UI/LobbyScreen.cs` | 수정 | 러너 스테이지면 시작 버튼 문구 변경 |
| `Scripts/Game/UI/Texts.cs` | 수정 | 문구 추가 |

경로는 `Assets/_Project/` 아래.

## 상세 요구사항

### 1. `StageRules` 추가 (Core)

```csharp
public const int RunnerStageEvery = 3;

public static bool IsRunnerStage(int stage);
// stage < 1 → ArgumentOutOfRangeException. stage % RunnerStageEvery == 0

public static int RunnerGemReward(int stage, float progress, bool victory);
// stage < 1, progress가 NaN → 예외. progress는 0~1로 Clamp.
// (int)Math.Floor(progress × 10) + (victory ? 20 + 10 × (stage - 1) : 0)
// 예: stage 3 승리 → 10 + 40 = 50, stage 3에서 진행 45%로 패배 → 4

public static RunnerSettings CreateRunnerSettings(PlayerProgress progress, float unitBaseAttack);
// progress null → 예외. UnitBaseAttack과 병종별 공격력 배율(progress.GetAttackMultiplier)만 채우고 나머지는 RunnerSettings 기본값
```

`using TopWar.Core.Runner;` 추가는 괜찮다 (Core 안의 참조).

### 2. `ResultScreen` 변경

- `public void Init(int stage, bool victory, string detail, int gems)`로 바꾼다. 기존 `Waves {n}/{total}` 줄 자리에 `detail`을 그대로 표시한다.
- 디펜스 결과는 `string.Format(Texts.WavesCleared, clearedWaves, StageRules.WavesPerStage)`를 넘긴다 (화면은 이전과 같음).
- `gems`가 0이면 보석 줄에 `Texts.Practice`(`Practice - no reward`)를 표시한다.

### 3. `GameBootstrap`

- `StartStage(int stage)`: 맨 앞에서 `StageRules.IsRunnerStage(stage)`면 `StartRunner(stage, practice: false)`를 호출하고 끝낸다.
- 로비의 개발용 버튼은 `StartRunner(stage, practice: true)`를 호출한다 (`LobbyScreen`에 넘기는 콜백을 바꾼다).
- `StartRunner(int stage, bool practice)`:
  - 설정은 `StageRules.CreateRunnerSettings(_progress, config.BaseAttack)`.
  - 트랙·무리·시뮬레이션 생성은 13-3과 같다.
  - 종료 시 `FinishRunner(stage, practice, victory, sim.Progress)`.
- `FinishRunner`:
  - `practice`가 아니면: `gems = StageRules.RunnerGemReward(stage, progress, victory)`, `AddGems`, 승리면 `RecordStageCleared(stage)`, `SaveProgress()`.
  - `practice`면 보상·기록·저장 없음, `gems = 0`.
  - `ResultScreen.Init(stage, victory, string.Format(Texts.RunProgress, Mathf.RoundToInt(progress × 100)), gems)`.

### 4. `LobbyScreen`

- 시작 버튼 문구: `StageRules.IsRunnerStage(NextStage)`면 `Texts.StartRunnerStage`(`Start Stage {0} - Runner`), 아니면 기존 `Texts.StartStage`.

### 5. `Texts` 추가

`StartRunnerStage = "Start Stage {0} - Runner"`, `RunProgress = "Distance {0}%"`, `Practice = "Practice - no reward"`

## 테스트 (`ProgressionTests`에 추가)

- `IsRunnerStage`: 1·2 false, 3·6 true, 0 예외
- `RunnerGemReward`: 위 두 예시, progress 1.5는 1로·-1은 0으로 Clamp, NaN·stage 0 예외
- `CreateRunnerSettings`: 공격력 업그레이드 레벨이 병종별 배율에 반영, `UnitBaseAttack` 반영, null 예외

## 하지 말 것

- 13-1~13-3의 Runner Core·화면 동작 변경 금지 (이번 범위는 흐름과 보상만).
- 디펜스 보상 공식(`GemReward`) 변경 금지.
- 씬·프리팹·ProjectSettings 편집 금지.

## 완료 조건

- [ ] 위 테스트 통과, 기존 EditMode 테스트도 통과
- [ ] 사용자 확인: Stage 2까지 클리어하면 로비에 `Start Stage 3 - Runner` → 러너 진행 → 승리 시 보석 지급, 로비에 `Start Stage 4`
- [ ] 러너에서 패배하면 진행 거리 비례 보석, 같은 스테이지 다시 시작
- [ ] 개발용 `Runner` 버튼은 결과에 `Practice - no reward`, 보석·스테이지 변화 없음
- [ ] 디펜스 결과 화면은 이전과 같음 (`Waves n/10`)

## 참고

- [StageRules.cs](../../Assets/_Project/Scripts/Core/Progression/StageRules.cs), [RunnerSettings.cs](../../Assets/_Project/Scripts/Core/Runner/RunnerSettings.cs), [GameBootstrap.cs](../../Assets/_Project/Scripts/Game/GameBootstrap.cs), [ResultScreen.cs](../../Assets/_Project/Scripts/Game/UI/ResultScreen.cs)
