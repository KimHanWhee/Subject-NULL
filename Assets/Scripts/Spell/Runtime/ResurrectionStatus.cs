using UnityEngine;

// ♥ Resurrection 상태 — 다음 사망 시 체력 50%로 부활(1회 소비). 부활 직후 짧은 무적.
public class ResurrectionStatus : MonoBehaviour, IPlayerDeathInterceptor, IBuffDisplay
{
    private float reviveRatio = 0.5f;
    private float postShield = 1.5f; // 부활 직후 무적(초)
    private int charges;             // 남은 부활 횟수(마블 중첩 시 누적)
    private SpellMarble marble;

    private Transform halo;          // 머리 위 천사 고리(부활 보유 표시)
    private SpriteRenderer haloSr;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return false; } }  // 횟수 표시
    public float BuffRemaining { get { return 0f; } }
    public int BuffCharges { get { return charges; } }

    public static void Apply(GameObject player, float reviveRatio, SpellMarble marble = null)
    {
        ResurrectionStatus s = player.GetComponent<ResurrectionStatus>();
        if (s == null) s = player.AddComponent<ResurrectionStatus>();
        s.reviveRatio = reviveRatio;
        s.marble = marble;
        s.charges++;                 // 중첩 사용 시 부활 횟수 누적
        s.EnsureHalo();
    }

    void EnsureHalo()
    {
        if (halo != null) return;
        Transform anchor = SpellVfx.VisualAnchor(gameObject);
        var go = new GameObject("ResurrectionHalo");
        go.transform.SetParent(transform, false);
        // 머리 위(몸통 중심 대비 위쪽). 스프라이트 상단 여백 고려해 앵커 기준 오프셋.
        Vector3 headLocal = transform.InverseTransformPoint(anchor.position) + new Vector3(0f, 0.62f, 0f);
        go.transform.localPosition = headLocal;
        halo = go.transform;
        baseHaloY = headLocal.y;
        haloSr = go.AddComponent<SpriteRenderer>();
        haloSr.sprite = SpellVfx.RingSprite();
        haloSr.color = new Color(1f, 0.92f, 0.45f, 0.95f); // 금빛
        go.transform.localScale = new Vector3(0.5f, 0.2f, 1f); // 납작한 고리(천사 링)
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) haloSr.sortingLayerID = layers[layers.Length - 1].id;
        haloSr.sortingOrder = 31700;
    }

    private float baseHaloY;

    void Update()
    {
        if (halo == null) return;
        // 은은한 상하 부유 + 회전 + 밝기 맥동(천사 고리 느낌)
        float bob = Mathf.Sin(Time.time * 2.2f) * 0.05f;
        halo.localPosition = new Vector3(halo.localPosition.x, baseHaloY + bob, halo.localPosition.z);
        halo.Rotate(0f, 0f, 40f * Time.deltaTime);
        if (haloSr != null)
        {
            var c = haloSr.color; c.a = 0.72f + 0.23f * Mathf.Sin(Time.time * 3f); haloSr.color = c;
        }
    }

    void OnDisable() { if (halo != null) Destroy(halo.gameObject); }
    void OnDestroy() { if (halo != null) Destroy(halo.gameObject); }

    public bool TryInterceptDeath()
    {
        if (charges <= 0) return false;
        Character ch = GetComponent<Character>();
        if (ch == null) return false;
        charges--;                   // 1회 소비
        ch.Revive(reviveRatio);
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.GrantShield(postShield); // 연속 피격 즉사 방지
        Color gold = new Color(1f, 0.95f, 0.5f, 1f);
        Transform anchor = SpellVfx.VisualAnchor(gameObject); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        SpellVfx.SpawnRing(anchor.position, 1.5f, gold, 0.8f);                // 부활 섬광
        SpellParticleVfx.SpawnBurst(anchor.position, 1.5f, gold, 36, 0.6f);   // 금빛 파편 폭발
        SpellParticleVfx.SpawnRise(anchor, gold, 1.2f);                       // 승천하는 금빛 입자
        if (charges <= 0) Destroy(this); // 남은 횟수 없으면 제거
        return true;
    }
}
