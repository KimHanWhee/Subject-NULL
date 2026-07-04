# near-miss-slowmo Completion Report

> **Feature**: 대시 니어미스 슬로우모션 (+ 대시 무적, 줌인 연출)
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Date**: 2026-07-04
> **Phase**: Report (완료)
> **Match Rate**: 99.2% | **Success Criteria**: 9/9

---

## 1. Executive Summary

| Perspective | Content |
|-------------|---------|
| **Problem** | 대시로 총알을 회피해도 피드백/보상이 없어 회피의 쾌감·리스크-리워드가 약함. Claude.md 대시 스펙의 니어미스 슬로우모션 미구현. |
| **Solution** | 플레이어 자식에 넓은 트리거 존을 두고, 대시 중 적 총알이 근접 반경을 스치면 짧은 슬로우모션 + 카메라 줌인을 발동. 대시 중엔 총알이 몸을 통과(닷지롤 무적). |
| **Function/UX Effect** | 총알을 대시로 아슬아슬하게 피하면 화면이 잠깐 느려지고 줌인되는 불릿타임 연출 → 회피 성공이 극적으로 체감. |
| **Core Value** | 회피 플레이의 리스크-리워드 강화 — 아슬아슬할수록 보상되는 능동적 회피 유도. |

### 1.3 Value Delivered (실측 반영)

| Perspective | Delivered |
|-------------|-----------|
| **Problem 해결** | ✅ 대시 스침 → 슬로우+줌인 발동, 정상 복구 (Play 확인) |
| **기술 완성도** | 신규 2 + 수정 2(near-miss) 파일, Match 99.2%, 컴파일/플레이스홀더 이슈 0 |
| **UX** | 슬로우모션·줌인 펀치·대시 무적(원거리 통과) 모두 동작 확인 |
| **확장성** | 화살/레이저 발사체도 `"EnemyBullet"` 계열 확장 시 그대로 니어미스 대상 편입 가능 |

---

## 2. Key Decisions & Outcomes (Plan→Design→Do Chain)

| 단계 | 결정 | 준수 여부 | 결과 |
|------|------|:--------:|------|
| Plan | 발동 조건: 대시 중 + 유예(0.1s) | ✅ | "대시로 피함" 체감 + 관대함 확보 |
| Plan | 감지 방식: 자식 트리거 콜라이더 | ✅ | 물리 이벤트 기반, 효율적 |
| Design | Option C: `SlowMotion`(시간제어) + `NearMissDetector`(감지) 분리 | ✅ | 책임 분리, 튜닝 용이 |
| Design | 복구는 unscaledTime 기준 + `OnDisable` 안전망 | ✅ | 반복 발동에도 timeScale 1.0 복구 |
| Design | 감지≠피격: NearMissZone 비-`"Player"` 태그 | ✅(보강) | 태그만으론 부족 → Kinematic RB로 완결 |
| Do 확장 | 대시 무적(FR-08) — 총알 통과 | 편차(사용자 요청) | Plan/Design 소급 반영 |
| Do 확장 | 줌인 연출(FR-09) — SlowMotion 통합 | 편차(사용자 요청) | 불릿타임 완성 |
| Do 발견 | 자식 콜라이더 콜백이 부모로 라우팅 | 실버그(에디터) | Kinematic RB로 감지·피격 실분리 |

---

## 3. Success Criteria Final Status

| FR | 내용 | 상태 | 근거 |
|----|------|:----:|------|
| FR-01 | 대시 중 스침 발동 | ✅ Met | `NearMissDetector` + `IsDashActive` |
| FR-02 | timeScale 감소·실시간 복구 | ✅ Met | `endUnscaled`/`unscaledTime` |
| FR-03 | 재발동 쿨다운 | ✅ Met | `active` + `nextAllowedUnscaled` |
| FR-04 | 감지·피격 분리 | ✅ Met | 비-`"Player"` 태그 + Kinematic RB |
| FR-05 | 명중 시 기존 피격 | ✅ Met | 비대시 경로 유지 |
| FR-06 | 데이터 튜닝 | ✅ Met | SlowMotion/Player Inspector 필드 |
| FR-07 | 확실한 복구 | ✅ Met | unscaled 복구 + `OnDisable`(시간+카메라) |
| FR-08 | 대시 무적 통과 | ✅ Met | `EnemyBullet`/`PlayerController` 대칭 가드 |
| FR-09 | 줌인 연출 | ✅ Met | orthographicSize unscaled 보간 |

**Overall Success Rate: 9/9 (100%)**

---

## 4. 구현 산출물

| 파일 | 유형 | 내용 |
|------|------|------|
| `Assets/Scripts/Combat/SlowMotion.cs` | 신규 | timeScale/fixedDeltaTime 제어 + unscaled 복구 + 쿨다운 + 카메라 줌인 |
| `Assets/Scripts/Player/NearMissDetector.cs` | 신규 | 자식 트리거 EnemyBullet 감지 + `IsDashActive` → `Trigger()` |
| `Assets/Scripts/Player/PlayerController.cs` | 수정 | `IsDashActive` + `dashGrace` + 유예 세팅 + 대시 무적(총알 데미지 무시) |
| `Assets/Scripts/Combat/EnemyBullet.cs` | 수정 | 대시 중 플레이어 통과(소멸 안 함) |
| NearMissZone(자식) / SlowMotion 오브젝트 | 에디터 | 트리거 콜라이더(반경≈0.75, Untagged) + **Kinematic RB** + SlowMotion 배치 |

---

## 5. 편차 및 학습

- **자식 트리거 콜라이더 콜백 라우팅**: 자식 콜라이더에 자기 Rigidbody2D가 없으면 트리거 콜백이 **부모 RB로 라우팅**되어 (a) 감지 스크립트 미작동, (b) 부모가 감지 존 반경에서 오작동(넓은 데미지). **Kinematic RB**로 독립 바디화하여 해결 — 태그 문제가 아님. (메모리 등재)
- **슬로우 복구는 unscaled 기준**: 슬로우 중엔 scaled 시간도 느려지므로 `unscaledTime`으로 재야 정확히 복구. 카메라 줌 보간도 `unscaledDeltaTime` 사용.
- **대시 무적 대칭 처리**: 총알 소멸(EnemyBullet)과 데미지(PlayerController) 양쪽 모두 `IsDashActive` 가드 → 하나만 막으면 "총알은 사라지는데 데미지만 없음" 같은 어색함 발생.
- **연출 통합**: 슬로우와 줌인을 한 컴포넌트(`SlowMotion`)에서 동기화 → 발동/복구/안전망이 일관.

---

## 6. 잔여/후속

- 튜닝(비차단): `zoomFactor`/`zoomLerpSpeed`/`slowScale`/감지 반경 취향 조정
- 무적 범위: 유예 포함 `IsDashActive` 사용 — 과관대하면 `dashGrace` 축소 여지
- 후속 사이클: 화살/레이저 발사체 → 스펠 마블 시스템 (Claude.md 로드맵)
- 별도 수정 완료(범위 밖): 적 사망 애니메이션 중 접촉 데미지 → `Die()`에서 콜라이더 즉시 비활성화 (근접/원거리 공통)
- 커밋: 사용자가 직접 수행 예정

---

## 7. 결론

Match Rate 99.2%, Success Criteria 9/9로 **near-miss-slowmo 완료**. Critical/Important 이슈 없음. 대시 회피에 슬로우모션+줌인 불릿타임과 닷지롤 무적을 더해 회피 플레이의 리스크-리워드를 강화한다는 핵심 가치 달성. 구현 중 발견한 Unity 자식 콜라이더 콜백 함정은 문서·메모리로 재발 방지책 확립.
