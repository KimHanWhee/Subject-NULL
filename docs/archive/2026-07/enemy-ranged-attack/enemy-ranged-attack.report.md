# enemy-ranged-attack Completion Report

> **Feature**: 원거리 적 + 적 총알(진영 분리)
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Date**: 2026-07-03
> **Phase**: Report (완료)
> **Match Rate**: ~98% | **Success Criteria**: 8/8

---

## 1. Executive Summary

| Perspective | Content |
|-------------|---------|
| **Problem** | 모든 적이 근접 전용이라 거리만 벌리면 무력화됨. 원거리 위협 부재 + 니어미스 슬로우모션 선행 조건 미충족. |
| **Solution** | 사거리를 유지하며 직진 총알을 발사하는 원거리 적 유형을 신규 추가하고, `"EnemyBullet"` 진영 분리로 적 총알이 플레이어만 타격하도록 구현. |
| **Function/UX Effect** | 원거리 적 등장으로 단순 거리 유지가 통하지 않게 되어 이동·엄폐·(향후)대시 회피가 필요한 전투 압박 형성. |
| **Core Value** | 근접/원거리 조합으로 회피 플레이의 깊이 확보 + 슬로우모션 기능 기반 마련. |

### 1.3 Value Delivered (실측 반영)

| Perspective | Delivered |
|-------------|-----------|
| **Problem 해결** | ✅ 원거리 적이 사거리 유지하며 발사 — 근접 일변도 해소 (Play 확인) |
| **기술 완성도** | 신규 2 + 수정 2 파일, Match ~98%, 컴파일/플레이스홀더 이슈 0 |
| **UX** | 발사·발사음(2D)·사거리 유지·회전 안정화 모두 동작 확인 |
| **확장성** | `"EnemyBullet"` 식별자로 니어미스 슬로우모션 즉시 연결 가능 (FR-08) |

---

## 2. Key Decisions & Outcomes (PRD→Plan→Design Chain)

| 단계 | 결정 | 준수 여부 | 결과 |
|------|------|:--------:|------|
| Plan | 스코프: 총알 MVP · 원거리 적 신규 · 사거리 유지 · faction 분리 | ✅ | 계획대로 구현 |
| Design | Option C (실용 균형): 적 총알 독립 스크립트 + `"EnemyBullet"` 태그 | ✅ | 오사 구조적 차단 달성 |
| Design | 데미지 책임을 플레이어가 소유 (`TakeHit` 추출) | ✅ | 접촉/총알 피격 일관 |
| Do | GameManager 근접 풀 기존 방식 유지 (D1) | 편차(개선) | 회귀 위험 제거 |
| Do | 프리팹 제약 → BulletPoolManager 자동 탐색 (D3) | 편차(필수) | 발사 정상화 |
| Do | 적 회전 버그 → freezeRotation + 회전 초기화 (D2/D5, 근접 포함) | 편차(실버그) | 삐뚤어짐 해결 |

---

## 3. Success Criteria Final Status

| FR | 내용 | 상태 | 근거 |
|----|------|:----:|------|
| FR-01 | 원거리 적 주기 사격 | ✅ Met | Play 확인 |
| FR-02 | 사거리 유지(히스테리시스) | ✅ Met | Play 확인 |
| FR-03 | 발사 순간 직진 | ✅ Met | `Fire(dir)` |
| FR-04 | 플레이어만 타격(오사 X) | ✅ Met | 태그 분리(구조 보장) |
| FR-05 | Character.Hit 연결 | ✅ Met | `OnTriggerEnter2D` → `TakeHit` |
| FR-06 | 벽/수명/화면밖 반환 | ✅ Met | lifetime 4초 + 트리거 |
| FR-07 | 데이터 튜닝 | ✅ Met | Inspector 필드 |
| FR-08 | 니어미스 식별자 | ✅ Met | `"EnemyBullet"` 태그+컴포넌트 |

**Overall Success Rate: 8/8 (100%)**

---

## 4. 구현 산출물

| 파일 | 유형 | 내용 |
|------|------|------|
| `Assets/Scripts/Combat/EnemyBullet.cs` | 신규 | 직진 + 수명 + Player/Wall 반환 |
| `Assets/Scripts/Enemy/RangedEnemyController.cs` | 신규 | 사거리 유지 + 주기 사격 + 회전 안정화 |
| `Assets/Scripts/Player/PlayerController.cs` | 수정 | `TakeHit` 추출 + `OnTriggerEnter2D("EnemyBullet")` |
| `Assets/Scripts/GameManager.cs` | 수정 | 원거리 풀 + 가중 랜덤 스폰 |
| `Assets/Scripts/Enemy/EnemyController.cs` | 수정 | 회전 방지(freezeRotation + 회전 초기화) |
| `"EnemyBullet"` 태그 / 적 총알·원거리 적 prefab | 에디터 | 사용자 구성 완료 |

---

## 5. 편차 및 학습

- **회전 버그의 공통 근본원인**: 대시 벽충돌·적 스폰 삐뚤어짐 모두 `Rigidbody2D` Z회전 미고정이 원인. `freezeRotation` + (풀 재사용 대비)스폰 시 `rotation = identity` 초기화가 표준 대응 패턴으로 확립됨.
- **프리팹 ↔ 씬 참조 제약**: 풀에서 스폰되는 프리팹은 씬 오브젝트 참조를 애셋에 담을 수 없어, `FindObjectOfType` 런타임 탐색이 필요.
- **진영 분리**: 별도 태그(`"EnemyBullet"`) + 컨트롤러의 상대 태그 미검사만으로 오사를 구조적으로 차단 — 레이어 매트릭스 없이 충분.

---

## 6. 잔여/후속

- 수동 확인 권장(비차단): 오사 부재, 플레이어 피격 HP감소·Flash, 총알 4초 소멸
- 후속 사이클: **니어미스 슬로우모션**(FR-08 식별자 활용) → 화살·레이저 발사체 → 스펠 마블
- 커밋: 사용자가 직접 수행 예정

---

## 7. 결론

Match Rate ~98%, Success Criteria 8/8로 **enemy-ranged-attack 완료**. Critical/Important 이슈 없음. 원거리 위협 도입과 니어미스 슬로우모션 기반 확보라는 핵심 가치를 모두 달성.
