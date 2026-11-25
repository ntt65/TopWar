# 5-1-progression-core: 스테이지, 보석, 영구 업그레이드 (Core)

- **이슈:** #5
- **브랜치:** `feature/5-economy-progression`
- **작성:** Claude / 2026-11-26
- **순서:** Phase 3 지시서 3개 중 1번째. 4-2 수준의 로직이라 **high** 추천.

## 목표

한 판을 "10웨이브 스테이지"로 만들고, 스테이지 결과로 보석을 주고, 보석으로 영구 업그레이드를 사는 규칙을 순수 C#으로 만든다.

## 배경 (확정된 기획)

- 스테이지 = 웨이브 10개. 5·10웨이브는 보스(기존 `BossEvery = 5` 그대로). 10웨이브를 클리어하면 **승리**.
- 스테이지가 오를수록 적 체력이 `1.6^(스테이지-1)`배.
- 보석 = 클리어한 웨이브 수 + (승리 시 `10 + 5 × (스테이지-1)`). 져도 넘긴 웨이브만큼 받는다.
- 영구 업그레이드 (보석으로 구매, 저장됨):

| 종류 | 효과 (레벨 L) | 최대 레벨 | 비용 (현재 레벨 L에서 다음 레벨로) |
| --- | --- | --- | --- |
| `ArmyAttack` | 육군 공격력 × (1 + 0.1L) | 20 | 5 + 5L |
| `AirForceAttack` | 공군 공격력 × (1 + 0.1L) | 20 | 5 + 5L |
| `NavyAttack` | 해군 공격력 × (1 + 0.1L) | 20 | 5 + 5L |
| `BaseHealth` | 기지 최대 내구도 + 20L | 10 | 5 + 5L |
| `GridSize` | 그리드 크기 5 + L (5×5 → 6×6 → 7×7) | 2 | L=0: 30, L=1: 60 |

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Scripts/Core/Battle/BattleState.cs` | 수정 | `Victory` 추가 |
| `Scripts/Core/Battle/BattleSettings.cs` | 수정 | 프로퍼티 추가 (아래) |
| `Scripts/Core/Battle/WaveGenerator.cs` | 수정 | `EnemyHpScale` 적용 |
| `Scripts/Core/Battle/BattleSimulation.cs` | 수정 | 승리 처리, 병종별 공격 배율, `ClearedWaves` |
| `Scripts/Core/Progression/UpgradeType.cs` | 추가 | enum |
| `Scripts/Core/Progression/PlayerProgress.cs` | 추가 | 보석·업그레이드·최고 스테이지 |
| `Scripts/Core/Progression/PlayerProgressData.cs` | 추가 | 저장용 데이터 |
| `Scripts/Core/Progression/StageRules.cs` | 추가 | 스테이지 난이도·보상·전투 설정 생성 |
| `Tests/EditMode/ProgressionTests.cs` | 추가 | |
| `Tests/EditMode/BattleSimulationTests.cs` | 수정 | 승리·배율 테스트 추가 (기존 테스트는 통과 유지) |

경로는 `Assets/_Project/` 아래. 새 네임스페이스 `TopWar.Core.Progression`.

## 상세 요구사항

### 1. `BattleSettings` 추가 프로퍼티

| 프로퍼티 | 타입 | 기본값 | 의미 |
| --- | --- | --- | --- |
| `WavesPerStage` | int | 0 | 0이면 무한(기존 동작). 1 이상이면 그 웨이브를 클리어할 때 승리 |
| `EnemyHpScale` | float | 1 | 모든 적(보스 포함) 체력에 곱함 |
| `ArmyAttackMultiplier` | float | 1 | |
| `AirForceAttackMultiplier` | float | 1 | |
| `NavyAttackMultiplier` | float | 1 | |

메서드 추가: `public float GetUnitAttackMultiplier(UnitType type)` — 위 세 값 중 하나를 반환.

기본값이 기존 동작과 같으므로 **기존 테스트가 그대로 통과해야 한다.**

### 2. `WaveGenerator`

- 일반 적과 보스의 MaxHp에 `settings.EnemyHpScale`을 추가로 곱한다. 나머지 규칙은 그대로.

### 3. `BattleState` / `BattleSimulation`

- `BattleState.Victory` 추가. Victory 상태에서 `Tick`은 아무것도 하지 않는다 (GameOver와 같음).
- `public int ClearedWaves { get; }` — 클리어한 웨이브 수. `WaveCleared` 발생할 때마다 1 증가.
- `public event Action StageCleared;`
- 웨이브 클리어 처리: 기존대로 보너스 골드 지급 → `WaveCleared` 발생 → **`WavesPerStage > 0`이고 `WaveNumber >= WavesPerStage`이면** `State = Victory`, `StageCleared` 발생. 아니면 기존대로 Break.
- 공격 피해: `profile.AttackPower(settings.UnitBaseAttack * settings.GetUnitAttackMultiplier(unit.Type), unit.Level) × 상성 배율`. 광역 피해도 같은 공격력 사용.

### 4. `UpgradeType` (enum)

`ArmyAttack, AirForceAttack, NavyAttack, BaseHealth, GridSize` — 이 순서 그대로 (저장 데이터의 배열 인덱스로 쓴다).

### 5. `PlayerProgressData` (저장용)

```csharp
[Serializable]
public sealed class PlayerProgressData
{
    public int version = 1;
    public int gems;
    public int highestClearedStage;
    public int[] upgradeLevels; // 인덱스 = (int)UpgradeType
}
```

`System.Serializable` 사용 (UnityEngine 아님). public 필드는 Unity `JsonUtility` 호환을 위해 필드로 둔다 (AGENTS.md의 public 필드 금지 규칙의 예외).

### 6. `PlayerProgress` (sealed class)

```csharp
public PlayerProgress()
public int Gems { get; }
public int HighestClearedStage { get; }
public int NextStage => HighestClearedStage + 1;
public event Action Changed;

public int GetLevel(UpgradeType type)
public int GetMaxLevel(UpgradeType type)   // 위 표
public bool IsMaxed(UpgradeType type)
public int GetCost(UpgradeType type)       // 위 표. 최대 레벨이면 0
public bool CanUpgrade(UpgradeType type)   // 최대 아님 && Gems >= 비용
public bool TryUpgrade(UpgradeType type)   // 가능하면 보석 차감, 레벨 +1, Changed, true

public void AddGems(int amount)            // amount < 0 예외. 0이면 아무것도 안 함. Changed
public void RecordStageCleared(int stage)  // stage < 1 예외. HighestClearedStage = max(기존, stage). 바뀌면 Changed

public float GetAttackMultiplier(UnitType type) // 1 + 0.1 × 해당 병종 공격 업그레이드 레벨
public int GetBaseMaxHealth(int baseValue)      // baseValue + 20 × BaseHealth 레벨
public int GetGridSize()                        // GameRules.DefaultGridSize + GridSize 레벨

public PlayerProgressData ToData()
public static PlayerProgress FromData(PlayerProgressData data)
```

`FromData` 규칙:
- `data`가 null이면 새 진행 상태.
- 음수 보석·스테이지는 0으로.
- `upgradeLevels`가 null이거나 길이가 짧으면 빠진 항목은 0. 길면 남는 값은 무시.
- 각 레벨은 0 ~ 최대 레벨로 자른다.

### 7. `StageRules` (static class)

```csharp
public const int WavesPerStage = 10;
public const float StageHpGrowth = 1.6f;
public const int BaseMaxHealth = 100;

public static float EnemyHpScale(int stage)  // stage < 1 예외. 1.6^(stage-1)
public static int GemReward(int stage, int clearedWaves, bool victory)
    // stage < 1 또는 clearedWaves < 0 예외. clearedWaves + (victory ? 10 + 5 × (stage-1) : 0)
public static BattleSettings CreateSettings(int stage, PlayerProgress progress, float unitBaseAttack)
    // 새 BattleSettings에 WavesPerStage, EnemyHpScale(stage), UnitBaseAttack,
    // 병종별 AttackMultiplier = progress.GetAttackMultiplier(...),
    // BaseMaxHealth = progress.GetBaseMaxHealth(BaseMaxHealth)를 넣어 반환
```

## 하지 말 것

- `Game` 폴더는 건드리지 않는다 (5-2에서 한다).
- 저장(PlayerPrefs, 파일)은 Core에서 하지 않는다. Core는 `ToData`/`FromData`까지만.
- 기존 public API의 시그니처를 바꾸지 않는다 (추가만).

## 완료 조건

EditMode 테스트:

- [ ] `WavesPerStage = 2`로 시뮬레이션: 웨이브 2 클리어 시 Victory, `StageCleared` 발생, 이후 Tick 변화 없음
- [ ] `WavesPerStage = 0`이면 기존처럼 계속 진행
- [ ] `ClearedWaves`가 웨이브 클리어마다 증가
- [ ] `ArmyAttackMultiplier = 2`이면 육군 피해 2배, 다른 병종은 그대로
- [ ] `EnemyHpScale = 2`이면 일반 적·보스 체력 2배
- [ ] 업그레이드 비용·최대 레벨·효과가 표와 같음, 보석 부족·최대 레벨이면 `TryUpgrade` false
- [ ] `GridSize` 2레벨이면 `GetGridSize() == 7`
- [ ] `ToData` → `FromData` 왕복 시 값 동일
- [ ] `FromData(null)`, 음수·짧은 배열·최대 초과 값 처리
- [ ] `GemReward(1, 4, false) == 4`, `GemReward(3, 10, true) == 30`
- [ ] `CreateSettings`가 업그레이드와 스테이지를 반영
- [ ] 기존 테스트 81개 통과 유지

## 참고

- [BattleSimulation.cs](../../Assets/_Project/Scripts/Core/Battle/BattleSimulation.cs), [BattleSettings.cs](../../Assets/_Project/Scripts/Core/Battle/BattleSettings.cs), [WaveGenerator.cs](../../Assets/_Project/Scripts/Core/Battle/WaveGenerator.cs)
