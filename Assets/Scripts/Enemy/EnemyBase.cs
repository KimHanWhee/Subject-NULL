using UnityEngine;

// 모든 적 컨트롤러의 공용 뼈대 — 스폰/피격/사망/추적대상/물리 규약을 한 곳에 소유.
// 새 몬스터 = 이 클래스를 상속해 Tick(이동+공격)만 구현하면
// 상태이상(빙결/혼란/감속)·디코이·아포칼립스·타임스톱·GameManager가 자동 호환된다.
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    protected enum Phase { Spawning, Active, Dying }

    [Header("Move")]
    public float speed = 2f; // SlowStatus가 감속 대상으로 사용

    [Header("Material")]
    public Material flashMaterial;
    public Material defaultMaterial;

    protected GameObject target;
    protected Phase phase;
    protected SpriteRenderer sr;
    protected Animator anim;
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 충돌 토크로 적이 회전(삐뚤어짐)하는 것 방지
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
    }

    // 스펠 마블 ♣ Decoy — 추적 대상 변경(분신 어그로). null 금지(FixedUpdate 방어 있음).
    public void SetTarget(GameObject newTarget)
    {
        if (newTarget != null) target = newTarget;
    }

    // 공통 스폰 흐름 — 자식은 override 후 base.Spawn(target) 다음에 자기 상태만 초기화
    public virtual void Spawn(GameObject target)
    {
        this.target = target;
        phase = Phase.Spawning;
        transform.rotation = Quaternion.identity; // 풀 재사용 시 남은 회전값 초기화
        GetComponent<Character>().Initialize();
        anim.SetTrigger("Spawn");
        GetComponent<Collider2D>().enabled = false;
        Invoke(nameof(StartMoving), 1f);
    }

    protected virtual void StartMoving()
    {
        GetComponent<Collider2D>().enabled = true;
        phase = Phase.Active;
    }

    void FixedUpdate()
    {
        // 이동은 Translate 전담 — 충돌(플레이어 대시 등)로 물리 엔진이 준 밀림 속도가
        // 잔류하면 멀리 날아가므로 매 프레임 제거
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (phase != Phase.Active || target == null) return;
        Tick(target.transform.position - transform.position, Time.fixedDeltaTime);
    }

    // 자식 구현: 이동+공격 행동. toTarget = 추적 대상까지의 벡터, dt = 고정 스텝
    // (상태 타이머는 dt 누적으로 잴 것 — 빙결이 컨트롤러를 끄면 자동으로 멈춘다)
    protected abstract void Tick(Vector2 toTarget, float dt);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Bullet")
            ApplyHit(collision.gameObject.GetComponent<Bullet>().damage); // 총알/스펠 공통 경로
    }

    // 스펠 등 외부 데미지 소스 공통 진입점 — 사망 시 Die() 애니메이션 보존
    public void ApplyHit(float damage)
    {
        if (phase == Phase.Dying) return; // 이미 죽는 중이면 중복 처리 방지
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

    protected virtual void Die()
    {
        phase = Phase.Dying;
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
