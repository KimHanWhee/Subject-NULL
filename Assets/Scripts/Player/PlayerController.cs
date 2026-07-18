using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    
    public float speed = 8;
    public WeaponData currentWeapon;
    public Vector2 muzzleOffset = new Vector2(0f, 0.05f); // 총알 발사 위치(원점=캐릭터 세로중앙 기준, 팔/몸통 높이)
    public BulletPoolManager bulletPoolManager; // currentBulletPool 대신
    public Material flashMaterial;
    public Material defaultMaterial;

    public float damageInterval = 1f; // 적과 붙어있을 때 데미지 간격(초)

    [Header("Dash")] // Design Ref: §3.1 — 대시 튜닝 파라미터
    public float dashSpeed = 24f;      // 대시 속도 (일반 speed의 3배)
    public float dashDuration = 0.2f;  // 대시 지속시간(초)
    public float dashCooldown = 1f;    // 대시 쿨다운(초) — Claude.md 스펙
    public AudioClip dashSound;        // 대시 시작음

    [Header("Dash Stamina")] // 대시 v2: 스태미너 시스템 (Claude.md "스테미너 추가 필요")
    public float maxStamina = 100f;        // 최대 스태미너
    public float dashStaminaCost = 34f;    // 대시 1회 소모량 (연속 2~3회)
    public float staminaRegenRate = 25f;   // 초당 회복량
    public float staminaRegenDelay = 0.5f; // 대시 후 회복 시작 지연(초)

    [Header("Dash Near-Miss")] // Design Ref: §2.1 — 니어미스 슬로우모션 판정용
    public float dashGrace = 0.1f;   // 대시 종료 후 니어미스 인정 유예(초)

    [Header("Dash VFX")] // 대시 가시성: 잔상 트레일 + 시작 버스트
    public Color dashGhostTint = new Color(0.55f, 0.85f, 1f, 0.55f); // 청백 잔상
    public float dashGhostInterval = 0.035f; // 잔상 생성 간격(초) — 대시 0.2초 동안 5~6장

    Vector3 move;
    private SpriteRenderer sr;
    private Animator anim;
    private PlayerAnim panim; // 방향별 스프라이트 애니메이션(Animator 대체)
    private Rigidbody2D rb;
    private float nextFireTime; // 다음 발사 허용 시각 (Time.time 기준)
    private float nextDamageTime; // 다음 피격 허용 시각 (Time.time 기준)

    // Design Ref: §3.2 — 대시 내부 상태
    private bool isDashing;
    private float nextGhostTime;        // 대시 잔상 다음 생성 시각
    private SpriteRenderer bodySprite;  // 대시 잔상 원본(지연 캐싱)
    private float dashEndTime;   // 대시 종료 시각
    private float nextDashTime;  // 다음 대시 허용 시각 (쿨다운)
    private Vector3 dashDir;
    private Vector3 lastMoveDir = Vector3.right; // 입력 없을 때 폴백 방향

    // 대시 v2: 스태미너 상태
    private float currentStamina;   // 현재 스태미너
    private float staminaRegenTime; // 이 시각 이후부터 회복 시작 (대시 후 지연)

    // Design Ref: §2.1 — 니어미스 유예: 이 시각까지 대시 판정 유지
    private float dashGraceUntil;

    // 스펠 마블 ♦ Fortress — true면 이동/대시 불가(피해 무효는 FortressStatus가 처리)
    [NonSerialized] public bool movementLocked;

    // 스펠 마블 ♥ Adrenaline 등 — 발사 속도 배율(1=기본). 상태 컴포넌트가 관리.
    [NonSerialized] public float fireRateMultiplier = 1f;

    [Header("Score Attack Speed")] // 점수가 오를수록 기본 공속 증가(성장감)
    public float scoreAtkFullAt = 1000f;   // 이 점수에서 보너스 최대
    [Range(0f, 2f)] public float scoreAtkMaxBonus = 0.8f; // 최대 추가 공속(+80%)

    // spell-marble Design §11.1 — ♦ 방어(SelfBuff 실드) 훅. 이 시각까지 데미지 무시.
    private float shieldUntil;
    public bool IsShieldActive => Time.time <= shieldUntil;
    public void GrantShield(float duration) => shieldUntil = Mathf.Max(shieldUntil, Time.time + duration);

    // 조커 집단 혼란 — 이 시각까지 조작 반전(입력 방향의 반대로 이동). 시각 표시는 JokerSpell 담당.
    private float invertUntil;
    public bool ControlsInverted => Time.time <= invertUntil;
    public void ApplyControlInvert(float duration) => invertUntil = Mathf.Max(invertUntil, Time.time + duration);

    // 스펠 마블 ♥ 무한 질주 — 이 시각까지 대시가 스태미너를 소모하지 않고 쿨타임도 짧아짐.
    private float staminaFreeUntil;
    private float staminaFreeCooldown = 0.2f; // 무한 질주 중 대시 쿨타임(초)
    public bool IsStaminaFree => Time.time <= staminaFreeUntil;
    public void GrantStaminaFree(float duration, float dashCooldownWhileActive = 0.2f)
    {
        staminaFreeUntil = Mathf.Max(staminaFreeUntil, Time.time + duration);
        staminaFreeCooldown = dashCooldownWhileActive;
    }

    // UI 연동용 (스태미너 바에서 0~1 비율로 사용) — 대시 v2 UI 사이클에서 연결
    public float StaminaRatio => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    // Plan SC: FR-01 — 대시 중 또는 대시 직후 유예 내면 true (NearMissDetector가 읽음)
    public bool IsDashActive => isDashing || Time.time <= dashGraceUntil;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        panim = GetComponent<PlayerAnim>();
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 대시 고속 이동 시 벽 터널링 방지
            rb.freezeRotation = true; // 대각선 대시로 벽 충돌 시 토크로 인한 회전(맵 빙글빙글) 방지
        }
        currentStamina = maxStamina; // 대시 v2: 시작 시 스태미너 풀 충전
        GetComponent<Character>().Initialize();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        move = Vector3.zero;
        
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
        {
            move += new Vector3(-1, 0, 0);
            // transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        } 
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            move += new Vector3(1, 0, 0);
            // transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        } 
        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
        {
            move += new Vector3(0, 1, 0);
            // transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        } 
        if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
        {
            move += new Vector3(0, -1, 0);
            // transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        } 
        
        move = move.normalized;

        if (ControlsInverted && move.magnitude > 0f) move = -move; // 조커 집단 혼란 — 조작 반전

        if (move.magnitude > 0) lastMoveDir = move; // Plan SC: FR-02 — 폴백용 마지막 이동 방향

        // 방향별 스프라이트(east/west) — 좌우 이동 시 갱신
        if (move.x < 0) panim.SetFacing(false);
        else if (move.x > 0) panim.SetFacing(true);

        panim.SetMoving(move.magnitude > 0);

        if (Mouse.current.leftButton.isPressed
            && currentWeapon != null
            && Time.time >= nextFireTime
            && !IsSpellSelecting()) // Ctrl 선택 모드 중엔 기본 공격 억제(드래그로 마블만 사용)
        {
            Shoot();
            // 점수 기반 공속 보너스(상한 있음) — 스펠 마블 배율과 곱연산
            float scoreMul = 1f;
            if (GameManager.Instance != null && scoreAtkFullAt > 0f)
                scoreMul = 1f + Mathf.Clamp01(GameManager.Instance.Score / scoreAtkFullAt) * scoreAtkMaxBonus;
            float rate = Mathf.Max(currentWeapon.fireRate * Mathf.Max(fireRateMultiplier, 0.01f) * scoreMul, 0.0001f);
            nextFireTime = Time.time + 1f / rate;
        }

        RegenStamina(); // 대시 v2: 스태미너 회복
        TryStartDash(); // Plan SC: FR-01
    }

    // Design Ref: §4.1 — Space 입력 감지 + 쿨다운/스태미너 게이트로 대시 시작
    void TryStartDash()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame
            && !movementLocked                   // 스펠 마블 ♦ Fortress — 대시도 불가
            && !isDashing                        // Plan SC: FR-05 — 대시 중 재입력 무시
            && Time.time >= nextDashTime          // Plan SC: FR-04 — 쿨다운
            && (IsStaminaFree || currentStamina >= dashStaminaCost)) // 무한 질주 중엔 스태미너 무관
        {
            dashDir = (move.magnitude > 0 ? move : lastMoveDir).normalized; // Plan SC: FR-02
            isDashing = true;
            dashEndTime = Time.time + dashDuration;
            nextDashTime = Time.time + (IsStaminaFree ? staminaFreeCooldown : dashCooldown); // 무한 질주 중 쿨타임 단축

            if (!IsStaminaFree) // 무한 질주 중이면 스태미너 소모/회복지연 없음
            {
                currentStamina -= dashStaminaCost;                   // 대시 v2: 스태미너 소모
                staminaRegenTime = Time.time + staminaRegenDelay;    // 대시 v2: 회복 지연 시작
            }

            if (dashSound != null)
                GetComponent<AudioSource>().PlayOneShot(dashSound); // 대시 시작음

            // 대시 가시성: 시작 버스트 + 잔상 트레일 시작
            if (bodySprite == null) bodySprite = GetComponent<SpriteRenderer>();
            DashGhost.Spawn(bodySprite, dashGhostTint);
            nextGhostTime = Time.time + dashGhostInterval;
            SpellParticleVfx.SpawnBurst(SpellVfx.VisualAnchor(gameObject).position, 0.55f, dashGhostTint, 12, 0.25f);
        }
    }

    // 대시 v2: 회복 지연이 지난 뒤 초당 staminaRegenRate만큼 회복
    void RegenStamina()
    {
        if (currentStamina >= maxStamina) return;
        if (Time.time < staminaRegenTime) return; // 대시 직후 지연 구간
        currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
    }

    // Design Ref: §4.2 — 대시 이동 처리. 이번 프레임 이동을 대시가 처리하면 true
    bool TickDash()
    {
        if (!isDashing) return false;

        if (Time.time >= dashEndTime)
        {
            isDashing = false;
            dashGraceUntil = Time.time + dashGrace; // Design Ref: §2.1 — 대시 직후 니어미스 유예
            return false;
        }

        // 대시 경로를 따라 일정 간격으로 잔상 생성(트레일)
        if (Time.time >= nextGhostTime)
        {
            if (bodySprite == null) bodySprite = GetComponent<SpriteRenderer>();
            DashGhost.Spawn(bodySprite, dashGhostTint);
            nextGhostTime = Time.time + dashGhostInterval;
        }

        // Plan SC: FR-03 — MovePosition으로 물리 충돌 존중 (transform.Translate는 벽 관통)
        Vector2 delta = (Vector2)dashDir * (dashSpeed * Time.fixedDeltaTime);
        if (rb != null)
            rb.MovePosition(rb.position + delta);
        else
            transform.Translate(delta);
        return true;
    }

    // 스펠 마블 선택 모드(Shift 홀드) 여부 — 이 동안 기본 공격 억제
    // (Ctrl → Shift: WebGL에서 Ctrl+W 등 브라우저 단축키 충돌 방지, SpellSelectionUI와 동일 판정)
    bool IsSpellSelecting()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
    }

    void Shoot()
    {
        GetComponent<AudioSource>().PlayOneShot(currentWeapon.shotSound);
    
        Vector3 muzzle = transform.position + (Vector3)muzzleOffset; // 총구(팔/몸통 높이)
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        worldPosition.z = 0;
        worldPosition -= muzzle;

        ObjectPool pool = bulletPoolManager.GetPool(currentWeapon.bulletPrefab);
        GameObject newBullet = pool.Get();
        if (newBullet != null)
        {
            Bullet bulletScript = newBullet.GetComponent<Bullet>();
            newBullet.transform.position = muzzle;
            bulletScript.Direction = worldPosition;
            float dmg = currentWeapon.damage;
            // 스펠 마블 주는 피해 수정(Counter 반격 배율 등) — 발사 시점 적용
            IPlayerOutgoingModifier[] outMods = GetComponents<IPlayerOutgoingModifier>();
            for (int i = 0; i < outMods.Length; i++)
                dmg = outMods[i].ModifyOutgoingDamage(dmg);
            bulletScript.damage = dmg;
            bulletScript.speed = currentWeapon.bulletSpeed;
            bulletScript.pierce = false; // 풀 재사용 대비 리셋 — 아래 수정자가 필요 시 켬
            // 스펠 마블 총알 속성 수정(Piercing 관통, Sniper 탄속 등)
            IPlayerBulletModifier[] bMods = GetComponents<IPlayerBulletModifier>();
            for (int i = 0; i < bMods.Length; i++)
                bMods[i].ModifyBullet(bulletScript);
        }
    }

    private void FixedUpdate()
    {
        if (movementLocked) return; // 스펠 마블 ♦ Fortress — 이동 불가
        if (TickDash()) return; // Plan SC: FR-05 — 대시 중엔 일반 이동 스킵
        transform.Translate(move * (speed * Time.fixedDeltaTime));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy" && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageInterval;
            TakeHit(1, collision.gameObject); // 적 접촉 데미지(공격자 전달 — Reflect 반사 대상)
        }
    }

    // Design Ref: §3.1 — 적 총알 피격 (트리거). Plan SC: FR-05
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "EnemyBullet")
        {
            if (IsDashActive) return; // 대시 무적 프레임 — 원거리 총알 데미지 무시(닷지롤)
            float d = collision.GetComponent<EnemyBullet>().damage;
            TakeHit(d); // 총알은 스스로 소멸(EnemyBullet), 여기선 데미지만
        }
    }

    // 레이저 등 히트스캔형 적 공격 공통 진입점 — 총알 피격과 동일하게 대시 무적 프레임 적용
    public void ApplyRangedHit(float damage, GameObject attacker = null)
    {
        if (IsDashActive) return;
        TakeHit(damage, attacker);
    }

    // Design Ref: §3.1 — 접촉/총알 공통 피격 처리 (Flash/Die 소유)
    // attacker: 접촉 피격 시 해당 적(반사 대상), 총알 등 불명이면 null
    void TakeHit(float damage, GameObject attacker = null)
    {
        if (IsShieldActive) return; // 스펠 마블 ♦ 실드 — 데미지 무시 (SelfBuffShieldAbility)

        // 스펠 마블 피해 수정 체인(Iron Skin 감소 / Fortress 무효 / Reflect 반사 / Mirror World 분산)
        IPlayerDamageModifier[] mods = GetComponents<IPlayerDamageModifier>();
        for (int i = 0; i < mods.Length; i++)
            damage = mods[i].ModifyIncomingDamage(damage, attacker);
        if (damage <= 0f) return; // 무효화됨 — 피격 아님

        // 피격 통지(Counter 반격 윈도우 등)
        IPlayerHitListener[] listeners = GetComponents<IPlayerHitListener>();
        for (int i = 0; i < listeners.Length; i++)
            listeners[i].OnPlayerHit(damage);

        if (GetComponent<Character>().Hit(damage))
        {
            Flash();
        }
        else
        {
            // 사망 가로채기(Resurrection) — 성공 시 사망 취소(부활 처리는 인터셉터 책임)
            IPlayerDeathInterceptor[] savers = GetComponents<IPlayerDeathInterceptor>();
            for (int i = 0; i < savers.Length; i++)
                if (savers[i].TryInterceptDeath()) { Flash(); return; }
            Die();
        }
    }
    
    void Flash()
    {
        sr.material = flashMaterial;
        Invoke("AfterFlash", 0.5f);
    }

    void AfterFlash()
    {
        sr.material = defaultMaterial;
    }

    void Die()
    {
        panim.Die();
        Invoke("AfterDying", 0.875f);
    }

    void AfterDying()
    {
        // 사망 점수 확정 → 계정별 최고기록 갱신 후 GameOverScene으로 전달
        int finalScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        GameStats.lastScore = finalScore;
        GameStats.isNewBest = HighScoreService.Submit(finalScore);
        GameStats.bestScore = HighScoreService.GetBest();
        RankingService.Submit(finalScore); // 글로벌 랭킹(서버가 Best만 유지)
        SceneLoader.Load("GameOverScene");
    }
}