using UnityEngine;

// Design Ref: §2.2 — 원거리 적. 사거리 유지(히스테리시스) + 주기 사격.
public class RangedEnemyController : MonoBehaviour, IDamageable
{
    enum State { Spawning, Moving, Dying }

    [Header("Move")]
    public float speed = 2f;

    [Header("Range Band")] // Plan SC: FR-02 — 히스테리시스로 경계 떨림 방지
    public float farBand = 7f;   // 이보다 멀면 접근
    public float nearBand = 4f;  // 이보다 가까우면 후퇴
    // nearBand ~ farBand 사이 = 정지(사격 밴드)

    [Header("Fire")] // Plan SC: FR-01/FR-03/FR-07
    public GameObject bulletPrefab;          // 적 총알 prefab ("EnemyBullet" 태그)
    public BulletPoolManager bulletPoolManager;
    public float fireInterval = 1.5f;        // 발사 간격(초)
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
    private Rigidbody2D rb;
    private float nextFireTime;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 충돌 토크로 적이 회전(삐뚤어짐)하는 것 방지
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        // 프리팹은 씬 오브젝트(BulletPoolManager)를 참조로 담을 수 없어 런타임에 탐색
        if (bulletPoolManager == null)
            bulletPoolManager = FindObjectOfType<BulletPoolManager>();
    }

    // 스펠 마블 ♣ Decoy — 추적 대상 변경(분신 어그로). null 금지.
    public void SetTarget(GameObject newTarget)
    {
        if (newTarget != null) target = newTarget;
    }

    // 기존 EnemyController.Spawn과 동일한 스폰 흐름
    public void Spawn(GameObject target)
    {
        this.target = target;
        state = State.Spawning;
        transform.rotation = Quaternion.identity; // 풀 재사용 시 남은 회전값 초기화
        GetComponent<Character>().Initialize();
        anim.SetTrigger("Spawn");
        GetComponent<Collider2D>().enabled = false;
        Invoke(nameof(StartMoving), 1f);
        nextFireTime = Time.time + fireInterval; // Design Ref: §6 E5 — 스폰 직후 즉발 방지
    }

    void StartMoving()
    {
        GetComponent<Collider2D>().enabled = true;
        state = State.Moving;
    }

    void FixedUpdate()
    {
        // 이동은 Translate 전담 — 충돌(플레이어 대시 등)로 물리 엔진이 준 밀림 속도가
        // 잔류하면 멀리 날아가므로 매 프레임 제거
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (state != State.Moving || target == null) return; // Design Ref: §6 E3 — target 방어

        Vector2 toTarget = target.transform.position - transform.position;
        float dist = toTarget.magnitude;
        Vector2 dir = toTarget.normalized;

        // Plan SC: FR-02 — 사거리 유지 (밴드 밖이면 접근/후퇴, 안이면 정지)
        if (dist > farBand)
            transform.Translate(dir * (speed * Time.fixedDeltaTime));
        else if (dist < nearBand)
            transform.Translate(-dir * (speed * Time.fixedDeltaTime));
        // else: 정지

        sr.flipX = dir.x < 0;

        // Plan SC: FR-01 — 사격 밴드 진입 + 쿨다운
        if (dist <= farBand && Time.time >= nextFireTime)
        {
            Fire(dir);
            nextFireTime = Time.time + fireInterval;
        }
    }

    // Plan SC: FR-03 — 발사 순간 플레이어 방향으로 직진(유도 없음)
    void Fire(Vector2 dir)
    {
        if (bulletPrefab == null || bulletPoolManager == null) return;

        ObjectPool pool = bulletPoolManager.GetPool(bulletPrefab);
        GameObject b = pool.Get();
        if (b == null) return; // Design Ref: §6 E1 — 풀 고갈 시 이번 발사 스킵

        b.transform.position = transform.position;
        EnemyBullet eb = b.GetComponent<EnemyBullet>();
        eb.Direction = dir;
        eb.speed = bulletSpeed;
        eb.damage = bulletDamage;

        if (fireSound != null)
        {
            AudioSource a = GetComponent<AudioSource>();
            if (a != null) a.PlayOneShot(fireSound);
        }
    }

    // 기존 EnemyController와 동일한 피격/사망 (플레이어 총알 "Bullet"에만 반응 → 오사 없음)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Bullet")
        {
            float d = collision.gameObject.GetComponent<Bullet>().damage;
            ApplyHit(d); // 총알/스펠 공통 경로
        }
    }

    // 스펠 등 외부 데미지 소스 공통 진입점 — 사망 시 Die() 애니메이션 보존
    public void ApplyHit(float damage)
    {
        if (state == State.Dying) return; // 이미 죽는 중이면 중복 처리 방지
        if (GetComponent<Character>().Hit(damage))
            Flash();
        else
            Die();
    }

    void Flash()
    {
        sr.material = flashMaterial;
        Invoke(nameof(AfterFlash), 0.5f);
    }

    void AfterFlash()
    {
        sr.material = defaultMaterial;
    }

    void Die()
    {
        state = State.Dying;
        GetComponent<Collider2D>().enabled = false; // 사망 애니메이션 중 접촉 데미지/중복 피격 방지
        anim.speed = 1f; // 빙결(FreezeStatus)로 애니메이터가 정지 중이어도 사망 연출은 재생
        anim.SetTrigger("Die");
        Invoke(nameof(AfterDying), 0.6f);
    }

    void AfterDying()
    {
        gameObject.SetActive(false);
    }
}
