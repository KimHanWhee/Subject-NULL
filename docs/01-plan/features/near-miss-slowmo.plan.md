# near-miss-slowmo Planning Document

> **Summary**: 플레이어가 **대시 중(또는 대시 직후 유예 내)** 적 총알을 **아슬아슬하게 스쳐 지나가면** 잠깐 슬로우모션(`Time.timeScale` 감소 후 복구)을 발동한다.
>
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Version**: 0.1 (dev)
> **Author**: KimHanWhee
> **Date**: 2026-07-03
> **Status**: Draft

---

## Executive Summary

| Perspective | Content |
|-------------|---------|
| **Problem** | 대시로 적 총알을 회피해도 아무런 피드백/보상이 없어, 회피의 쾌감과 리스크-리워드가 약하다. Claude.md 대시 스펙의 니어미스 슬로우모션이 미구현. |
| **Solution** | 플레이어 자식에 넓은 트리거 콜라이더를 두고, 대시 중 적 총알이 근접 반경에 진입하면(맞지 않고 스침) 짧은 슬로우모션을 발동한다. 이미 만든 `"EnemyBullet"` 식별자를 활용. |
| **Function/UX Effect** | 총알을 대시로 아슬아슬하게 피하면 게임이 잠깐 느려지고 카메라가 줌인되는 불릿타임 연출 → 회피 성공이 극적으로 체감된다. 대시 중엔 총알이 몸을 통과(닷지롤 무적). |
| **Core Value** | 회피 플레이의 리스크-리워드 강화 — 아슬아슬할수록 보상되는 능동적 회피 유도. |

> **Do 단계 확장(2026-07-04)**: 구현 중 사용자 요청으로 두 요구사항 추가 — FR-08(대시 무적 프레임), FR-09(줌인 연출). 원 스코프(감지·슬로우·쿨다운)와 자연스럽게 결합됨.

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 대시 회피에 피드백/보상이 없음. Claude.md 대시 스펙의 니어미스 슬로우모션 미구현 |
| **WHO** | 플레이어(대시로 총알을 회피하는 조작), 튜닝할 개발자 |
| **RISK** | 근접 감지가 실제 피격과 섞여 총알이 조기 소멸/무피해 통과, timeScale 복구 실패로 게임이 계속 느림, 연속 발동으로 슬로우 중첩 |
| **SUCCESS** | 대시 중 총알이 근접 반경을 스치면(맞지 않음) 슬로우모션이 발동하고 실시간 지속 후 정상 속도로 복구된다 |
| **SCOPE** | 적 총알(EnemyBullet) 대상 니어미스 + timeScale 슬로우 + 쿨다운. 화살/레이저 대상 확장·연출(파티클/사운드)은 최소/후속 |

---

## 1. Overview

### 1.1 Purpose

플레이어가 대시로 적 총알을 아슬아슬하게 회피했을 때 짧은 슬로우모션을 발동하여, 회피의 쾌감과 리스크-리워드를 강화한다.

### 1.2 Background

`enemy-ranged-attack` 사이클에서 적 총알(`"EnemyBullet"` 태그 + `EnemyBullet` 컴포넌트)과 플레이어 대시가 이미 구현되어 있다. 다만:

- **`PlayerController.isDashing`이 private** → 니어미스 판정에서 대시 여부를 알 수 없음 → 노출 필요.
- **플레이어의 실제 피격 콜라이더로 니어미스를 판정하면 안 됨**: 총알의 `OnTriggerEnter2D`가 `"Player"` 태그에 소멸하므로, 넓은 감지 콜라이더를 `"Player"`로 두면 총알이 스치기만 해도 조기 소멸한다. ∴ **피격과 분리된 별도 감지 콜라이더**가 필요.
- **`Time.timeScale` 슬로우는 복구 타이밍을 실시간(unscaled)으로 재야** 슬로우 중에도 정확히 복구된다.

### 1.3 Related Documents

- 대상/참조 코드: `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Combat/EnemyBullet.cs`
- 선행 사이클: `docs/archive/2026-07/enemy-ranged-attack/` (EnemyBullet, 대시)
- 로드맵: `Claude.md` — 대시 니어미스 슬로우모션
- 후속: 화살/레이저 발사체(니어미스 대상 확장), 슬로우모션 연출(파티클/사운드)

---

## 2. Scope

### 2.1 In Scope

- [ ] `PlayerController.isDashing`(및 유예 포함) 대시 상태 노출
- [ ] 플레이어 자식에 넓은 트리거 콜라이더 기반 근접 감지 (`NearMissDetector`)
- [ ] 대시 중 + 짧은 유예(대시 직후) 내에 적 총알이 감지 반경 진입 시 슬로우모션 발동
- [ ] `Time.timeScale` 감소 → 실시간 지속(0.35s) 후 1.0 복구 (`SlowMotion` 제어)
- [ ] 슬로우모션 재발동 쿨다운(1초)
- [ ] 감지 반경·슬로우 강도(0.3)·지속·쿨다운·유예 데이터 튜닝
- [ ] 실제 피격(총알이 플레이어에 명중)과 니어미스의 분리

### 2.2 Out of Scope

- 화살/레이저 등 다른 발사체 대상 (현재 EnemyBullet만 존재 → 후속 자동 확장 여지)
- 슬로우모션 시각/청각 연출(파티클, 후처리, 전용 사운드) — 최소 구현
- 근접 근접도에 따른 슬로우 강도 가변 (고정값)
- 플레이어 근접 접촉 적(근접 슬라임) 대상 니어미스

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-01 | 대시 중(또는 대시 직후 유예 내) 적 총알이 근접 감지 반경에 진입하면 슬로우모션 발동 | High | Pending |
| FR-02 | 슬로우모션은 `Time.timeScale`을 낮춘 뒤 **실시간** 지속시간 후 1.0으로 복구 | High | Pending |
| FR-03 | 슬로우모션에 재발동 쿨다운을 적용해 중첩/연속 발동을 막는다 | Medium | Pending |
| FR-04 | 근접 감지는 실제 피격과 분리 — 니어미스로 총알이 소멸/데미지 없이 통과 | High | Pending |
| FR-05 | 총알이 실제로 플레이어에 명중하면 슬로우모션이 아니라 기존 피격 처리 | High | Pending |
| FR-06 | 감지 반경·슬로우 강도·지속·쿨다운·유예를 Inspector에서 튜닝 가능 | Medium | Pending |
| FR-07 | 슬로우모션 중에도 게임이 정상 속도로 확실히 복구된다(멈춤/느림 잔존 없음) | High | Pending |
| FR-08 | 대시 중(닷지롤 무적 프레임)에는 적 총알이 통과하며 소멸·데미지 없음 | High | Pending |
| FR-09 | 니어미스 발동 시 카메라 줌인 펀치 연출(슬로우와 동기화, 복구 시 줌아웃) | Medium | Pending |

### 3.2 Non-Functional Requirements

| Category | Criteria | Measurement Method |
|----------|----------|-------------------|
| 정확도 | 대시가 아닐 때는 스쳐도 발동 안 함 | Play 모드 확인 |
| 안정성 | 슬로우 후 timeScale이 항상 1.0로 복구 | 반복 발동 후 확인 |
| 성능 | 감지는 물리 트리거 이벤트 기반(상수 시간) | 코드 리뷰 |
| 유지보수 | 코드 수정 없이 반경/강도/지속/쿨다운 튜닝 | Inspector 값 변경 후 확인 |
| 회귀 | 기존 대시·적 총알·피격 로직에 영향 없음 | 기존 플레이 확인 |

---

## 4. Success Criteria

### 4.1 Definition of Done

- [ ] FR-01~FR-07 구현
- [ ] 대시로 총알을 스치면 슬로우모션 발동, 지속 후 정상 복구를 Play에서 확인
- [ ] 대시가 아닐 때 총알이 스쳐도 발동 안 함
- [ ] 총알 명중 시 슬로우모션 없이 피격(HP 감소) 처리
- [ ] 반복 발동 후에도 게임 속도가 정상(1.0)

### 4.2 Quality Criteria

- [ ] 컴파일 에러/경고 0
- [ ] 기존 대시·사격·피격·스폰 로직 회귀 없음

---

## 5. Risks and Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| 넓은 감지 콜라이더를 `"Player"`로 두어 총알이 스치기만 해도 조기 소멸 | High | Medium | 감지 콜라이더를 **별도 태그/오브젝트**로 분리, 총알은 여전히 실제 `"Player"` 콜라이더에만 소멸 |
| 슬로우 지속을 scaled 시간으로 재서 복구 지연/실패 | High | Medium | 복구 타이머를 **unscaled**(실시간)로 계산, `WaitForSecondsRealtime`/`unscaledTime` |
| timeScale만 바꾸고 fixedDeltaTime 미조정으로 물리가 끊김 | Medium | Medium | 슬로우 시 `Time.fixedDeltaTime`도 비례 조정 후 복구 (Design 확정) |
| 여러 총알 동시 스침으로 슬로우 중첩/재설정 | Medium | Medium | 재발동 쿨다운 + 진행 중이면 무시 |
| 대시 상태 노출 방식이 캡슐화 훼손 | Low | Low | 읽기 전용 프로퍼티(`IsDashActive`)만 공개 |
| 씬 전환/사망 시 timeScale이 슬로우로 남음 | Medium | Low | 복구 보장(코루틴 정리 or 항상 1.0 세팅), 사망 흐름 점검 |

---

## 6. Impact Analysis

### 6.1 Changed Resources

| Resource | Type | Change Description |
|----------|------|--------------------|
| `PlayerController.cs` | Script (수정) | 대시 상태 읽기 전용 노출(유예 포함) |
| 근접 감지 | Script (신규) | `NearMissDetector` — 트리거로 EnemyBullet 감지 → 대시면 슬로우 트리거 |
| 슬로우모션 제어 | Script (신규) | `SlowMotion` — timeScale/fixedDeltaTime 감소·복구(unscaled)·쿨다운 |
| 플레이어 자식 감지 오브젝트 | Asset (신규) | CircleCollider2D(isTrigger) + `NearMissDetector` (에디터 구성) |

### 6.2 Current Consumers

| Resource | Operation | Code Path | Impact |
|----------|-----------|-----------|--------|
| `PlayerController.isDashing` | READ (private) | 내부 대시 로직 | 읽기 전용 노출 추가, 기존 로직 불변 |
| `EnemyBullet` (`"EnemyBullet"`) | 트리거 | `PlayerController.OnTriggerEnter2D`(피격) | 유지 — 니어미스는 별도 감지 오브젝트에서 처리 |
| `Time.timeScale` | 전역 | (현재 변경처 없음) | 신규 소비처 — 복구 보장 필요 |

### 6.3 Verification

- [ ] 니어미스 감지가 실제 피격/총알 소멸에 영향 없음
- [ ] 대시·적 총알·스폰 회귀 없음
- [ ] 반복/동시 발동에도 timeScale 정상 복구

---

## 7. Architecture Considerations

### 7.1 프로젝트 성격

Unity 2D 탑다운 슈터. 신규 소규모 2컴포넌트(`NearMissDetector`, `SlowMotion`) + `PlayerController` 소폭 수정. 상세(감지 오브젝트 구성, timeScale/fixedDeltaTime 처리, 대시 상태 노출 방식)는 Design에서 3안 비교로 결정.

### 7.2 주요 설계 결정 (Plan 방향 — 상세는 Design)

| Decision | Options | 방향(잠정) | Rationale |
|----------|---------|-----------|-----------|
| 발동 조건 | 대시 중만 / 대시+유예 / 대시 무관 | **대시 중 + 유예** | 사용자 선택. "대시로 피했다" 체감 + 관대함 |
| 감지 방식 | 자식 트리거 콜라이더 / 매 프레임 거리 스캔 | **자식 트리거 콜라이더** | 사용자 선택. 물리엔진 활용, 효율적 |
| 슬로우 값 | 0.3/0.35s (기본) | **0.3 / 0.35s** | 사용자 선택 |
| 재발동 | 쿨다운 있음 / 없음 | **쿨다운 1초** | 사용자 선택. 중첩 방지 |
| 복구 타이밍 | scaled / unscaled | **unscaled** | 슬로우 중 정확 복구 |
| 대시 상태 노출 | public 필드 / 읽기 전용 프로퍼티 | **읽기 전용 프로퍼티** | 캡슐화 유지 |

### 7.3 구현 스케치 (참고용, 상세는 Design)

```text
[PlayerController]
  public bool IsDashActive => isDashing || Time.time <= dashGraceUntil; // 유예 포함

[NearMissDetector]  (플레이어 자식, 넓은 CircleCollider2D isTrigger, "Player" 아님)
  OnTriggerEnter2D(col):
    if col.tag == "EnemyBullet" && player.IsDashActive:
        SlowMotion.Trigger();

[SlowMotion]
  Trigger(): if Time.unscaledTime < nextAllowed: return
             timeScale = 0.3; fixedDeltaTime *= 0.3
             나머지 복구는 unscaled 타이머로
  복구: timeScale = 1; fixedDeltaTime 원복; nextAllowed = unscaledTime + cooldown
```

---

## 8. Convention Prerequisites

### 8.1 기존 컨벤션

- [x] MonoBehaviour, `Time.time`/`unscaledTime` 기반 타이머, 태그 체크(`"EnemyBullet"`) — 이를 따른다
- [x] 대시/적 총알 등 선행 사이클 자산 재사용

### 8.2 확인/준수 사항

| Category | Current State | To Define | Priority |
|----------|---------------|-----------|:--------:|
| timeScale 관리 | 현재 변경처 없음 | 복구 보장 규칙(항상 1.0) | High |
| 감지 콜라이더 | 없음 | 피격과 분리된 별도 오브젝트/태그 | High |
| 대시 상태 | private | 읽기 전용 노출 | High |

---

## 9. Next Steps

1. [ ] Design 문서 작성 (`near-miss-slowmo.design.md`) — 3안 비교 후 선택
2. [ ] 구현 (`NearMissDetector`/`SlowMotion` 신규 + `PlayerController` 노출 + 감지 오브젝트 에디터 구성)
3. [ ] Play 검증 (대시 스침 발동, 비대시 미발동, 명중 시 피격, 복구 보장)
4. [ ] Gap 분석 → 리포트 → 아카이브

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-03 | Initial draft (대시+유예 · 트리거 감지 · 0.3/0.35s · 쿨다운) | KimHanWhee |
