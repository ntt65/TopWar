# 4-3-battle-view: 전투 화면 연결 (Game)

- **이슈:** #4
- **브랜치:** `feature/4-wave-combat`
- **작성:** Claude / 2026-11-25
- **순서:** Phase 2 지시서 3개 중 3번째. **4-2가 끝난 뒤** 진행한다.

## 목표

`BattleSimulation`을 게임 화면에 연결한다. 적이 레인을 따라 내려오고, 유닛이 투사체를 쏘고, 적이 죽거나 기지에 닿고, 웨이브 번호·기지 내구도·보스 타이머·게임 오버가 화면에 보이게 한다.

## 배경

- Phase 1 구조를 그대로 따른다: `GameBootstrap`이 모든 것을 코드로 만든다. **씬·프리팹은 편집하지 않는다.**
- 좌표 변환: `GridView`의 칸 좌표 → 월드 좌표 규칙을 실수 좌표로 확장한다. 적의 위치는 (X = `enemy.Lane`, Y = `enemy.Y`).
- 적과 아군을 구분하기 위해 적은 같은 병종 도형을 쓰되 **어두운 빨강 계열 색**으로 칠한다.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Scripts/Game/View/GridView.cs` | 수정 | `public Vector3 GridToWorld(float x, float y)` 추가. 기존 `CellToLocal` 계산과 같은 공식을 실수로. `CellToWorld`는 이것을 쓰도록 바꿔도 된다 (동작 동일) |
| `Scripts/Game/View/BattlefieldView.cs` | 추가 | 레인 바닥과 기지 표시 (정적) |
| `Scripts/Game/View/EnemyView.cs` | 추가 | 적 하나 표시 + 체력 바 |
| `Scripts/Game/View/ProjectileView.cs` | 추가 | 투사체 연출 |
| `Scripts/Game/Battle/BattleRunner.cs` | 추가 | 매 프레임 Tick, 이벤트 → 뷰 생성/제거 |
| `Scripts/Game/UI/UiFactory.cs` | 추가 | `Hud`의 UI 생성 헬퍼(`CreateRect`, `CreateText`, `CreateButton`, 폰트)를 static 클래스로 옮긴다 |
| `Scripts/Game/UI/Hud.cs` | 수정 | `UiFactory`를 쓰도록 바꾼다 (화면 결과 동일) |
| `Scripts/Game/UI/BattleHud.cs` | 추가 | 웨이브·기지·보스·게임 오버 UI |
| `Scripts/Game/GameBootstrap.cs` | 수정 | 전투 객체 생성과 연결 |

경로는 모두 `Assets/_Project/` 아래. 네임스페이스는 폴더를 따른다 (`TopWar.Game.Battle` 등).

## 상세 요구사항

### 1. `BattlefieldView` (MonoBehaviour)

`public void Init(GridView grid, BattleSimulation sim, BattleSettings settings)`

- **레인:** 각 열 X마다 그리드 맨 윗줄 바로 위부터 `SpawnY`까지 이어지는 얇고 납작한 큐브(폭 = `GridView.CellSize × 0.8`, 높이 0.05). 색은 타일보다 조금 어둡게. 콜라이더 제거.
- **기지:** `Y = settings.BaseY` 위치에 그리드 폭 전체를 덮는 납작한 큐브(깊이 `CellSize × 0.6`, 높이 0.3). 색은 회청색.
- 생성 후 갱신 없음.

### 2. `EnemyView` (MonoBehaviour)

`public static EnemyView Create(Enemy enemy, Transform parent, GridView grid)`

- 몸체: `UnitPalette.Shape(enemy.Type)`와 `UnitPalette.Proportion(enemy.Type)` 사용, 크기 0.5배. 색은 병종별 어두운 빨강 계열(예: Army `(0.6,0.15,0.15)`, AirForce `(0.75,0.3,0.1)`, Navy `(0.45,0.1,0.3)`). 보스는 크기 1.6배, 색 더 진하게.
- 체력 바: 몸체 위에 떠 있는 납작한 큐브 두 개(배경 검정, 전경 빨강 단색). 전경의 X 스케일 = `enemy.HpRatio`. 왼쪽 정렬. 체력 바는 `LateUpdate`에서 카메라 회전을 따라 돌린다 (`UnitView`의 레벨 라벨과 같은 방식, 카메라는 `Init`이나 `Create`에서 캐시).
- `Update`: `transform.position = grid.GridToWorld(enemy.Lane, enemy.Y)`, 체력 바 갱신.
- 콜라이더는 모두 제거.

### 3. `ProjectileView` (MonoBehaviour)

`public static void Fire(Vector3 from, EnemyView target, Color color, Transform parent)`

- 작은 구(지름 0.15)가 0.15초 동안 `from`에서 대상의 현재 위치로 이동한 뒤 사라진다. 대상이 도중에 사라지면 마지막 위치까지 가서 사라진다.
- 색은 쏜 유닛의 `UnitPalette.Color`.
- 프로토타입이므로 오브젝트 풀은 쓰지 않아도 된다.

### 4. `BattleRunner` (MonoBehaviour)

`public void Init(BattleSimulation sim, GridModel grid, GridView gridView)`

- `Update`: `sim.Tick(Time.deltaTime)`.
- `EnemySpawned` → `EnemyView.Create`, `Dictionary<int, EnemyView>`(Enemy.Id 키)에 보관.
- `EnemyKilled`, `EnemyReachedBase`, `BossTimedOut` → 해당 뷰 제거.
- `UnitFired(pos, target, damage)` → 그 칸의 유닛 위치(`gridView.CellToWorld(pos)` + 위로 0.5)에서 대상 뷰로 `ProjectileView.Fire`. 대상 뷰가 없으면 생략. 유닛 색은 `grid.Get(pos)`의 병종.
- `OnDestroy`에서 모든 이벤트 구독 해제.

### 5. `BattleHud` (MonoBehaviour)

`public void Init(BattleSimulation sim, BaseHealth baseHealth)` — `UiFactory`로 기존 Canvas와 별개의 자기 Canvas를 만든다 (기존 Hud의 Canvas 설정과 동일하게).

- 상단 가운데: `Wave {n}` (Break 중이면 `Next wave in {올림한 초}`)
- 상단 가운데 아래: `Base {current}/{max}`
- 보스가 있으면 그 아래 노란색 `BOSS {올림한 초}s`
- `GameOver` 이벤트 → 화면 가운데 반투명 검정 패널 + `GAME OVER` + `Wave {n}` + **Restart** 버튼. Restart는 `SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)`.
- 매 프레임 갱신이 필요한 텍스트(카운트다운, 보스 시간)는 값이 바뀔 때만 문자열을 다시 만든다 (정수 초 캐시).
- 기존 Hud의 골드 표시(왼쪽 위)와 `+100G` 버튼(오른쪽 위)을 가리지 않게 배치한다.

### 6. `GameBootstrap` 수정

`Awake`에서 기존 생성 다음에 추가:

```csharp
var battleSettings = new BattleSettings { UnitBaseAttack = config.BaseAttack };
var baseHealth = new BaseHealth(battleSettings.BaseMaxHealth);
var battle = new BattleSimulation(Grid, Wallet, baseHealth, new WaveGenerator(battleSettings), battleSettings);
```

- `BattlefieldView`, `BattleRunner`, `BattleHud`를 만들어 `Init`한다.
- `Battle`, `BaseHealth` 프로퍼티를 `Grid`, `Wallet`처럼 public getter로 노출한다.

### 7. 게임 오버 시 입력

- GameOver 상태에서는 소환 버튼과 드래그가 동작하지 않아야 한다. 가장 간단한 방법: GameOver 패널이 화면 전체를 덮는 `raycastTarget = true` 이미지라서 UI 클릭을 막고, `DragController`는 `EventSystem.IsPointerOverGameObject()` 검사로 이미 막힌다. 이 동작을 확인만 하고 별도 코드는 필요 없으면 추가하지 않는다.

## 하지 말 것

- `.unity`, `.prefab`, `ProjectSettings/` 편집 금지.
- Core 클래스 수정 금지. Core에 필요한 게 있으면 멈추고 보고한다.
- `CameraRig` 수정 금지 (적이 화면 위 밖에서 걸어 들어오는 것은 의도한 동작).
- 한글 텍스트, TextMeshPro 도입 금지 (Phase 3).

## 완료 조건

사용자가 에디터에서 확인:

- [ ] Play 후 `Next wave in 5` 카운트다운 → `Wave 1` 시작
- [ ] 화면 위쪽 레인을 따라 빨간 계열 적이 내려오고, 그리드를 지나 기지에 닿으면 사라지며 `Base` 수치가 줄어듦
- [ ] 사거리 안에 적이 오면 유닛이 투사체를 쏘고 적 체력 바가 줄며, 죽으면 사라지고 골드가 오름
- [ ] 웨이브가 끝나면 보너스 골드, 다음 웨이브 카운트다운
- [ ] 웨이브 5에서 큰 보스 등장, `BOSS 60s` 카운트다운
- [ ] 유닛이 없으면 결국 `GAME OVER`, Restart로 처음부터
- [ ] Phase 1 기능(소환·드래그·합성·스왑) 그대로 동작
- [ ] EditMode 테스트 전부 통과 (기존 + 4-1, 4-2)

## 참고

- 기존 뷰 패턴: [UnitView.cs](../../Assets/_Project/Scripts/Game/View/UnitView.cs), [GridView.cs](../../Assets/_Project/Scripts/Game/View/GridView.cs)
- 기존 UI: [Hud.cs](../../Assets/_Project/Scripts/Game/UI/Hud.cs)
- 진입점: [GameBootstrap.cs](../../Assets/_Project/Scripts/Game/GameBootstrap.cs)
