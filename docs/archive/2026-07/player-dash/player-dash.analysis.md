# player-dash Gap Analysis (Check Phase)

> **Project**: MiniGungeon (Unity)
> **Feature**: player-dash (v1)
> **Date**: 2026-07-01
> **Design Doc**: [player-dash.design.md](../02-design/features/player-dash.design.md)
> **Plan Doc**: [player-dash.plan.md](../01-plan/features/player-dash.plan.md)

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 회피 수단이 일반 이동뿐이라 적 무리 사이 기동성이 부족함 |
| **WHO** | 플레이어(회피·포지셔닝 조작) |
| **RISK** | 대시/일반 이동 겹침, 벽 통과, 쿨다운 없는 연속 대시 |
| **SUCCESS** | Space 시 이동 방향 즉시 대시, 쿨다운 동안 재대시 불가 |
| **SCOPE** | 단일 파일(PlayerController.cs), 스태미너·슬로우모션 제외(연기) |

---

## 1. Strategic Alignment Check

| 질문 | 판정 |
|------|------|
| Claude.md 스펙의 핵심 문제(기동성)를 해결했는가? | ✅ Space 대시로 회피 기동 제공 |
| Plan Success Criteria가 충족되는가? | ✅ FR-01~05 모두 충족 (아래) |
| 핵심 설계 결정(Option C, Time.time 쿨다운)이 지켜졌는가? | ✅ TryStartDash/TickDash 캡슐화 그대로 구현 |

> 전략적 미정렬 없음.

---

## 2. Plan Success Criteria 평가

| ID | 기준 | 판정 | 근거 (file:symbol) |
|----|------|------|--------------------|
| FR-01 | Space 입력 시 대시 시작 | ✅ Met | `PlayerController.TryStartDash()` — `spaceKey.wasPressedThisFrame` |
| FR-02 | 이동 방향, 없으면 lastMoveDir 폴백 | ✅ Met | `dashDir = (move.magnitude>0 ? move : lastMoveDir).normalized`; `lastMoveDir` 갱신 `Update()` |
| FR-03 | dashDuration 동안 dashSpeed 이동 | ✅ Met | `TickDash()` — `dashEndTime` 검사 + `rb.MovePosition` |
| FR-04 | dashCooldown 동안 재대시 불가 | ✅ Met | `Time.time >= nextDashTime` 게이트, `nextDashTime = 시작+dashCooldown` |
| FR-05 | 대시 중 일반 이동 미덮어씀 | ✅ Met | `!isDashing` 가드 + `FixedUpdate`의 `if (TickDash()) return;` |

**Success Rate: 5/5 (100%)**

---

## 3. 정적 갭 분석 (Static)

### 3.1 Structural Match — 100%

| 설계 항목 | 구현 | 상태 |
|-----------|------|------|
| §3.1 인스펙터 필드 (dashSpeed/Duration/Cooldown) | 존재 | ✅ |
| §3.2 내부 상태 (isDashing/dashEndTime/nextDashTime/dashDir/lastMoveDir) | 존재 | ✅ |
| §4.1 TryStartDash() | 존재 | ✅ |
| §4.2 TickDash() | 존재 | ✅ |
| §5 Update/FixedUpdate 통합 지점 | 반영 | ✅ |

### 3.2 Functional Depth — 100%

- 플레이스홀더/미구현 없음. 모든 FR이 실제 로직으로 동작.
- 엣지 케이스(설계 §6) 처리 확인: 입력 없을 때 폴백, 대시 중 재입력 무시, 쿨다운 위반 차단.

### 3.3 Contract (설계 의도 ↔ 코드) — 100%

- 메서드 시그니처/책임이 설계 §4와 일치. `TickDash()`가 bool 반환으로 이동 소유권 조율.

**정적 Overall = Structural×0.2 + Functional×0.4 + Contract×0.4 = 100%**

---

## 4. Runtime Verification (Unity Play 모드 — 수동)

자동화 테스트 인프라 없음 → 수동 검증.

| # | 시나리오 | 상태 |
|---|----------|------|
| 1 | 기본 대시 (이동 방향) | ✅ 사용자 확인 |
| 6 | 벽 관통 회귀 | ✅ 사용자 확인 ("이제 괜찮다") — MovePosition+Continuous로 해결 |
| 2 | 대각선 방향 정확도 | ⚠️ 미확인 (코드상 정상) |
| 3 | 정지 중 대시 (폴백) | ⚠️ 미확인 (코드상 정상) |
| 4 | 쿨다운 재대시 차단 | ⚠️ 미확인 (코드상 정상) |
| 5 | 대시 중 방향키 궤적 유지 | ⚠️ 미확인 (코드상 정상) |

> 핵심 시나리오(1, 6)는 확인됨. 나머지는 정적으로 정상이나 빠른 수동 확인 권장.

---

## 5. Decision Record Verification

| 결정 | 준수 여부 | 비고 |
|------|-----------|------|
| Option C (전용 메서드 캡슐화) | ✅ 준수 | TryStartDash/TickDash |
| Time.time 쿨다운 | ✅ 준수 | nextDashTime |
| 무적 없음 (v1) | ✅ 준수 | 피격 로직 그대로 |
| 대시 이동 방식 | ⚠️ **긍정적 편차** | 설계 §4.2 기본은 `transform.Translate`였으나, §6 우발 대응(벽 관통 시 MovePosition 전환)을 실제로 적용. 설계 문서 갱신 필요 |

---

## 6. Gap 목록 (심각도별)

**Critical: 없음**
**Important: 없음**

**Minor (참고):**

| # | 내용 | 권장 조치 |
|---|------|-----------|
| M1 | 설계 문서 §4.2/§6은 `transform.Translate` 기준이나 구현은 `rb.MovePosition` 사용 (문서 드리프트) | 설계 문서를 MovePosition 기준으로 갱신 |
| M2 | 일반 이동은 `transform.Translate`, 대시는 `MovePosition` — Dynamic Rigidbody2D에서 두 이동 방식 혼용 | 현재 정상 동작. 추후 일반 이동도 MovePosition 통일 검토 |
| M3 | 런타임 시나리오 2~5 미검증 | 빠른 Play 확인 |

---

## 7. Match Rate 종합

| 축 | 비율 |
|----|------|
| Structural | 100% |
| Functional | 100% |
| Contract | 100% |
| **정적 Overall** | **100%** |
| 런타임(수동) | 핵심 2/6 확인, 나머지 정적 정상 |

**결론: Match Rate 100% (정적) — Critical/Important 갭 없음. Report 진행 가능.**
Minor 3건은 품질 개선 항목으로, 차단 요소 아님.

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 0.1 | 2026-07-01 | Initial gap analysis |
