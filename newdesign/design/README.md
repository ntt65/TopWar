# 상세 설계 목차

| 문서 | 내용 |
| --- | --- |
| [architecture.md](architecture.md) | Core/Game 구조, 시뮬레이션 루프, 데이터 구조, 클래스, 충돌, 렌더링, 성능 예산, 입력, 테스트 |
| [rules.md](rules.md) | 규칙과 수치: 지휘관, 무기, 병사, 대형, 총알, 상성, 적, 접촉 피해, 캡슐, 게이트, 보스, 승패, 이벤트 |
| [presentation.md](presentation.md) | 카메라, 환경, 캐릭터, 연출 목록, 에셋 계획과 임포트 절차, HUD |
| [stage-1.md](stage-1.md) | 수직 슬라이스 스테이지 1 배치표 |

## 참고 요소 대응표

[runner-shooter.md](../reference/runner-shooter.md)의 42개 요소가 어디서 구현되는지.

| 요소 | 설계 위치 | 작업 |
| --- | --- | --- |
| C1 세로 화면 | presentation 1 | R1-1 |
| C2 하이앵글 카메라 | presentation 1 | R1-1 |
| C3 부드러운 추적 | presentation 1 | R1-1 |
| C4 바다 위 다리 | presentation 2, rules 1 | R1-1 |
| C5 단순한 배경 | presentation 2 | R1-1 |
| P1 소수로 시작 | rules 3 | R1-2 |
| P2 둥근 대형 | rules 3, presentation 3 | R1-2 |
| P3 연속 좌우 이동 | rules 2, architecture 6 | R1-1 |
| P4 분리·스프링 추종 | rules 3 | R1-2 |
| P5 병사별 자동 사격 | rules 4 | R1-2 |
| P6 레벨별 외형 | presentation 3 | R1-5 (R2에서 인스턴싱) |
| P7 지휘관 구분 | presentation 3 | R1-1 |
| P8 병종 유지 | rules 3·5 | R1-6 |
| S1 한쪽 캡슐 줄 | rules 7, stage-1 | R1-4 |
| S2 게이지 0 → 합류 | rules 7 | R1-4 |
| S3 무기 획득 | rules 2·7 | R1-4 |
| S4 병종·무기 보상 | rules 7 | R1-4 |
| S5 파괴 이펙트 | presentation 4 | R1-4 |
| S6 대형 캡슐 | rules 7, stage-1 | R1-6 |
| S7 지나치면 소멸 | rules 7 | R1-4 |
| G1 `+N` | rules 8 | R1-5 |
| G2 `MERGE`, `xN` | rules 8 | R1-5 |
| G3 판넬 위치·폭 | rules 8 | R1-5 |
| E1 적 무리 | rules 6 | R1-3 |
| E2 피해 숫자·쓰러짐 | presentation 4 | R1-3 |
| E3 정예 적 | rules 6, presentation 3 | R1-6 |
| E4 방패·돌진 적 | 보류 | R4 |
| E5 돌파 시 손실 | rules 6 | R1-3 |
| B1 거대 보스 | rules 9, presentation 3 | R1-7 |
| B2 보스 여러 종 | 보류 | R4 |
| B3 체력 깎이는 쾌감 | presentation 4 | R1-7 |
| F1 탄환 진화 | rules 2 | R1-4 |
| F2 플래시·파편·넉백 | presentation 4 | R1-3 |
| F3 큰 텍스트 | presentation 4 | R1-4, R1-7 |
| F4 금빛 링 | presentation 4 | R1-5 |
| F5 일시정지 | presentation 6 | R1-8 |
| U1 소환 버튼 없음 | rules 7 | R1-4 |
| U2 자유 좌표 | rules 1, architecture 4 | R1-1 |
| U3 지휘관 | rules 2 | R1-1 |
| U4 키 조작 | architecture 6 | R1-1 |
| U5 박스·판넬 캡슐 허용 | presentation 5 (에셋 소품 사용) | R1-4 |
| U6 손실·무기 하락 | rules 6 | R1-3 |
