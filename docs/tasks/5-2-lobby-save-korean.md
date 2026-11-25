# 5-2-lobby-save-korean: 로비·결과 화면, 저장, 한글 UI (Game)

- **이슈:** #5
- **브랜치:** `feature/5-economy-progression`
- **작성:** Claude / 2026-11-26
- **순서:** Phase 3 지시서 3개 중 2번째. **5-1이 끝난 뒤**, 그리고 **사용자가 한글 폰트 파일을 넣은 뒤** 진행한다.

## 목표

게임 흐름을 **로비 → 전투 → 결과 → 로비**로 만든다. 로비에서 보석으로 업그레이드를 사고, 진행 상황이 저장되며, 모든 UI가 한글로 보인다.

## 배경

- 5-1의 `PlayerProgress`, `StageRules`, `BattleState.Victory`, `StageCleared`, `ClearedWaves`를 사용한다.
- 지금처럼 **씬 하나, 코드로 UI 생성**을 유지한다. 씬·프리팹 편집 금지.
- 화면 전환은 단순하게: 결과 화면에서 "로비로"를 누르면 **씬을 다시 로드**하고, 로드된 씬은 항상 로비부터 시작한다. 진행 상황은 저장소에서 다시 읽는다.
- 한글 폰트: 사용자가 `Assets/_Project/Resources/Fonts/NotoSansKR-Bold.ttf`를 넣는다. 코드는 `Resources.Load<Font>("Fonts/NotoSansKR-Bold")`로 읽고, 없으면 기존 `LegacyRuntime.ttf`로 대체한다.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Scripts/Game/Save/ProgressStore.cs` | 추가 | PlayerPrefs 저장·불러오기 |
| `Scripts/Game/UI/UiFactory.cs` | 수정 | 한글 폰트 로드 + 대체 |
| `Scripts/Game/UI/LobbyScreen.cs` | 추가 | 로비 화면 |
| `Scripts/Game/UI/ResultScreen.cs` | 추가 | 승리·패배 결과 화면 |
| `Scripts/Game/UI/Hud.cs` | 수정 | 한글 문구 |
| `Scripts/Game/UI/BattleHud.cs` | 수정 | 한글 문구, `웨이브 n/10`, 기존 게임 오버 패널·Restart 제거 |
| `Scripts/Game/UI/Texts.cs` | 추가 | 화면 문구 상수 모음 (`static class`) |
| `Scripts/Game/GameBootstrap.cs` | 수정 | 흐름 제어 (로비 → 전투 → 결과) |

경로는 `Assets/_Project/` 아래.

## 상세 요구사항

### 1. `ProgressStore` (static class)

```csharp
public const string Key = "TopWar.Progress";
public static PlayerProgress Load()            // PlayerPrefs 문자열 → JsonUtility.FromJson<PlayerProgressData> → PlayerProgress.FromData. 없거나 파싱 실패면 새 진행 상태
public static void Save(PlayerProgress progress) // ToData → JsonUtility.ToJson → PlayerPrefs.SetString + PlayerPrefs.Save()
public static void Clear()                     // DeleteKey + Save
```

### 2. `UiFactory`

- 폰트를 한 번만 로드해서 캐시한다: `Resources.Load<Font>("Fonts/NotoSansKR-Bold")`, null이면 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`이고 `Debug.LogWarning`을 한 번 남긴다.
- 기존 메서드 시그니처는 유지한다.

### 3. `Texts` (static class)

모든 화면 문구를 여기에 모은다. 최소 아래를 포함한다 (이름은 자유, 문구는 그대로):

| 용도 | 문구 |
| --- | --- |
| 병종 | `육군`, `공군`, `해군` |
| 골드 | `골드 {n}` |
| 소환 비용 | `소환 비용 {n}` |
| 메시지 | `골드가 부족합니다`, `빈 칸이 없습니다` |
| 웨이브 | `웨이브 {n}/{total}` |
| 대기 | `다음 웨이브 {초}` |
| 기지 | `기지 {current}/{max}` |
| 보스 | `보스 {초}초` |
| 로비 제목 | `TopWar Defense` |
| 보석 | `보석 {n}` |
| 시작 버튼 | `스테이지 {n} 시작` |
| 업그레이드 이름 | `육군 공격력`, `공군 공격력`, `해군 공격력`, `기지 내구도`, `전장 확장` |
| 업그레이드 버튼 | `보석 {cost}` / 최대면 `최대` (이모지는 폰트에 없으니 쓰지 않는다) |
| 결과 | `스테이지 {n} 클리어!`, `패배`, `넘긴 웨이브 {n}/{total}`, `획득 보석 {n}`, `로비로` |
| 초기화 | `진행 초기화` |

`UnitPalette.Label`은 그대로 두고, HUD 버튼은 `Texts`의 병종 이름을 쓴다.

### 4. `LobbyScreen` (MonoBehaviour)

`public void Init(PlayerProgress progress, Action<int> onStart)` — 자기 Canvas를 `UiFactory.CreateCanvas`로 만든다.

- 상단: 제목, 그 아래 `보석 {n}`.
- 가운데: 업그레이드 5줄. 각 줄 = 이름 / `Lv.{level}` / 현재 효과 요약 / 구매 버튼.
  - 효과 요약 예: 공격 `+{10L}%`, 기지 `내구도 {GetBaseMaxHealth(100)}`, 전장 `{size}×{size}`.
  - 버튼은 `progress.TryUpgrade(type)` 호출. 살 수 없으면 `Button.interactable = false`. 최대면 `최대`.
- 하단: 큰 버튼 `스테이지 {NextStage} 시작` → `onStart(progress.NextStage)`.
- 오른쪽 위 작은 버튼 `진행 초기화`: `ProgressStore.Clear()` 후 씬 다시 로드. (확인 창은 없어도 된다. 개발용)
- `progress.Changed`를 구독해 모든 표시를 갱신하고, `OnDestroy`에서 해제한다.

### 5. `ResultScreen` (MonoBehaviour)

`public void Init(int stage, bool victory, int clearedWaves, int totalWaves, int gems)`

- 화면 전체 반투명 검정 패널 (`raycastTarget = true`, 뒤쪽 입력 차단).
- 제목: 승리면 `스테이지 {n} 클리어!`(노란색), 패배면 `패배`(빨간색).
- `넘긴 웨이브 {cleared}/{total}`, `획득 보석 {gems}`.
- 버튼 `로비로` → `SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)`.

### 6. `BattleHud` 수정

- `웨이브 {n}/{WavesPerStage}`로 표시 (Break 중이면 `다음 웨이브 {초}`).
- `기지`, `보스` 문구를 한글로.
- **게임 오버 패널과 Restart 버튼은 제거**한다 (ResultScreen이 대신한다). 필요 없어진 코드도 정리한다.

### 7. `GameBootstrap` 흐름

- `Awake`: `progress = ProgressStore.Load()`. EventSystem 보장(기존 `Hud`의 방식 재사용 — 필요하면 `UiFactory`로 옮긴다). 카메라는 기존처럼 준비. **로비만 표시**하고 전투 객체는 아직 만들지 않는다.
- `StartStage(int stage)`: 로비 오브젝트 제거 → 기존 전투 생성 코드 실행. 단:
  - 그리드 크기 = `progress.GetGridSize()` (가로·세로 동일).
  - `battleSettings = StageRules.CreateSettings(stage, progress, config.BaseAttack)`.
  - `BaseHealth = new BaseHealth(battleSettings.BaseMaxHealth)`.
- `Battle.GameOver`와 `Battle.StageCleared`에서 `FinishStage(victory)`:
  1. 기존 `DisableGameplayInput()` 실행.
  2. `gems = StageRules.GemReward(stage, Battle.ClearedWaves, victory)`, `progress.AddGems(gems)`, 승리면 `progress.RecordStageCleared(stage)`.
  3. `ProgressStore.Save(progress)`.
  4. `ResultScreen` 표시.
- 로비에서 업그레이드를 살 때도 저장한다 (`progress.Changed` → `ProgressStore.Save`).
- 카메라: 그리드가 생긴 뒤 기존처럼 `CameraRig.Init(gridView)`. 로비에서는 카메라 위치가 어디든 상관없다.

## 하지 말 것

- Core 수정 금지 (필요하면 멈추고 보고).
- 씬·프리팹·ProjectSettings 편집 금지.
- TextMeshPro 도입 금지.
- 폰트 파일을 직접 추가하지 않는다 (사용자가 넣는다). 없을 때도 실행은 되어야 한다.

## 완료 조건

사용자가 에디터에서 확인:

- [ ] Play → 로비: 제목, 보석, 업그레이드 5줄, `스테이지 1 시작` (한글이 정상 표시)
- [ ] 시작 → 전투. HUD가 모두 한글, `웨이브 1/10`
- [ ] 패배 → 결과 `패배`, 넘긴 웨이브, 획득 보석 → `로비로` → 보석이 늘어 있음
- [ ] 업그레이드 구매 → 보석 감소, 레벨 증가. Play를 껐다 켜도 유지
- [ ] `전장 확장` 구매 후 시작 → 6×6 그리드
- [ ] 10웨이브 클리어 → `스테이지 1 클리어!` → 로비에 `스테이지 2 시작`
- [ ] `진행 초기화` → 처음 상태
- [ ] EditMode 테스트 전부 통과

## 참고

- [GameBootstrap.cs](../../Assets/_Project/Scripts/Game/GameBootstrap.cs), [Hud.cs](../../Assets/_Project/Scripts/Game/UI/Hud.cs), [BattleHud.cs](../../Assets/_Project/Scripts/Game/UI/BattleHud.cs), [UiFactory.cs](../../Assets/_Project/Scripts/Game/UI/UiFactory.cs)
