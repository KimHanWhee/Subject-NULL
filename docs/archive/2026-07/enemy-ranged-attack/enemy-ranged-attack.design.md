# enemy-ranged-attack Design Document

> **Feature**: 원거리 적 + 적 총알(진영 분리)
> **Project**: MiniGungeon (Unity 2022.3, 2D 탑다운 슈터)
> **Version**: 0.1 (dev)
> **Author**: KimHanWhee
> **Date**: 2026-07-03
> **Status**: Draft
> **Selected Architecture**: **Option C — Pragmatic Balance**
> **Upstream**: `docs/01-plan/features/enemy-ranged-attack.plan.md`

---

## Context Anchor

| Key | Value |
|-----|-------|
| **WHY** | 근접 전용 적만 있어 거리 유지로 무력화됨. 원거리 위협 부재 + 니어미스 슬로우모션 선행 조건 |
| **WHO** | 플레이어(회피 대상), 향후 슬로우모션 기능을 붙일 개발자 |
| **RISK** | 아군/적 총알 태그 혼동으로 인한 오사 또는 무피해, 사거리 유지 로직의 위치 떨림, 총알 풀 고갈 |
| **SUCCESS** | 원거리 적이 사거리를 유지하며 플레이어에게 총알을 발사하고, 그 총알은 플레이어에게만 데미지를 준다 |
| **SCOPE** | 직진 총알 1종 · 원거리 적 신규 유형 · faction 분리 · 플레이어 총알 피격 처리. 화살/레이저·슬로우모션·유도탄 제외 |

---

## 1. Overview

### 1.1 Selected Architecture — Option C (Pragmatic Balance)

핵심 원칙: **동작 중인 근접 코드(`EnemyController`, `Bullet`)를 건드리지 않고**, 적 진영 요소를 **독립 컴포넌트로 추가**한다.

| 축 | 결정 |
|----|------|
| 진영 분리 | `"EnemyBullet"` 태그 신설 (기존 태그 체크 방식 유지, 레이어 매트릭스 미변경) |
| 적 총알 | 신규 `EnemyBullet.cs` (플레이어 `Bullet.cs`와 독립) |
| 원거리 적 | 신규 `RangedEnemyController.cs` (기존 `EnemyController` 미변경, `Character` 재사용) |
| 플레이어 피격 | `PlayerController`에 `TakeHit(float)` 추출 + `OnTriggerEnter2D("EnemyBullet")` 추가 |
| 스폰 | `GameManager`에 원거리 적 전용 풀 참조 + 가중 랜덤 |

### 1.2 Component Map

```
[GameManager]  ── 근접 풀(기존 ObjectPool) ──▶ Slime(EnemyController)
     └── 원거리 풀(신규 ObjectPool 참조) ──▶ RangedEnemy(RangedEnemyController)
                                                    │ FireBulletTowards(player)
                                                    ▼
                                          [BulletPoolManager] ──▶ EnemyBullet("EnemyBullet")
                                                    │ OnTrigger("Player"/"Wall") → 반환
                                                    ▼
                                          [PlayerController.OnTriggerEnter2D("EnemyBullet")]
                                                    └── TakeHit(damage) → Character.Hit → Flash/Die
```

### 1.3 Faction Collision Matrix (who damages whom)

| 발사체 | 태그 | 타격 대상 | 무시 대상 | 소멸 조건 |
|--------|------|-----------|-----------|-----------|
| 플레이어 총알 | `"Bullet"` | Enemy (근접/원거리 공통) | Player, 다른 총알 | Wall, Enemy |
| **적 총알(신규)** | `"EnemyBullet"` | **Player** | Enemy(오사 X), 아군 총알 | **Player, Wall, 수명 초과** |

> 원거리 적도 플레이어 총알(`"Bullet"`)에 맞아 죽어야 하므로, `RangedEnemy`는 기존 적처럼 `"Enemy"` 태그 + `Character`를 유지한다. 적 총알(`"EnemyBullet"`)은 `EnemyController`/`RangedEnemyController`가 검사하지 않으므로 오사가 원천 차단된다.

---

## 2. New Components

### 2.1 `EnemyBullet.cs` (신규)

플레이어 `Bullet.cs`와 동일한 직진 이동을 하되, **타격 대상이 플레이어**이고 **수명(lifetime)** 을 가진다.

```csharp
using UnityEngine;

// Design Ref: §2.1 — 적 진영 총알. 플레이어만 타격, 수명/벽/플레이어 충돌 시 풀 반환.
public class EnemyBullet : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 1f;
    public float lifetime = 4f; // Plan FR-06: 수명 초과 시 반환 (풀 고갈 방지)

    private Vector2 direction;
    private float despawnTime;

    public Vector2 Direction
    {
        get { return direction; }
        set { direction = value.normalized; }
    }

    void OnEnable()
    {
        despawnTime = Time.time + lifetime; // 활성화 시점 기준 수명 시작
    }

    void Update()
    {
        transform.Translate(direction * (speed * Time.deltaTime));
        if (Time.time >= despawnTime)
            gameObject.SetActive(false); // 수명 초과 반환
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Plan FR-04/FR-05: 플레이어만 타격, 데미지는 PlayerController가 처리(TakeHit)
        if (collision.CompareTag("Player") || collision.CompareTag("Wall"))
        {
            gameObject.SetActive(false); // 풀 반환
        }
    }
}
```

> **데미지 처리 위치**: 총알이 직접 `Character.Hit`를 부르지 않고, 플레이어 쪽 `OnTriggerEnter2D("EnemyBullet")`가 `TakeHit`을 호출한다. 이유: Flash/Die 연출·씬전환이 `PlayerController`에 있어, 데미지 책임을 플레이어가 소유해야 접촉 데미지와 일관된다. (트리거 이벤트는 양쪽 GameObject에 모두 전달되므로 총알은 소멸만, 플레이어는 피격만 담당)

**식별자(FR-08)**: `"EnemyBullet"` 태그 + `EnemyBullet` 컴포넌트 자체가 향후 니어미스 감지의 식별 수단이 된다 (다음 사이클에서 플레이어 근접 스캔이 `EnemyBullet`를 대상으로 거리 측정).

### 2.2 `RangedEnemyController.cs` (신규)

기존 `EnemyController`의 Spawn/Flash/Die 흐름을 따르되, Moving 상태에서 **사거리 유지 + 주기 사격**을 한다.

```csharp
using UnityEngine;

// Design Ref: §2.2 — 원거리 적. 사거리 유지(히스테리시스) + 주기 사격.
public class RangedEnemyController : MonoBehaviour
{
    enum State { Spawning, Moving, Dying }

    [Header("Move")]
    public float speed = 2f;

    [Header("Range Band")] // Plan FR-02: 히스테리시스로 경계 떨림 방지
    public float farBand = 7f;   // 이보다 멀면 접근
    public float nearBand = 4f;  // 이보다 가까우면 후퇴
    // nearBand ~ farBand 사이 = 정지(사격 밴드)

    [Header("Fire")] // Plan FR-01/FR-03/FR-07
    public GameObject bulletPrefab;      // 적 총알 prefab ("EnemyBullet" 태그)
    public BulletPoolManager bulletPoolManager;
    public float fireInterval = 1.5f;    // 발사 간격(초)
    public float bulletSpeed = 8f;
    public float bulletDamage = 1f;
    public AudioClip fireSound;

    [Header("Material")]
    public Material flashMaterial;
    public Material defaultMaterial;

    private GameObject target;
    private State state;
    private SpriteRenderer sr;
    private Animator anim;
    private float nextFireTime;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    // 기존 EnemyController.Spawn과 동일한 스폰 흐름
    public void Spawn(GameObject target)
    {
        this.target = target;
        state = State.Spawning;
        GetComponent<Character>().Initialize();
        anim.SetTrigger("Spawn");
        GetComponent<Collider2D>().enabled = false;
        Invoke(nameof(StartMoving), 1f);
        nextFireTime = Time.time + fireInterval; // 스폰 직후 즉발 방지
    }

    void StartMoving()
    {
        GetComponent<Collider2D>().enabled = true;
        state = State.Moving;
    }

    void FixedUpdate()
    {
        if (state != State.Moving || target == null) return;

        Vector2 toTarget = target.transform.position - transform.position;
        float dist = toTarget.magnitude;
        Vector2 dir = toTarget.normalized;

        // Plan FR-02: 사거리 유지 (밴드 밖이면 접근/후퇴, 안이면 정지)
        if (dist > farBand)
            transform.Translate(dir * (speed * Time.fixedDeltaTime));
        else if (dist < nearBand)
            transform.Translate(-dir * (speed * Time.fixedDeltaTime));
        // else: 정지

        sr.flipX = dir.x < 0;

        // Plan FR-01: 사격 밴드(대략 nearBand~farBand) + 쿨다운
        if (dist <= farBand && Time.time >= nextFireTime)
        {
            Fire(dir);
            nextFireTime = Time.time + fireInterval;
        }
    }

    // Plan FR-03: 발사 순간 플레이어 방향으로 직진(유도 없음)
    void Fire(Vector2 dir)
    {
        if (bulletPrefab == null || bulletPoolManager == null) return;

        ObjectPool pool = bulletPoolManager.GetPool(bulletPrefab);
        GameObject b = pool.Get();
        if (b == null) return; // 풀 고갈 시 이번 발사 스킵

        b.transform.position = transform.position;
        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        eb.Direction = dir;
        eb.speed = bulletSpeed;
        eb.damage = bulletDamage;

        if (fireSound != null)
            GetComponent<AudioSource>().PlayOneShot(fireSound);
    }

    // 기존 EnemyController와 동일한 피격/사망 (플레이어 총알 "Bullet"에 반응)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            float d = collision.GetComponent<Bullet>().damage;
            if (GetComponent<Character>().Hit(d)) Flash();
            else Die();
        }
    }

    void Flash() { sr.material = flashMaterial; Invoke(nameof(AfterFlash), 0.5f); }
    void AfterFlash() { sr.material = defaultMaterial; }
    void Die() { state = State.Dying; anim.SetTrigger("Die"); Invoke(nameof(AfterDying), 0.6f); }
    void AfterDying() { gameObject.SetActive(false); }
}
```

> **Note**: 적 총알(`"EnemyBullet"`)은 이 컨트롤러가 검사하지 않으므로 다른 원거리 적을 통과한다 → 오사 없음. 자기 총알에도 안전.

---

## 3. Modified Components

### 3.1 `PlayerController.cs` (수정)

접촉 데미지 로직을 `TakeHit(float)`로 추출하고, 적 총알 트리거 핸들러를 추가한다.

```csharp
// 기존 OnCollisionStay2D 내부 로직을 공통 메서드로 추출
private void OnCollisionStay2D(Collision2D collision)
{
    if (collision.gameObject.CompareTag("Enemy") && Time.time >= nextDamageTime)
    {
        nextDamageTime = Time.time + damageInterval;
        TakeHit(1); // 접촉 데미지
    }
}

// Plan FR-05: 적 총알 피격 (트리거)
private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("EnemyBullet"))
    {
        float d = collision.GetComponent<EnemyBullet>().damage;
        TakeHit(d); // 총알은 스스로 소멸(§2.1), 여기선 데미지만
    }
}

// Design Ref: §3.1 — 접촉/총알 공통 피격 처리 (Flash/Die 소유)
void TakeHit(float damage)
{
    if (GetComponent<Character>().Hit(damage)) Flash();
    else Die();
}
```

> 접촉 데미지는 `damageInterval` 쿨다운으로 게이트되지만, 총알 피격은 **매 명중마다** 1회 적용(총알이 곧 소멸하므로 중복 없음). 총알 피격에는 별도 쿨다운을 두지 않는다.

### 3.2 `GameManager.cs` (수정)

원거리 적 전용 풀 참조와 가중 랜덤 스폰을 추가한다.

```csharp
public ObjectPool meleePool;        // 기존 근접 풀 (Inspector 재지정)
public ObjectPool rangedPool;       // 신규: 원거리 적 풀 (자식 오브젝트의 ObjectPool)
[Range(0f, 1f)] public float rangedSpawnChance = 0.4f; // 원거리 적 비율

void Start()
{
    meleePool.Initialize();
    rangedPool.Initialize();
    timeAfterLastSpawn = 0; score = 0;
}

void SpawnEnemy()
{
    float x = Random.Range(-9f, 9f);
    float y = Random.Range(-5f, 5f);

    bool ranged = Random.value < rangedSpawnChance;
    ObjectPool pool = ranged ? rangedPool : meleePool;

    GameObject obj = pool.Get();
    if (obj == null) return; // 풀 고갈 방어
    obj.transform.position = new Vector3(x, y, 0);

    if (ranged) obj.GetComponent<RangedEnemyController>().Spawn(player);
    else        obj.GetComponent<EnemyController>().Spawn(player);
}
```

> **마이그레이션 주의**: 현재 `GameManager`는 `GetComponent<ObjectPool>()`(자기 자신의 단일 풀)을 사용한다. 이를 `meleePool` 참조로 바꾼다. 근접 풀 컴포넌트는 그대로 두고 Inspector에서 `meleePool`에 드래그하면 된다. `rangedPool`은 자식 GameObject에 `ObjectPool`을 붙여 원거리 적 prefab을 지정한다.

---

## 4. Data & Project Settings

| 항목 | 값/방식 |
|------|---------|
| 신규 태그 | `"EnemyBullet"` (Project Settings > Tags) |
| 적 총알 prefab | Sprite + `CircleCollider2D(isTrigger=true)` + `Rigidbody2D(선택, kinematic)` + `EnemyBullet` + 태그 `"EnemyBullet"` |
| 원거리 적 prefab | 기존 Slime 구성 복제 → `EnemyController` 제거, `RangedEnemyController` 부착, `Character`/`Animator`/`AudioSource` 유지, 태그 `"Enemy"` |
| BulletPoolManager | 씬에 이미 존재하는 것 재사용 (원거리 적의 `bulletPoolManager`에 할당) |

---

## 5. State Machine (RangedEnemy)

```
Spawning ──(1초 후 StartMoving)──▶ Moving ──(Character.Hit=false)──▶ Dying ──▶ (풀 반환)

Moving 매 FixedUpdate:
  dist = |player - self|
  dist > farBand   → 접근
  dist < nearBand  → 후퇴
  nearBand..farBand→ 정지
  if dist ≤ farBand && Time.time ≥ nextFireTime → Fire, 쿨다운 갱신
```

---

## 6. Edge Cases & Contingencies

| # | 상황 | 처리 |
|---|------|------|
| E1 | 풀 고갈(30개 소진) | `Get()==null` → 이번 발사/스폰 스킵 (크래시 없음) |
| E2 | 총알이 아무것도 안 맞고 계속 날아감 | `lifetime`(4초) 초과 시 자동 반환 |
| E3 | target(player) 사망/파괴 | `target == null` 가드로 이동/사격 중단 |
| E4 | 경계 거리에서 접근/후퇴 반복(떨림) | `nearBand < farBand` 데드존(히스테리시스) |
| E5 | 스폰 직후 즉발 | `nextFireTime = Time.time + fireInterval`로 지연 |
| E6 | 적 총알이 다른 적/자기 적 통과 | 적 컨트롤러가 `"EnemyBullet"` 미검사 → 통과(오사 X) |
| E7 | 적 총알이 플레이어 총알과 겹침 | 서로 태그 미검사 → 통과(상쇄 없음) |
| E8 | 원거리 적이 벽 뒤에서 사격 | MVP는 시야판정 없음 — 벽이 총알을 막음(E: Wall 충돌 반환)으로 자연 처리 |

---

## 7. Success Criteria Mapping

| Plan FR | Design 반영 |
|---------|-------------|
| FR-01 원거리 적 주기 사격 | §2.2 `Fire` + `fireInterval` 쿨다운 |
| FR-02 사거리 유지 | §2.2 farBand/nearBand 히스테리시스 |
| FR-03 발사 순간 직진 | §2.2 `Fire(dir)` 방향 고정, 유도 없음 |
| FR-04 플레이어만 타격 | §1.3 태그 분리, 적 컨트롤러 `"EnemyBullet"` 미검사 |
| FR-05 Character.Hit 연결 | §3.1 `OnTriggerEnter2D` → `TakeHit` |
| FR-06 벽/수명/화면밖 반환 | §2.1 `lifetime` + Wall 충돌 |
| FR-07 데이터 튜닝 | §2.2 Inspector 필드(간격/밴드/속도/데미지) |
| FR-08 니어미스 식별자 | §2.1 `"EnemyBullet"` 태그 + `EnemyBullet` 컴포넌트 |

---

## 8. Test Plan (Play 모드 수동 검증)

| ID | 시나리오 | 기대 결과 |
|----|----------|-----------|
| T1 | 원거리 적 스폰 후 접근 | farBand 밖→접근, nearBand 안→후퇴, 밴드 내 정지 |
| T2 | 사격 밴드 진입 | `fireInterval` 간격으로 플레이어 방향 총알 발사 |
| T3 | 총알 플레이어 명중 | HP 감소 + Flash, 0이면 GameOver 씬 전환 |
| T4 | 총알이 다른 적 통과 | 다른 적 HP 불변 (오사 없음) |
| T5 | 플레이어 총알로 원거리 적 처치 | 기존처럼 Flash→Die→반환 |
| T6 | 총알 빗나감 | 4초 후 소멸(풀 반환), 벽 충돌 시 즉시 소멸 |
| T7 | 근접 슬라임 회귀 | 기존 추적·접촉 데미지 정상 |
| T8 | 장시간 플레이 | 풀 고갈로 인한 발사 누락/크래시 없음 |

---

## 9. Out of Scope (재확인)

- 화살/레이저 발사체, 유도/예측 조준, 시야(LoS) 판정
- 니어미스 슬로우모션 (식별자만 §2.1에 준비)
- 적 공격 전조 애니메이션 정교화

---

## 10. Impact Summary

| 파일 | 유형 | 변경 |
|------|------|------|
| `Assets/Scripts/Combat/EnemyBullet.cs` | 신규 | 적 총알 (직진+수명+플레이어/벽 충돌) |
| `Assets/Scripts/Enemy/RangedEnemyController.cs` | 신규 | 사거리 유지 + 주기 사격 |
| `Assets/Scripts/Player/PlayerController.cs` | 수정 | `TakeHit` 추출 + `OnTriggerEnter2D("EnemyBullet")` |
| `Assets/Scripts/GameManager.cs` | 수정 | 근접/원거리 풀 참조 + 가중 랜덤 스폰 |
| `"EnemyBullet"` 태그 | 신규 | Project Settings |
| 원거리 적 prefab / 적 총알 prefab | 신규 | 에디터 구성 (안내 제공) |

---

## 11. Implementation Guide

### 11.1 구현 순서

1. `"EnemyBullet"` 태그 추가 (Project Settings)
2. `EnemyBullet.cs` 작성
3. `RangedEnemyController.cs` 작성
4. `PlayerController.cs` 수정 (`TakeHit` 추출 + 트리거 핸들러)
5. `GameManager.cs` 수정 (풀 참조 + 가중 스폰)
6. 에디터: 적 총알 prefab / 원거리 적 prefab 구성 + 풀 배선
7. Play 모드 검증 (§8 T1~T8)

### 11.2 핵심 파일/컴포넌트

- 신규: `EnemyBullet`, `RangedEnemyController`
- 수정: `PlayerController`, `GameManager`
- 재사용: `Character`, `BulletPoolManager`, `ObjectPool`

### 11.3 Session Guide (Module Map)

| Module | 범위 | 파일 | 의존 |
|--------|------|------|------|
| **module-1** | 진영 분리 + 적 총알 | `EnemyBullet.cs` + `"EnemyBullet"` 태그 | 없음 |
| **module-2** | 원거리 적 | `RangedEnemyController.cs` | module-1 (bulletPrefab) |
| **module-3** | 플레이어 피격 | `PlayerController.cs` | module-1 (태그) |
| **module-4** | 스폰 통합 | `GameManager.cs` | module-2 |
| **module-5** | 에디터 구성 | prefab/태그/풀 배선 | module-1~4 |

**권장 세션 분할**: 코드(module-1~4)를 한 세션에, 에디터 구성(module-5)을 사용자 작업 세션으로. 코드 규모가 작아 `/pdca do enemy-ranged-attack` 전체 스코프 1회 진행도 무리 없음.

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 0.1 | 2026-07-03 | Initial design (Option C 선택) | KimHanWhee |
