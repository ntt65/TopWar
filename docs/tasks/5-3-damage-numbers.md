# 5-3-damage-numbers: 피해 숫자와 상성 표시 (Game)

- **이슈:** #5
- **브랜치:** `feature/5-economy-progression`
- **작성:** Claude / 2026-11-26
- **순서:** Phase 3 지시서 3개 중 3번째. **5-2가 끝난 뒤** 진행한다 (`Texts` 사용). medium으로 충분.

## 목표

유닛이 적을 맞힐 때 피해 숫자가 떠오르고, 상성에 따라 색이 달라서 "이 배치가 유리한지"가 눈에 보이게 한다. 소환 버튼에 각 병종이 무엇에 강한지 표시한다.

## 변경할 파일

| 파일 | 작업 | 내용 |
| --- | --- | --- |
| `Scripts/Game/View/DamageNumberView.cs` | 추가 | 떠오르는 피해 숫자 |
| `Scripts/Game/Battle/BattleRunner.cs` | 수정 | `UnitFired`에서 피해 숫자 생성 |
| `Scripts/Game/UI/Hud.cs` | 수정 | 소환 버튼에 상성 힌트 |
| `Scripts/Game/UI/Texts.cs` | 수정 | 힌트 문구 |

## 상세 요구사항

### 1. `DamageNumberView` (MonoBehaviour)

`public static void Show(Vector3 worldPosition, float damage, float counterMultiplier, Transform parent)`

- `TextMesh`로 표시 (기존 `UnitView` 레벨 라벨과 같은 방식, 내장 `LegacyRuntime.ttf`).
- 문구: 피해를 반올림한 정수.
- 색과 크기:
  - 유리 (`counterMultiplier > 1`): 노란색, 1.3배 크기
  - 불리 (`< 1`): 회색, 0.85배 크기
  - 보통: 흰색
- 0.6초 동안 위로 0.8만큼 떠오르며 마지막 0.2초에 투명해진 뒤 사라진다. 카메라를 향해 회전한다 (카메라 Transform 캐시).
- 위치는 매번 x·z를 ±0.15 안에서 살짝 흩어 겹침을 줄인다.

### 2. `BattleRunner`

- `UnitFired(pos, target, damage)`에서 투사체와 함께 `DamageNumberView.Show(대상 뷰의 머리 위 위치, damage, CounterTable.DamageMultiplier(유닛 병종, target.Type), transform)`.
- 대상 뷰가 없으면 생략.
- 광역 피해(주 대상 외)는 숫자를 띄우지 않는다 (화면이 지저분해지지 않게).

### 3. 소환 버튼 상성 힌트

- 각 소환 버튼 라벨을 두 줄로: 첫 줄 병종 이름(기존 크기), 둘째 줄 작은 글씨(크기 32)로 `Strong vs {이긴 상대}`.
  - ARMY → `Strong vs AIR`, AIR → `Strong vs NAVY`, NAVY → `Strong vs ARMY`.
- 이긴 상대는 `CounterTable.Beats`로 계산한다 (하드코딩 금지).

## 하지 말 것

- Core 수정 금지.
- 오브젝트 풀은 이번에는 만들지 않는다 (Phase 4 최적화).

## 완료 조건

- [ ] 유닛이 쏠 때 적 머리 위에 숫자가 떠올랐다 사라짐
- [ ] 상성 유리 노란색·크게, 불리 회색·작게
- [ ] 소환 버튼 아래 줄에 `Strong vs ...`
- [ ] EditMode 테스트 전부 통과

## 참고

- [UnitView.cs](../../Assets/_Project/Scripts/Game/View/UnitView.cs) (TextMesh 라벨), [ProjectileView.cs](../../Assets/_Project/Scripts/Game/View/ProjectileView.cs), [BattleRunner.cs](../../Assets/_Project/Scripts/Game/Battle/BattleRunner.cs)
