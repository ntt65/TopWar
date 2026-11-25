# 4-2-battle-simulation: 전투 시뮬레이션 (Core)

- **이슈:** #4
- **브랜치:** `feature/4-wave-combat`
- **작성:** Claude / 2026-11-25
- **순서:** Phase 2 지시서 3개 중 2번째. **4-1이 끝난 뒤** 진행한다.

## 목표

시간을 받아(Tick) 웨이브 진행, 적 출현·이동, 아군 자동 공격, 보상, 기지 피해, 보스 제한 시간, 게임 오버를 처리하는 `BattleSimulation`을 순수 C#으로 만든다. 화면 표시는 4-3에서 이 클래스의 이벤트와 상태를 읽어서 한다.

## 배경

- 4-1에서 만든 `BattleSettings`, `Enemy`, `EnemyStats`, `CombatProfile`, `BaseHealth`, `WavePlan`, `WaveGenerator`를 사용한다.
- 투사체는 **화면 연출일 뿐**이다. 피해는 발사 순간 즉시 적용한다 (hitscan). 4-3에서 `UnitFired` 이벤트를 받아 투사체를 그린다.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Assets/_Project/Scripts/Core/Battle/BattleState.cs` | 추가 | 상태 enum |
| `Assets/_Project/Scripts/Core/Battle/BattleSimulation.cs` | 추가 | 시뮬레이션 본체 |
| `Assets/_Project/Tests/EditMode/BattleSimulationTests.cs` | 추가 | 테스트 |

네임스페이스 `TopWar.Core.Battle`.

## 상세 요구사항

### 1. `BattleState` (enum)

```csharp
Break,     // 웨이브 사이 대기 (첫 웨이브 전 포함)
InWave,    // 웨이브 진행 중
GameOver,  // 기지 파괴
```

### 2. `BattleSimulation` (sealed class) — 공개 API

```csharp
public BattleSimulation(GridModel grid, Wallet wallet, BaseHealth baseHealth,
                        WaveGenerator waveGenerator, BattleSettings settings)
// 인자 null이면 ArgumentNullException

public BattleState State { get; }
public int WaveNumber { get; }               // 현재(또는 마지막으로 시작한) 웨이브. 시작 전 0
public bool IsBossWave { get; }              // 현재 웨이브가 보스 웨이브인지
public float BreakRemaining { get; }         // Break 상태일 때 남은 대기 시간, 아니면 0
public float? BossTimeRemaining { get; }     // 보스가 살아 있을 때만 값, 아니면 null
public IReadOnlyList<Enemy> Enemies { get; } // 살아 있는 적 (출현 순서)
public float SpawnY { get; }                 // grid.Height - 1 + settings.SpawnDistance

public event Action<int> WaveStarted;        // 웨이브 번호
public event Action<int, int> WaveCleared;   // 웨이브 번호, 받은 보너스 골드
public event Action<Enemy> EnemySpawned;
public event Action<Enemy> EnemyKilled;
public event Action<Enemy> EnemyReachedBase;
public event Action<Enemy> BossTimedOut;
public event Action<GridPos, Enemy, float> UnitFired; // 쏜 유닛 칸, 주 대상, 주 대상에게 준 피해
public event Action GameOver;

public void Tick(float deltaTime) // deltaTime < 0이면 ArgumentOutOfRangeException
```

생성 직후 상태: `State = Break`, `BreakRemaining = settings.BreakDuration`, `WaveNumber = 0`.

### 3. `Tick` 처리 순서

`State == GameOver`이면 아무것도 하지 않는다. 그 외에는 아래 순서로 처리한다.

1. **Break:** `BreakRemaining -= dt`. 0 이하가 되면 다음 웨이브 시작:
   - `WaveNumber++`, `waveGenerator.Create(WaveNumber, grid.Width)`로 계획을 받고 웨이브 경과 시간 0, `State = InWave`, `WaveStarted` 발생.
   - 같은 Tick에서 InWave 처리를 이어서 하지 않는다 (다음 Tick부터).
2. **InWave:**
   1. 웨이브 경과 시간 += dt. `Time <= 경과 시간`인 아직 안 나온 `WaveSpawn`을 모두 출현시킨다: `new Enemy(nextId++, spawn.Type, spawn.Stats, spawn.Lane, SpawnY)`를 목록 끝에 추가하고 `EnemySpawned`. 보스가 나오면 보스 타이머를 `BossTimeLimit`으로 시작한다.
   2. **이동:** 모든 적 `Y -= Stats.Speed × dt`.
   3. **기지 도달:** `Y <= settings.BaseY`인 적을 목록에서 제거하고 `EnemyReachedBase`. 일반 적은 `baseHealth.TakeDamage(Stats.BaseDamage)`, 보스는 `baseHealth.TakePercentOfMax(BossBaseDamagePercent)` 후 보스 타이머를 끈다.
   4. **보스 타이머:** 보스가 살아 있으면 남은 시간 -= dt. 0 이하가 되면 보스를 목록에서 제거하고 `BossTimedOut`, `baseHealth.TakePercentOfMax(BossBaseDamagePercent)`, 타이머를 끈다.
   5. **기지 파괴 확인:** `baseHealth.IsDestroyed`이면 `State = GameOver`, `GameOver` 발생 후 이번 Tick 종료.
   6. **아군 공격:** 아래 "공격 규칙".
   7. **웨이브 클리어:** 모든 `WaveSpawn`이 출현했고 `Enemies`가 비었으면: 보너스 = `WaveClearBonusBase + WaveClearBonusPerWave × WaveNumber`, `wallet.Add(보너스)`, `WaveCleared(WaveNumber, 보너스)`, `State = Break`, `BreakRemaining = BreakDuration`.

### 4. 공격 규칙

- 칸마다 쿨다운을 `Dictionary<GridPos, float>`로 관리한다. 유닛이 있는데 항목이 없으면 쿨다운 0(즉시 발사 가능). 매 Tick 시작 시 **유닛이 없는 칸의 항목은 지운다** (이동·합성으로 바뀐 칸 정리).
- `grid.Units()`의 각 유닛에 대해 (순서: Y 오름차순, 같은 Y면 X 오름차순 — 기지 쪽 유닛 먼저):
  1. 쿨다운 -= dt. 0보다 크면 넘어간다.
  2. `profile = CombatProfile.For(unit.Type)`. 사거리 안의 살아 있는 적 중 **Y가 가장 작은 적**(기지에 가장 가까운 적)을 고른다. 같으면 Id가 작은 적. 거리는 `sqrt((enemy.Lane - pos.X)² + (enemy.Y - pos.Y)²)`, `거리 <= Range`이면 사거리 안.
  3. 대상이 없으면 쿨다운을 0으로 두고 넘어간다 (적이 들어오면 바로 쏜다).
  4. 피해 = `profile.AttackPower(settings.UnitBaseAttack, unit.Level) × CounterTable.DamageMultiplier(unit.Type, target.Type)`. 대상에게 적용하고 `UnitFired(pos, target, 피해)`.
  5. `SplashRadius > 0`이면 대상 위치 기준 그 반경 안의 **다른** 살아 있는 적에게도 피해를 준다 (각 적의 병종으로 상성 배율을 다시 계산).
  6. 이번 공격으로 죽은 적마다: `wallet.Add(Stats.Reward)`, `EnemyKilled`, 목록에서 제거. 보스가 죽으면 보스 타이머를 끈다.
  7. 쿨다운 = `profile.AttackInterval`.
- 이벤트 핸들러가 목록을 바꿔도 안전하도록, 순회 중에는 목록을 직접 수정하지 말고 처리 후 제거한다.

### 5. 기타

- `Enemies`는 내부 `List<Enemy>`를 `IReadOnlyList`로 노출한다.
- `Update` 루프에서 매 프레임 호출되므로 `Tick` 안에서 LINQ와 불필요한 할당을 피한다 (재사용 리스트 사용).

## 하지 말 것

- `Game` 폴더(MonoBehaviour)는 건드리지 않는다 (4-3에서 한다).
- 4-1에서 만든 클래스의 public API를 바꾸지 않는다. 꼭 필요하면 멈추고 보고한다.
- 아군 유닛 체력, 보스 특수 패턴(방어막·소환)은 만들지 않는다 (Phase 4).

## 완료 조건

EditMode 테스트로 검증 (고정 seed `Random`과 직접 만든 설정 사용, 필요하면 `BattleSettings` 값을 테스트에서 바꾼다):

- [ ] 생성 직후 Break, `BreakDuration` 지난 뒤 Tick → InWave, WaveNumber 1, `WaveStarted(1)`
- [ ] 웨이브 시작 후 시간이 흐르면 `WaveSpawn.Time` 순서대로 출현, 적의 Y는 `SpawnY`에서 시작
- [ ] 적이 Speed만큼 내려가고, BaseY에 닿으면 기지 내구도가 BaseDamage만큼 줄고 목록에서 빠짐
- [ ] 사거리 안 적에게 공격, 사거리 밖이면 공격 안 함
- [ ] 여러 적 중 Y가 가장 작은 적을 노림
- [ ] 상성 배율 적용 (예: Army Lv.1 → AirForce 적 피해 12.5)
- [ ] 쿨다운: AttackInterval 안에는 다시 쏘지 않음
- [ ] AirForce 광역: 반경 안의 다른 적도 피해
- [ ] 적 처치 시 골드 Reward 증가, `EnemyKilled`
- [ ] 모든 적 처리 후 웨이브 클리어 보너스 지급, Break로 전환
- [ ] 보스 제한 시간 초과 → 보스 제거, 기지 50% 피해, `BossTimedOut`
- [ ] 기지 내구도 0 → GameOver, 이후 Tick은 아무 변화 없음
- [ ] 유닛을 옮긴(Move) 뒤에도 예외 없이 동작 (빈 칸 쿨다운 정리)

## 참고

- 4-1 지시서: [4-1-battle-core-data.md](4-1-battle-core-data.md)
- 그리드 API: [GridModel.cs](../../Assets/_Project/Scripts/Core/Grid/GridModel.cs) (`Units()`, `Width`, `Height`)
- 골드: [Wallet.cs](../../Assets/_Project/Scripts/Core/Economy/Wallet.cs)
