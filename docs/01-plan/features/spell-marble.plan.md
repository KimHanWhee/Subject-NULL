# spell-marble Planning Document

> **Summary**: 카드 슈트(♠♥♣♦) 기반 특수 능력 "스펠 마블" 시스템 — 데이터 주도 능력/등급, 덱 구성, 인게임 손패 5·Ctrl 선택(슬로우)·드래그앤드롭 발동, 같은 타입 합성.
>
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Version**: Unity 2022.3 / URP 2D
> **Author**: KimHanWhee
> **Date**: 2026-07-06
> **Status**: Draft

---

## Executive Summary

| Perspective | Content |
|-------------|---------|
| **Problem** | 현재 전투는 이동·발사·대시뿐이라 플레이어의 능동적 선택지·빌드 다양성이 부족하다. Claude.md의 핵심 로드맵 기능인 스펠 마블(특수 능력) 미구현. |
| **Solution** | 슈트(♠공격 ♥회복 ♣유틸 ♦방어)·등급을 가진 데이터 주도 마블 시스템 구축. 사전 덱 구성 → 인게임 손패 5 → Ctrl 선택(슬로우) → 드래그앤드롭 발동 → 같은 타입 합성. 개별 능력 콘텐츠는 데이터로 후속 추가. |
| **Function/UX Effect** | 전투 중 상황에 맞는 능력을 슬로우모션 속에서 골라 원하는 위치/자신에게 발동하는 전술 레이어 추가. 덱 구성·합성으로 메타 성장 요소 확보. |
| **Core Value** | "무기 슈팅 + 능력 카드"의 로그라이크식 빌드 다양성 — 반복 플레이 동기와 전투 깊이 강화. |

---

## Context Anchor

> Auto-generated from Executive Summary. Propagated to Design/Do documents for context continuity.

| Key | Value |
|-----|-------|
| **WHY** | 능동적 선택지·빌드 다양성 부재 → 스펠 마블로 전술/성장 레이어 추가 (Claude.md 로드맵) |
| **WHO** | GameScene 플레이어(인게임 사용) + MainScene 덱 빌더(전투 전 구성) |
| **RISK** | 시스템이 8개 서브로 과대 → 단계 분할 필수. timeScale/입력 처리 복잡. 개별 능력 미정. |
| **SUCCESS** | 데이터로 마블 추가 가능 + 손패 5 표시 + Ctrl 슬로우 선택 + 드래그 발동 + 덱 저장 + 합성 동작 |
| **SCOPE** | P1 코어 런타임 · P2 덱 빌더+저장 · P3 합성 (개별 능력 콘텐츠·경제는 범위 밖) |

---

## 1. Overview

### 1.1 Purpose

플레이어가 전투 중 사용하는 카드형 특수 능력 "스펠 마블" 시스템을 구축한다. 핵심은 **개별 능력 콘텐츠를 확정하지 않고도**(Claude.md: "능력 세부 사항은 나중에 직접 정할 예정") 마블을 데이터로 계속 추가할 수 있는 **확장 가능한 프레임워크**를 만드는 것이다.

### 1.2 Background

- 대시·스태미너·니어미스 슬로우모션까지 회피 축은 완성됨. 다음 로드맵 축이 능동적 "능력" 시스템(스펠 마블).
- 기존 자산 재사용 가능: `ObjectPool`(발동 이펙트/투사체), `SlowMotion`(선택 슬로우), `Character.Hit`(공격 능력 적용 대상), `PlayerController`(버프 대상), `WeaponData` 스타일의 ScriptableObject 데이터 패턴.

### 1.3 Related Documents

- 명세: `CLAUDE.md` §추가 예정 기능 2. 스펠 마블 (L28–50)
- 참고 자산: `Assets/Scripts/Weapon/WeaponData.cs`(SO 패턴), `Assets/Scripts/Combat/SlowMotion.cs`, `Assets/Scripts/Combat/ObjectPool.cs`

---

## 2. Scope

> 시스템이 크므로 **3단계 In-Scope**로 분할한다. 각 단계는 독립적으로 플레이 검증 가능하며, Do 단계에서 `--scope`로 나눠 구현한다.

### 2.1 In Scope

**Phase 1 — Core Runtime (인게임 사용 축)**
- [ ] `SpellMarble` 데이터 모델(ScriptableObject): 슈트·등급·능력 참조·아이콘·설명 (FR-01)
- [ ] `SpellAbility` 능력 추상화 + 타겟 모드(Self/Targeted) (FR-02)
- [ ] 등급 4단계 시각 표현(무색/금색/푸른색/무지개빛) (FR-03)
- [ ] 손패 5 관리 + 우측 하단 HUD 동그라미 5(타입 표시) (FR-05)
- [ ] 사용 후 리필(쿨다운 → 덱 다음 마블) (FR-06)
- [ ] Ctrl 홀드 선택 모드: 5개 상승 + 선택창 + 호버 상세 (FR-07)
- [ ] 선택 모드 중 슬로우모션(선택 UI 제외) (FR-08)
- [ ] 드래그앤드롭 발동(Targeted=위치, Self=플레이어) (FR-09)
- [ ] 능력 실행 파이프라인 `SpellContext`(시전자/위치/등급) + 풀링 이펙트 (FR-10)
- [ ] 프레임워크 검증용 **참조 능력 최소 2종**(Self 버프 1 + Targeted 1) — 후속 교체 전제

**Phase 2 — Deck Building + Persistence (전투 전 구성 축)**
- [ ] 덱 데이터(15~25 마블 참조) 모델 (FR-04)
- [ ] 덱 저장/로드(JSON, persistentDataPath) (FR-04)
- [ ] MainScene 덱 구성 씬: 보유 마블에서 덱 편성 후 저장 (FR-11)

**Phase 3 — Synthesis (합성 축)**
- [ ] 같은 타입 마블 드롭 → 합성, 등급업 확률(placeholder·튜닝), 레전드 포함 시 불가 (FR-12)

### 2.2 Out of Scope

- **개별 능력 콘텐츠 확정**(공격/회복/유틸/방어 각각의 구체 수치·연출) — 명세상 "나중에 직접" 정의. 프레임워크만 제공, 콘텐츠는 데이터로 후속.
- 마블 **획득 경제**(드롭/상점/보상/인벤토리 증가) — 본 사이클은 "보유 마블 풀"을 고정 가정.
- 합성 **확정 확률 밸런싱**(placeholder만; 상세는 명세상 "추후 예정").
- 멀티플레이/세이브 슬롯 다중화/클라우드 저장.

---

## 3. Requirements

### 3.1 Functional Requirements

| ID | Requirement | Phase | Priority | Status |
|----|-------------|:-----:|----------|--------|
| FR-01 | `SpellMarble` SO: 슈트(♠♥♣♦)·등급(일반/골드/다이아/레전드)·능력 참조·아이콘·이름·설명 | P1 | High | Pending |
| FR-02 | `SpellAbility` 추상화: `Activate(SpellContext)` + 타겟 모드(Self/Targeted). 구체 능력은 하위 데이터/타입으로 확장 | P1 | High | Pending |
| FR-03 | 등급 시각: 무색→금색→푸른색→무지개빛(테두리/틴트) 매핑 | P1 | High | Pending |
| FR-05 | 인게임 손패 5 + 우측 하단 HUD 동그라미 5개에 슈트 타입 표시 | P1 | High | Pending |
| FR-06 | 마블 사용 시 쿨다운 후 덱 다음 마블로 슬롯 리필(덱 소진 시 재순환) | P1 | Medium | Pending |
| FR-07 | Ctrl 홀드 시 선택 모드: 동그라미 5개 위로 상승 + 선택창, 마우스 호버 시 능력·등급 상세 툴팁 | P1 | High | Pending |
| FR-08 | 선택 모드 동안 게임 슬로우모션(선택 UI/입력은 unscaled로 정상 동작), 해제 시 복구 | P1 | High | Pending |
| FR-09 | 드래그앤드롭 발동: Targeted 능력=드롭 위치, Self 버프=임의 드롭→플레이어 적용 | P1 | High | Pending |
| FR-10 | 능력 실행 파이프라인: `SpellContext`(caster/위치/등급) 전달, 이펙트·투사체는 `ObjectPool` 재사용 | P1 | High | Pending |
| FR-04 | 덱 데이터(15~25 마블) 모델 + JSON 저장/로드 | P2 | High | Pending |
| FR-11 | MainScene 덱 구성 씬: 보유 마블 목록에서 덱 편성/저장, 개수 제약(15~25) 검증 | P2 | High | Pending |
| FR-12 | 합성: 같은 슈트 마블에 드롭 시 소비→등급업 확률(placeholder), 레전드 포함 시 차단 | P3 | Medium | Pending |

### 3.2 Non-Functional Requirements

| Category | Criteria | Measurement Method |
|----------|----------|-------------------|
| 확장성 | 새 마블/능력을 **코드 수정 없이 데이터(에셋)로 추가** 가능 | 능력 1종 추가에 SO 생성만으로 동작하는지 확인 |
| 시간 안전성 | 선택 슬로우 진입/해제/씬 전환 시 `Time.timeScale`이 항상 1.0로 복구 | 반복 진입 후 timeScale 검사(니어미스 학습 재사용) |
| 저장 무결성 | 덱 JSON 손상/부재 시 크래시 없이 빈 덱/기본값으로 폴백 | 파일 삭제·손상 케이스 수동 테스트 |
| 입력 반응성 | 슬로우모션 중에도 선택/드래그 입력이 실시간(unscaled) 반응 | Ctrl 홀드 중 드래그 지연 체감 확인 |

---

## 4. Success Criteria

### 4.1 Definition of Done (Phase별)

**Phase 1**
- [ ] 테스트 덱(코드/인스펙터 지정)으로 손패 5개가 HUD에 슈트별로 표시된다
- [ ] Ctrl 홀드 시 5개가 상승·선택창 표시되고 게임이 슬로우, 놓으면 복구된다
- [ ] 호버 시 능력·등급 상세가 보인다
- [ ] 참조 능력을 드래그: Self는 플레이어에, Targeted는 드롭 위치에 발동된다
- [ ] 사용한 슬롯이 쿨다운 후 리필된다
- [ ] 새 능력 1종을 에셋 생성만으로 추가해 손패에 태울 수 있다(확장성 증명)

**Phase 2**
- [ ] MainScene 덱 씬에서 보유 마블로 15~25개 덱을 편성·저장할 수 있다
- [ ] 저장한 덱이 GameScene 손패에 반영된다
- [ ] 저장 파일 부재/손상 시 폴백 동작(크래시 없음)

**Phase 3**
- [ ] 같은 슈트 마블에 드롭 시 합성되어 하나가 소비되고 확률적으로 등급이 오른다
- [ ] 레전드가 낀 합성은 차단된다

### 4.2 Quality Criteria

- [ ] 컴파일 경고/에러 0, 플레이스홀더(NotImplemented) 0
- [ ] 슬로우모션 timeScale 복구 회귀 없음
- [ ] gap-detector Match Rate ≥ 90% (단계별 analyze)

---

## 5. Risks and Mitigation

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| 시스템 과대 → 단일 Do 폭주 | High | High | 3-Phase 분할, `--scope`로 점진 구현·단계별 analyze |
| 개별 능력 미정 → 설계 공회전 | High | Medium | 능력을 데이터/추상화로 분리, 참조 능력 2종만으로 프레임워크 검증. 콘텐츠는 후속 |
| 슬로우모션 중 입력/시간 처리 | Medium | Medium | 선택·드래그는 `unscaledDeltaTime`/`unscaledTime` 기반. 기존 `SlowMotion` OnDisable 안전망 재사용 |
| 드래그앤드롭 좌표 변환(스크린↔월드) | Medium | Medium | 카메라 `ScreenToWorldPoint` 헬퍼 일원화, Targeted/Self 분기 명확화 |
| 덱 저장 스키마 변경 취약 | Medium | Low | JSON에 version 필드, 로드 실패 시 기본값 폴백 |
| Ctrl 키가 기존 입력과 충돌 | Low | Low | InputSystem에서 Ctrl 홀드 전용 처리, 기존 대시(Space)와 독립 |

---

## 6. Impact Analysis

### 6.1 Changed / New Resources

| Resource | Type | Change Description |
|----------|------|--------------------|
| `SpellMarble`, `SpellAbility`, `SpellContext`, `Grade`/`Suit` enum | 신규 스크립트/SO | 데이터 모델·능력 추상화 |
| `SpellDeck`(런타임 손패/리필 관리), `SpellHandHUD` | 신규 | 인게임 손패·HUD |
| `SpellSelectionUI`(Ctrl 선택+슬로우+호버), `SpellDragHandler` | 신규 | 선택/발동 입력 |
| `SpellDeckStore`(JSON 저장/로드), 덱 구성 씬 스크립트 | 신규(P2) | 저장·덱 빌더 |
| `SpellSynthesis` | 신규(P3) | 합성 규칙 |
| `SlowMotion.cs` | 수정 | 홀드형 슬로우 모드 추가(니어미스 펄스와 공존) |
| `PlayerController.cs` | 수정(소규모) | Self 버프 적용 훅(예: 실드/회복 진입점) |
| GameScene / MainMenuScene | 에디터 | HUD 캔버스·선택 UI 배치, 덱 구성 씬 추가 |

### 6.2 Current Consumers (기존 기능 영향)

| Resource | 영향 | 비고 |
|----------|------|------|
| `SlowMotion` (니어미스에서 사용 중) | Needs verification | 니어미스 펄스 슬로우와 선택 홀드 슬로우가 동시에 걸릴 때 우선순위/복구 정의 필요 |
| `Time.timeScale` | Needs verification | 두 슬로우 소스 공존 시 단일 소스로 조정하거나 스택 관리 |
| `ObjectPool` / `BulletPoolManager` | None~verify | 능력 이펙트용 새 풀 추가(기존 총알 풀과 독립) |
| `PlayerController` | Needs verification | 버프 훅 추가가 기존 피격/대시 흐름과 충돌 없어야 함 |

### 6.3 Verification

- [ ] 니어미스 슬로우 + 선택 슬로우 공존 시 timeScale 복구 정상(설계 단계에서 슬로우 소스 통합 방안 확정)
- [ ] 새 능력 풀이 기존 총알 풀과 간섭 없음
- [ ] 버프 훅이 대시/피격/사망 상태와 상호작용 시 안전

---

## 7. Architecture Considerations

### 7.1 Project Level

Unity 게임 프로젝트(웹 레벨 표 N/A). 기존 관례 유지: 기능별 폴더(`Assets/Scripts/Spell/` 신규), ScriptableObject 데이터 주도(`WeaponData` 선례), MonoBehaviour 매니저.

### 7.2 Key Architectural Decisions (Plan 수준, 상세는 Design에서 확정)

| Decision | Options | 잠정 | Rationale |
|----------|---------|------|-----------|
| 능력 표현 | SO별 서브클래스 / enum+switch / Strategy 컴포넌트 | SO 기반 `SpellAbility`(추상) | 데이터로 추가·인스펙터 편집, 콘텐츠 후속 확장 용이 |
| 등급 효과 | 전역 배율 / 마블 고유 | **마블 고유**(명세: 능력에 따라 등급 결정) | 명세 반영, 배율 가정 배제 |
| 손패↔덱 | 고정5 / 소모 / 리필 손패 | **리필 손패 5**(가정) | "덱에 있는 5개" 자연 해석, 로그라이크 감각 |
| 슬로우 소스 | 니어미스와 별도 / 통합 | Design에서 통합 검토 | timeScale 단일 진실원 필요 |
| 저장 | PlayerPrefs / JSON 파일 | **JSON**(persistentDataPath) | 덱(15~25) 구조적 저장에 적합 |
| 드래그 좌표 | 스크린 UI / 월드 투영 | 혼합(UI 드래그 → 월드 드롭 투영) | Targeted 위치 발동 요구 |

### 7.3 신규 폴더(안)

```
Assets/Scripts/Spell/
  Data/      SpellMarble.cs, SpellAbility.cs, Suit.cs, Grade.cs, SpellContext.cs
  Runtime/   SpellDeck.cs, SpellCaster.cs, SpellSelectionUI.cs, SpellDragHandler.cs
  Abilities/ (참조 능력 2종; 이후 콘텐츠 추가 위치)
  Persistence/ SpellDeckStore.cs   (P2)
  Synthesis/  SpellSynthesis.cs    (P3)
Assets/Scripts/UI/ SpellHandHUD.cs
```

---

## 8. Assumptions (미정 항목 기본값 — 수정 시 알려주세요)

> Claude.md 미정 부분을 아래 기본값으로 가정하고 진행. 다르면 지적해 주세요.

1. **손패 운용**: 덱(15~25)에서 5개를 손패로. 사용 시 쿨다운 후 덱의 다음 마블로 슬롯 리필, 덱 소진 시 재순환. (FR-06)
2. **개별 능력**: 지금은 확정하지 않음. 프레임워크 검증용 참조 능력 **2종**(Self 버프 1 + Targeted 1)만 실제 구현, 이후 데이터로 교체/추가.
3. **등급 효과**: 등급은 각 마블 고유 속성(전역 위력 배율 아님). 시각 색상 + 합성 결과 축으로만 사용.
4. **보유 마블**: 별도 획득 시스템 없이, 정의된 마블 에셋 = 보유 풀로 가정(덱 편성 대상). 경제는 범위 밖.
5. **저장**: 덱 구성은 JSON 1개 파일(persistentDataPath), version 필드 포함.
6. **합성 확률**: 예: 일반→골드 40% / 골드→다이아 20% / 다이아→레전드 8% (placeholder, 인스펙터 튜닝). 실패 시 등급 유지·1개만 소비. 레전드 포함 불가.
7. **Ctrl**: 홀드 동안 선택 모드+슬로우, 마우스로 드래그해 발동. 기존 대시(Space)와 독립.

---

## 9. Next Steps

1. [ ] 위 Assumptions 확인/수정
2. [ ] `/pdca design spell-marble` — 3안 아키텍처 비교 후 선택(특히 슬로우 소스 통합·능력 추상화 방식)
3. [ ] `/pdca do spell-marble --scope phase-1` 부터 점진 구현

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-06 | 초안(3-Phase 분할, 데이터 주도 능력/등급, Assumptions 명시) | KimHanWhee |
