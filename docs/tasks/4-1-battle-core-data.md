# 4-1-battle-core-data: 전투 데이터와 웨이브 생성 규칙 (Core)

- **이슈:** #4
- **브랜치:** `feature/4-wave-combat`
- **작성:** Claude / 2026-11-25
- **순서:** Phase 2 지시서 3개 중 1번째. 이 작업이 끝나야 4-2, 4-3을 진행한다.

## 목표

적, 기지 내구도, 유닛 전투 능력치, 웨이브 구성을 순수 C#으로 정의한다. 아직 시뮬레이션(시간 진행)은 만들지 않는다.

## 배경

- README "4. 주요 시스템 명세"의 ①전장 구조, ④병종 및 상성, ⑤웨이브 및 보스를 구현하는 첫 단계.
- 좌표계는 **그리드 칸 단위**를 쓴다. 기존 `GridPos`와 같다: X = 열(레인, 0 ~ Width-1), Y = 행(0이 기지 쪽, 위로 갈수록 적 쪽).
- 적은 `Y = Height - 1 + SpawnDistance`에서 생겨 Y가 줄어드는 방향(아래)으로 이동하고, 그리드를 **통과**해서 `Y <= BaseY`에 닿으면 기지를 공격한다. 아군 유닛은 길을 막지 않는다.
- 아군 유닛은 체력이 없다 (적은 기지만 공격한다).

## 변경할 파일

모두 `Assets/_Project/Scripts/Core/Battle/` 아래에 새로 만든다. 네임스페이스 `TopWar.Core.Battle`.

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `BattleSettings.cs` | 추가 | 전투 밸런스 수치 모음 (기본값 포함) |
| `EnemyStats.cs` | 추가 | 적 한 종류의 능력치 (불변) |
| `Enemy.cs` | 추가 | 전장에 있는 적 하나의 런타임 상태 |
| `CombatProfile.cs` | 추가 | 아군 병종별 사거리·공격 간격·배율·광역 반경 |
| `BaseHealth.cs` | 추가 | 기지 내구도 |
| `WaveSpawn.cs` | 추가 | 웨이브 안 적 하나의 출현 정보 |
| `WavePlan.cs` | 추가 | 한 웨이브의 구성 |
| `WaveGenerator.cs` | 추가 | 웨이브 번호로 WavePlan 생성 |
| `Tests/EditMode/BattleDataTests.cs` | 추가 | 위 클래스 테스트 |
| `Tests/EditMode/WaveGeneratorTests.cs` | 추가 | 웨이브 생성 테스트 |

## 상세 요구사항

### 1. `BattleSettings` (sealed class)

public 프로퍼티 `{ get; set; }`, 아래 기본값으로 초기화. 생성자 인자 없음.

| 프로퍼티 | 타입 | 기본값 | 의미 |
| --- | --- | --- | --- |
| `SpawnDistance` | float | 6 | 그리드 맨 윗줄에서 적 출현 지점까지 칸 수 |
| `BaseY` | float | -1 | 적 Y가 이 값 이하가 되면 기지 도달 |
| `BaseMaxHealth` | int | 100 | 기지 최대 내구도 |
| `UnitBaseAttack` | float | 10 | Lv.1 유닛 기준 공격력 (병종 배율 곱하기 전) |
| `BreakDuration` | float | 5 | 웨이브 사이 대기 시간(초). 첫 웨이브 전에도 적용 |
| `WaveClearBonusBase` | int | 20 | 웨이브 클리어 보너스 = Base + PerWave × 웨이브번호 |
| `WaveClearBonusPerWave` | int | 5 | |
| `EnemyCountBase` | int | 6 | 일반 웨이브 적 수 = Base + PerWave × (n-1) |
| `EnemyCountPerWave` | int | 2 | |
| `SpawnIntervalStart` | float | 1.2 | 적 출현 간격(초) = max(Min, Start - Step × (n-1)) |
| `SpawnIntervalStep` | float | 0.05 | |
| `SpawnIntervalMin` | float | 0.4 | |
| `EnemyHpGrowth` | float | 1.18 | 적 체력 배율 = Growth^(n-1) |
| `BossEvery` | int | 5 | n % BossEvery == 0 이면 보스 웨이브 |
| `BossHpMultiplier` | float | 25 | 보스 체력 = 그 웨이브 해당 병종 일반 적 체력 × 이 값 |
| `BossSpeed` | float | 0.3 | 칸/초 |
| `BossReward` | int | 50 | |
| `BossTimeLimit` | float | 60 | 보스 출현 후 제한 시간(초) |
| `BossBaseDamagePercent` | float | 0.5 | 보스 시간 초과 또는 기지 도달 시 최대 내구도 대비 피해 비율 |

일반 적 기본 능력치(웨이브 1 기준)는 `public EnemyStats GetEnemyBaseStats(UnitType type)` 메서드로 제공한다:

| 병종 | MaxHp | Speed (칸/초) | BaseDamage | Reward |
| --- | --- | --- | --- | --- |
| Army | 30 | 0.6 | 5 | 5 |
| AirForce | 20 | 0.9 | 5 | 5 |
| Navy | 45 | 0.45 | 8 | 7 |

### 2. `EnemyStats` (readonly struct)

```csharp
public EnemyStats(float maxHp, float speed, int baseDamage, int reward, bool isBoss)
public float MaxHp { get; }
public float Speed { get; }
public int BaseDamage { get; }
public int Reward { get; }
public bool IsBoss { get; }
public EnemyStats WithHpMultiplier(float multiplier) // MaxHp만 곱한 새 값
```

- `maxHp <= 0`, `speed < 0`, `baseDamage < 0`, `reward < 0`이면 `ArgumentOutOfRangeException`.

### 3. `Enemy` (sealed class)

```csharp
public Enemy(int id, UnitType type, EnemyStats stats, int lane, float y)
public int Id { get; }
public UnitType Type { get; }
public EnemyStats Stats { get; }
public int Lane { get; }
public float Y { get; set; }       // 시뮬레이션이 갱신
public float Hp { get; private set; } // 시작값 = stats.MaxHp
public bool IsAlive => Hp > 0;
public float HpRatio => Hp / Stats.MaxHp;
public bool TakeDamage(float amount) // amount < 0이면 ArgumentOutOfRangeException. 이번 피해로 죽었으면 true (이미 죽어 있으면 false). Hp는 0 아래로 내려가지 않는다.
```

### 4. `CombatProfile` (readonly struct) + 병종별 값

```csharp
public CombatProfile(float range, float attackInterval, float attackMultiplier, float splashRadius)
public float Range { get; }          // 칸 단위 거리
public float AttackInterval { get; } // 초
public float AttackMultiplier { get; } // UnitBaseAttack에 곱한다
public float SplashRadius { get; }   // 0이면 단일 대상
public static CombatProfile For(UnitType type)
```

| 병종 | Range | AttackInterval | AttackMultiplier | SplashRadius | README 근거 |
| --- | --- | --- | --- | --- | --- |
| Army | 3 | 1.0 | 1.0 | 0 | 안정적인 화력 |
| AirForce | 2.5 | 0.5 | 0.45 | 0.8 | 빠른 공격 + 광역 |
| Navy | 5 | 2.0 | 2.6 | 0 | 긴 사거리 + 단일 폭딜 |

추가로 `public float AttackPower(float unitBaseAttack, int level)` = `UnitStats.AttackAtLevel(unitBaseAttack * AttackMultiplier, level)`.

### 5. `BaseHealth` (sealed class)

```csharp
public BaseHealth(int max)            // max <= 0이면 ArgumentOutOfRangeException
public int Max { get; }
public int Current { get; private set; }
public bool IsDestroyed => Current <= 0;
public event Action Changed;
public void TakeDamage(int amount)    // amount < 0 예외. 0 아래로 내려가지 않음. 값이 바뀌면 Changed
public void TakePercentOfMax(float ratio) // ratio는 0~1 (범위 밖 예외). 피해 = Ceiling(Max × ratio)
```

### 6. `WaveSpawn` (readonly struct)

```csharp
public WaveSpawn(float time, UnitType type, int lane, EnemyStats stats)
public float Time { get; }   // 웨이브 시작 후 출현 시각(초)
public UnitType Type { get; }
public int Lane { get; }
public EnemyStats Stats { get; }
```

### 7. `WavePlan` (sealed class)

```csharp
public WavePlan(int number, IReadOnlyList<WaveSpawn> spawns)
public int Number { get; }
public IReadOnlyList<WaveSpawn> Spawns { get; } // Time 오름차순
public bool IsBoss { get; }  // spawns 중 Stats.IsBoss가 하나라도 있으면 true
```

### 8. `WaveGenerator` (sealed class)

```csharp
public WaveGenerator(BattleSettings settings, Random random = null) // System.Random. null이면 new Random()
public WavePlan Create(int waveNumber, int laneCount) // waveNumber < 1 또는 laneCount < 1이면 ArgumentOutOfRangeException
```

규칙 (n = waveNumber):

- **일반 웨이브:** 적 수 `EnemyCountBase + EnemyCountPerWave × (n-1)`. i번째 적(0부터)의 `Time = i × interval`, interval은 위 공식. 병종은 Army/AirForce/Navy 중 무작위, 레인은 0 ~ laneCount-1 중 무작위. 능력치는 `GetEnemyBaseStats(type).WithHpMultiplier(EnemyHpGrowth^(n-1))`.
- **보스 웨이브 (n % BossEvery == 0):** 일반 웨이브 적 수의 **절반(내림)** 만큼 일반 적을 같은 규칙으로 만들고, 마지막 일반 적 다음 interval 시각에 보스 1기를 추가한다.
  - 보스 병종: 보스 순번 k = n / BossEvery 일 때 `(UnitType)((k - 1) % 3)` (Army → AirForce → Navy 순환)
  - 보스 레인: `laneCount / 2` (가운데)
  - 보스 능력치: MaxHp = 그 병종 일반 적 체력(성장 배율 적용 후) × BossHpMultiplier, Speed = BossSpeed, BaseDamage = 0 (기지 피해는 BossBaseDamagePercent로 처리), Reward = BossReward, IsBoss = true
- 같은 seed의 `Random`이면 같은 결과가 나와야 한다.

## 하지 말 것

- 시간 진행(Tick), 이동, 공격 로직은 만들지 않는다 (4-2에서 한다).
- `Game` 폴더(MonoBehaviour)는 건드리지 않는다.
- 기존 `GridModel`, `UnitData`, `SummonService` 등의 public API를 바꾸지 않는다.
- `UnityEngine` 참조 금지 (Core).

## 완료 조건

EditMode 테스트로 검증:

- [ ] `EnemyStats` 잘못된 값이면 예외, `WithHpMultiplier`는 MaxHp만 바뀜
- [ ] `Enemy.TakeDamage`: 피해 누적, 죽는 순간에만 true, Hp 0 아래로 안 감, 음수 예외
- [ ] `CombatProfile.For` 세 병종 값이 표와 같음, `AttackPower`가 레벨당 2배
- [ ] `BaseHealth`: 피해, 0 하한, `TakePercentOfMax(0.5f)`가 Max 100에서 50 감소, `Changed` 발생
- [ ] `WaveGenerator` 웨이브 1: 적 6명, 간격 1.2초, 보스 없음, 레인 범위 안
- [ ] 웨이브 3: 적 10명, 체력이 기본 × 1.18²
- [ ] 웨이브 5: 일반 적 7명(14/2) + 마지막에 보스 1기, 보스는 Army, 가운데 레인, 체력 배율 적용
- [ ] 웨이브 10: 보스가 AirForce
- [ ] 같은 seed → 같은 결과

## 참고

- 기존 패턴: [GridModel.cs](../../Assets/_Project/Scripts/Core/Grid/GridModel.cs), [UnitData.cs](../../Assets/_Project/Scripts/Core/Units/UnitData.cs), [CounterTable.cs](../../Assets/_Project/Scripts/Core/Units/CounterTable.cs), [UnitStats.cs](../../Assets/_Project/Scripts/Core/Units/UnitStats.cs)
- 테스트 패턴: [GridModelTests.cs](../../Assets/_Project/Tests/EditMode/GridModelTests.cs)
