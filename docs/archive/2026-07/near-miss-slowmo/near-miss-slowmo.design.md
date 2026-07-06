# near-miss-slowmo Design Document

> **Feature**: 대시 니어미스 슬로우모션
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Version**: 0.1 (dev)
> **Author**: KimHanWhee
> **Date**: 2026-07-03
> **Status**: Draft
> **Selected Architecture**: **Option C — Pragmatic Balance**
> **Upstream**: `docs/01-plan/features/near-miss-slowmo.plan.md`

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 대시 회피에 피드백/보상이 없음. Claude.md 대시 스펙의 니어미스 슬로우모션 미구현 |
| **WHO** | 플레이어(대시로 총알 회피), 튜닝할 개발자 |
| **RISK** | 근접 감지가 실제 피격과 섞여 총알 조기 소멸/무피해, timeScale 복구 실패, 슬로우 중첩 |
| **SUCCESS** | 대시 중 총알이 근접 반경을 스치면(맞지 않음) 슬로우모션 발동 후 정상 복구 |
| **SCOPE** | EnemyBullet 대상 니어미스 + timeScale 슬로우 + 쿨다운. 연출·화살/레이저는 후속 |

---

## 1. Overview

### 1.1 Selected Architecture — Option C

**슬로우모션 제어를 독립 컴포넌트로 분리**하고, 감지는 별도 컴포넌트가 담당한다.

| 축 | 결정 |
|----|------|
| 슬로우 제어 | `SlowMotion` 씬 컴포넌트 — timeScale/fixedDeltaTime/unscaled 복구/쿨다운 일원화 |
| 근접 감지 | `NearMissDetector` — 플레이어 자식 트리거 콜라이더, EnemyBullet 진입 + 대시 판정 → SlowMotion 호출 |
| 대시 노출 | `PlayerController.IsDashActive` 읽기 전용 프로퍼티(유예 포함) |

### 1.2 Component Map

```
[Player]
 ├─ PlayerController (IsDashActive 노출)
 └─ NearMissZone (자식, 넓은 CircleCollider2D isTrigger, "Player" 태그 아님)
      └─ NearMissDetector
           OnTriggerEnter2D("EnemyBullet") + player.IsDashActive → slowMotion.Trigger()

[SlowMotion] (씬 오브젝트, 예: GameManager 옆)
   Trigger(): timeScale=0.3, fixedDeltaTime*=0.3 → unscaled 0.35s 후 복구, 쿨다운 1s
```

### 1.3 감지 ≠ 피격 분리 (FR-04 핵심)

| 콜라이더 | 소속 | 태그 | 역할 | 총알 반응 |
|----------|------|------|------|-----------|
| 플레이어 실제 콜라이더 | Player | `"Player"` | 접촉/총알 피격 | 총알이 여기 닿으면 **소멸+데미지** |
| 니어미스 감지 콜라이더 | NearMissZone(자식) | `"Player"` 아님 | 스침 감지 | 총알이 여기 닿아도 **무시(통과)** — `EnemyBullet`는 `"Player"/"Wall"`만 소멸 |

> 감지 존을 `"Player"`로 태깅하지 않으므로 총알이 넓은 반경을 스쳐도 소멸하지 않고, 실제 명중 시에만 피격(FR-05)된다.

---

## 2. Modified Component

### 2.1 `PlayerController.cs` — 대시 상태 노출 (유예 포함)

기존 대시 로직은 불변, 읽기 전용 프로퍼티와 유예 필드만 추가.

```csharp
[Header("Dash Near-Miss")] // Design Ref: §2.1
public float dashGrace = 0.1f;   // 대시 종료 후 니어미스 인정 유예(초)
private float dashGraceUntil;    // 이 시각까지 대시 판정 유지

// Plan SC: FR-01 — 대시 중 또는 대시 직후 유예 내면 true
public bool IsDashActive => isDashing || Time.time <= dashGraceUntil;
```

`TickDash()`에서 대시가 끝나는 지점에 유예 시작:

```csharp
if (Time.time >= dashEndTime)
{
    isDashing = false;
    dashGraceUntil = Time.time + dashGrace; // 대시 직후 유예
    return false;
}
```

### 2.2 신규 `SlowMotion.cs` — 시간 제어 (복구 보장)

```csharp
using UnityEngine;

// Design Ref: §1.1 — 전역 슬로우모션 제어. unscaled 타이머로 복구 보장, 쿨다운.
public class SlowMotion : MonoBehaviour
{
    public float slowScale = 0.3f;  // Plan SC: FR-06 — 슬로우 강도
    public float duration = 0.35f;  // 실시간 지속(초)
    public float cooldown = 1f;     // 재발동 쿨다운(초)

    private float defaultFixedDelta;
    private float endUnscaled;
    private float nextAllowedUnscaled;
    private bool active;

    void Awake()
    {
        defaultFixedDelta = Time.fixedDeltaTime;
    }

    // Plan SC: FR-03 — 진행 중이거나 쿨다운 중이면 무시
    public void Trigger()
    {
        if (active || Time.unscaledTime < nextAllowedUnscaled) return;

        Time.timeScale = slowScale;
        Time.fixedDeltaTime = defaultFixedDelta * slowScale; // 물리도 비례 (부드러움)
        endUnscaled = Time.unscaledTime + duration;          // Plan SC: FR-02 — 실시간 기준
        active = true;
    }

    void Update()
    {
        if (!active) return;
        if (Time.unscaledTime >= endUnscaled) // Plan SC: FR-07 — unscaled로 확실히 복구
        {
            Restore();
            nextAllowedUnscaled = Time.unscaledTime + cooldown;
        }
    }

    void Restore()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        active = false;
    }

    // 안전망: 씬 종료/비활성 시 시간 정상화 (느림 잔존 방지)
    void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
    }
}
```

### 2.2b `SlowMotion.cs` — 카메라 줌인 펀치 (FR-09, Do 확장)

슬로우와 동기화되는 줌인 연출. `orthographicSize`를 `zoomFactor` 배율로 당기고, 복구 시 원복. **매 프레임 `unscaledDeltaTime` 보간**이라 슬로우 중에도 부드럽고, 복구도 자연스럽다.

```csharp
[Header("Camera Zoom")]
public Camera targetCamera;      // 비우면 Camera.main 자동
public float zoomFactor = 0.9f;  // orthographicSize 배율 (작을수록 확대)
public float zoomLerpSpeed = 8f; // 줌 보간 속도(unscaled)

private float defaultOrthoSize;  // 원래 사이즈
private float targetOrthoSize;   // 보간 목표

// Trigger(): targetOrthoSize = defaultOrthoSize * zoomFactor; (줌인)
// Restore(): targetOrthoSize = defaultOrthoSize;              (줌아웃)
// Update():  orthographicSize = Lerp(현재, target, zoomLerpSpeed * unscaledDeltaTime); // 항상 수행
// OnDisable(): orthographicSize = defaultOrthoSize;           // 안전망
```

> Awake에서 `Camera.main`을 자동 참조하고 `defaultOrthoSize`를 캡처하므로 에디터 배선 불필요. 원근 카메라 방어(`orthographic` 체크) 포함.

### 2.3 신규 `NearMissDetector.cs` — 감지 + 대시 판정

```csharp
using UnityEngine;

// Design Ref: §1.2 — 플레이어 자식 트리거. EnemyBullet 스침 + 대시면 슬로우 발동.
public class NearMissDetector : MonoBehaviour
{
    public PlayerController player;
    public SlowMotion slowMotion;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (slowMotion == null) slowMotion = FindObjectOfType<SlowMotion>();
    }

    // Plan SC: FR-01/FR-04 — 감지 존은 "Player" 아님 → 총알 소멸 없음
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "EnemyBullet"
            && player != null && player.IsDashActive
            && slowMotion != null)
        {
            slowMotion.Trigger();
        }
    }
}
```

### 2.4 대시 무적 프레임 (FR-08, Do 확장) — `EnemyBullet.cs` + `PlayerController.cs`

대시 중(`IsDashActive`)에는 적 총알이 **소멸도 데미지도 없이 통과**(닷지롤). 두 지점에서 대칭 처리.

```csharp
// EnemyBullet.OnTriggerEnter2D — 대시 중 플레이어는 통과(소멸 안 함)
if (collision.tag == "Player")
{
    PlayerController pc = collision.GetComponent<PlayerController>();
    if (pc != null && pc.IsDashActive) return; // 무적 → 통과
}
// (이후 기존 "Player"/"Wall" 소멸 로직)

// PlayerController.OnTriggerEnter2D — 대시 중 총알 데미지 무시
if (collision.tag == "EnemyBullet")
{
    if (IsDashActive) return; // 무적 프레임
    ...기존 TakeHit...
}
```

> 무적 판정도 `IsDashActive`(대시 + 유예)를 재사용 → 니어미스 슬로우와 무적이 같은 상태로 일관되게 묶인다. 근접 슬라임은 물리 충돌(`OnCollisionStay2D`)이라 무관.

---

## 3. Data & Project Settings

| 항목 | 값/방식 |
|------|---------|
| NearMissZone 오브젝트 | Player의 **자식** GameObject, `CircleCollider2D(isTrigger=true, radius=감지반경≈0.75)`, 태그는 `"Player"`가 **아닌** 것(Untagged) + `NearMissDetector` + **`Rigidbody2D(Kinematic)` 필수** |
| SlowMotion 오브젝트 | 씬에 1개 (예: GameManager와 같은 오브젝트나 별도 빈 오브젝트) |
| 튜닝 | `dashGrace`(Player), `slowScale/duration/cooldown/zoomFactor/zoomLerpSpeed`(SlowMotion), 감지 반경(콜라이더) |

> ⚠️ **NearMissZone에 Kinematic Rigidbody2D가 반드시 필요**하다. 자식 콜라이더에 자기 RB가 없으면 트리거 콜백이 **부모(Player)의 Rigidbody2D로 라우팅**되어 (a) `NearMissDetector`가 콜백을 못 받아 슬로우 미발동, (b) `PlayerController.OnTriggerEnter2D`가 감지 존 반경에서 총알에 반응해 **넓은 반경 데미지**가 발생한다. 자식에 Kinematic RB를 주면 독립 바디가 되어 콜백이 자식으로 가고 감지·피격이 실제로 분리된다.

---

## 4. Flow

```
적 총알(EnemyBullet) 이동
   │
   ├─ NearMissZone(자식 트리거) 진입
   │     └─ NearMissDetector.OnTriggerEnter2D
   │           player.IsDashActive == true ? → SlowMotion.Trigger()
   │           (감지 존은 "Player" 아님 → 총알 계속 진행)
   │
   ├─ 실제 Player 콜라이더 명중 → 총알 소멸 + PlayerController 피격(기존)
   └─ 빗나감 → lifetime/벽에서 소멸(기존)

SlowMotion.Trigger → timeScale 0.3 → (unscaled 0.35s) → 1.0 복구 → 쿨다운 1s
```

---

## 5. Edge Cases & Contingencies

| # | 상황 | 처리 |
|---|------|------|
| E1 | 대시 아닐 때 스침 | `IsDashActive=false` → 발동 안 함 |
| E2 | 슬로우 중 다른 총알 스침 | `active=true` → Trigger 무시(중첩 방지) |
| E3 | 슬로우 직후 연속 스침 | 쿨다운(`nextAllowedUnscaled`)으로 억제 |
| E4 | 슬로우 도중 씬 전환/사망 | `OnDisable`에서 timeScale/fixedDeltaTime 복구 |
| E5 | 감지 존을 총알이 소멸시킴 | 존이 `"Player"` 아님 → `EnemyBullet`이 무시 → 소멸 없음 |
| E6 | 총알이 감지 없이 바로 명중 | 실제 콜라이더 피격(FR-05), 슬로우 없음 |
| E7 | timeScale이 대시 타이머(Time.time)에 영향 | 의도된 불릿타임 — 대시가 실시간으로 약간 길어짐(허용) |
| E8 | 자식 감지 콜라이더 콜백이 부모로 라우팅 | NearMissZone에 **Kinematic Rigidbody2D** 부여 → 독립 바디로 콜백 분리 (§3 경고) |

---

## 6. Success Criteria Mapping

| Plan FR | Design 반영 |
|---------|-------------|
| FR-01 대시 중 스침 발동 | §2.3 `IsDashActive` 체크 + 트리거 감지 |
| FR-02 timeScale 감소·실시간 복구 | §2.2 `endUnscaled`/`unscaledTime` |
| FR-03 재발동 쿨다운 | §2.2 `active` + `nextAllowedUnscaled` |
| FR-04 감지·피격 분리 | §1.3 감지 존 비-`"Player"` 태그 |
| FR-05 명중 시 기존 피격 | §4 실제 콜라이더 경로 유지 |
| FR-06 데이터 튜닝 | §2.2/§3 Inspector 필드 |
| FR-07 확실한 복구 | §2.2 unscaled 복구 + `OnDisable` 안전망 |
| FR-08 대시 무적 통과 | §2.4 `EnemyBullet`/`PlayerController` `IsDashActive` 가드 |
| FR-09 줌인 연출 | §2.2b `SlowMotion` orthographicSize unscaled 보간 |

---

## 7. Test Plan (Play 모드 수동 검증)

| ID | 시나리오 | 기대 결과 |
|----|----------|-----------|
| T1 | 대시로 총알 스침(맞지 않음) | 슬로우모션 발동 후 0.35초(실시간) 뒤 정상 복구 |
| T2 | 대시 아닐 때 총알 스침 | 발동 안 함 |
| T3 | 대시 직후 유예 내 스침 | 발동함 |
| T4 | 총알 실제 명중 | 슬로우 없이 HP 감소(기존 피격) |
| T5 | 짧은 시간 연속 스침 | 쿨다운으로 중첩/재발동 억제 |
| T6 | 반복 발동 후 | timeScale 항상 1.0 정상 |
| T7 | 슬로우 중 사망/씬전환 | 시간 정상화(느림 잔존 없음) |

---

## 8. Out of Scope

- 화살/레이저 등 발사체 확장(EnemyBullet만), 근접 적 대상 니어미스
- 슬로우 연출(파티클/후처리/전용 사운드), 근접도별 가변 강도

---

## 9. Impact Summary

| 파일 | 유형 | 변경 |
|------|------|------|
| `Assets/Scripts/Player/PlayerController.cs` | 수정 | `IsDashActive` 프로퍼티 + `dashGrace` + 유예 세팅 + 대시 중 총알 데미지 무시(FR-08) |
| `Assets/Scripts/Combat/SlowMotion.cs` | 신규 | timeScale/fixedDeltaTime 제어 + 복구 + 쿨다운 + 카메라 줌인(FR-09) |
| `Assets/Scripts/Player/NearMissDetector.cs` | 신규 | 자식 트리거 감지 + 대시 판정 → 슬로우 호출 |
| `Assets/Scripts/Combat/EnemyBullet.cs` | 수정 | 대시 중 플레이어 통과(소멸 안 함) — FR-08 |
| NearMissZone 자식 / SlowMotion 오브젝트 | 에디터 | 콜라이더·컴포넌트 배선 (안내 제공) |

---

## 10. Implementation Guide

### 10.1 구현 순서

1. `SlowMotion.cs` 작성
2. `PlayerController.cs` 수정 (`IsDashActive` + 유예)
3. `NearMissDetector.cs` 작성
4. 에디터: SlowMotion 오브젝트, Player 자식 NearMissZone(트리거 콜라이더) 구성
5. Play 검증 (§7 T1~T7)

### 10.2 핵심 파일/컴포넌트

- 신규: `SlowMotion`, `NearMissDetector`
- 수정: `PlayerController`
- 재사용: `EnemyBullet`(`"EnemyBullet"` 태그), 기존 대시 로직

### 10.3 Session Guide (Module Map)

| Module | 범위 | 파일 | 의존 |
|--------|------|------|------|
| **module-1** | 시간 제어 | `SlowMotion.cs` | 없음 |
| **module-2** | 대시 상태 노출 | `PlayerController.cs` | 없음 |
| **module-3** | 근접 감지 | `NearMissDetector.cs` | module-1,2 |
| **module-4** | 에디터 구성 | NearMissZone/SlowMotion 배선 | module-1~3 |

**권장**: 코드 규모가 작아 `/pdca do near-miss-slowmo` 전체 1회 진행 권장. 에디터 구성(module-4)만 사용자 작업.

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-03 | Initial design (Option C) | KimHanWhee |
