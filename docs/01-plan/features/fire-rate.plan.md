# fire-rate Planning Document

> **Summary**: `WeaponData.fireRate`를 실제로 사용해, 마우스 좌클릭을 누르고 있는 동안 발사 간격을 제어하는 연사(hold-to-fire) 시스템 구현
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
| **Problem** | `WeaponData.fireRate` 필드가 선언만 되어 있고 `PlayerController.Shoot()`에서 사용되지 않아, 마우스 연타로 무한 발사가 가능하고 무기별 연사 속도 밸런싱이 불가능하다. |
| **Solution** | `PlayerController`에 발사 쿨다운 타이머를 추가하고, `Update()`의 발사 트리거를 `wasPressedThisFrame`(단발) → `isPressed`(홀드) + fireRate 간격 검사로 변경한다. |
| **Function/UX Effect** | 좌클릭을 누르고 있으면 무기의 `fireRate`(초당 발사 수)에 맞춰 자동 연사된다. 무기 데이터만 바꾸면 연사 속도가 조절되어 향후 무기 밸런싱의 기반이 된다. |
| **Core Value** | 데이터 기반 무기 설계 완성 — 코드 수정 없이 `WeaponData` 값만으로 사격감을 튜닝할 수 있다. |

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | fireRate 필드가 사용되지 않아 무한 발사가 가능하고 무기 밸런싱이 불가능함 |
| **WHO** | 플레이어(사격 조작), 향후 무기 데이터를 튜닝할 개발자 |
| **RISK** | 프레임레이트 의존/부동소수 오차로 연사 간격이 불안정해지거나, 홀드 방식 전환 시 사운드가 매 발사마다 겹쳐 재생됨 |
| **SUCCESS** | 좌클릭 홀드 시 1/fireRate 초 간격으로 정확히 발사되고, 연타로도 그 간격을 넘겨 발사할 수 없음 |
| **SCOPE** | 단일 파일(PlayerController.cs) 수정 중심. 무기 교체·HP UI는 명시적 제외 |

---

## 1. Overview

### 1.1 Purpose

마우스 좌클릭을 누르고 있는 동안 무기의 `fireRate`에 따라 자동 연사되도록 하여, 선언만 되어 있던 `WeaponData.fireRate` 필드를 실제 게임 로직에 연결한다.

### 1.2 Background

현재 `PlayerController.Update()`는 `Mouse.current.leftButton.wasPressedThisFrame` 시 매번 `Shoot()`을 호출한다. 이 방식은:
- 클릭 1회 = 발사 1회로, 물리적으로 빠르게 연타하면 발사 속도 제한이 없다.
- `WeaponData.fireRate` 값이 게임 플레이에 아무 영향을 주지 않는다 (dead field).

`WeaponData`는 이미 damage, bulletSpeed 등을 데이터로 분리한 확장 가능한 구조이므로, fireRate만 연결하면 데이터 기반 무기 설계가 완성된다.

### 1.3 Related Documents

- 대상 코드: `Assets/Scripts/Player/PlayerController.cs`
- 무기 데이터: `Assets/Scripts/Weapon/WeaponData.cs`, `Assets/Weapons/DefaultData.asset` (현재 `fireRate: 1`)

---

## 2. Scope

### 2.1 In Scope

- [ ] `PlayerController`에 발사 쿨다운 타이머 필드 추가
- [ ] `Update()`의 발사 조건을 `isPressed`(홀드) + fireRate 간격 검사로 변경
- [ ] `fireRate`를 "초당 발사 수"로 해석 (발사 간격 = 1 / fireRate)
- [ ] `fireRate <= 0` 방어 처리 (0 나눗셈 방지)

### 2.2 Out of Scope

- 무기 교체(currentWeapon 런타임 변경) — 사용자 요청으로 제외
- 플레이어 HP UI(heartPoint) 연결 점검 — 사용자 요청으로 제외
- 탄 퍼짐/샷건형 다중 발사, 재장전, 탄약 시스템

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-01 | 좌클릭을 누르고 있는 동안 자동으로 반복 발사한다 | High | Pending |
| FR-02 | 발사 간격은 `currentWeapon.fireRate` 기반 (간격 = 1/fireRate 초) | High | Pending |
| FR-03 | 마우스 연타로도 fireRate가 정한 간격보다 빠르게 발사할 수 없다 | High | Pending |
| FR-04 | `fireRate <= 0`일 때 크래시 없이 안전하게 처리한다 | Medium | Pending |

### 3.2 Non-Functional Requirements

| Category | Criteria | Measurement Method |
|----------|----------|-------------------|
| 정확도 | 발사 간격 오차가 프레임 시간 이내 | Play 모드에서 발사 로그/시각 확인 |
| 성능 | Update 내 추가 연산은 상수 시간(타이머 비교 1회) | 코드 리뷰 |
| 유지보수 | 코드 수정 없이 WeaponData 값만으로 연사 속도 조절 가능 | DefaultData.asset의 fireRate 변경 후 확인 |

---

## 4. Success Criteria

### 4.1 Definition of Done

- [ ] FR-01~FR-04 모두 구현
- [ ] 좌클릭 홀드 시 `fireRate`에 맞춰 연사됨을 Play 모드에서 확인
- [ ] 연타해도 발사 간격이 유지됨을 확인
- [ ] 발사 사운드가 발사 시점마다 정상 재생됨

### 4.2 Quality Criteria

- [ ] 컴파일 에러/경고 0
- [ ] 기존 이동·피격·사망 로직에 회귀 없음

---

## 5. Risks and Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| 부동소수 누적 오차로 간격이 미세하게 드리프트 | Low | Medium | `Time.time` 기준 절대 비교(`Time.time >= nextFireTime`) 사용 |
| `fireRate` 0 또는 음수 시 1/fireRate 무한대/음수 | Medium | Low | `fireRate <= 0` 가드로 최소 간격 또는 단발 폴백 |
| 홀드 연사로 사운드가 과도하게 겹쳐 재생 | Low | Medium | 발사 성공 시점에만 `PlayOneShot` 호출 (간격 제어에 의해 자연 제한됨) |
| currentWeapon이 null | Medium | Low | 발사 조건에서 null 체크 후 진입 |

---

## 6. Impact Analysis

### 6.1 Changed Resources

| Resource | Type | Change Description |
|----------|------|--------------------|
| `PlayerController.cs` | Script | `Update()` 발사 트리거 변경 + 쿨다운 타이머 필드/로직 추가 |

### 6.2 Current Consumers

| Resource | Operation | Code Path | Impact |
|----------|-----------|-----------|--------|
| `PlayerController.Shoot()` | 호출 | `PlayerController.Update()` (좌클릭 시) | 변경 대상 — 호출 조건만 교체, Shoot 내부 로직은 유지 |
| `WeaponData.fireRate` | READ | (현재 소비처 없음 / dead field) | 신규 소비처 추가, 기존 영향 없음 |
| `currentWeapon` | READ | `Shoot()` 내부 (bulletPrefab, damage, bulletSpeed, shotSound) | 변경 없음 |

### 6.3 Verification

- [ ] `Shoot()` 내부 로직(총알 풀·방향·사운드)은 그대로 동작
- [ ] 이동/애니메이션/피격 등 `Update()`의 다른 분기에 영향 없음
- [ ] fireRate를 여러 값으로 바꿔도 정상 동작

---

## 7. Architecture Considerations

### 7.1 프로젝트 성격

Unity 2D 탑다운 슈터. 단일 `PlayerController` MonoBehaviour 수정으로 완결되는 소규모 변경(Starter 수준 복잡도).

### 7.2 주요 설계 결정

| Decision | Options | Selected | Rationale |
|----------|---------|----------|-----------|
| fireRate 의미 | 초당 발사 수 / 발사 간격(초) | **초당 발사 수** | 이름 관례상 rate=빈도. 간격 = 1/fireRate. 현재 값 1이면 초당 1발 |
| 타이머 방식 | 누적 카운터(-=deltaTime) / 절대 시각(Time.time 비교) | **Time.time 절대 비교** | 드리프트 없음, 코드 간결 (`nextFireTime` 갱신) |
| 입력 감지 | wasPressedThisFrame(단발) / isPressed(홀드) | **isPressed** | 사용자 선택: 누르고 있으면 연사 |
| 발사 위치 | Update / FixedUpdate | **Update** | 입력·발사는 프레임 단위 처리가 자연스러움(기존과 동일) |

### 7.3 구현 스케치 (참고용, 상세는 Design 단계)

```csharp
// PlayerController 필드
private float nextFireTime;   // 다음 발사 허용 시각

// Update() 내 기존 좌클릭 분기 교체
if (Mouse.current.leftButton.isPressed
    && currentWeapon != null
    && Time.time >= nextFireTime)
{
    Shoot();
    float rate = Mathf.Max(currentWeapon.fireRate, 0.0001f); // 0 나눗셈 방지
    nextFireTime = Time.time + 1f / rate;
}
```

---

## 8. Convention Prerequisites

### 8.1 기존 컨벤션

- [ ] `CLAUDE.md` 코딩 컨벤션 섹션 (없음)
- [x] 기존 코드 스타일: MonoBehaviour, PascalCase 메서드, camelCase private 필드 — 이를 따른다

### 8.2 확인/준수 사항

| Category | Current State | To Define | Priority |
|----------|---------------|-----------|:--------:|
| 네이밍 | 기존 패턴 존재 | camelCase 필드(`nextFireTime`), PascalCase 메서드 유지 | High |
| 입력 처리 | 기존 `Keyboard/Mouse.current` 사용 | 동일 InputSystem API 유지 | High |

---

## 9. Next Steps

1. [ ] Design 문서 작성 (`fire-rate.design.md`) — 타이머 로직/엣지케이스 상세화
2. [ ] 구현 (`PlayerController.cs` 수정)
3. [ ] Play 모드 검증 (연사 간격, 연타 방지, 사운드)

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-01 | Initial draft | KimHanWhee |
