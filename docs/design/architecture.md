# 상세 설계: 아키텍처·성능

- **작성:** Claude / 2026-11-26
- 관련: [rules.md](rules.md) (규칙·수치), [presentation.md](presentation.md) (화면·연출·에셋), [stage-1.md](stage-1.md) (스테이지 1)

---

## 1. 전체 구조

```text
Core (순수 C#, TopWar.Core.asmdef)                Game (MonoBehaviour, TopWar.Game.asmdef)
┌──────────────────────────────────┐              ┌───────────────────────────────────────┐
│ Runner/                          │   Tick(dt)   │ GameBootstrap  로비 → 러너 → 결과       │
│  RunnerSimulation  ◄─────────────┼──────────────┤ RunnerLoop     고정 틱, 입력 전달        │
│   Squad (지휘관+병사, 대형)        │   입력 x      │ RunnerInput    드래그·키 → 목표 x        │
│   BulletPool                     │              │ Views/                                 │
│   EnemyField, Boss               │   읽기 전용    │  ICrowdRenderer (Squad, Enemy)         │
│   Crates, Gates                  ├──────────────► │  BulletRenderer (인스턴싱)              │
│   SpatialBuckets (충돌)           │   상태 배열    │  CrateView, GateView, BossView         │
│   RunnerEvents (이벤트 큐)        ├──────────────► │ Fx/ (파편, 섬광, 링, 숫자, 팝업, 흔들림)  │
│ Stages/ StageLayout, Stage1       │   이벤트      │ UI/  RunnerHud, PauseMenu             │
│ Units/ UnitType, CounterTable    │              │ Config/ RunnerConfig (SO)              │
└──────────────────────────────────┘              └───────────────────────────────────────┘
```

- Core는 `UnityEngine`을 참조하지 않는다. 좌표는 `float x, z`.
- Game은 Core 상태를 **매 프레임 읽어서 그리기만** 한다. Core 상태를 바꾸는 것은 `RunnerSimulation`의 메서드(입력)뿐이다.
- Core → Game 알림은 C# 이벤트가 아니라 **구조체 이벤트 큐**로 한다 (수천 발 피격에서 할당·델리게이트 비용 없음).

## 2. 시뮬레이션 루프

- 고정 틱 `1/60초`. `RunnerLoop.Update`가 `Time.deltaTime`을 누적해 틱을 돌린다. 한 프레임 최대 4틱(느린 기기에서 폭주 방지).
- 화면은 마지막 틱 상태를 그대로 그린다 (60fps 기준 보간 불필요. R2에서 필요하면 보간 추가).
- 틱 순서:

```text
1. 지휘관: 전진(z += speed·dt), 목표 x로 이동(최대 속도 제한), 도로 폭 클램프
2. 병사: 대형 슬롯을 향해 스프링 이동
3. 적·보스: 이동
4. 사격: 사격 타이머가 다 된 병사·지휘관이 총알 생성
5. 총알: 이동 → 버킷으로 충돌 검사 → 피해 → 사라짐/사거리 초과 제거
6. 접촉: 적 ↔ 무리, 게이트 통과, 지나친 캡슐 제거
7. 승패 판정
```

## 3. 데이터 구조 (Core)

모두 `struct` 배열 + 개수. 삭제는 마지막 원소와 바꿔치기(swap-remove). 각 개체는 화면 연결용 `Id`(증가하는 int)를 가진다.

```csharp
public struct Soldier   { int Id; UnitType Type; int Level; float X, Z; float VX, VZ; float FireTimer; int Slot; }
public struct Enemy     { int Id; UnitType Type; EnemyKind Kind; float X, Z; float Hp, MaxHp; float Speed; float FlashTimer; }
public struct Bullet    { float X, Z; float Damage; UnitType Type; float Traveled; byte Tier; }
public struct Crate     { int Id; float X, Z; float Gauge, MaxGauge; CrateReward Reward; bool Big; }
public struct Gate      { int Id; float XMin, XMax, Z; GateKind Kind; int Value; int PairId; bool Used; }
public struct CrateReward { RewardKind Kind; UnitType Type; int Count; int Level; }  // Soldiers / Weapon / LevelUp
public struct RunnerEvent { EventKind Kind; int Id; float X, Z; float Value; UnitType Type; byte Flags; }
```

주요 클래스:

| 클래스 | 책임 | 주요 메서드 |
| --- | --- | --- |
| `RunnerSimulation` | 전체 상태·틱·승패 | `Tick(float dt)`, `SetTargetX(float x)`, `State`, `Events` |
| `RunnerSettings` | 모든 수치 (Game의 `RunnerConfig` SO에서 채움) | - |
| `Squad` | 지휘관·병사 목록, 대형, 추가·제거·합성·레벨업 | `Add(type, level, count, fromX, fromZ)`, `ApplyDamage(power)`, `Merge()`, `LevelUp(n)`, `Multiply(k)` |
| `Formation` | 슬롯 i의 상대 좌표 (해바라기 나선) | `SlotOffset(int i, out float dx, out float dz)` |
| `BulletPool` | 총알 배열 (용량 4096) | `Spawn(...)`, `Update(dt, targets)` |
| `EnemyField` | 적 목록, 스테이지 배치에서 생성 | `SpawnUpTo(z)`, `Update(dt)` |
| `SpatialBuckets` | z축 1m 버킷에 적·캡슐·보스 등록, 총알이 자기 버킷만 검사 | `Clear()`, `Insert(...)`, `Query(z, x, r)` |
| `StageLayout` | 거리별 배치 목록 (캡슐, 게이트, 적 무리, 보스) | `Placements` (z 순 정렬) |
| `RunnerEvents` | 틱 동안 쌓인 이벤트, Game이 읽고 비움 | `Push(in RunnerEvent)`, `Span`, `Clear()` |

- 배치는 지휘관 앞 40m 안에 들어올 때 생성한다 (스테이지 전체를 한 번에 만들지 않음).
- 지휘관 뒤 8m보다 뒤로 간 적·캡슐·게이트는 제거한다.

## 4. 충돌

- 모든 판정은 원(반경) 또는 x 범위. 물리 엔진(Collider) 쓰지 않는다.
- **총알 → 대상:** 총알은 직선으로 +z 이동. 틱마다 총알이 속한 z 버킷(1m)과 이전 위치 버킷을 검사해 x 거리 < 대상 반경인 **첫 대상**에 맞는다 (터널링 방지: 총알 속도 35m/s × 1/60초 = 0.58m < 1m 버킷).
- **적 → 무리:** 적과 지휘관 사이 거리 < `무리 반경 + 적 반경`이면 접촉. 무리 반경 = 가장 바깥 슬롯 거리.
- **게이트:** 지휘관 z가 게이트 z를 넘는 틱에 지휘관 x가 `[XMin, XMax]` 안이면 적용. 같은 `PairId`의 다른 게이트는 사용 처리.
- 비용: 총알 2000 × 버킷당 대상 평균 3 = 6000 검사/틱. 충분히 싸다.

## 5. 렌더링과 성능

### 5.1 성능 예산 (중급 안드로이드, 60fps = 16.6ms)

| 항목 | 예산 |
| --- | --- |
| Core 시뮬레이션 (R2 스트레스: 캐릭터 400, 총알 2000) | 3ms 이하 |
| 렌더링 (CPU) | 6ms 이하 |
| 드로우콜 | 150 이하 |
| 삼각형 | 30만 이하 (캐릭터 1개 1.5k 이하) |
| 매 프레임 GC 할당 | 0 바이트 (풀링, 구조체 이벤트, 문자열 캐시) |
| 그림자 | 지휘관·보스·정예만 그림자 투사. 일반 병사·적은 바닥 원형 블롭 그림자(인스턴싱) |

### 5.2 단계별 렌더링

| 대상 | R1 | R2 |
| --- | --- | --- |
| 병사·적 | 풀링한 프리팹 + `Animator`(Run/Shoot/Death). `SkinnedMeshRenderer`, `Animator.cullingMode = CullUpdateTransforms` | **VAT**: 애니메이션을 텍스처로 구워 `Graphics.RenderMeshInstanced`. 인스턴스 데이터 = 위치·크기·색·애니 시간·플래시 |
| 지휘관·보스·정예 | 프리팹 + Animator (그대로) | 그대로 |
| 총알 | `Graphics.RenderMeshInstanced` (1023개씩), 발광 머티리얼 + URP Bloom | 그대로 |
| 블롭 그림자 | 인스턴싱 | 그대로 |
| 피해 숫자 | TextMeshPro 풀 64개 | 그대로 |

- 병사·적 그리기는 `ICrowdRenderer` 인터페이스 뒤에 둔다. R2에서 구현만 바꾼다.

```csharp
public interface ICrowdRenderer
{
    void Sync(ReadOnlySpan<Soldier> soldiers);   // Id로 인스턴스 매칭, 위치·애니 갱신
    void Flash(int id);
    void PlayDeath(int id, float x, float z);
}
```

- URP 설정: MSAA 끔(모바일), Bloom 켬(낮은 강도), 그림자 거리 30m, Shadow Cascade 1, HDR 켬.

## 6. 입력

- Input System. 터치/마우스 **상대 드래그**: 누른 지점 대비 화면 x 이동량 × 감도 → 목표 x. 손을 떼도 목표 x 유지.
  - 감도: 화면 폭 전체 드래그 = 도로 폭 × 1.2.
- 키보드 `←→` `A D`: 목표 x를 초당 8m 이동.
- 지휘관은 목표 x로 최대 12m/s로 따라간다 (순간이동 없음).

## 7. 테스트 (EditMode, Core)

| 대상 | 테스트 |
| --- | --- |
| `Formation` | 슬롯 간 최소 거리 ≥ 간격 × 0.9, 슬롯 0 = 중심 |
| `Squad.Add` | 병사 수 증가, 새 병사는 지정 위치에서 시작 |
| `Squad.ApplyDamage` | 낮은 레벨부터 제거, 남은 피해 < 다음 병사 체력이면 레벨 하락, 병사 0이면 무기 단계 하락, 무기 1단계면 패배 |
| `Squad.Merge` | 같은 병종·레벨 2명 → 레벨+1 1명, 홀수 1명 남음, 최대 레벨 10 유지 |
| `Squad.Multiply` | 수 ×k, 상한 300 |
| `BulletPool` | 사거리 초과 시 제거, 첫 대상에만 피해, 용량 초과 시 생성 무시 |
| 충돌 | 빠른 총알이 대상을 건너뛰지 않음 |
| 상성 | 유리 ×1.25, 불리 ×0.85 |
| 캡슐 | 게이지 0 → 보상 이벤트 1회, 지나치면 보상 없음 |
| 게이트 | 범위 안에서만 적용, 짝 게이트는 하나만 적용 |
| 보스 | 제한 시간 초과 → 패배, 체력 0 → 승리 |
| 결정성 | 같은 입력 시퀀스 → 같은 결과 (난수 seed 고정) |

## 8. 파일 배치

```text
Assets/_Project/Scripts/Core/Runner/      RunnerSimulation.cs, RunnerSettings.cs, Squad.cs, Formation.cs,
                                          BulletPool.cs, EnemyField.cs, Boss.cs, Crates.cs, Gates.cs,
                                          SpatialBuckets.cs, RunnerEvents.cs, Types.cs
Assets/_Project/Scripts/Core/Runner/Stages/  StageLayout.cs, Stage1.cs
Assets/_Project/Scripts/Game/Runner/      RunnerLoop.cs, RunnerInput.cs, RunnerCamera.cs
Assets/_Project/Scripts/Game/Runner/Views/   AnimatorCrowdRenderer.cs, BulletRenderer.cs, CrateView.cs, GateView.cs, BossView.cs, Environment.cs
Assets/_Project/Scripts/Game/Runner/Fx/      FxPool.cs, DamageNumbers.cs, Popups.cs, CameraShake.cs
Assets/_Project/Scripts/Game/Runner/UI/      RunnerHud.cs, PauseMenu.cs
Assets/_Project/Scripts/Game/Config/      RunnerConfig.cs (ScriptableObject)
Assets/_Project/Tests/EditMode/Runner/    *Tests.cs
```
