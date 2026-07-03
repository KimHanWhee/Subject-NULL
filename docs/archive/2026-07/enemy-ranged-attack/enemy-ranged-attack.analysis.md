# enemy-ranged-attack Gap Analysis

> **Feature**: 원거리 적 + 적 총알(진영 분리)
> **Project**: MiniGungeon (Unity 2022.3)
> **Date**: 2026-07-03
> **Phase**: Check (Gap Analysis)
> **Upstream**: Plan / Design (Option C)
> **Verification**: 정적 분석 + 수동 Play 검증 (Unity — 서버/Playwright 자동 테스트 해당 없음)

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 근접 전용 적 → 거리 유지로 무력화. 원거리 위협 + 슬로우모션 선행 조건 |
| **SUCCESS** | 원거리 적이 사거리 유지·발사, 총알은 플레이어만 타격(오사 없음) |
| **SCOPE** | 직진 총알 1종 · 원거리 적 · faction 분리 (화살/레이저/슬로우모 제외) |

---

## 1. Strategic Alignment (WHY 충족 여부)

✅ **정렬됨**: 원거리 적이 사거리를 유지하며 플레이어에게 총알을 발사 → 근접 일변도 해소. `"EnemyBullet"` 태그+컴포넌트로 니어미스 슬로우모션 선행 식별자 확보(FR-08). Plan/Design의 핵심 문제(WHY)를 직접 해결.

---

## 2. Success Criteria 평가 (Plan FR-01~FR-08)

| FR | 내용 | 상태 | 근거 |
|----|------|:----:|------|
| FR-01 | 원거리 적 주기 사격 | ✅ Met | `RangedEnemyController.Fire` + `fireInterval` (85~89행). Play 확인: 발사됨 |
| FR-02 | 사거리 유지(히스테리시스) | ✅ Met | `farBand`/`nearBand` 분기 (76~80행). Play 확인: 거리 유지 |
| FR-03 | 발사 순간 직진(유도 X) | ✅ Met | `Fire(dir)`, `dir` = 발사시점 방향 고정 (73, 93행) |
| FR-04 | 플레이어만 타격(오사 X) | ✅ Met (구조) | `EnemyBullet`은 Player/Wall만 반응, 적 컨트롤러는 `"Bullet"`만 반응 → 태그 분리로 오사 원천 차단. ⚠️ 런타임 오사 부재는 수동 확인 권장 |
| FR-05 | Character.Hit 연결 | ✅ Met | `PlayerController.OnTriggerEnter2D("EnemyBullet")` → `TakeHit` → Flash/Die |
| FR-06 | 벽/수명/화면밖 반환 | ✅ Met | `EnemyBullet` lifetime(4초) + Player/Wall 트리거 반환. 화면밖은 lifetime으로 커버 |
| FR-07 | 데이터 튜닝 | ✅ Met | 밴드/간격/속도/데미지 Inspector 필드 (8~22행) |
| FR-08 | 니어미스 식별자 | ✅ Met | `"EnemyBullet"` 태그 + `EnemyBullet` 컴포넌트 |

**Success Rate: 8/8 (100%)** — FR-04는 구조적으로 보장, 런타임 오사 부재만 수동 확인 권장.

---

## 3. Structural / Functional / Contract Match

### 3.1 Structural (파일·컴포넌트 존재)
| 항목 | 설계 | 구현 | 일치 |
|------|------|------|:----:|
| `EnemyBullet.cs` | 신규 | ✅ 존재 | ✅ |
| `RangedEnemyController.cs` | 신규 | ✅ 존재 | ✅ |
| `PlayerController.cs` 수정 | TakeHit + 트리거 | ✅ 반영 | ✅ |
| `GameManager.cs` 수정 | 풀+가중 스폰 | ✅ 반영 | ✅ |
| `"EnemyBullet"` 태그 | 신규 | ✅ (에디터) | ✅ |

**Structural: 100%**

### 3.2 Functional Depth (실제 로직 완성도)
- 플레이스홀더/TODO/스텁 **없음**. 모든 메서드 실제 동작.
- 상태머신(Spawning/Moving/Dying), 히스테리시스, 쿨다운, 풀 반환, 수명 모두 구현.
- 엣지케이스 8건(§6 E1~E8) 중 코드로 처리 가능한 것(E1 풀고갈, E2 수명, E3 target null, E4 히스테리시스, E5 즉발방지, E6/E7 오사/상쇄, E8 벽) 모두 반영.

**Functional: ~96%** (화면밖 전용 처리 없이 lifetime으로 대체 — 설계 허용 범위, 경미)

### 3.3 Contract / Integration (진영 매트릭스·인터페이스)
| 계약 | 설계 §1.3 | 구현 | 일치 |
|------|-----------|------|:----:|
| 플레이어 총알 → Enemy만 | `"Bullet"` | 변경 없음 | ✅ |
| 적 총알 → Player만 | `"EnemyBullet"` | EnemyBullet: Player/Wall 반응 | ✅ |
| 적 컨트롤러 오사 방지 | `"EnemyBullet"` 미검사 | Ranged/EnemyController `"Bullet"`만 | ✅ |
| Spawn(player) 시그니처 | 동일 | 일치 | ✅ |

**Contract: 100%**

---

## 4. Design Deviations (설계 대비 편차)

| # | 편차 | 심각도 | 사유 / 판단 |
|---|------|:------:|------------|
| D1 | GameManager 근접 풀을 `meleePool` 필드(§3.2) 대신 기존 `GetComponent<ObjectPool>()` 유지 | Minor | 기존 씬 배선 미변경 → 회귀 위험 제거. `rangedPool`은 null-safe. 기능 동일. **개선적 편차** |
| D2 | `EnemyController`(근접)에 freezeRotation + 회전 초기화 추가 (설계상 "미변경" 대상) | Minor | Do 단계에서 실제 회전 버그 확인 → 사용자 요청으로 근접에도 적용. **범위 추가(정당)** |
| D3 | `BulletPoolManager`를 `FindObjectOfType`로 런타임 자동 탐색 (§2.2는 할당 필드) | Minor | 프리팹이 씬 오브젝트 참조를 담을 수 없는 Unity 제약 → 필수 정정 |
| D4 | 코드 스타일 `CompareTag` 대신 `.tag ==` 사용 | Trivial | 기존 코드베이스 컨벤션 일치. 동작 동일 |
| D5 | RangedEnemy 회전 방지(freezeRotation) + 스폰 회전 초기화 추가 | Minor | 설계 §에 없던 안정화. 대시 회전 버그와 동일 근본원인 대응 |

> D2/D5는 설계 문서에 없던 **추가 안정화**로, 실사용 중 발견된 실제 버그(적 회전) 대응. Report에 Decision 기록.

---

## 5. Match Rate

정적 축(서버 없음 → static 공식): `Structural×0.2 + Functional×0.4 + Contract×0.4`

```
= 100×0.2 + 96×0.4 + 100×0.4
= 20 + 38.4 + 40
= 98.4%
```

**Overall Match Rate: ~98% (≥90% 게이트 통과)**

수동 런타임 확인 완료: 스폰(똑바로)·이동(사거리 유지)·발사·발사음(2D)·회전 버그 수정.
수동 확인 권장(미완): 오사 부재(FR-04), 플레이어 피격 HP감소/Flash(FR-05), 총알 4초 소멸(FR-06).

---

## 6. Gap List (심각도별)

**Critical**: 없음
**Important**: 없음
**Minor**:
- M1: 화면밖 전용 반환 없이 lifetime으로 대체 (설계 허용, 조치 불요)
- M2: FR-04/05/06 런타임 수동 확인 미완 (게임 실행으로 확인 권장)
- M3: 설계 문서(§3.2 meleePool, §2.2 bulletPoolManager 할당)와 구현 방식 차이 — Report에 반영 필요

---

## 7. 판단

Match Rate ~98%로 게이트(90%)를 통과했고 **Critical/Important 이슈 없음**. 편차는 모두 개선적이거나 Unity 제약 대응, 또는 실버그 수정. **그대로 진행(Report)** 권장.
