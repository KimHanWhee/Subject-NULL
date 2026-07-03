# enemy-ranged-attack Planning Document

> **Summary**: 플레이어와 일정 사거리를 유지하며 주기적으로 총알을 발사하는 **원거리 적 유형**을 신규 추가한다. 아군/적 총알을 구분하는 진영(faction) 분리를 도입해 적 총알이 플레이어만 타격하도록 한다.
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
| **Problem** | 모든 적이 근접(접촉 데미지)만 가능해 플레이어가 거리를 벌리며 회피하면 위협이 사라진다. Claude.md 로드맵의 원거리 공격이 없고, 이는 '니어미스 슬로우모션' 대시 메커닉의 선행 조건이다. |
| **Solution** | 플레이어와 사거리를 유지하며 주기적으로 직진 총알을 쏘는 원거리 적 유형을 추가한다. `"EnemyBullet"` 진영 분리로 적 총알은 플레이어만 타격하고, 기존 `BulletPoolManager`/`ObjectPool`/`Character`를 재사용한다. |
| **Function/UX Effect** | 원거리 적이 등장하면 플레이어는 단순 거리 유지로는 안전하지 않게 되어, 엄폐·이동·(향후) 대시 회피가 필요한 전투 압박이 생긴다. |
| **Core Value** | 전투의 차원 확장 — 근접/원거리 위협 조합으로 회피 플레이의 깊이를 만들고, 니어미스 슬로우모션의 기반을 마련한다. |

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 근접 전용 적만 있어 거리 유지로 무력화됨. 원거리 위협 부재 + 니어미스 슬로우모션 선행 조건 |
| **WHO** | 플레이어(회피 대상), 향후 슬로우모션 기능을 붙일 개발자 |
| **RISK** | 아군/적 총알 태그 혼동으로 인한 오사(friendly fire) 또는 무피해, 사거리 유지 로직의 위치 떨림, 총알 풀 고갈 |
| **SUCCESS** | 원거리 적이 사거리를 유지하며 플레이어에게 총알을 발사하고, 그 총알은 플레이어에게만 데미지를 준다(적끼리 오사 없음) |
| **SCOPE** | 직진 총알 1종 · 원거리 적 신규 유형 · faction 분리 · 플레이어 총알 피격 처리. 화살/레이저·슬로우모션·유도탄은 제외 |

---

## 1. Overview

### 1.1 Purpose

플레이어와 일정 사거리를 유지하며 주기적으로 총알을 발사하는 원거리 적 유형을 추가하여, 근접 일변도였던 전투에 원거리 위협을 도입한다.

### 1.2 Background

현재 `EnemyController`는 상태머신(Spawning/Moving/Dying)으로 플레이어를 향해 직진 추적하고 **접촉 시에만** 데미지를 준다(`PlayerController.OnCollisionStay2D`가 `"Enemy"` 태그를 검사). 원거리 공격에 필요한 발사·조준·쿨다운 로직은 존재하지 않는다.

핵심 기술 제약(코드 분석 결과):

- **`Bullet.cs`는 `"Wall"/"Enemy"` 태그에 닿으면 자기 비활성화** → 적 총알이 플레이어를 때리는 경로가 전혀 없다.
- **플레이어에는 총알 피격 핸들러가 없다** → 신규 필요.
- **플레이어 총알은 태그 `"Bullet"`** 이고 `EnemyController.OnTriggerEnter2D`가 `"Bullet"` 태그면 데미지를 받는다 → 적 총알을 같은 태그로 만들면 **적끼리 오사**가 발생한다.
- **`GameManager`는 단일 `ObjectPool` 컴포넌트로 한 종류의 적 prefab만 스폰**한다 → 원거리 적을 추가하려면 두 번째 풀/스폰 경로가 필요하다.

∴ 이 기능의 핵심 설계 포인트는 **아군/적 총알을 구분하는 진영(faction) 분리**와 **다중 적 유형 스폰**이다.

### 1.3 Related Documents

- 대상/참조 코드: `Assets/Scripts/Enemy/EnemyController.cs`, `Assets/Scripts/Combat/Bullet.cs`, `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/GameManager.cs`
- 재사용 인프라: `Assets/Scripts/BulletPoolManager.cs`, `Assets/Scripts/Combat/ObjectPool.cs`, `Assets/Scripts/Combat/Character.cs`
- 로드맵: `Claude.md` — "적의 원거리 공격 (총알, 화살, 레이저)" / 니어미스 슬로우모션 선행 조건
- 후속: 니어미스 슬로우모션(별도 사이클), 화살·레이저 발사체(별도 사이클)

---

## 2. Scope

### 2.1 In Scope

- [ ] 원거리 적 유형 신규 추가 (기존 근접 슬라임은 유지)
- [ ] 사거리 유지 이동 로직 (너무 가까우면 후퇴 / 멀면 접근 / 밴드 안이면 정지 사격)
- [ ] 발사 쿨다운 기반 주기 사격 (발사 순간 플레이어 방향으로 직진, 유도 없음)
- [ ] 진영 분리: 적 총알은 플레이어만 타격, 적끼리/아군총알과 오사 없음 (`"EnemyBullet"` 식별)
- [ ] 플레이어의 적 총알 피격 처리 (`Character.Hit` 재사용 → 기존 Flash/Die 로직 연결)
- [ ] 적 총알 수명/화면밖/벽 충돌 시 풀 반환 (풀 고갈 방지)
- [ ] `GameManager` 스폰에 원거리 적을 확률적으로 포함
- [ ] 발사 간격·사거리 밴드·총알 속도/데미지의 데이터화(Inspector 튜닝)

### 2.2 Out of Scope

- 화살·레이저 발사체 (다음 사이클)
- 니어미스 슬로우모션 (다음 기능) — 단, 감지 가능한 식별자만 남김 (FR-08)
- 유도/예측 조준 (발사 시점 위치로 직진만)
- 장전/탄약, 탄 퍼짐/샷건형 다중 발사
- 적 공격 전조(telegraph) 애니메이션의 정교화 — 기본 수준만

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-01 | 원거리 적 유형이 스폰되어 플레이어를 향해 주기적으로 총알을 발사한다 | High | Pending |
| FR-02 | 원거리 적은 플레이어와 사거리를 유지한다 (근접 시 후퇴 / 원거리 시 접근 / 밴드 내 정지) | High | Pending |
| FR-03 | 적 총알은 발사 시점의 플레이어 방향으로 직진한다 (유도 없음) | High | Pending |
| FR-04 | 적 총알은 플레이어에게만 데미지를 준다 (다른 적·아군총알과 오사 없음) | High | Pending |
| FR-05 | 플레이어가 적 총알에 맞으면 `Character.Hit`로 처리되어 기존 피격/사망 흐름을 탄다 | High | Pending |
| FR-06 | 적 총알은 벽 충돌 / 수명 초과 / 화면 밖에서 풀로 반환된다 | Medium | Pending |
| FR-07 | 발사 간격·사거리 밴드·총알 속도·데미지를 Inspector에서 튜닝할 수 있다 | Medium | Pending |
| FR-08 | 적 총알은 향후 니어미스 감지가 가능하도록 식별 가능한 태그/컴포넌트를 가진다 | Low | Pending |

### 3.2 Non-Functional Requirements

| Category | Criteria | Measurement Method |
|----------|----------|-------------------|
| 정확도 | 발사 방향이 발사 순간 플레이어 위치를 향함 | Play 모드 육안 확인 |
| 안정성 | 적 총알 풀(기본 30) 고갈로 발사 누락 없음 | 장시간 플레이 관찰 |
| 성능 | 적별 Update 추가 연산은 상수 시간(거리 계산 + 타이머 비교) | 코드 리뷰 |
| 유지보수 | 코드 수정 없이 발사 간격/사거리/총알 값 튜닝 가능 | Inspector 값 변경 후 확인 |
| 회귀 | 기존 근접 슬라임·플레이어 사격·접촉 데미지에 영향 없음 | 기존 플레이 확인 |

---

## 4. Success Criteria

### 4.1 Definition of Done

- [ ] FR-01~FR-08 구현 (FR-08은 식별자 부여까지)
- [ ] 원거리 적이 사거리를 유지하며 플레이어에게 총알을 발사함을 Play 모드에서 확인
- [ ] 적 총알이 플레이어를 맞히면 HP가 감소하고 Flash/사망이 정상 동작
- [ ] 적 총알이 **다른 적에게 데미지를 주지 않음**을 확인 (오사 없음)
- [ ] 플레이어 총알은 여전히 적만 타격 (기존 동작 유지)
- [ ] 적 총알이 화면을 벗어나거나 벽에 닿으면 사라짐(풀 반환)

### 4.2 Quality Criteria

- [ ] 컴파일 에러/경고 0
- [ ] 기존 이동·사격·접촉 데미지·스폰 로직에 회귀 없음
- [ ] 사거리 유지 시 위치 떨림(진동)이 눈에 띄지 않음

---

## 5. Risks and Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| 적 총알 태그를 `"Bullet"`로 공유해 적끼리 오사 발생 | High | Medium | `"EnemyBullet"` 전용 식별 도입, `EnemyController`는 `"EnemyBullet"` 무시 |
| 플레이어에 총알 피격 핸들러 부재로 무피해 | High | Medium | `PlayerController`에 적 총알 트리거/충돌 핸들러 추가 → `Character.Hit` 연결 |
| 사거리 유지 로직이 접근/후퇴 경계에서 떨림 | Medium | High | 접근/후퇴 사이 데드존(히스테리시스) 밴드 적용 |
| 총알 수명 없으면 풀(30개) 고갈로 발사 누락 | Medium | Medium | 수명 타이머 + 화면밖/벽 충돌 시 비활성화 |
| `GameManager` 단일 풀 구조라 2번째 적 유형 스폰 불가 | Medium | High | 원거리 적 전용 풀/스폰 경로 추가 (Design에서 상세) |
| 적 총알과 플레이어 총알이 서로 상쇄/충돌 처리 꼬임 | Low | Medium | 총알끼리는 상호작용하지 않도록 태그/레이어 정리 |
| 기존 `Bullet.cs`가 `"Enemy"`에 비활성화 → 적 총알이 적을 통과 못함 | Medium | Medium | 적 총알은 별도 충돌 규칙(플레이어/벽만) 적용 (Design 결정) |

---

## 6. Impact Analysis

### 6.1 Changed Resources

| Resource | Type | Change Description |
|----------|------|--------------------|
| 원거리 적 컨트롤러 | Script (신규) | 사거리 유지 이동 + 발사 쿨다운 상태머신 |
| 적 총알 처리 | Script (신규 or `Bullet.cs` 확장) | 플레이어/벽만 타격, 수명·faction 식별 (Design에서 방식 확정) |
| `PlayerController.cs` | Script (수정) | 적 총알 피격 핸들러 추가 → `Character.Hit` 연결 |
| `GameManager.cs` | Script (수정) | 원거리 적을 확률적으로 스폰 (2번째 풀/스폰 경로) |
| 원거리 적 prefab / 적 총알 prefab | Asset (신규) | Unity 에디터에서 구성 (안내 제공) |
| `"EnemyBullet"` 태그 | Project Setting (신규) | 진영 분리용 태그 (또는 레이어 — Design 결정) |

### 6.2 Current Consumers

| Resource | Operation | Code Path | Impact |
|----------|-----------|-----------|--------|
| `Bullet` (태그 `"Bullet"`) | 충돌 | `EnemyController.OnTriggerEnter2D` | 유지 — 플레이어 총알은 계속 적만 타격 |
| `Character.Hit()` | 호출 | `EnemyController`, `PlayerController` | 재사용 — 플레이어 총알 피격에서 추가 호출 |
| `BulletPoolManager.GetPool()` | 호출 | `PlayerController.Shoot()` | 재사용 — 적 총알 풀도 동일 방식 사용 가능 |
| `ObjectPool` | 스폰 | `GameManager` | 원거리 적용 풀 추가 필요 (단일 풀 한계) |
| `PlayerController.OnCollisionStay2D` | 접촉 데미지 | `"Enemy"` 태그 | 유지 — 접촉 데미지 로직 변경 없음 |

### 6.3 Verification

- [ ] 플레이어 총알 → 적 타격, 적 총알 → 플레이어 타격 (교차 오사 없음)
- [ ] 근접 슬라임 스폰·추적·접촉 데미지 회귀 없음
- [ ] 적 총알 풀 재사용/반환 정상 (장시간 관찰)
- [ ] 원거리/근접 적 혼합 스폰 정상

---

## 7. Architecture Considerations

### 7.1 프로젝트 성격

Unity 2D 탑다운 슈터. 단일 파일 변경이 아닌 **신규 유형 + 진영 분리 + 스폰 통합**이 얽힌 중간 규모 기능. 상세 구조(진영 분리 방식, 적 총알 스크립트 재사용 여부, 스폰 통합 방식)는 Design 단계에서 3안 비교로 결정한다.

### 7.2 주요 설계 결정 (Plan 수준 방향 — 상세는 Design)

| Decision | Options | 방향(잠정) | Rationale |
|----------|---------|-----------|-----------|
| 진영 분리 | `"EnemyBullet"` 태그 / Physics Layer / `Bullet`에 faction 필드 | Design에서 확정 | 기존 코드가 태그 기반이라 태그가 자연스럽지만, 레이어가 물리 충돌 제어에 강함 |
| 적 총알 스크립트 | `Bullet.cs` 재사용+faction 필드 / 신규 `EnemyBullet.cs` | Design에서 확정 | 재사용은 코드 少, 분리는 규칙 명확 |
| 조준 방식 | 발사 순간 위치 직진 / 예측·유도 | **발사 순간 위치 직진** | MVP·회피 가능성 확보. 유도 제외 |
| 사거리 유지 | 단순 거리 밴드 / 히스테리시스 밴드 | **히스테리시스 밴드** | 경계 떨림 방지 |
| 스폰 통합 | 2번째 ObjectPool / 스폰 로직 리팩터 | Design에서 확정 | 현재 단일 풀 한계 해소 필요 |
| 니어미스 대비 | 태그만 / 태그+전용 컴포넌트 | **식별 가능 태그(+여지)** | 다음 사이클에서 근접 감지 쉽게 |

### 7.3 구현 스케치 (참고용, 상세는 Design 단계)

```text
[원거리 적]
  dist = |player - self|
  if dist > farBand    → 접근 (플레이어 방향 이동)
  elif dist < nearBand → 후퇴 (반대 방향 이동)
  else                 → 정지
  if inFireBand && Time.time >= nextFireTime:
      FireBulletTowards(player)   // 발사 순간 방향 고정
      nextFireTime = Time.time + fireInterval

[적 총알]  태그 "EnemyBullet"
  Update: 직진
  OnTrigger: "Player" → player.Character.Hit(damage) 후 반환
             "Wall"   → 반환
  수명 초과 → 반환
  (EnemyController는 "EnemyBullet"을 무시 → 오사 방지)

[플레이어]  적 총알 피격 핸들러
  OnTrigger/Collision "EnemyBullet" → Character.Hit → Flash/Die
```

---

## 8. Convention Prerequisites

### 8.1 기존 컨벤션

- [x] MonoBehaviour, PascalCase 메서드, camelCase private 필드, `Time.time` 절대 비교 쿨다운(대시/발사에서 확립) — 이를 따른다
- [x] 풀링은 `ObjectPool`/`BulletPoolManager` 재사용
- [x] HP·피격·사망은 `Character` 컴포넌트 재사용

### 8.2 확인/준수 사항

| Category | Current State | To Define | Priority |
|----------|---------------|-----------|:--------:|
| 태그/레이어 | `"Bullet"`, `"Enemy"`, `"Wall"`, `"Player"` 존재 | `"EnemyBullet"`(또는 레이어) 신설 방식 확정 | High |
| 쿨다운 | `Time.time` 절대 비교 패턴 확립 | 발사 간격에 동일 패턴 적용 | High |
| 스폰 | `GameManager` 단일 풀 | 다중 적 유형 스폰 방식 확정 | High |

---

## 9. Next Steps

1. [ ] Design 문서 작성 (`enemy-ranged-attack.design.md`) — 3안 비교(진영 분리·총알 스크립트·스폰 통합) 후 선택
2. [ ] 구현 (원거리 적/적 총알/플레이어 피격/스폰 + prefab·태그 에디터 안내)
3. [ ] Play 모드 검증 (사거리 유지, 발사, 플레이어 피격, 오사 없음, 풀 반환)
4. [ ] Gap 분석 → 리포트 → 아카이브

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-03 | Initial draft (총알 MVP · 원거리 적 신규 · 사거리 유지 · faction 분리) | KimHanWhee |
