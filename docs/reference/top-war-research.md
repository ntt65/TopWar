# 참고 자료: Top War 조사

- **작성:** Claude / 2026-11-26
- **용도:** 장르와 원작 구조를 파악하기 위한 배경 자료. 구현 기준은 [runner-shooter.md](runner-shooter.md)의 요소 목록이다.

## 1. 원작 개요

| 항목 | 내용 | 출처 |
| --- | --- | --- |
| 개발 | Beijing River Games (RiverGame). 일본판 *Vivid Army*(브라우저, 2019.3) → 글로벌 모바일 *Top War: Battle Game* 2020.4.26 | 나무위키 |
| 규모 | Google Play 5천만+ 다운로드, 평점 4.5~4.6 | Play 스토어 |
| 핵심 루프 | 기지 건설 → 월드맵 확장 → 플레이어·NPC 전투 (4X 전략 MMO) | GameRefinery |
| 합성 | 같은 레벨 유닛·건물 2개를 합치면 즉시 레벨업 | 스토어 설명 |
| 전투 | 유닛·영웅을 고르면 자동 전투 | GameRefinery |
| 러너 미니게임 | 레벨 10부터 이벤트 페이지 "Mini-Game Center"에서 플레이. 리뷰에 따르면 레벨 5마다 5스테이지씩 열리고 에너지로 반복 플레이 | 개발사 답변, 사용자 리뷰 |

## 2. 병종 상성 (원작)

육군 > 공군 > 해군 > 육군. 우리 [CounterTable](../../Assets/_Project/Scripts/Core/Units/CounterTable.cs)과 방향이 같다.

## 3. 우리 게임에 반영

- 원작의 **러너 미니게임**을 메인 게임으로 만든다 (README).
- 원작의 "같은 레벨 2개 → 레벨+1" 합성은 `MERGE` 게이트로 넣는다.
- 상세 규칙(캡슐 게이지, 보스, 보상)은 공개 자료가 없으므로, 사용자가 준 영상·스크린샷([runner-shooter.md](runner-shooter.md))을 기준으로 한다.

## 출처

- [App Store](https://apps.apple.com/us/app/top-war-battle-game/id1479198816)
- [Google Play](https://play.google.com/store/apps/details?id=com.topwar.gp)
- [GameRefinery](https://www.gamerefinery.com/top-war-battle-game-a-quick-glimpse-on-the-4x-strategy-hit/)
- [나무위키](https://namu.wiki/w/Top%20War:%20Battle%20Game)
- [Last War Frontline Breakthrough 가이드](https://heaven-guardian.com/last-war-frontline-breakthrough-guide/) (같은 장르의 미니게임 비교)
