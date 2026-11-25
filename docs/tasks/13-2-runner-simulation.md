# 13-2-runner-simulation: 러너 트랙·적 무리·보스전 시뮬레이션 (Core)

- **이슈:** #13
- **브랜치:** `feature/13-runner-stage`
- **작성:** Claude / 2026-11-26
- **순서:** 러너 지시서 4개 중 2번째. **13-1이 끝난 뒤** 진행한다. 로직이 얽혀 있으니 **high**.

## 목표

러너 한 판을 순수 C#으로 시뮬레이션한다: 무리가 전진하며 게이트를 지나고, 적 무리를 쏘고, 끝에서 보스와 싸워 승패가 난다. 트랙은 stage와 seed로 재현 가능하게 생성한다. 화면은 만들지 않는다.

## 배경

- 기획: [README.md](../../README.md) ⑦ 러너 보너스 스테이지.
- 13-1의 `Squad`, `Gate`를 사용한다. 상성은 `CounterTable.DamageMultiplier`, 적 체력 배율은 `StageRules.EnemyHpScale(stage)`.
- 기존 `BattleSimulation`(Tick·이벤트·State 패턴)과 `BattleSettings`(설정 클래스 패턴)를 참고한다.
- 다음: 13-3 러너 화면(Game), 13-4 스테이지 흐름·보상.

## 좌표

- 트랙은 앞으로 뻗은 직선. **`Distance`**(시작 0에서 앞으로 증가)와 **`X`**(좌우, `-HalfWidth`~`+HalfWidth`)만 쓴다.
- 게이트는 좌·우 두 칸: `X < 0`이면 왼쪽, `X >= 0`이면 오른쪽.
- 적 무리와 보스는 트랙 폭 전체를 막는다 (좌우로 피할 수 없음).

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Core/Runner/RunnerSettings.cs` | 추가 | 수치 설정 |
| `Core/Runner/GateRow.cs` | 추가 | 게이트 한 줄 (좌·우) |
| `Core/Runner/EnemyGroup.cs` | 추가 | 길 중간 적 무리 (런타임 체력 포함) |
| `Core/Runner/RunnerBoss.cs` | 추가 | 끝 보스 (런타임 체력 포함) |
| `Core/Runner/RunnerTrack.cs` | 추가 | 게이트·적·보스 배치 |
| `Core/Runner/RunnerTrackGenerator.cs` | 추가 | stage·seed로 트랙 생성 |
| `Core/Runner/RunnerState.cs` | 추가 | enum |
| `Core/Runner/RunnerSimulation.cs` | 추가 | 한 판 진행 |
| `Tests/EditMode/RunnerSimulationTests.cs` | 추가 | 테스트 |

경로는 `Assets/_Project/Scripts/` (테스트는 `Assets/_Project/Tests/`). 네임스페이스 `TopWar.Core.Runner`.

## 상세 요구사항

### 1. `RunnerSettings` (sealed class, 프로퍼티 기본값)

```csharp
public float ForwardSpeed { get; set; } = 6f;        // 초당 전진 거리
public float HalfWidth { get; set; } = 2f;           // X 범위
public float FireRange { get; set; } = 12f;          // 이 거리 안의 적·보스를 쏜다
public float FireInterval { get; set; } = 0.25f;     // 사격 간격(초)
public float BossStopGap { get; set; } = 8f;         // 보스 앞 이 거리에서 멈추고 보스전 시작
public float BossTimeLimit { get; set; } = 20f;
public float BossAttackInterval { get; set; } = 2f;  // 보스가 아군을 줄이는 간격
public int StartCount { get; set; } = 5;             // 시작 Lv.1 유닛 수
public UnitType StartType { get; set; } = UnitType.Army;
public float UnitBaseAttack { get; set; } = 10f;
public float ArmyAttackMultiplier { get; set; } = 1f;
public float AirForceAttackMultiplier { get; set; } = 1f;
public float NavyAttackMultiplier { get; set; } = 1f;
public float GetUnitAttackMultiplier(UnitType type);  // BattleSettings와 같은 방식
```

### 2. 트랙 데이터

```csharp
public sealed class GateRow
{
    public GateRow(float distance, Gate left, Gate right);
    public float Distance { get; }
    public Gate Left { get; }
    public Gate Right { get; }
    public bool Passed { get; internal set; }
}

public sealed class EnemyGroup
{
    public EnemyGroup(float distance, UnitType type, int count, float hpPerEnemy); // count < 1, hp <= 0 → 예외
    public float Distance { get; }
    public UnitType Type { get; }
    public float HpPerEnemy { get; }
    public float MaxHp { get; }                 // count × hpPerEnemy
    public float Hp { get; }                    // 남은 체력 (0 이상)
    public int RemainingCount { get; }          // Ceiling(Hp / HpPerEnemy), Hp가 0이면 0
    public bool IsAlive => Hp > 0;
    internal void TakeDamage(float damage);     // 0 아래로 내려가지 않음
    internal void Clear();                      // Hp = 0 (충돌 처리용)
}

public sealed class RunnerBoss
{
    public RunnerBoss(float distance, UnitType type, float maxHp, int killsPerAttack); // 잘못된 값 → 예외
    public float Distance { get; }
    public UnitType Type { get; }
    public float MaxHp { get; }
    public float Hp { get; }
    public int KillsPerAttack { get; }          // 공격 1회에 줄이는 아군 수
    public bool IsAlive => Hp > 0;
    internal void TakeDamage(float damage);
}

public sealed class RunnerTrack
{
    public RunnerTrack(IReadOnlyList<GateRow> gates, IReadOnlyList<EnemyGroup> enemies, RunnerBoss boss);
    // null → 예외. gates·enemies는 Distance 오름차순으로 정렬해 보관한다.
    public IReadOnlyList<GateRow> Gates { get; }
    public IReadOnlyList<EnemyGroup> Enemies { get; }
    public RunnerBoss Boss { get; }
    public float Length => Boss.Distance;
}
```

### 3. `RunnerTrackGenerator` (static)

`public static RunnerTrack Create(int stage, int seed)` — stage < 1이면 예외. `new System.Random(seed)`로 같은 입력이면 같은 트랙.

배치 (거리 고정):

| 거리 | 내용 |
| --- | --- |
| 20, 40 | 게이트 |
| 60 | 적 무리 #0 |
| 80, 100 | 게이트 |
| 120 | 적 무리 #1 |
| 140 | 게이트 |
| 160 | 적 무리 #2 |
| 200 | 보스 |

- `scale = StageRules.EnemyHpScale(stage)`
- **게이트 한 줄:** 아래 후보 중 서로 다른 종류 2개를 뽑아 좌·우에 랜덤 배치한다. **둘 다 `Subtract`인 줄은 없다** (종류가 다르므로 자동).
  - `Add(random 3~10)`, `Multiply(random 2~3)`, `Subtract(random 2~6)`, `Merge()`, `ChangeType(random 병종)`
- **적 무리 #i:** 병종 랜덤, `count = 10 + 10 × i`, `hpPerEnemy = 10 × scale`.
- **보스:** 병종 랜덤, `maxHp = 2000 × scale`, `killsPerAttack = 3`.

### 4. `RunnerState` (enum)

`Running`, `BossFight`, `Victory`, `Defeat`

### 5. `RunnerSimulation` (sealed class)

```csharp
public RunnerSimulation(Squad squad, RunnerTrack track, RunnerSettings settings); // null → 예외

public Squad Squad { get; }
public RunnerTrack Track { get; }
public RunnerState State { get; }          // 시작 Running
public float Distance { get; }             // 시작 0
public float X { get; }                    // 시작 0
public float Progress { get; }             // Clamp(Distance / Track.Length, 0, 1). 보스전 중·승리 시 1
public float BossTimeRemaining { get; }    // 보스전 전에는 BossTimeLimit

public void SetX(float x);                 // -HalfWidth~HalfWidth로 Clamp. Running일 때만 반영
public void Tick(float deltaTime);         // deltaTime < 0 또는 NaN → ArgumentOutOfRangeException. Victory·Defeat면 무시

public event Action<GateRow, Gate, bool> GatePassed;        // 줄, 적용된 게이트, 왼쪽이면 true
public event Action<EnemyGroup, float> EnemyGroupHit;       // 대상, 피해
public event Action<EnemyGroup> EnemyGroupDestroyed;        // 사격으로 체력 0
public event Action<EnemyGroup, int> EnemyGroupCollided;    // 닿음, 잃은 아군 수
public event Action BossFightStarted;
public event Action<float> BossHit;                         // 피해
public event Action<int> BossAttacked;                      // 잃은 아군 수
public event Action<bool> Finished;                         // 승리면 true. 한 번만
```

**Tick 순서 (Running):**

1. `Distance += ForwardSpeed × dt`. 단 `Track.Length - BossStopGap`을 넘지 않는다.
2. **게이트:** `Passed`가 아니고 `Distance >= row.Distance`인 줄마다 (거리순) `X`로 좌·우를 골라 `ApplyTo(Squad)`, `Passed = true`, `GatePassed`.
3. **충돌:** 살아 있고 `Distance >= group.Distance`인 적 무리마다 `lost = Min(RemainingCount, Squad.Count)`, `lost > 0`이면 `Squad.Remove(lost)`, `group.Clear()`, `EnemyGroupCollided(group, lost)`.
4. **사격:** 아래 "사격" 규칙.
5. 거리가 `Track.Length - BossStopGap`에 도달했으면 `State = BossFight`, `BossFightStarted`.
6. `Squad.IsDefeated`면 패배 처리.

**사격 (Running·BossFight 공통):**

- 사격 타이머를 `dt`만큼 진행하고, `FireInterval`이 찰 때마다 1회 사격 (큰 dt면 여러 번).
- 대상: Running이면 `0 < group.Distance - Distance <= FireRange`인 살아 있는 적 무리 중 가장 가까운 것. BossFight면 보스. 대상이 없으면 그 사격은 버린다 (타이머만 소모).
- 피해 = `Squad.Firepower(UnitBaseAttack, GetUnitAttackMultiplier(Squad.Type)) × FireInterval × CounterTable.DamageMultiplier(Squad.Type, 대상.Type)`. (Firepower는 초당 피해로 본다.)
- 적 무리: `TakeDamage` → `EnemyGroupHit` → 체력 0이면 `EnemyGroupDestroyed`.
- 보스: `TakeDamage` → `BossHit` → 체력 0이면 승리 처리.

**BossFight:**

1. `BossTimeRemaining -= dt`.
2. 사격.
3. 보스 공격 타이머가 `BossAttackInterval`마다: `lost = Min(KillsPerAttack, Squad.Count)`, `lost > 0`일 때만 `Squad.Remove(lost)`, `BossAttacked(lost)`.
4. 보스가 살아 있고 `BossTimeRemaining <= 0`이거나 `Squad.IsDefeated`면 패배 처리.

**승리·패배 처리:** `State = Victory/Defeat`, `Finished(victory)` 1회. 이후 Tick은 아무것도 하지 않는다. 같은 Tick 안에서 보스 체력이 0이 되면 그 뒤의 보스 공격·시간 초과는 처리하지 않는다.

## 테스트 (`RunnerSimulationTests`)

손으로 만든 작은 트랙으로 검증한다 (생성기 테스트만 생성기 사용).

- 생성자 null 예외, Tick 음수·NaN 예외
- 전진: `Tick(1)` → `Distance == 6`, `Progress` 계산
- `SetX` Clamp, X < 0 → 왼쪽 게이트 / X >= 0 → 오른쪽 게이트 적용, `GatePassed` 1회, 다시 Tick해도 재적용 없음
- 큰 dt 한 번에 게이트 두 줄을 넘으면 거리순으로 둘 다 적용
- 사격: 사거리 밖이면 피해 없음, 사거리 안이면 피해 = 공식대로 (상성 유리 1.25배 확인), `EnemyGroupDestroyed`
- `RemainingCount`가 체력에 따라 올림으로 줄어듦
- 충돌: 남은 적 수만큼 아군 감소, 아군이 더 적으면 전멸 → `Defeat`, `Finished(false)` 1회
- 보스전 진입 위치(`Length - BossStopGap`)와 `BossFightStarted`, 이후 전진 없음
- 보스 처치 → `Victory`, `Finished(true)`, 이후 Tick 무시
- 보스 시간 초과 → `Defeat`
- 보스 공격 간격마다 `KillsPerAttack`만큼 감소
- 생성기: 같은 stage·seed → 같은 트랙(게이트 종류·값, 적 병종·수, 보스), 배치 거리 표, 한 줄의 두 게이트 종류가 다름, stage가 오르면 적·보스 체력 증가, stage 0 예외

## 하지 말 것

- 기존 Core 클래스 수정 금지 (13-1의 `Squad`·`Gate` 포함. 필요하면 멈추고 보고).
- `UnityEngine` 참조 금지.
- Game 코드, 보상 계산, 스테이지 흐름은 만들지 않는다 (13-3, 13-4).

## 완료 조건

- [ ] 위 테스트 전부 통과, 기존 EditMode 테스트도 통과

## 참고

- [Squad.cs](../../Assets/_Project/Scripts/Core/Runner/Squad.cs), [Gate.cs](../../Assets/_Project/Scripts/Core/Runner/Gate.cs), [BattleSimulation.cs](../../Assets/_Project/Scripts/Core/Battle/BattleSimulation.cs), [BattleSettings.cs](../../Assets/_Project/Scripts/Core/Battle/BattleSettings.cs), [StageRules.cs](../../Assets/_Project/Scripts/Core/Progression/StageRules.cs)
