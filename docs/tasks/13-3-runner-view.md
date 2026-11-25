# 13-3-runner-view: 러너 화면과 좌우 조작 (Game)

- **이슈:** #13
- **브랜치:** `feature/13-runner-stage`
- **작성:** Claude / 2026-11-26
- **순서:** 러너 지시서 4개 중 3번째. **13-2가 끝난 뒤** 진행한다. 화면 요소가 많으니 **high**.

## 목표

13-2의 `RunnerSimulation`을 화면에 그려서 실제로 플레이할 수 있게 한다. 무리는 자동으로 전진하고, 손가락(에디터에서는 마우스) 좌우 드래그로 움직인다. 이번에는 **로비의 개발용 `Runner` 버튼**으로만 들어간다. 정식 스테이지 흐름과 보상은 13-4에서 붙인다.

## 배경

- 기획: [README.md](../../README.md) ⑦ 러너 보너스 스테이지.
- 그래픽은 지금처럼 **기본 도형 + `TextMesh` 라벨**. 보급 상자는 박스, 게이트는 판넬이면 된다. 다른 게임의 캐릭터·아트는 따라 하지 않는다.
- 기존 패턴: 도형 생성은 `BattlePrimitive.Create`([BattlefieldView.cs](../../Assets/_Project/Scripts/Game/View/BattlefieldView.cs)), 병종 도형·색은 `UnitPalette`, 라벨은 `UnitView`의 `TextMesh` 방식, 피해 숫자는 `DamageNumberView.Show`, UI는 `UiFactory`, 문구는 `Texts`.

## 좌표 변환

- 월드 `X` = 시뮬레이션 `X`, 월드 `Z` = 시뮬레이션 `Distance`, 바닥 `Y = 0`.
- 게이트·상자의 왼쪽 칸 중심 `X = -HalfWidth / 2`, 오른쪽 칸 중심 `X = +HalfWidth / 2`.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Scripts/Game/Runner/RunnerController.cs` | 추가 | 시뮬레이션 진행, 화면 요소 생성, 종료 처리 |
| `Scripts/Game/Runner/RunnerInput.cs` | 추가 | 상대 드래그 → `SetX` |
| `Scripts/Game/Runner/RunnerCamera.cs` | 추가 | 무리를 따라가는 카메라 |
| `Scripts/Game/Runner/RunnerTrackView.cs` | 추가 | 도로, 게이트, 상자, 적 무리, 보스 |
| `Scripts/Game/Runner/SquadView.cs` | 추가 | 아군 무리 표시 |
| `Scripts/Game/Runner/RunnerShotView.cs` | 추가 | 사격 연출 (직선으로 날아가는 작은 구) |
| `Scripts/Game/Runner/RunnerHud.cs` | 추가 | 진행도, 무리 수, 보스 시간·체력 |
| `Scripts/Game/UI/Texts.cs` | 수정 | 게이트·HUD 문구 |
| `Scripts/Game/UI/LobbyScreen.cs` | 수정 | 개발용 `Runner` 버튼 |
| `Scripts/Game/GameBootstrap.cs` | 수정 | `StartRunner(int stage)` |

경로는 `Assets/_Project/` 아래. 네임스페이스 `TopWar.Game.Runner`.

## 상세 요구사항

### 1. 진입 (개발용)

- `LobbyScreen.Init(PlayerProgress progress, Action<int> onStart, Action<int> onStartRunner)`로 바꾼다.
- 오른쪽 위 `+50 Gems` 버튼 왼쪽에 같은 모양의 작은 버튼 `Runner`(문구는 `Texts.DevRunner`)를 둔다 → `onStartRunner(progress.NextStage)`.
- `GameBootstrap.StartRunner(int stage)`:
  1. 로비 제거 (`StartStage`와 같은 방식, `SaveProgress` 구독 해제 포함).
  2. 설정: `new RunnerSettings { UnitBaseAttack = config.BaseAttack, Army/AirForce/NavyAttackMultiplier = _progress.GetAttackMultiplier(...) }`.
  3. `track = RunnerTrackGenerator.Create(stage, seed: stage)`, `squad = new Squad(settings.StartType, settings.StartCount)`, `sim = new RunnerSimulation(squad, track, settings)`.
  4. `new GameObject("Runner").AddComponent<RunnerController>().Init(sim, settings, _camera, victory => 결과 표시)`.
  5. 결과: 이번에는 보상·저장 없이 `ResultScreen.Init(stage, victory, 0, 0, 0)`을 띄운다 (13-4에서 정식으로 바꾼다).

### 2. `RunnerController` (MonoBehaviour)

`public void Init(RunnerSimulation sim, RunnerSettings settings, Camera camera, Action<bool> onFinished)`

- 자식으로 `RunnerTrackView`, `SquadView`, `RunnerHud`를 만들고, 카메라에 `RunnerCamera`(없으면 추가), 자기에게 `RunnerInput`을 붙인다. 기존 `CameraRig`가 카메라에 붙어 있으면 `enabled = false`.
- `Update`: `sim.Tick(Time.deltaTime)`.
- `sim.Finished` → `RunnerInput.enabled = false` → `onFinished(victory)`. `OnDestroy`에서 모든 구독 해제.

### 3. `RunnerInput` (MonoBehaviour) — 상대 드래그

`public void Init(RunnerSimulation sim, float halfWidth)`

- `Pointer.current`(Input System) 사용. 누를 때 UI 위면(`EventSystem.current.IsPointerOverGameObject()`) 무시.
- 누른 순간의 화면 x와 `sim.X`를 기억한다. 누르는 동안 `sim.SetX(startX + (현재 x - 시작 x) / Screen.width × 2 × halfWidth × Sensitivity)`.
  - `const float Sensitivity = 1.2f`. 화면 가로를 끝까지 끌면 트랙 폭보다 조금 더 움직인다.
- 손을 떼면 그 자리에 멈춘다. 손가락 위치로 순간이동하지 않는다.

### 4. `RunnerCamera` (MonoBehaviour)

`public void Init(Func<float> getDistance, float halfWidth)`

- 피치 50°, FOV 50, 정면(Y축 회전 0).
- 가로: 도로 폭 `2 × halfWidth + 1`이 화면 가로에 들어가는 거리를 `CameraRig`처럼 계산한다 (화면 비율이 바뀌면 다시 계산).
- 바라보는 지점 = `(0, 0, getDistance() + 6)` (무리보다 앞쪽을 더 보이게). `LateUpdate`에서 Z를 부드럽게 따라간다 (`Mathf.Lerp`, 계수 `1 - exp(-8 × dt)`).

### 5. `RunnerTrackView` (MonoBehaviour)

`public void Init(RunnerSimulation sim, RunnerSettings settings)` — 시작 시 트랙 전체를 만들고 이벤트로 갱신한다.

- **도로:** 회색 Cube, 폭 `2 × HalfWidth + 0.6`, 길이 `Track.Length + 20`, 두께 0.1, 윗면이 Y=0. 가운데 흰 선(폭 0.06)으로 좌·우를 구분한다. 양옆에 낮은 난간(어두운 Cube).
- **게이트 줄:** 칸마다 판넬(Cube, 폭 `HalfWidth × 0.9`, 높이 1.4, 두께 0.1, 반투명이 어려우면 불투명)과 큰 `TextMesh` 라벨.
  - 라벨: Add `+{n}`, Multiply `x{n}`, Subtract `-{n}`, Merge `MERGE`, ChangeType `UnitPalette.Label(type)`.
  - 색: Add·Multiply·Merge 파랑, Subtract 빨강, ChangeType은 `UnitPalette.Color(type)`.
  - `GatePassed` → 그 줄 판넬 두 개를 제거한다.
- **보급 상자:** 해당 칸 중심에 갈색 Cube(0.9 × 0.9 × 0.9), 위에 남은 체력 라벨(`Ceil(Hp)`)과 그 아래 작은 보상 라벨(`+{RewardCount}`).
  - `CrateHit` → 체력 라벨 갱신, 0.1초 살짝 커졌다 돌아오기. `CrateOpened` → 제거. `CrateMissed` → 회색으로 바꾸고 라벨 숨김.
- **적 무리:** 거리 위치에 도로 폭 전체로 작은 적 도형(병종 `UnitPalette` 도형, 색은 병종 색을 어둡게 + 빨간 기운)을 격자로 배치. 보이는 수는 `Min(RemainingCount, 30)`. 위에 `RemainingCount` 라벨.
  - `EnemyGroupHit` → 라벨과 보이는 수 갱신, `DamageNumberView.Show(라벨 아래 위치, damage, CounterTable.DamageMultiplier(squad.Type, group.Type), transform)`.
  - `EnemyGroupDestroyed`·`EnemyGroupCollided` → 제거.
- **보스:** 끝 위치에 큰 도형(병종 도형, 스케일 2.5) + 위에 `Ceil(Hp)` 라벨. `BossHit` → 라벨 갱신 + 피해 숫자.
- 모든 라벨은 `UnitView`처럼 카메라를 향하게 회전한다.

### 6. `SquadView` (MonoBehaviour)

`public void Init(RunnerSimulation sim)`

- 무리 중심: `(displayX, 0, sim.Distance)`. `displayX`는 `sim.X`를 부드럽게 따라간다 (계수 `1 - exp(-12 × dt)`).
- 병사: `UnitPalette` 도형·색(`Squad.Type`)으로 만든다. **보이는 수는 `Min(Count, 60)`**, 높은 레벨부터 채운다. 크기 `0.28 + 0.03 × (level - 1)`.
- 배치: 해바라기 나선. i번째 병사(0부터) 오프셋 = `(r cosθ, 0, r sinθ)`, `r = 0.22 × sqrt(i)`, `θ = i × 137.5°`. 가장 바깥 반경이 `HalfWidth × 0.9`를 넘으면 모든 오프셋을 같은 비율로 줄여 그 안에 넣는다.
- `Squad.Changed` → 병사 다시 만들기 (이번에는 풀 없이 Destroy/생성).
- 무리 위에 `Count` 라벨 (크게). 레벨이 2 이상인 병사가 있으면 아래 줄에 `Lv.{HighestLevel}`.

### 7. `RunnerShotView`

`public static void Fire(Vector3 from, Vector3 to, Color color, Transform parent)` — 작은 구(0.12)가 0.12초 동안 직선 이동 후 사라진다.

- `RunnerTrackView`가 `EnemyGroupHit`·`CrateHit`·`BossHit` 때 무리 중심(+Y 0.5)에서 대상 위치로 1발 쏜다 (사격 1회당 1발).

### 8. `RunnerHud` (MonoBehaviour)

`public void Init(RunnerSimulation sim)` — `UiFactory.CreateCanvas`.

- 위쪽: 진행 바(가로 막대, `sim.Progress`), 그 아래 `{병종} x{Count}` (예 `ARMY x23`).
- 보스전(`BossFightStarted` 이후): `BOSS {초}s`(기존 `Texts.Boss`)와 보스 체력 바(`Hp / MaxHp`, 빨강).
- 게이트를 지나면 화면 가운데에 적용된 게이트 라벨을 0.6초 띄운다 (예 `x2`, `MERGE`).

### 9. `Texts` 추가

`DevRunner = "Runner"`, `GateAdd = "+{0}"`, `GateMultiply = "x{0}"`, `GateSubtract = "-{0}"`, `GateMerge = "MERGE"`, `SquadCount = "{0} x{1}"`, `CrateReward = "+{0}"`

## 하지 말 것

- Core 수정 금지 (필요하면 멈추고 보고).
- 씬·프리팹·ProjectSettings 편집 금지. 모두 코드로 생성.
- 디펜스 전투 코드(`BattleRunner`, `BattlefieldView`, `CameraRig` 등)의 동작 변경 금지.
- 보상 지급·저장·스테이지 흐름 변경 금지 (13-4).
- 오브젝트 풀 금지 (Phase 4).

## 완료 조건

사용자가 에디터에서 확인:

- [ ] 로비 `Runner` → 러너 화면. 무리가 자동 전진하고 카메라가 따라감
- [ ] 마우스(터치) 좌우 드래그로 무리가 부드럽게 이동, 손을 떼면 멈춤, 도로 밖으로 나가지 않음
- [ ] 게이트 라벨이 보이고, 지나간 쪽 효과가 무리 수에 반영됨
- [ ] 같은 쪽 보급 상자를 쏴서 부수면 무리 수 증가
- [ ] 적 무리를 쏘면 수가 줄고, 닿으면 아군이 줄어듦
- [ ] 보스 앞에서 멈추고 보스 시간·체력 표시, 승리 또는 패배 후 결과 화면 → `Lobby`
- [ ] EditMode 테스트 전부 통과 (Core 변경 없음)

## 참고

- [RunnerSimulation.cs](../../Assets/_Project/Scripts/Core/Runner/RunnerSimulation.cs), [GameBootstrap.cs](../../Assets/_Project/Scripts/Game/GameBootstrap.cs), [LobbyScreen.cs](../../Assets/_Project/Scripts/Game/UI/LobbyScreen.cs), [CameraRig.cs](../../Assets/_Project/Scripts/Game/View/CameraRig.cs), [DragController.cs](../../Assets/_Project/Scripts/Game/Controls/DragController.cs) (Pointer 입력), [UnitView.cs](../../Assets/_Project/Scripts/Game/View/UnitView.cs), [DamageNumberView.cs](../../Assets/_Project/Scripts/Game/View/DamageNumberView.cs)
