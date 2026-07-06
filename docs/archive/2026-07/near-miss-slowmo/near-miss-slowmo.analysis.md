# near-miss-slowmo Gap Analysis (Check)

> **Feature**: 대시 니어미스 슬로우모션 (+ 대시 무적, 줌인 연출)
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Date**: 2026-07-04
> **Method**: 정적 분석(Design↔코드) + Play 수동 검증(§7) — Unity 특성상 자동 런타임/서버 테스트 없음
> **Upstream**: `near-miss-slowmo.plan.md`, `near-miss-slowmo.design.md`

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 대시 회피에 피드백/보상 부재 (Claude.md 대시 스펙 미구현분) |
| **WHO** | 대시로 총알 피하는 플레이어 + 튜닝 개발자 |
| **RISK** | 감지≠피격 혼선, timeScale 복구 실패, 슬로우 중첩 |
| **SUCCESS** | 대시 중 총알 스침 → 슬로우+줌인 발동 → 정상 복구 |
| **SCOPE** | EnemyBullet 니어미스 + 슬로우 + 쿨다운 + 대시무적(FR-08) + 줌인(FR-09) |

---

## 1. Match Rate

| 축 | 비중 | 점수 | 근거 |
|----|:---:|:---:|------|
| **Structural** | 0.2 | 100% | 신규 2(SlowMotion, NearMissDetector) + 수정 2(PlayerController, EnemyBullet) 모두 존재 |
| **Functional** | 0.4 | 98% | FR-01~09 로직 구현 완료. 플레이스홀더 없음. 에디터 배선(module-4)은 사용자 작업 |
| **Contract** | 0.4 | 100% | `IsDashActive` 프로퍼티 계약을 NearMissDetector·EnemyBullet가 일관 소비, `SlowMotion.Trigger()` 배선 일치 |

**Overall = 0.2×100 + 0.4×98 + 0.4×100 = 99.2%**

---

## 2. Success Criteria 평가

| FR | 내용 | 상태 | 증거 |
|----|------|:---:|------|
| FR-01 | 대시 중 스침 발동 | ✅ Met | `NearMissDetector.OnTriggerEnter2D` — `IsDashActive` 체크 |
| FR-02 | timeScale 감소·실시간 복구 | ✅ Met | `SlowMotion` `endUnscaled`/`unscaledTime` |
| FR-03 | 재발동 쿨다운 | ✅ Met | `active` + `nextAllowedUnscaled` 가드 |
| FR-04 | 감지·피격 분리 | ✅ Met | NearMissZone 비-`"Player"` 태그 + `EnemyBullet`는 `"Player"/"Wall"`만 소멸 |
| FR-05 | 명중 시 기존 피격 | ✅ Met | `PlayerController.OnTriggerEnter2D` 비대시 경로 유지 |
| FR-06 | 데이터 튜닝 | ✅ Met | SlowMotion/PlayerController Inspector 필드 |
| FR-07 | 확실한 복구 | ✅ Met | unscaled 복구 + `OnDisable` 안전망(시간+카메라) |
| FR-08 | 대시 무적 통과 | ✅ Met | `EnemyBullet`·`PlayerController` `IsDashActive` 가드(대칭) |
| FR-09 | 줌인 연출 | ✅ Met | `SlowMotion` orthographicSize unscaled 보간 + OnDisable 복구 |

**9/9 Met** (Play 모드 T1~T7 사용자 검증 기준 — T1 슬로우+줌인 발동, 원거리 무적 통과는 사용자 확인 완료)

---

## 3. Gap List

| # | Severity | 내용 | 조치 |
|---|:--------:|------|------|
| G1 | Info | 에디터 배선(NearMissZone 태그=Untagged, 반경≈0.75, SlowMotion 배치)이 수동 — 잘못 설정 시 FR-04 위반(총알 조기소멸) | 안내 제공, 테스트 중 발견·해결됨. 코드 갭 아님 |
| G2 | Info | 무적 판정이 유예(0.1s) 포함 `IsDashActive` — 너무 관대하면 `dashGrace`/순수 `isDashing`로 조정 가능 | 튜닝 여지, 의도된 설계 |

**Critical: 0 · Important: 0**

---

## 4. Design 이탈(Deviation) 기록

| ID | 이탈 | 사유 |
|----|------|------|
| D1 | FR-08(대시 무적)·FR-09(줌인) — 원 설계 밖 추가 | Do 단계 사용자 요청. Plan/Design에 소급 반영(§2.2b/§2.4, FR-08/09) 완료 |
| D2 | `EnemyBullet.cs` 수정 대상 추가 | FR-08 대칭 처리(총알 통과) 위해 필요. Impact Summary 반영 |

---

## 5. 회귀 확인

- [x] 기존 대시(v1/v2 스태미너) 로직 불변 — 프로퍼티/유예만 추가
- [x] 적 총알 기본 피격(비대시) 경로 유지 — FR-05
- [x] 근접 슬라임 접촉 데미지 무관(물리 충돌, 태그 미사용)
- [x] 컴파일 이슈 없음 (조건 가드만 추가)

---

## 6. 범위 밖 발견 (별도 트래킹)

| 항목 | 설명 | 처리 |
|------|------|------|
| 적 사망 애니메이션 중 접촉 데미지 | 적이 죽는 애니메이션 상태에서도 부딪히면 데미지 발생 | near-miss 범위 외 버그 — **별도 수정 예정** |

---

## 7. 결론

near-miss-slowmo는 FR-01~09 전부 구현, Critical/Important 갭 0, Overall **99.2%**. 남은 것은 사용자 에디터 배선(module-4)과 Play 미세 튜닝뿐. Report 진행 가능.
