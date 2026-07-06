# spell-marble Design Document

> **Summary**: 데이터 주도 스펠 마블 시스템 — 추상 SO 능력 + 역할별 매니저 + 단일 `TimeController` 슬로우. (Option C: 실용 균형)
>
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Version**: Unity 2022.3 / URP 2D
> **Author**: KimHanWhee
> **Date**: 2026-07-06
> **Status**: Draft
> **Planning Doc**: [spell-marble.plan.md](../../01-plan/features/spell-marble.plan.md)

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 능동적 선택지·빌드 다양성 부재 → 스펠 마블로 전술/성장 레이어 추가 (Claude.md 로드맵) |
| **WHO** | GameScene 플레이어(인게임 사용) + MainScene 덱 빌더(전투 전 구성) |
| **RISK** | 시스템이 8개 서브로 과대 → 단계 분할 필수. timeScale/입력 처리 복잡. 개별 능력 미정. |
| **SUCCESS** | 데이터로 마블 추가 가능 + 손패 5 표시 + Ctrl 슬로우 선택 + 드래그 발동 + 덱 저장 + 합성 동작 |
| **SCOPE** | P1 코어 런타임 · P2 덱 빌더+저장 · P3 합성 (개별 능력 콘텐츠·경제는 범위 밖) |

---

## 1. Overview

### 1.1 Design Goals

1. **콘텐츠 무편집 확장**: 새 능력을 코드 수정 없이 ScriptableObject 에셋 생성만으로 추가.
2. **단일 시간 진실원**: 니어미스 슬로우 + 선택 슬로우가 충돌 없이 `Time.timeScale`을 공유·복구.
3. **감지≠발동≠데이터 분리**: 데이터(SO) / 런타임 상태(덱·손패) / 입력·UI(선택·드래그) 책임 분리.
4. **기존 자산 재사용**: `ObjectPool`(이펙트), `Character.Hit`(공격), `PlayerController`(버프), `WeaponData` SO 패턴.

### 1.2 Design Principles

- **Data-Driven**: 능력·등급·마블은 데이터. 로직은 능력 SO의 `Activate()`에 캡슐화(다형성, 스위치 없음).
- **Single Source of Truth (시간)**: `TimeController`만 `Time.timeScale`/`fixedDeltaTime`을 씀.
- **unscaled 입력**: 슬로우 중에도 선택/드래그 UI·타이머는 `unscaledDeltaTime`/`unscaledTime` 기반.
- **Fail-safe 복구**: 슬로우 소스 비면 자동 1.0 복구, `OnDisable`/씬 전환 안전망.

---

## 2. Architecture Options (v1.7.0)

### 2.0 Architecture Comparison

| Criteria | Option A: Minimal | Option B: Clean | Option C: Pragmatic |
|----------|:-:|:-:|:-:|
| **Approach** | enum+switch 실행기, SlowMotion 확장 | 3계층 분리 + 스택 TimeScaleManager | 추상 SO 능력 + 역할별 매니저 + 경량 TimeController |
| **New Files** | ~7 | ~16 | ~11 |
| **Modified Files** | 2 | 3 | 3 |
| **Complexity** | Low | High | Medium |
| **Maintainability** | Medium(스위치 수정) | High | High |
| **Effort** | Low | High | Medium |
| **Risk** | 능력↑ 시 결합↑ | 솔로 게임 과설계 | Low(균형) |
| **Recommendation** | 프로토타입 | 대형 장기 | **기본 권장** |

**Selected**: **Option C** — **Rationale**: Plan이 이미 "데이터로 능력 무한 추가"(Claude.md L47) + 기존 `WeaponData` SO 선례 전제. 추상 SO 능력은 에셋 생성만으로 확장(스위치 수정 0)해 확장성 SC 직결. `TimeController`로 니어미스/선택 슬로우 timeScale 충돌(Plan §6.2)을 단일 소스로 근본 해결. B의 3계층은 Unity 솔로엔 과함.

### 2.1 Component Diagram

```
                         ┌────────────────────┐
              (data)     │  SpellMarble (SO)   │──has──▶ SpellAbility (SO, 추상)
                         │  suit·grade·icon    │         └ Activate(SpellContext)
                         └─────────▲──────────┘
                                   │ 참조
   ┌──────────────┐   draw/refill  │        ┌──────────────────┐
   │  DeckData    │───────────────▶│        │  SpellHandHUD    │ (우측 하단 5 동그라미)
   │ (15~25 참조) │        ┌────────┴─────┐  └─────▲────────────┘
   └──────▲───────┘        │  SpellCaster │────────┘ 손패 상태 통지
          │ load/save      │ (손패5·리필) │
   ┌──────┴────────┐       └───▲───┬──────┘
   │ SpellDeckStore│           │   │ Activate(ctx)
   │  (JSON, P2)   │   Ctrl/hover│   │drop
   └───────────────┘    ┌───────┴───┴──────┐        ┌──────────────┐
                        │ SpellSelectionUI │──slow──▶│ TimeController│◀──niearmiss
                        │ + SpellDragHandler│         │ (단일 timeScale)│  (SlowMotion)
                        └──────────────────┘         └──────────────┘
                        ┌──────────────────┐
                        │ SpellSynthesis(P3)│  같은 슈트 드롭 → 등급업
                        └──────────────────┘
```

### 2.2 Data Flow

```
[전투 전] MainScene 덱 빌더 → DeckData 편성 → SpellDeckStore.Save(JSON)
[전투 시작] SpellCaster ← SpellDeckStore.Load → 손패 5 채움 → SpellHandHUD 표시
[선택] Ctrl 홀드 → SpellSelectionUI 상승+툴팁 + TimeController.PushHold(slow)
[발동] 드래그 → 드롭: TargetMode.Targeted → 드롭 월드좌표 / Self → 플레이어
        → SpellCaster.Activate(SpellContext) → ability.Activate(ctx) (풀 이펙트)
        → 슬롯 소비 → 쿨다운 후 DeckData 다음 마블로 리필
[해제] Ctrl 릴리즈 → TimeController.PopHold → timeScale 복구
[합성/P3] 손패 마블을 같은 슈트 마블에 드롭 → SpellSynthesis.Try → 확률 등급업
```

### 2.3 Dependencies

| Component | Depends On | Purpose |
|-----------|-----------|---------|
| SpellCaster | DeckData, SpellMarble, SpellContext | 손패·리필·발동 오케스트레이션 |
| SpellAbility(구체) | SpellContext, ObjectPool, Character/PlayerController | 실제 효과 적용 |
| SpellSelectionUI | SpellCaster, TimeController | 선택 표시·슬로우 요청 |
| SpellDragHandler | SpellCaster, Camera | 드래그·드롭 좌표·발동 트리거 |
| SpellHandHUD | SpellCaster | 손패 5 슈트 표시 |
| SlowMotion(기존) | TimeController | 니어미스 펄스 슬로우를 TimeController 경유로 전환 |
| SpellDeckStore(P2) | DeckData | JSON 직렬화 |
| SpellSynthesis(P3) | DeckData, SpellMarble | 합성 규칙 |

---

## 3. Data Model

### 3.1 Core Types

```csharp
// Domain enums
public enum Suit { Spade, Heart, Club, Diamond }        // ♠공격 ♥회복 ♣유틸 ♦방어
public enum Grade { Normal, Gold, Diamond, Legend }     // 무색→금색→푸른색→무지개
public enum TargetMode { SelfBuff, Targeted }           // 아무데나 드롭=자신 / 위치 지정

// 개별 능력: 추상 SO — 구체 능력은 이 클래스를 상속한 에셋으로 추가(스위치 없음)
public abstract class SpellAbility : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;
    public TargetMode targetMode = TargetMode.Targeted;
    public GameObject effectPrefab;      // 풀링될 이펙트/투사체(선택)
    // 등급별 위력은 마블 고유 속성이므로 ctx.grade를 참조해 구현체가 자유롭게 반영
    public abstract void Activate(SpellContext ctx);
}

// 마블 = 슈트 + 등급 + 능력참조 + 표시용
[CreateAssetMenu(fileName = "NewMarble", menuName = "Spell/SpellMarble")]
public class SpellMarble : ScriptableObject
{
    public string marbleName;
    public Suit suit;
    public Grade grade;                  // 능력에 따라 부여(명세: 능력별 등급)
    public SpellAbility ability;         // 다형성 진입점
    public Sprite icon;
}

// 발동 컨텍스트 — 능력이 필요로 하는 모든 것
public struct SpellContext
{
    public GameObject caster;            // 플레이어
    public Vector2 targetPosition;       // Targeted 드롭 위치(Self면 caster 위치)
    public Grade grade;                  // 위력 스케일 힌트
    public ObjectPool effectPool;        // effectPrefab용(없으면 null)
}
```

### 3.2 Deck & Save (P2)

```csharp
[CreateAssetMenu(fileName = "NewDeck", menuName = "Spell/DeckData")]
public class DeckData : ScriptableObject           // 런타임 편집 대상(디폴트 덱)
{
    public List<SpellMarble> marbles = new();      // 15~25 (검증)
}

// JSON 저장 스키마 — 마블은 이름/식별자로 참조(에셋 GUID 대신 marbleName 키)
[Serializable] public class DeckSaveModel
{
    public int version = 1;
    public List<string> marbleIds = new();         // SpellMarble.marbleName
}
```

> **참조 해석**: `SpellMarbleRegistry`(Resources 또는 인스펙터 배열)가 `marbleName → SpellMarble` 매핑을 제공 → JSON은 문자열 id만 저장. 로드 실패/미존재 id는 스킵(폴백).

### 3.3 등급 시각 매핑 (FR-03)

| Grade | 색상 | 표현 |
|-------|------|------|
| Normal | 무색 | 흰/회색 테두리 |
| Gold | 금색 | 금색 테두리 |
| Diamond | 푸른색 | 청색 테두리 |
| Legend | 무지개 | 무지개 셰이더/그라디언트 테두리 |

`GradePalette`(static 또는 SO)가 `Grade → Color/Material` 반환, HUD·선택창·툴팁 공용.

---

## 4. Component Contracts (REST API 대체 — Unity)

> 서버 없음. 컴포넌트 간 계약(공개 API)을 명세.

### 4.1 TimeController (단일 timeScale 진실원)

| Member | Signature | 설명 |
|--------|-----------|------|
| PushHold | `int PushHold(float scale)` | 홀드형 슬로우 소스 등록(선택 모드). 핸들 반환 |
| PopHold | `void PopHold(int handle)` | 홀드 해제 |
| Pulse | `void Pulse(float scale, float duration)` | 시간제 슬로우(니어미스). unscaled 타이머 |
| (내부) | 활성 소스들의 **최소 scale** 적용, 없으면 1.0 | `fixedDeltaTime = default * scale` 동기화 |
| OnDisable/씬전환 | timeScale=1, fixedDeltaTime 복구 | 안전망 |

> `SlowMotion`(기존)은 timeScale 직접 쓰기 → `TimeController.Pulse()` 호출로 전환. 카메라 줌은 SlowMotion에 잔류(니어미스 전용 연출).

### 4.2 SpellCaster (손패·발동)

| Member | Signature | 설명 |
|--------|-----------|------|
| Slots | `IReadOnlyList<SpellMarble> Slots` (5) | 현재 손패 |
| OnHandChanged | `event Action` | HUD 갱신 트리거 |
| Activate | `bool Activate(int slotIndex, Vector2 dropWorldPos)` | 발동+소비. TargetMode에 따라 위치/자신 |
| (리필) | 사용 슬롯 → `refillCooldown` 후 DeckData 다음 마블 | Time.unscaledTime 기준 |

### 4.3 SpellAbility (확장 지점)

- 계약: `void Activate(SpellContext ctx)` — 부작용으로 효과 실행(풀 이펙트/데미지/버프).
- 신규 능력 = `SpellAbility` 상속 클래스 + `[CreateAssetMenu]` 에셋. **SpellCaster 수정 불필요.**

### 4.4 SpellDeckStore (P2)

| Member | Signature | 설명 |
|--------|-----------|------|
| Save | `void Save(DeckData deck)` | JSON → persistentDataPath/deck.json |
| Load | `DeckData Load()` | 실패/부재 시 기본 덱 폴백(크래시 없음) |

### 4.5 SpellSynthesis (P3)

| Member | Signature | 설명 |
|--------|-----------|------|
| CanSynthesize | `bool CanSynthesize(SpellMarble a, SpellMarble b)` | 같은 Suit && 둘 다 Legend 아님 |
| Synthesize | `SpellMarble Synthesize(a, b)` | 확률 등급업, 실패 시 유지. 1개 소비 |

---

## 5. UI/UX Design

### 5.1 GameScene HUD (우측 하단)

```
                                             ┌ 선택 모드(Ctrl 홀드): 위로 상승 + 툴팁 ┐
                                             │   (○)  (○)  (○)  (○)  (○)          │
                          ...게임 화면...     └────────────────────────────────────┘
                                                 ○   ○   ○   ○   ○   ← 평상시(슈트만)
                                              [우측 하단 5 동그라미]
```

- 평상시: 5 동그라미에 **슈트 아이콘 + 등급 색 테두리**만.
- Ctrl 홀드: 5개가 위로 슬라이드(unscaled 애니메이션) + 선택창, 마우스 호버 시 **능력명/설명/등급 툴팁**. 이때 게임은 슬로우(선택 UI 제외 unscaled 동작).

### 5.2 User Flow

```
[전투] Ctrl 누름 → 슬로우+상승 → 마블에 마우스 오버(상세) → 드래그
      → Targeted: 적/바닥 위치에 드롭 → 그 지점 발동
      → SelfBuff: 아무데나 드롭 → 플레이어 적용
      → 슬롯 소비 → 쿨다운 리필 → Ctrl 뗌 → 복구
[구성] MainScene → 덱 빌더 → 보유 마블 그리드에서 15~25 선택 → 저장 → 전투 반영
```

### 5.3 Component List

| Component | Location | Responsibility |
|-----------|----------|----------------|
| SpellHandHUD | `Assets/Scripts/UI/` | 손패 5 슈트/등급 표시, OnHandChanged 구독 |
| SpellSelectionUI | `Assets/Scripts/Spell/Runtime/` | Ctrl 홀드 상승/툴팁/슬로우 요청 |
| SpellDragHandler | `Assets/Scripts/Spell/Runtime/` | 드래그·드롭 좌표(ScreenToWorld)·발동 트리거 |
| DeckBuilderUI (P2) | `Assets/Scripts/Spell/Runtime/` | 덱 편성·개수 검증·저장 |
| SynthesisDropZone (P3) | `Assets/Scripts/Spell/Synthesis/` | 마블→마블 드롭 감지·합성 호출 |

### 5.4 Page UI Checklist

#### GameScene — Spell HUD
- [ ] Circle x5: 우측 하단 손패 슬롯, 각 슈트 아이콘(♠♥♣♦) 표시
- [ ] Border: 슬롯별 등급 색(무색/금/청/무지개)
- [ ] State: Ctrl 홀드 시 5개 위로 상승(unscaled 트윈)
- [ ] Tooltip: 마우스 호버 시 능력명·설명·등급 표시
- [ ] Cooldown: 사용된 슬롯 리필 대기 시각 표시(딤/타이머)
- [ ] Drag ghost: 드래그 중 커서에 마블 아이콘 추종
- [ ] Slow feedback: 선택 모드 진입 시 화면 슬로우(선택 UI는 정상 반응)

#### MainScene — Deck Builder (P2)
- [ ] Grid: 보유 마블 목록(슈트/등급 표시)
- [ ] Deck panel: 선택된 마블(현재 개수 / 15~25 제약 표시)
- [ ] Validation: 15 미만/25 초과 시 저장 차단 + 안내
- [ ] Button: 저장(→ JSON), 뒤로

#### Synthesis (P3)
- [ ] Drop rule: 같은 슈트에만 드롭 허용(다른 슈트/레전드 포함 시 거부 피드백)
- [ ] Result: 성공/실패(등급 유지) 시각 피드백

---

## 6. Error Handling

| 상황 | 원인 | 처리 |
|------|------|------|
| 이펙트 풀 고갈 | 동시 발동 과다 | 이번 발동 스킵(로그), 크래시 없음 |
| 덱 JSON 부재/손상 | 최초 실행/파일 오류 | 기본 덱(DeckData 에셋)으로 폴백 |
| 미존재 marbleId | 저장 후 에셋 삭제/개명 | 해당 id 스킵, 나머지 로드 |
| 덱 개수 위반 | 15 미만/25 초과 | 저장 차단 + UI 안내 |
| 레전드 합성 시도 | 규칙 위반 | 드롭 거부 + 피드백(FR-12) |
| timeScale 미복구 | 예외/씬 전환 | TimeController 소스 비면 1.0, OnDisable 안전망 |
| Ctrl 중 다른 슬로우(니어미스) | 두 소스 동시 | TimeController 최소 scale 적용, 각각 독립 해제 |

---

## 7. Security Considerations

- 로컬 싱글플레이 게임 — 웹 보안 항목 대부분 N/A.
- [ ] 저장 파일 파싱은 try/catch로 감싸 손상 입력에 안전(무결성 폴백).
- [ ] Resources/Registry 로드 실패 시 방어적 null 체크.

---

## 8. Test Plan (v2.3.0)

> 서버·Playwright 없음(Unity 클라이언트). L1/L2/L3를 **PlayMode 수동 검증 + 에디터 확인**으로 대체. Check 단계는 static 공식 적용.

### 8.1 Test Scope

| Type | Target | Tool | Phase |
|------|--------|------|-------|
| L1: 계약/단위 | TimeController 최소scale·복구, Synthesis 규칙, DeckStore 폴백 | EditMode 테스트 또는 수동 | Do |
| L2: 컴포넌트 액션 | HUD 표시, Ctrl 상승/슬로우, 드래그 발동 | PlayMode 수동 | Do |
| L3: 시나리오 | 덱구성→전투→선택→발동→리필 | PlayMode 수동 | Do |

### 8.2 L1 시나리오 (계약)

| # | 대상 | 검증 | 기대 |
|---|------|------|------|
| 1 | TimeController | Hold+Pulse 동시 | 최소 scale 적용, 각 해제 후 1.0 |
| 2 | TimeController | 소스 전부 해제 | timeScale=1, fixedDeltaTime 복구 |
| 3 | SpellDeckStore | 파일 삭제 후 Load | 기본 덱 반환, 예외 없음 |
| 4 | SpellSynthesis | Legend 포함 | CanSynthesize=false |
| 5 | SpellSynthesis | 같은 슈트 정상 | 확률 등급업, 1개 소비 |

### 8.3 L2 시나리오

| # | 화면 | 액션 | 기대 |
|---|------|------|------|
| 1 | GameScene | 시작 | 손패 5개 슈트/등급색 표시 |
| 2 | GameScene | Ctrl 홀드 | 5개 상승 + 슬로우 진입, 릴리즈 시 복구 |
| 3 | GameScene | 호버 | 능력 툴팁 표시 |
| 4 | GameScene | Self 드래그→아무데나 | 플레이어에 버프 적용, 슬롯 소비 |
| 5 | GameScene | Targeted 드래그→위치 | 드롭 지점 발동 |
| 6 | GameScene | 사용 후 대기 | 쿨다운 뒤 슬롯 리필 |

### 8.4 L3 시나리오

| # | 시나리오 | 단계 | 성공 기준 |
|---|----------|------|-----------|
| 1 | 풀 루프(P2 포함) | 덱 구성·저장 → 전투 → 선택·발동 → 리필 | 저장 덱 반영 + 발동 정상 + timeScale 복구 |
| 2 | 확장성 증명 | 새 SpellAbility 에셋 1종 추가 → 마블 생성 → 손패 태움 | 코드 수정 없이 발동됨 |

### 8.5 Seed Data

| Entity | Min | 필드 |
|--------|:---:|------|
| SpellMarble 에셋 | 5+ | suit, grade, ability(참조 능력 2종 중) |
| 참조 SpellAbility | 2 | SelfBuff 1(예: 순간 실드) + Targeted 1(예: 지점 폭발) |
| DeckData 기본 덱 | 1 | 15~25 marble |

---

## 9. Clean Architecture (Unity 적용)

### 9.1 Layer Assignment

| Layer | 책임 | 위치 | 본 기능 컴포넌트 |
|-------|------|------|------------------|
| **Domain(데이터)** | 순수 데이터/규칙 | `Spell/Data/` | Suit, Grade, SpellMarble, SpellAbility(추상), SpellContext, DeckData |
| **Application(런타임)** | 오케스트레이션 | `Spell/Runtime/` | SpellCaster, TimeController, SpellSelectionUI, SpellDragHandler |
| **Presentation(UI)** | 표시 | `Scripts/UI/` | SpellHandHUD, DeckBuilderUI |
| **Infrastructure** | 저장/외부 | `Spell/Persistence/` | SpellDeckStore(JSON) |
| **Abilities(확장)** | 콘텐츠 | `Spell/Abilities/` | 구체 SpellAbility 에셋(후속 추가) |

### 9.2 Dependency Rule

```
Presentation(HUD) ──▶ Application(Caster/UI) ──▶ Domain(Data)
                                   └──▶ Infrastructure(Store)
Abilities(구체) ──▶ Domain(SpellContext) + 기존(Character/PlayerController/ObjectPool)
Rule: Domain(데이터/추상)은 상위 레이어를 모른다.
```

---

## 10. Coding Convention Reference

### 10.1 Naming (C# / Unity)

| Target | Rule | Example |
|--------|------|---------|
| 클래스/SO | PascalCase | `SpellCaster`, `SpellMarble` |
| 메서드 | PascalCase | `Activate()`, `PushHold()` |
| 필드(public) | camelCase | `refillCooldown`, `targetMode` |
| 필드(private) | camelCase | `slots`, `nextRefillTime` |
| enum | PascalCase | `Suit.Spade` |
| 파일 | 클래스명.cs | `SpellCaster.cs` |
| 폴더 | PascalCase(기존 관례) | `Spell/Runtime/` |

### 10.2 This Feature's Conventions

| Item | 적용 |
|------|------|
| 데이터 | ScriptableObject(`WeaponData` 선례 준수) |
| 상태관리 | MonoBehaviour 매니저 + event(OnHandChanged) |
| 시간 | `TimeController` 단일 창구, UI는 unscaled |
| 주석 | `// Design Ref: §N` / `// Plan SC: FR-0X` |

---

## 11. Implementation Guide

### 11.1 File Structure

```
Assets/Scripts/Spell/
  Data/        Suit.cs, Grade.cs, SpellContext.cs, SpellAbility.cs, SpellMarble.cs, DeckData.cs, GradePalette.cs, SpellMarbleRegistry.cs
  Runtime/     TimeController.cs, SpellCaster.cs, SpellSelectionUI.cs, SpellDragHandler.cs
  Abilities/   SelfBuffShieldAbility.cs, TargetedBurstAbility.cs   (참조 2종)
  Persistence/ SpellDeckStore.cs                                   (P2)
  Synthesis/   SpellSynthesis.cs, SynthesisDropZone.cs             (P3)
Assets/Scripts/UI/  SpellHandHUD.cs, DeckBuilderUI.cs(P2)
수정: Assets/Scripts/Combat/SlowMotion.cs (Pulse를 TimeController 경유),
      Assets/Scripts/Player/PlayerController.cs (버프 훅: 실드/회복 진입점)
```

### 11.2 Implementation Order

1. [ ] Domain 데이터(enum/SO/Context) + GradePalette + Registry
2. [ ] TimeController + SlowMotion 리팩터(니어미스 회귀 없음 확인)
3. [ ] SpellCaster(손패/리필) + 참조 능력 2종
4. [ ] SpellHandHUD(5 동그라미)
5. [ ] SpellSelectionUI(Ctrl 상승/툴팁/슬로우) + SpellDragHandler(발동)
6. [ ] (P2) SpellDeckStore + DeckBuilderUI + MainScene 덱 씬
7. [ ] (P3) SpellSynthesis + DropZone

### 11.3 Session Guide

#### Module Map

| Module | Scope Key | Description | Est. Turns |
|--------|-----------|-------------|:----------:|
| 코어 데이터+시간 | `phase-1a` | Data SO 일체 + TimeController + SlowMotion 전환 | 20-25 |
| 코어 런타임+UI | `phase-1b` | SpellCaster + 참조능력2 + HUD + 선택/드래그 발동 | 35-45 |
| 덱 빌더+저장 | `phase-2` | DeckStore(JSON) + DeckBuilderUI + MainScene 씬 | 30-40 |
| 합성 | `phase-3` | SpellSynthesis + DropZone | 20-25 |

#### Recommended Session Plan

| Session | Phase | Scope | Turns |
|---------|-------|-------|:-----:|
| S1 | Plan+Design | 전체 | ✅ 완료 |
| S2 | Do | `--scope phase-1a` | 20-25 |
| S3 | Do | `--scope phase-1b` | 35-45 |
| S4 | Check+Report(P1) | 전체 | 25-30 |
| S5+ | Do | `--scope phase-2` → `phase-3` | 각 30-40 |

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-06 | 초안(Option C: 추상 SO 능력 + TimeController 단일 슬로우 + 3-Phase 모듈맵) | KimHanWhee |
