using UnityEngine;

// 모든 적 컨트롤러의 공용 뼈대 — 스폰/피격/사망/추적대상/물리 규약을 한 곳에 소유.
// 새 몬스터 = 이 클래스를 상속해 Tick(이동+공격)만 구현하면
// 상태이상(빙결/혼란/감속)·디코이·아포칼립스·타임스톱·GameManager가 자동 호환된다.
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    protected enum Phase { Spawning, Active, Dying }

    [Header("Move")]
    public float speed = 2f; // SlowStatus가 감속 대상으로 사용

    [Header("Score")]
    public int scoreValue = 10; // 처치 시 기본 점수(강한 적일수록 크게 — 프리팹별 지정)

    // ♦ SlowAura 등 국소 시간감속 — Tick의 dt에 곱해져 이동/돌진/타이머가 함께 느려짐(1=정상).
    [System.NonSerialized] public float localTimeScale = 1f;

    [Header("Material")]
    public Material flashMaterial;
    public Material defaultMaterial;

    protected GameObject target;
    protected Phase phase;
    protected SpriteRenderer sr;
    protected Animator anim;
    protected Rigidbody2D rb;

    // ---- 고난(HardshipSystem) ----
    // 프리팹 원본 maxHp를 Awake에서 1회 캐시(풀 재사용 시 배율 중첩 방지).
    private float baseMaxHp;
    private float regenCarry;   // 재생 조직 — 1 미만 회복량 누적

    // 적의 "체감 속도" 배율 = 외부 감속(localTimeScale) × 고난 가속.
    // ⚠️ speed 필드를 직접 건드리지 않는 이유: 그 필드는 SlowStatus가 원본을 캐시해 쓰므로
    //    매 프레임 덮어쓰면 감속 스펠이 무효화된다. 시간 배율로 처리하면 둘이 곱연산으로 공존한다.
    //    이동(Translate dt)·공격 타이머·돌진 속도가 한 번에 스케일되는 이점도 있다.
    public float TimeMult
    {
        get
        {
            float m = localTimeScale * HardshipSystem.EnemySpeedMult; // 신경 가속
            if (HardshipSystem.FrenzyOn && character != null
                && character.HpRatio > 0f && character.HpRatio <= HardshipSystem.FrenzyHpThreshold)
                m *= HardshipSystem.FrenzySpeedMult;                  // 광폭화(빈사 시)
            return m;
        }
    }

    // ---- HP 바(코드 생성 월드 스프라이트) ----
    private Character character;
    private Transform hpBarRoot;
    private SpriteRenderer hpBarFill;
    private const float BarW = 0.9f, BarH = 0.13f, BarY = 0.7f;
    private static Sprite barSprite;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 충돌 토크로 적이 회전(삐뚤어짐)하는 것 방지
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        character = GetComponent<Character>();
        if (character != null) baseMaxHp = character.maxHp;
        BuildHpBar();
    }

    static Sprite BarSprite()
    {
        if (barSprite != null) return barSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        barSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return barSprite;
    }

    void BuildHpBar()
    {
        var root = new GameObject("HpBar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, BarY, 0f);
        hpBarRoot = root.transform;

        var bg = new GameObject("bg").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.sprite = BarSprite();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.transform.localScale = new Vector3(BarW + 0.06f, BarH + 0.05f, 1f);
        SortBar(bg, 20);

        hpBarFill = new GameObject("fill").AddComponent<SpriteRenderer>();
        hpBarFill.transform.SetParent(root.transform, false);
        hpBarFill.sprite = BarSprite();
        SortBar(hpBarFill, 21);

        hpBarRoot.gameObject.SetActive(false);
    }

    void SortBar(SpriteRenderer s, int extra)
    {
        if (sr != null) { s.sortingLayerID = sr.sortingLayerID; s.sortingOrder = sr.sortingOrder + extra; }
        else s.sortingOrder = 5000 + extra;
    }

    // 매 프레임 HP 바 갱신(피해 입은 적만 표시)
    void LateUpdate()
    {
        if (hpBarRoot == null) return;
        float r = character != null ? character.HpRatio : 1f;
        bool show = phase != Phase.Dying && r > 0f && r < 0.999f;
        if (hpBarRoot.gameObject.activeSelf != show) hpBarRoot.gameObject.SetActive(show);
        if (!show) return;
        hpBarFill.transform.localScale = new Vector3(BarW * r, BarH, 1f);
        hpBarFill.transform.localPosition = new Vector3(-BarW * 0.5f + BarW * r * 0.5f, 0f, -0.01f);
        hpBarFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.4f, 0.9f, 0.35f), r);
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
        localTimeScale = 1f;                        // 풀 재사용 시 감속 잔존 방지
        transform.rotation = Quaternion.identity; // 풀 재사용 시 남은 회전값 초기화

        // 고난 적용 — 항상 "프리팹 원본값 × 현재 배율"로 계산(누적 중첩 방지).
        // Initialize() 앞에서 maxHp를 바꿔야 현재 HP도 강화된 값으로 시작한다.
        // (속도는 TimeMult가 매 프레임 반영하므로 여기서 만지지 않는다)
        regenCarry = 0f;
        if (character != null) character.maxHp = baseMaxHp * HardshipSystem.EnemyHpMult;

        GetComponent<Character>().Initialize();
        anim.SetTrigger("Spawn");
        GetComponent<Collider2D>().enabled = false;
        spawnTimer = SpawnDuration;
    }

    // 등장 연출 길이(초). 이 시간이 지나면 콜라이더가 켜지고 실제로 움직인다.
    const float SpawnDuration = 1f;
    private float spawnTimer;

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

        // 등장 카운트다운.
        // ⚠️ 예전에는 Invoke(StartMoving, 1f)를 썼는데, Invoke는 컴포넌트를 비활성화해도
        //    계속 진행된다. ♣ Time Stop은 EnemyBase를 비활성화해 적을 멈추는 방식이라,
        //    정지된 세상에서도 1초 뒤 콜라이더가 켜져 버렸다. 그 결과 등장 연출 중(반투명)인
        //    적이 총알에 맞고, 플레이어도 그 적에 닿아 갑자기 피해를 입었다.
        //    FixedUpdate에서 세면 컴포넌트가 꺼진 동안 카운트다운도 함께 멈춘다.
        //    TimeMult를 곱해 빙결·감속 중에는 등장도 느려진다(정지 = 0).
        if (phase == Phase.Spawning)
        {
            spawnTimer -= Time.fixedDeltaTime * TimeMult;
            if (spawnTimer <= 0f) StartMoving();
            return;
        }

        if (phase != Phase.Active || target == null) return;
        float dt = Time.fixedDeltaTime * TimeMult;
        TickHardship(dt);
        Tick(target.transform.position - transform.position, dt);
    }

    // 고난 중 매 프레임 갱신이 필요한 것(재생 조직). dt에 TimeMult가 반영돼 있어
    // 빙결/감속 중에는 재생도 함께 느려진다(광폭화는 TimeMult가 처리).
    void TickHardship(float dt)
    {
        if (character == null) return;

        // 재생 조직 — 초당 최대 체력의 일정 비율. 소수 회복이 묻히지 않게 누적 후 반영.
        float ratePerSec = HardshipSystem.RegenRatioPerSec;
        if (ratePerSec > 0f && character.HpRatio > 0f && character.HpRatio < 1f)
        {
            regenCarry += character.maxHp * ratePerSec * dt;
            if (regenCarry >= 0.05f) { character.Heal(regenCarry); regenCarry = 0f; }
        }
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

        damage *= HardshipSystem.DamageTakenMult; // 경화 외피(체감형 — 0이 되지 않아 불사 불가)

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
        if (HardshipSystem.VolatileOn) VolatileBurst.Spawn(transform.position); // 자폭 조직 — 시체가 터진다
        if (GameManager.Instance != null) GameManager.Instance.ReportKill(scoreValue, transform.position); // 처치 점수/콤보
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
