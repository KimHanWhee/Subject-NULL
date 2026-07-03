# player-dash Design Document

> **Summary**: Space 키로 이동 방향 대시(v1, 쿨다운만)를 PlayerController 내 전용 메서드로 캡슐화하여 구현
>
> **Project**: MiniGungeon (Unity)
> **Version**: 0.1 (dev)
> **Author**: KimHanWhee
> **Date**: 2026-07-01
> **Status**: Draft
> **Planning Doc**: [player-dash.plan.md](../../01-plan/features/player-dash.plan.md)
> **원본 스펙**: `Claude.md` → 추가 예정 기능 1. 플레이어 대시

---

## Context Anchor

> Plan 문서에서 복사. Design→Do 핸드오프 시 전략 컨텍스트 유지.

| Key | Value |
|-----|-------|
| **WHY** | 회피 수단이 일반 이동뿐이라 적 무리 사이 기동성이 부족함 |
| **WHO** | 플레이어(회피·포지셔닝 조작) |
| **RISK** | 대시 이동과 일반 이동의 겹침/덮어쓰기, 벽 통과, 쿨다운 없는 연속 대시 |
| **SUCCESS** | Space 입력 시 이동 방향으로 즉시 대시, 대시 후 쿨다운 동안 재대시 불가 |
| **SCOPE** | 단일 파일(PlayerController.cs). 스태미너·슬로우모션·무적·전용 애니메이션 제외(연기) |

---

## 1. Overview

### 1.1 Design Goals

- Space 입력 → 이동 방향으로 짧고 빠른 대시, 쿨다운(1초)으로 남용 방지
- 기존 이동/사격/피격 로직에 회귀 없이 통합
- 향후 스태미너·니어미스 슬로우모션 확장 지점을 대시 메서드에 국소화

### 1.2 Design Principles

- **Single Responsibility**: 대시 상태·전이는 전용 메서드(`TryStartDash`, `TickDash`)로 응집
- **기존 패턴 재사용**: `Time.time` 절대 비교 쿨다운(fire-rate와 동일), InputSystem `Keyboard.current`
- **확장 대비**: 대시 시작/종료 지점을 명확히 분리해 스태미너 차감·슬로우모션 훅을 나중에 삽입 가능

---

## 2. Architecture

### 2.0 Architecture Selection

**Selected: Option C — Pragmatic**

| Criteria | Option A: Minimal | Option B: Clean | Option C: Pragmatic |
|----------|:-:|:-:|:-:|
| **Approach** | 인라인 삽입 | 별도 PlayerDash 컴포넌트 | 전용 메서드로 캡슐화 |
| **New Files** | 0 | 1 | 0 |
| **Modified Files** | 1 | 1~2 | 1 |
| **Complexity** | Low | High | Medium |
| **Maintainability** | Medium | High | High |
| **Effort** | Low | High | Medium |
| **Recommendation** | 빠른 구현 | 대시 공유 계획 시 | **기본값** |

**Rationale**: 단일 파일 규모라 별도 컴포넌트(B)는 과하고, 인라인(A)은 대시 코드가 Update/FixedUpdate에 분산되어 확장 시 지저분해진다. 전용 메서드(C)로 응집도를 확보하면 연기된 스태미너/슬로우모션 확장이 국소적으로 붙는다.

### 2.1 Component Diagram

```
┌─────────────────────────────────────────────────┐
│ PlayerController (MonoBehaviour)                │
│                                                 │
│  Update()                                       │
│    ├─ 이동 입력 산출 (move, lastMoveDir 갱신)   │
│    ├─ 사격 (기존)                               │
│    └─ TryStartDash()  ◀── Space 감지·쿨다운 게이트│
│                                                 │
│  FixedUpdate()                                  │
│    └─ if (TickDash()) return;  ◀── 대시 이동 우선 │
│       else transform.Translate(일반 이동)        │
└─────────────────────────────────────────────────┘
```

### 2.2 Data Flow (State Transition)

```
[Idle/Move] ──Space && Time.time>=nextDashTime──▶ [Dashing]
   ▲                                                  │
   │                                     Time.time>=dashEndTime
   └──────────────────────────────────────────────────┘
              (쿨다운: nextDashTime = 시작시각 + dashCooldown)
```

- **Idle/Move**: 일반 이동. Space 입력 & 쿨다운 완료 시 Dashing 진입.
- **Dashing**: `dashDir`로 `dashSpeed` 고속 이동. 일반 이동 입력 무시. `dashDuration` 경과 시 복귀.
- 쿨다운은 **대시 시작 시각 기준**으로 설정(시작~시작+1초). 지속시간(0.2s)이 쿨다운(1s)보다 짧으므로 연속 대시 불가.

---

## 3. State & Fields

### 3.1 Inspector Fields (튜닝 파라미터)

| Field | Type | Default | 설명 |
|-------|------|:-------:|------|
| `dashSpeed` | float | 24 | 대시 속도 (일반 speed 8의 3배) |
| `dashDuration` | float | 0.2 | 대시 지속시간(초) |
| `dashCooldown` | float | 1.0 | 대시 쿨다운(초) — Claude.md 스펙 |

### 3.2 Internal State

| Field | Type | 초기값 | 설명 |
|-------|------|:------:|------|
| `isDashing` | bool | false | 대시 중 여부 |
| `dashEndTime` | float | 0 | 대시 종료 시각 (Time.time 기준) |
| `nextDashTime` | float | 0 | 다음 대시 허용 시각 |
| `dashDir` | Vector3 | - | 대시 진행 방향(정규화) |
| `lastMoveDir` | Vector3 | (1,0,0) | 마지막 이동 방향(입력 없을 때 폴백) |

---

## 4. Method Design

### 4.1 `TryStartDash()` — Update에서 호출

```
if (spaceKey.wasPressedThisFrame && !isDashing && Time.time >= nextDashTime):
    dashDir = (move.magnitude > 0 ? move : lastMoveDir).normalized
    isDashing = true
    dashEndTime  = Time.time + dashDuration
    nextDashTime = Time.time + dashCooldown   // 시작 기준 쿨다운
    // [확장 훅] 스태미너 차감 위치
```

### 4.2 `TickDash()` — FixedUpdate에서 호출, 대시 처리 여부 bool 반환

```
if (!isDashing) return false
if (Time.time >= dashEndTime):
    isDashing = false
    return false
transform.Translate(dashDir * dashSpeed * Time.fixedDeltaTime)
return true   // 이번 프레임 이동은 대시가 처리 → 일반 이동 스킵
```

### 4.3 `lastMoveDir` 갱신 — Update의 move 산출 직후

```
if (move.magnitude > 0) lastMoveDir = move
```

입력이 완전히 없고 `lastMoveDir`도 이론상 0일 가능성은 초기값 (1,0,0)으로 방지. 필요 시 `sr.flipX` 기반 좌/우 폴백 추가 가능(현재 초기값으로 충분).

---

## 5. Integration Points (기존 코드 변경)

| 위치 | 변경 |
|------|------|
| 필드 영역 | §3.1 인스펙터 필드 + §3.2 내부 상태 추가 |
| `Update()` move 산출 후 | `lastMoveDir` 갱신 |
| `Update()` 사격 분기 뒤 | `TryStartDash()` 호출 |
| `FixedUpdate()` 최상단 | `if (TickDash()) return;` → 그 뒤 기존 일반 이동 |
| 신규 메서드 | `TryStartDash()`, `TickDash()` 추가 |

> 기존 이동(`transform.Translate(move * speed * dt)`), 사격, `OnCollisionStay2D` 피격, `Die()`는 변경 없음.

---

## 6. Error / Edge Cases

| Case | 처리 |
|------|------|
| 입력 없이 대시 | `lastMoveDir`(기본 (1,0,0))로 대시 → 제자리 대시 방지 |
| 대시 중 재입력(Space) | `!isDashing` 가드로 무시 |
| 쿨다운 중 대시 | `Time.time >= nextDashTime` 가드로 무시 |
| 대시 중 피격 | v1은 무적 없음 → 정상 피격(의도된 동작) |
| 대시로 벽 접근 | 기존 이동과 동일하게 콜라이더가 처리. Play 검증에서 관통 발견 시 Rigidbody2D `MovePosition` 전환 검토 |
| `dashDuration >= dashCooldown` 오설정 | 연속 대시 가능해짐 → 문서상 권장값(0.2 < 1.0) 준수. 코드 방어는 하지 않음(튜닝 책임) |

---

## 8. Test Plan (Unity Play 모드 수동 검증)

> 이 프로젝트는 자동화 테스트 인프라(Playwright 등)가 없어, Play 모드 수동 시나리오로 검증한다.

### 8.1 검증 시나리오

| # | 시나리오 | 조작 | 기대 결과 | 매핑 |
|---|----------|------|-----------|------|
| 1 | 기본 대시 | 우측 이동 중 Space | 우측으로 순간 고속 이동 | FR-01, FR-03 |
| 2 | 방향 정확도 | 대각선(예: W+D) 이동 중 Space | 해당 대각선 방향 대시 | FR-02 |
| 3 | 정지 중 대시 | 이동 없이 Space | 마지막 이동 방향(초기엔 우측)으로 대시 | FR-02 |
| 4 | 쿨다운 | 대시 직후 즉시 Space 연타 | 1초 전엔 재대시 안 됨 | FR-04 |
| 5 | 대시 중 방향키 | 대시 중 반대 방향키 입력 | 대시 궤적 유지(덮어쓰기 없음) | FR-05 |
| 6 | 회귀 | 대시 미사용 시 일반 이동/사격/피격 | 기존과 동일 | 회귀 안전 |

---

## 10. Coding Convention

| 항목 | 적용 |
|------|------|
| 메서드 | PascalCase (`TryStartDash`, `TickDash`) |
| private 필드 | camelCase (`isDashing`, `dashEndTime`, `nextDashTime`) |
| 인스펙터 필드 | public camelCase (`dashSpeed`, `dashDuration`, `dashCooldown`) |
| 입력 API | `Keyboard.current.spaceKey.wasPressedThisFrame` |
| 주석 | 핵심 결정에 `// Design Ref: §4` / `// Plan SC: FR-0X` |

---

## 11. Implementation Guide

### 11.2 Implementation Order

1. [ ] 필드 추가 (§3.1 인스펙터 + §3.2 내부 상태) — `// Plan SC: FR-03,FR-04`
2. [ ] `Update()`에 `lastMoveDir` 갱신 + `TryStartDash()` 호출 — `// Plan SC: FR-01,FR-02`
3. [ ] `TryStartDash()` / `TickDash()` 메서드 추가 — `// Design Ref: §4`
4. [ ] `FixedUpdate()` 최상단에 `if (TickDash()) return;` — `// Plan SC: FR-05`
5. [ ] Play 모드 §8.1 시나리오 검증

### 11.3 Session Guide

#### Module Map

| Module | Scope Key | Description | Estimated Turns |
|--------|-----------|-------------|:---------------:|
| 대시 코어 | `dash-core` | 필드 + TryStartDash/TickDash + FixedUpdate 분기 (전체) | 1 |

> 단일 모듈이라 세션 분할 불필요. `/pdca do player-dash`로 한 번에 구현 권장.

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-01 | Initial draft (Option C 선택) | KimHanWhee |
