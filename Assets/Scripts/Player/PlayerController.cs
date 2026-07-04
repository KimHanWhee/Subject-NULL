using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    
    public float speed = 8;
    public WeaponData currentWeapon;
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

    Vector3 move;
    private SpriteRenderer sr;
    private Animator anim;
    private Rigidbody2D rb;
    private float nextFireTime; // 다음 발사 허용 시각 (Time.time 기준)
    private float nextDamageTime; // 다음 피격 허용 시각 (Time.time 기준)

    // Design Ref: §3.2 — 대시 내부 상태
    private bool isDashing;
    private float dashEndTime;   // 대시 종료 시각
    private float nextDashTime;  // 다음 대시 허용 시각 (쿨다운)
    private Vector3 dashDir;
    private Vector3 lastMoveDir = Vector3.right; // 입력 없을 때 폴백 방향

    // 대시 v2: 스태미너 상태
    private float currentStamina;   // 현재 스태미너
    private float staminaRegenTime; // 이 시각 이후부터 회복 시작 (대시 후 지연)

    // Design Ref: §2.1 — 니어미스 유예: 이 시각까지 대시 판정 유지
    private float dashGraceUntil;

    // UI 연동용 (스태미너 바에서 0~1 비율로 사용) — 대시 v2 UI 사이클에서 연결
    public float StaminaRatio => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    // Plan SC: FR-01 — 대시 중 또는 대시 직후 유예 내면 true (NearMissDetector가 읽음)
    public bool IsDashActive => isDashing || Time.time <= dashGraceUntil;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
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

        if (move.magnitude > 0) lastMoveDir = move; // Plan SC: FR-02 — 폴백용 마지막 이동 방향

        if (move.x < 0)
        {
            sr.flipX = true;
        }

        if (move.x > 0)
        {
            sr.flipX = false;
        }

        if (move.magnitude > 0)
        {
            anim.SetTrigger("Move");
        }
        else
        {
            anim.SetTrigger("Stop");
        }

        if (Mouse.current.leftButton.isPressed
            && currentWeapon != null
            && Time.time >= nextFireTime)
        {
            Shoot();
            float rate = Mathf.Max(currentWeapon.fireRate, 0.0001f); // fireRate <= 0 방어 (0 나눗셈 방지)
            nextFireTime = Time.time + 1f / rate;
        }

        RegenStamina(); // 대시 v2: 스태미너 회복
        TryStartDash(); // Plan SC: FR-01
    }

    // Design Ref: §4.1 — Space 입력 감지 + 쿨다운/스태미너 게이트로 대시 시작
    void TryStartDash()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame
            && !isDashing                        // Plan SC: FR-05 — 대시 중 재입력 무시
            && Time.time >= nextDashTime          // Plan SC: FR-04 — 쿨다운
            && currentStamina >= dashStaminaCost) // 대시 v2: 스태미너 충분해야 대시
        {
            dashDir = (move.magnitude > 0 ? move : lastMoveDir).normalized; // Plan SC: FR-02
            isDashing = true;
            dashEndTime = Time.time + dashDuration;
            nextDashTime = Time.time + dashCooldown; // 쿨다운은 시작 시각 기준

            currentStamina -= dashStaminaCost;                   // 대시 v2: 스태미너 소모
            staminaRegenTime = Time.time + staminaRegenDelay;    // 대시 v2: 회복 지연 시작

            if (dashSound != null)
                GetComponent<AudioSource>().PlayOneShot(dashSound); // 대시 시작음
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

        // Plan SC: FR-03 — MovePosition으로 물리 충돌 존중 (transform.Translate는 벽 관통)
        Vector2 delta = (Vector2)dashDir * (dashSpeed * Time.fixedDeltaTime);
        if (rb != null)
            rb.MovePosition(rb.position + delta);
        else
            transform.Translate(delta);
        return true;
    }

    void Shoot()
    {
        GetComponent<AudioSource>().PlayOneShot(currentWeapon.shotSound);
    
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        worldPosition.z = 0;
        worldPosition -= (transform.position + new Vector3(0, -0.5f, 0));

        ObjectPool pool = bulletPoolManager.GetPool(currentWeapon.bulletPrefab);
        GameObject newBullet = pool.Get();
        if (newBullet != null)
        {
            Bullet bulletScript = newBullet.GetComponent<Bullet>();
            newBullet.transform.position = transform.position + new Vector3(0, -0.5f);
            bulletScript.Direction = worldPosition;
            bulletScript.damage = currentWeapon.damage;
            bulletScript.speed = currentWeapon.bulletSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (TickDash()) return; // Plan SC: FR-05 — 대시 중엔 일반 이동 스킵
        transform.Translate(move * (speed * Time.fixedDeltaTime));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy" && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + damageInterval;
            TakeHit(1); // 적 접촉 데미지
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

    // Design Ref: §3.1 — 접촉/총알 공통 피격 처리 (Flash/Die 소유)
    void TakeHit(float damage)
    {
        if (GetComponent<Character>().Hit(damage))
            Flash();
        else
            Die();
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
        anim.SetTrigger("Die");
        Invoke("AfterDying", 0.875f);
    }

    void AfterDying()
    {
        SceneManager.LoadScene("GameOverScene");
    }
}