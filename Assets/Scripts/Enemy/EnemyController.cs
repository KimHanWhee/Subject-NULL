using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    enum State
    {
        Spawning,
        Moving,
        Dying
    }
    public float speed = 2;
    
    public Material flashMaterial;
    public Material defaultMaterial;

    GameObject target;
    State state;
    
    private SpriteRenderer sr;
    private Animator anim;
    private Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 충돌 토크로 적이 회전(삐뚤어짐)하는 것 방지
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // 스펠 마블 ♣ Decoy — 추적 대상 변경(분신 어그로). null 금지(FixedUpdate 방어 있음).
    public void SetTarget(GameObject newTarget)
    {
        if (newTarget != null) target = newTarget;
    }

    public void Spawn(GameObject target)
    {
        this.target = target;
        Debug.Log("Spawn called, target: " + target);
        state = State.Spawning;
        transform.rotation = Quaternion.identity; // 풀 재사용 시 남은 회전값 초기화
        GetComponent<Character>().Initialize();
        GetComponent<Animator>().SetTrigger("Spawn");
        Invoke("StartMoving", 1);
        GetComponent<Collider2D>().enabled = false;
    }

    void StartMoving()
    {
        GetComponent<Collider2D>().enabled = true;
        state = State.Moving;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        // 이동은 Translate 전담 — 충돌(플레이어 대시 등)로 물리 엔진이 준 밀림 속도가
        // 잔류하면 멀리 날아가므로 매 프레임 제거
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (state == State.Moving)
        {
            Vector2 direction = target.transform.position - transform.position;
            transform.Translate(direction.normalized * (speed * Time.fixedDeltaTime));

            if (direction.x < 0)
            {
                sr.flipX = true;
            }

            if (direction.x > 0)
            {
                sr.flipX = false;
            }
        }
    }

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
            Flash();   // 살아있음
        else
            Die();     // 사망 애니메이션
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
        state = State.Dying;
        GetComponent<Collider2D>().enabled = false; // 사망 애니메이션 중 접촉 데미지/중복 피격 방지
        anim.SetTrigger("Die");
        Invoke("AfterDying", 0.6f);
    }
    
    void AfterDying()
    {
        gameObject.SetActive(false);
    }
}
