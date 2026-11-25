# 13-1-runner-squad-gates: 러너 무리와 게이트 규칙 (Core)

- **이슈:** #13
- **브랜치:** `feature/13-runner-stage`
- **작성:** Claude / 2026-11-26
- **순서:** 러너 지시서 4개 중 1번째. medium으로 충분.

## 목표

러너 스테이지의 무리(`Squad`)와 게이트(`Gate`) 규칙을 순수 C#으로 만들고 EditMode 테스트를 붙인다. 화면은 만들지 않는다.

## 배경

- 기획: [README.md](../../README.md) ⑦ 러너 보너스 스테이지.
- 레벨 공격력은 기존 `UnitStats.AttackAtLevel`(Lv마다 2배), 레벨 범위는 `GameRules.MinLevel`·`MaxLevel`(1~10)을 쓴다.
- 이후 지시서: 13-2 트랙·적 무리·보스 시뮬레이션(Core), 13-3 러너 화면·입력(Game), 13-4 스테이지 흐름·보상 연결.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Assets/_Project/Scripts/Core/Runner/Squad.cs` | 추가 | 무리 |
| `Assets/_Project/Scripts/Core/Runner/GateKind.cs` | 추가 | 게이트 종류 enum |
| `Assets/_Project/Scripts/Core/Runner/Gate.cs` | 추가 | 게이트 값과 적용 |
| `Assets/_Project/Tests/EditMode/RunnerSquadTests.cs` | 추가 | 테스트 |

네임스페이스는 `TopWar.Core.Runner`. 폴더 `.meta`는 Unity가 만든다.

## 상세 요구사항

### 1. `Squad` (sealed class)

레벨별 유닛 수를 가진다. 내부 저장은 `int[]` (인덱스 = 레벨, 1~10 사용).

```csharp
public const int MaxCount = 500;              // 전체 유닛 수 상한

public Squad(UnitType type, int initialCount) // Lv.1 유닛 initialCount기. initialCount < 1이면 ArgumentOutOfRangeException

public UnitType Type { get; }                 // ChangeType으로 바뀜
public int Count { get; }                     // 전체 유닛 수
public bool IsDefeated => Count == 0;
public int HighestLevel { get; }              // 유닛이 있는 가장 높은 레벨, 없으면 0
public int GetCount(int level);               // level이 1~10 밖이면 ArgumentOutOfRangeException

public event Action Changed;                  // 상태가 실제로 바뀐 경우에만 1회 호출

public void Add(int count);                   // Lv.1에 count 추가. count < 1이면 예외. 상한 적용
public void Multiply(int factor);             // 모든 레벨의 수 × factor. factor < 2면 예외. 상한 적용
public void Remove(int count);                // 낮은 레벨부터 count기 제거. count < 1이면 예외. 0 아래로 내려가지 않음
public void Merge();                          // 아래 규칙
public void ChangeType(UnitType type);        // 같은 병종이면 아무것도 안 함 (Changed 없음)
public float Firepower(float baseAttack, float attackMultiplier);
```

- **상한:** `Add`·`Multiply` 뒤 `Count > MaxCount`면 초과분을 **낮은 레벨부터** 제거한다 (`Remove`와 같은 순서). 이때 `Changed`는 1회만.
- **Merge (한 번만 합성):** 레벨 9부터 1까지 내려가며, 각 레벨에서 `pairs = count / 2`만큼 그 레벨에서 `2 × pairs`를 빼고 다음 레벨에 `pairs`를 더한다. 높은 레벨부터 처리하므로 이번에 새로 생긴 유닛은 같은 Merge에서 다시 합성되지 않는다. Lv.10은 합성하지 않는다. 바뀐 게 없으면 `Changed` 없음.
  - 예: Lv.1 ×5 → Lv.1 ×1, Lv.2 ×2
  - 예: Lv.1 ×4, Lv.2 ×1 → Lv.2 ×3 (Lv.2의 기존 1기는 짝이 없어 그대로, Lv.1 4기가 Lv.2 2기로)
- **Firepower:** `Σ GetCount(L) × UnitStats.AttackAtLevel(baseAttack, L) × attackMultiplier`. 상성은 여기서 적용하지 않는다 (13-2에서 대상별로 곱한다).

### 2. `GateKind` (enum)

`Add`, `Multiply`, `Subtract`, `Merge`, `ChangeType`

### 3. `Gate` (readonly struct)

```csharp
public GateKind Kind { get; }
public int Value { get; }          // Add·Subtract: 수량, Multiply: 배수, 나머지: 0
public UnitType UnitType { get; }  // ChangeType에서만 의미 있음

public static Gate Add(int count);          // count < 1이면 예외
public static Gate Multiply(int factor);    // factor < 2면 예외
public static Gate Subtract(int count);     // count < 1이면 예외
public static Gate Merge();
public static Gate ChangeType(UnitType type);

public void ApplyTo(Squad squad);           // squad null이면 ArgumentNullException. 종류별로 Squad 메서드 호출
```

생성자는 private. 화면 표시용 문구(`+5`, `x2` 등)는 Core에 두지 않는다 (Game의 `Texts`에서 13-3에 만든다).

## 테스트 (`RunnerSquadTests`)

- 생성: 병종, `Count`, `GetCount(1)`, `HighestLevel == 1`. `initialCount` 0 → 예외
- `Add`: Lv.1 증가, `Changed` 1회. 0 → 예외
- `Multiply`: 레벨별로 배수 (Lv.1 ×3, Lv.2 ×1에 ×2 → 6, 2). 1 → 예외
- 상한: 400기에 `Add(200)` → 500. Lv.1 300 + Lv.3 100에 `Multiply(2)` → Lv.3 200, Lv.1 300 (Lv.1 600에서 300 제거)
- `Remove`: 낮은 레벨부터 제거. 남은 수보다 많이 빼면 0, `IsDefeated`, `HighestLevel == 0`
- `Merge`: 위 두 예시. Lv.10 ×2는 그대로. Lv.1 ×1만 있으면 `Changed` 없음
- `ChangeType`: 병종 변경과 `Changed`. 같은 병종 → `Changed` 없음
- `Firepower`: Lv.1 ×2 + Lv.3 ×1, baseAttack 10, 배율 1.5 → (2×10 + 1×40) × 1.5 = 90
- `Gate`: 각 팩토리 값 검증과 잘못된 값 예외. `ApplyTo`가 종류별로 Squad를 바꿈. null → 예외

## 하지 말 것

- 기존 Core 클래스 수정 금지 (`UnitStats`, `GameRules` 등은 읽기만).
- `UnityEngine` 참조 금지.
- Game 코드, 트랙·적·보스 로직은 만들지 않는다 (13-2 이후).

## 완료 조건

- [ ] 위 테스트 전부 통과, 기존 EditMode 테스트도 통과

## 참고

- [UnitStats.cs](../../Assets/_Project/Scripts/Core/Units/UnitStats.cs), [GameRules.cs](../../Assets/_Project/Scripts/Core/GameRules.cs), [PlayerProgress.cs](../../Assets/_Project/Scripts/Core/Progression/PlayerProgress.cs) (`Changed` 이벤트 패턴)
