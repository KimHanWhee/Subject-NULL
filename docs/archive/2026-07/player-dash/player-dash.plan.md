# player-dash Planning Document

> **Summary**: Space 키로 현재 이동 방향으로 짧고 빠르게 대시하는 회피 기동 추가 (무적 없음, 쿨다운 포함)
>
> **Project**: MiniGungeon (Unity)
> **Version**: 0.1 (dev)
> **Author**: KimHanWhee
> **Date**: 2026-07-01
> **Status**: Draft

---

## Executive Summary

| Perspective | Content |
|-------------|---------|
| **Problem** | 플레이어의 회피 수단이 일반 이동(속도 8)뿐이라, 몰려오는 적 사이를 빠르게 빠져나가는 기동성이 부족하다. |
| **Solution** | Space 입력 시 현재 이동 방향으로 짧은 시간(약 0.2초) 고속 이동하는 대시를 추가하고, 쿨다운으로 남용을 막는다. 무적 프레임은 넣지 않는다. |
| **Function/UX Effect** | 순간적인 위치 이동으로 적 무리를 회피/재배치할 수 있어 조작 손맛과 생존 전략성이 늘어난다. |
| **Core Value** | 기동성 강화 — 위험 회피와 포지셔닝이라는 새로운 플레이 선택지를 제공한다. |

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 회피 수단이 일반 이동뿐이라 적 무리 사이 기동성이 부족함 |
| **WHO** | 플레이어(회피·포지셔닝 조작) |
| **RISK** | 대시 중 일반 이동 입력과 충돌/덮어쓰기, 벽 통과, 쿨다운 없이 연속 대시로 밸런스 붕괴 |
| **SUCCESS** | Space 입력 시 이동 방향으로 즉시 대시하고, 대시 종료 후 쿨다운 동안 재대시 불가 |
| **SCOPE** | 단일 파일(PlayerController.cs) 수정. 무적/전용 애니메이션은 제외 |

---

## 1. Overview

### 1.1 Purpose

Space 키를 누르면 플레이어가 현재 이동 방향으로 짧은 시간 동안 고속 이동(대시)하도록 하여 회피·포지셔닝 기동을 제공한다.

### 1.2 Background

MiniGungeon은 시간이 지날수록 적 스폰이 빨라져 화면에 다수의 적이 몰린다. 현재 회피 수단은 일반 이동(`speed = 8`)뿐이라, 적에게 둘러싸였을 때 빠져나갈 폭발적 기동이 없다. 원작 Enter the Gungeon의 회피 구르기에서 착안하되, 이번 범위에서는 **무적 프레임 없이 순수 이동기**로 구현한다.

### 1.3 Related Documents

- **원본 스펙**: `Claude.md` → "추가 예정 기능 > 1. 플레이어 대시 기능"
- 대상 코드: `Assets/Scripts/Player/PlayerController.cs`
- 선행 참고: `docs/01-plan/features/fire-rate.plan.md` (동일한 `Time.time` 기반 쿨다운 패턴 사용)

> **스펙 대비 범위 결정 (사용자 확정)**: `Claude.md` 원본 스펙은 ① 스태미너 소모 ② 니어미스 슬로우모션까지 포함하나,
> 이번 v1에서는 **쿨다운(1초)만** 구현하고 나머지 둘은 별도 기능으로 연기한다 (아래 2.2 참조).

---

## 2. Scope

### 2.1 In Scope

- [ ] Space 입력 감지로 대시 시작
- [ ] 대시 방향 = 현재 이동 입력 방향 (입력 없으면 마지막 이동 방향 / 바라보는 방향)
- [ ] 대시 지속시간 동안 고속 이동 (기본 약 0.2초)
- [ ] 대시 쿨다운 (기본 약 1초) — `Time.time` 기반
- [ ] 대시 중 일반 이동 입력이 대시를 방해하지 않도록 상태 분리
- [ ] 파라미터(속도/지속/쿨다운)를 인스펙터 노출 필드로

### 2.2 Out of Scope (연기/제외)

- **스태미너 시스템** — `Claude.md` 원본 스펙 항목이나 v1에서 연기 (다음 단계 별도 기능). 이번엔 쿨다운으로만 남용 방지.
- **니어미스 슬로우모션** — `Claude.md` 원본 스펙 항목이나 연기.
  > ⚠️ **선행 의존성**: 이 기능은 "적의 원거리 공격(총알/화살/레이저)"을 피하는 것이 발동 조건인데,
  > 현재 `EnemyController`는 근접 충돌만 하고 원거리 공격이 없다. **적 원거리 공격 시스템 구현 후**에나 가능.
- 무적 프레임(i-frame) — 사용자 확정으로 제외 (대시 중에도 피격됨)
- 대시 전용 애니메이션/잔상 이펙트, 대시 사운드
- 연속 대시(대시 캔슬 콤보)

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-01 | Space 입력 시 대시를 시작한다 | High | Pending |
| FR-02 | 대시 방향은 현재 이동 방향, 입력이 없으면 마지막 이동 방향(없으면 바라보는 방향) | High | Pending |
| FR-03 | 대시는 `dashDuration` 동안 `dashSpeed`로 고속 이동한다 | High | Pending |
| FR-04 | 대시 종료 후 `dashCooldown` 동안 재대시 불가 | High | Pending |
| FR-05 | 대시 중에는 일반 이동 입력이 대시 이동을 덮어쓰지 않는다 | High | Pending |

### 3.2 Non-Functional Requirements

| Category | Criteria | Measurement Method |
|----------|----------|-------------------|
| 반응성 | 입력 프레임에 즉시 대시 시작 | Play 모드 확인 |
| 유지보수 | 속도/지속/쿨다운을 코드 수정 없이 인스펙터로 튜닝 | 필드 값 변경 후 확인 |
| 회귀 안전 | 기존 이동·사격·피격 로직에 영향 없음 | Play 모드 회귀 확인 |

---

## 4. Success Criteria

### 4.1 Definition of Done

- [ ] FR-01~FR-05 구현
- [ ] Space로 이동 방향 대시가 되는 것을 Play 모드에서 확인
- [ ] 대시 직후 쿨다운 동안 재대시가 안 되는 것을 확인
- [ ] 대시 중 방향키를 눌러도 대시 궤적이 유지됨

### 4.2 Quality Criteria

- [ ] 컴파일 에러/경고 0
- [ ] 이동/사격/피격/사망 회귀 없음

---

## 5. Risks and Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| 대시 이동과 일반 이동이 같은 프레임에 겹쳐 이중 이동 | Medium | Medium | `isDashing` 상태로 FixedUpdate에서 분기 (대시 중 일반 이동 스킵) |
| 입력 없는 상태에서 대시 방향이 0벡터 → 제자리 대시 | Medium | Medium | `lastMoveDir` 유지, 그래도 0이면 flipX 기반 좌/우 폴백 |
| 쿨다운 없이 연타로 무한 대시 | Medium | Low | `nextDashTime` 게이트로 차단 |
| 대시로 벽을 통과 | Medium | Low | 기존 이동과 동일하게 `transform.Translate` + 콜라이더 사용(기존 벽 처리 방식 계승). 통과 발생 시 Design에서 Rigidbody MovePosition 검토 |

---

## 6. Impact Analysis

### 6.1 Changed Resources

| Resource | Type | Change Description |
|----------|------|--------------------|
| `PlayerController.cs` | Script | 대시 상태/타이머 필드 추가, `Update`에 Space 감지, `FixedUpdate`에 대시 이동 분기 |

### 6.2 Current Consumers

| Resource | Operation | Code Path | Impact |
|----------|-----------|-----------|--------|
| `PlayerController.FixedUpdate()` | 이동 처리 | 기존 `transform.Translate(move * speed * dt)` | 대시 중 분기 추가, 일반 이동 로직 유지 |
| `PlayerController.Update()` | 입력 처리 | 이동/사격 입력 | Space 입력 감지 및 `lastMoveDir` 갱신 추가 |
| `move` 벡터 | READ | FixedUpdate 이동 | 대시 방향 산출에 재사용, 기존 의미 유지 |

### 6.3 Verification

- [ ] 대시 비활성 시 이동은 기존과 100% 동일
- [ ] 사격/피격(OnCollisionStay2D)/사망 흐름에 영향 없음
- [ ] Space가 다른 입력에 바인딩되어 있지 않은지 확인

---

## 7. Architecture Considerations

### 7.1 프로젝트 성격

Unity 2D 탑다운 슈터. 단일 `PlayerController` MonoBehaviour 수정으로 완결되는 소규모 변경.

### 7.2 주요 설계 결정

| Decision | Options | Selected | Rationale |
|----------|---------|----------|-----------|
| 입력 키 | Space / Shift / 우클릭 | **Space** | 이동·사격과 안 겹치는 표준 회피 키 |
| 대시 방향 | 이동 방향 / 마우스 방향 | **이동 방향** | 건전 스타일 회피, 사격과 독립적 조작 |
| 무적 | 있음 / 없음 | **없음** | 사용자 선택 — 순수 이동기 |
| 대시 방식 | 시간 기반 고속이동 / 순간이동 | **시간 기반(약 0.2초)** | 애니메이션·충돌과 자연 연동, 벽 통과 위험 낮음 |
| 쿨다운 방식 | 누적 카운터 / `Time.time` 절대 비교 | **`Time.time` 절대 비교** | fire-rate와 동일 패턴, 드리프트 없음 |

### 7.3 구현 스케치 (참고용, 상세는 Design 단계)

```csharp
// 필드
public float dashSpeed = 24f;      // 대시 속도 (일반 speed 8의 약 3배)
public float dashDuration = 0.2f;  // 대시 지속시간(초)
public float dashCooldown = 1f;    // 대시 쿨다운(초)

private bool isDashing;
private float dashEndTime;
private float nextDashTime;
private Vector3 dashDir;
private Vector3 lastMoveDir = Vector3.right; // 기본 바라보는 방향

// Update(): move 산출 후
if (move.magnitude > 0) lastMoveDir = move;

if (Keyboard.current.spaceKey.wasPressedThisFrame
    && !isDashing
    && Time.time >= nextDashTime)
{
    isDashing = true;
    dashEndTime = Time.time + dashDuration;
    nextDashTime = Time.time + dashCooldown; // 쿨다운은 시작 기준
    dashDir = (move.magnitude > 0 ? move : lastMoveDir).normalized;
}

// FixedUpdate()
if (isDashing)
{
    if (Time.time >= dashEndTime) { isDashing = false; }
    else { transform.Translate(dashDir * (dashSpeed * Time.fixedDeltaTime)); return; }
}
transform.Translate(move * (speed * Time.fixedDeltaTime));
```

---

## 8. Convention Prerequisites

### 8.1 기존 컨벤션

- [x] 기존 코드 스타일: MonoBehaviour, PascalCase 메서드, camelCase private 필드, `Keyboard/Mouse.current` InputSystem — 이를 따른다
- [x] `Time.time` 기반 쿨다운 패턴 (fire-rate에서 확립) 재사용

### 8.2 확인/준수 사항

| Category | Current State | To Define | Priority |
|----------|---------------|-----------|:--------:|
| 입력 처리 | InputSystem 사용 중 | `Keyboard.current.spaceKey` 사용 | High |
| 네이밍 | 기존 패턴 존재 | `isDashing`, `dashEndTime`, `nextDashTime` 등 | High |

---

## 9. Next Steps

1. [ ] Design 문서 작성 (`player-dash.design.md`) — 상태 전이/방향 산출/벽 처리 상세화
2. [ ] 구현 (`PlayerController.cs` 수정)
3. [ ] Play 모드 검증 (방향/쿨다운/일반 이동 회귀)

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-01 | Initial draft | KimHanWhee |
