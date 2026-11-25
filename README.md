# TopWar Defense (가제)

**Top War: Battle Game**의 핵심 메커니즘인 **그리드 기반 유닛 합성(Merge)**과 **웨이브 디펜스(Wave Defense)**를 결합한 2D 전략 디펜스 게임 프로젝트입니다.

---

## 1. 게임 개요 (Game Overview)
* **장르:** 2D 합성(Merge) + 웨이브 디펜스(Wave Defense)
* **플랫폼:** Mobile (Android / iOS) & Web (Flutter + Flame Engine)
* **핵심 재미 요소:** 
  1. 제한된 그리드 공간 내에서 하위 유닛을 합성하여 상위 병기로 진화시키는 쾌감
  2. 병종 간 상성(육군·해군·공군)을 고려한 전략적 진형 배치
  3. 점진적으로 강해지는 적 웨이브를 방어하며 기지를 확장하는 성장 루프

---

## 2. 세계관 및 시나리오 (Scenario & Lore)
* **배경 설정:** 자원 고갈 이후, 남은 에너지 코어(Energy Core)가 매장된 중앙 섬(Central Island)을 차지하기 위해 다국적 용병단과 정체불명의 기계 군단(Dark Legion)이 충돌한다.
* **플레이어 역할:** 전초기지 사령관(Commander). 한정된 보급품으로 기초 병력을 소집하고, 현장 병합(Field Merge) 기술을 통해 최정예 기갑·항공·함대 전력을 구축하여 방어선을 사수해야 한다.
* **진행 흐름:**
  * **Chapter 1 (해안 방어선):** 상륙하는 적 보병 및 경장갑 차량 저지
  * **Chapter 2 (폭풍의 해협):** 해상 전함 및 공중 폭격기 편대 방어
  * **Chapter 3 (코어 요새):** 거대 보스 병기(Boss Unit) 공략 및 최종 방어선 구축

---

## 3. 핵심 게임 루프 (Core Gameplay Loop)

```text
[골드 소모: Lv.1 유닛 소환] 
       │
       ▼
[동일 병종·레벨 드래그 & 합성 (Lv.N + Lv.N -> Lv.N+1)]
       │
       ▼
[전선 그리드 배치 및 상성 기반 자동 교전]
       │
       ▼
[적 웨이브 격퇴 및 골드/보석 획득] ──(반복)──▶ [기지 확장 및 연구 업그레이드]

```

---

## 4. 주요 시스템 명세 (Core Systems)

### ① 그리드 및 합성 시스템 (Grid & Merge)

* **전장 규격:** 기본 $5 \times 5$ 타일 그리드 (스테이지 진행에 따라 최대 $7 \times 7$ 까지 확장)
* **합성 규칙:** 동일한 병종(Type)과 레벨(Level)을 가진 두 유닛을 겹치면 상위 레벨($N + 1$) 유닛 1기로 즉시 진화
* **전술 재배치:** 전투 중에도 유닛 위치를 자유롭게 스왑(Swap)하여 적의 공격 경로에 대응

### ② 병종 및 상성 시스템 (Unit Types & Counter)

* **육군 (Army):** 높은 체력과 안정적인 지상 화력 / **공군에게 강함**
* **공군 (Air Force):** 빠른 공격 속도와 광역 타격 / **해군에게 강함**
* **해군 (Navy):** 긴 사거리와 강력한 단일 폭딜 / **육군에게 강함**
* **상성 보정:** 우위 병종 공격 시 데미지 **+25%**, 열위 병종 공격 시 **-15%** 적용

### ③ 웨이브 및 보스 시스템 (Wave & Boss)

* **일반 웨이브:** 정해진 경로(Lane)를 따라 다수의 적 유닛이 순차적으로 진격
* **보스 웨이브 (매 5 Wave 단위):** 높은 체력과 특수 패턴(방어막 생성, 소환 등)을 가진 거대 보스 등장. 제한 시간 내 격퇴 실패 시 방어선 내구도 대폭 감소

---

## 5. 기술 스택 (Tech Stack)

* **Framework:** Flutter (Dart)
* **Game Engine:** Flame (2D Game Loop, Sprite Animation, Hitbox Collision)
* **State Management:** Riverpod (재화, 그리드 상태, 업그레이드 UI 동기화)
* **Data Format:** JSON (유닛 스탯 테이블 및 웨이브 데이터 관리)

---

## 6. 개발 마일스톤 (Roadmap)

* [ ] **Phase 1:** $5 \times 5$ 그리드 생성 및 드래그 앤 드롭 합성 프로토타입 구현
* [ ] **Phase 2:** 적 웨이브 스폰, 이동 경로, 자동 사격 및 투사체 충돌 로직 구현
* [ ] **Phase 3:** 병종 상성 연산, 골드 경제 시스템, 소환/업그레이드 HUD 연동
* [ ] **Phase 4:** 합성 파티클 이펙트, 사운드 적용 및 밸런싱 튜닝

```

---

GitHub 새 저장소 화면에서 **Add README** 토글을 **On**으로 켜고 저장소를 생성하신 뒤, 생성된 `README.md` 파일에 위 내용을 그대로 복사해 넣으시면 됩니다.

<ElicitationsGroup message="다음으로 진행할 작업을 선택해 주세요:">
  <Elicitation label="Flutter + Flame 초기 폴더 구조와 pubspec.yaml 생성" query="Flutter와 Flame 엔진으로 TopWar 프로젝트를 시작하기 위한 초기 폴더 구조와 pubspec.yaml 파일 내용을 작성해 줘."/>
  <Elicitation label="유닛 스탯 및 웨이브 구성 JSON 데이터 샘플 작성" query="TopWar 게임에 사용할 병종별/레벨별 유닛 스탯과 웨이브 정보가 담긴 JSON 데이터 샘플을 만들어 줘."/>
</ElicitationsGroup>

```
