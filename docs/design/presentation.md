# 상세 설계: 화면·연출·에셋

- **작성:** Claude / 2026-11-26
- 원칙: 처음부터 애니메이션 캐릭터와 연출을 넣는다. 기본 도형 캐릭터는 쓰지 않는다. 모양·색은 우리 디자인이다.

---

## 1. 카메라 (C1~C3)

| 항목 | 값 |
| --- | --- |
| 화면 | 세로 9:16 고정 |
| 위치 | 지휘관 기준 (x=0, y=11, z=-9), 아래로 약 50° |
| FOV | 50° (도로 폭 10m가 화면 폭에 맞게 화면 비율로 보정) |
| 지휘관 위치 | 화면 아래 1/4 |
| 추적 | z만 부드럽게 (감쇠 8/s). x는 지휘관의 15%만 따라감 |
| 보스전 | 3m 뒤로 빠지고 약간 위로 |
| 흔들림 | 약 0.08m·0.12초 / 중 0.18m·0.2초 / 강 0.35m·0.35초, Perlin 노이즈 |

## 2. 환경 (C4, C5)

- **바다 위 직선 다리**를 코드로 생성 (모듈 20m를 재사용하며 앞쪽에 이어 붙임).
  - 도로: 짙은 청회색 아스팔트, 흰 점선 차선, 가운데 노란 분리선
  - 난간: 기둥(인스턴싱)과 가로대, 도로 양 끝
  - 물: 짙은 청록 평면, 텍스처 오프셋을 흘려 움직임 표현
  - 하늘: 그라디언트, 옅은 안개로 먼 곳을 흐림
- 조명: 따뜻한 방향광 1개(그림자 1단계) + 차가운 환경광. 배경 요소는 최소화한다.

## 3. 캐릭터

| 대상 | 모델 | 구분 방법 |
| --- | --- | --- |
| 지휘관 (P7) | 에셋 팩의 병사와 **다른** 캐릭터 | 크기 ×1.3, 머리 위 파란 화살표, 발밑 파란 링, 그림자 투사 |
| 병사 (P2, P8) | 에셋 팩 병사 1종 | 병종 색: ARMY 올리브 녹색 / NAVY 남색 / AIR 하늘색 (`MaterialPropertyBlock`) |
| 병사 레벨 (P6) | 같은 모델 | Lv1~3 기본 / Lv4~6 크기 ×1.15, 밝은 색 / Lv7~9 ×1.3, 발광 테두리 / Lv10 ×1.45, 금색 |
| 일반 적 (E1) | 에셋 팩 적 캐릭터 (없으면 병사 모델 + 빨간 색) | 붉은 계열, 발밑 블롭 그림자 색 = 병종 색 |
| 정예 적 (E3) | 같은 적 모델 | 크기 ×2.2, 큰 무기, 머리 위 체력 바 + 숫자 |
| 보스 (B1) | 에셋 팩의 가장 큰 적/괴수 모델 | 크기 ×4, 화면 위 큰 체력 바 + 이름 |

애니메이션: Run(전진), Shoot 또는 Run-Shoot(사격), Death(쓰러짐), Idle(보스전 대기). 실제 클립 이름은 임포트 후 확인해서 정한다.

## 4. 연출 목록

| 연출 | 트리거 | 구현 | 참고 |
| --- | --- | --- | --- |
| 총구 섬광 | 발사 | 총구 위치에 짧은 빌보드 (인스턴싱) | P5 |
| 탄환 | 발사 | 발광 캡슐 메시 인스턴싱, 무기 단계별 색·굵기 + Bloom | F1 |
| 명중 불꽃 | `EnemyHit` | 파티클 풀 (Kenney spark) | F2 |
| 피격 플래시 | `EnemyHit` | 0.08초 흰색 (`_FlashAmount`) | F2 |
| 넉백 | `EnemyHit` (정예·보스 제외) | 화면에서 0.1m 뒤로 밀렸다 복귀 | F2 |
| 피해 숫자 | `EnemyHit`, `BossHit` | TMP 풀 64개, 같은 대상 0.1초 합산. 유리=큰 노랑, 보통=흰, 불리=작은 회색 | E2, B3 |
| 쓰러짐 | `EnemyKilled` | Death 애니 0.6초 → 가라앉으며 사라짐 | E2 |
| 캡슐 흔들림 | `CrateHit` | 스케일 펀치 + 숫자 갱신 | S2 |
| 캡슐 파괴 | `CrateOpened` | 파편 파티클 + 물보라 + 흰 섬광 | S5 |
| 획득 팝업 | `CrateOpened`, `GatePassed` | 큰 굵은 글자 `+5 ARMY`, 위로 떠오르며 사라짐 | F3, G1 |
| 합류 | `SoldierJoined` | 생성 위치에서 달려옴 + 작은 반짝임 | S2 |
| 레벨업 링 | `LevelUp`, MERGE | 바닥에 금빛 링이 퍼짐 + `LV UP` | F4, P6 |
| 무기 강화 | `WeaponUp` | 무리 전체 빛 펄스 + 총 아이콘 팝업 | S3 |
| 손실 | `SoldiersLost` | 빨간 `-3`, 흔들림(약/중) | E5 |
| 게이트 통과 | `GatePassed` | 판넬이 깨지며 흩어짐, 선택 안 한 쪽은 어두워짐 | G1~G3 |
| 정예 처치 | `EnemyKilled`(정예) | 큰 폭발 + 흔들림(중) | E3 |
| 보스 피격 | `BossHit` | 플래시 + 체력 바 깎임(흰 잔상 바) | B3 |
| 보스 처치 | `BossKilled` | 슬로모션 0.6초 + 흔들림(강) + `VICTORY!` | F3 |
| 패배 | `Defeat` | 슬로모션 0.5초 → 결과 화면 | - |

## 5. 에셋 계획 (사용자 다운로드·임포트 절차)

라이선스는 모두 CC0. `Assets/ThirdParty/<팩 이름>/`에 넣고 라이선스 파일을 함께 둔다 (WORKFLOW 3장).

| 팩 | 용도 | 받을 곳 |
| --- | --- | --- |
| **Quaternius – Toon Shooter Game Kit** | 지휘관·병사·적·보스 캐릭터(애니메이션 포함), 총, 소품 | quaternius.com → Packs → Toon Shooter Game Kit |
| **Kenney – Particle Pack** | 섬광·불꽃·연기·파편 텍스처 | kenney.nl/assets/particle-pack |
| Kenney – Fonts (선택) | 숫자·팝업용 굵은 폰트 | kenney.nl/assets/kenney-fonts |

### 사용자 할 일 (R1-0)

1. Toon Shooter Game Kit을 내려받아 압축을 푼다. **FBX** 폴더를 쓴다.
2. Unity Project 창에서 `Assets/ThirdParty/Quaternius_ToonShooter/` 폴더를 만든다.
3. 압축 푼 폴더의 캐릭터 FBX, 총 FBX, 텍스처, `License.txt`를 그 폴더로 끌어다 놓는다. 라이선스 파일이 없으면 알려 준다 (Claude가 만든다).
4. Particle Pack을 내려받아 `Assets/ThirdParty/Kenney_ParticlePack/`에 PNG 폴더와 `License.txt`를 넣는다.
5. 처음 한 번: 메뉴 **Window → TextMeshPro → Import TMP Essential Resources**.
6. 캐릭터 FBX 하나를 선택하고 Inspector에서:
   - **Rig** 탭: Animation Type = `Generic`, Avatar Definition = `Create From This Model` → Apply
   - **Animation** 탭: 클립 목록을 확인
7. 아래를 스크린샷으로 보내 준다. Claude가 이것을 보고 모델·클립 이름을 코드에 넣고, 에셋을 커밋한다.
   - Project 창의 `ThirdParty` 폴더 내용
   - 캐릭터 FBX 하나의 Animation 탭 클립 목록
   - 캐릭터를 씬에 끌어다 놓은 Scene 화면 (머티리얼이 분홍이면 알려 준다)

## 6. HUD (F5)

| 위치 | 요소 |
| --- | --- |
| 왼쪽 위 | 일시정지 버튼 → 계속 / 로비로 |
| 위 가운데 | 진행 바 (출발 → 보스 아이콘) |
| 오른쪽 위 | 병력 수 (병사 아이콘 + 숫자), 무기 단계 아이콘 |
| 보스전 | 화면 위 큰 보스 체력 바 + 남은 시간 |

화면 문구는 영어 (기존 결정). 숫자·팝업은 TextMeshPro (Unity 6 `ugui`에 포함, 영문 폰트만).
