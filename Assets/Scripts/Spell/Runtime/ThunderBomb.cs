using UnityEngine;

// ♠ 비격진천뢰 투하체 — 목표 지점 위에서 떨어져 착탄 시 소규모 폭발(수류탄 연출 재사용).
// 낙하 중 목표 지점에 예고 링을 그려 회피/조준 판단이 가능하게 한다.
public class ThunderBomb : MonoBehaviour
{
    private Vector2 target;
    private float radius;
    private float damage;
    private float dropTime;
    private float elapsed;
    private float startY;
    private GameObject explosionPrefab;

    private static Sprite bombSprite;
    private static readonly Color emberColor = new Color(1f, 0.55f, 0.15f, 1f);

    public static void Spawn(Vector2 target, float radius, float damage, GameObject explosionPrefab)
    {
        GameObject go = new GameObject("ThunderBomb");
        ThunderBomb b = go.AddComponent<ThunderBomb>();
        b.target = target;
        b.radius = radius;
        b.damage = damage;
        b.dropTime = 0.35f;
        b.startY = target.y + 3.2f; // 화면 위에서 낙하 시작
        b.explosionPrefab = explosionPrefab;
        go.transform.position = new Vector3(target.x, b.startY, 0f);

        // 본체(작은 검은 구 + 불씨색 심지 느낌은 틴트로)
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BombSprite();
        sr.color = new Color(0.16f, 0.13f, 0.12f, 1f);
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = 31000;
        go.transform.localScale = Vector3.one * 0.34f;

        SpellVfx.SpawnRing(target, radius, emberColor, b.dropTime, 0.07f); // 착탄 예고
    }

    static Sprite BombSprite()
    {
        if (bombSprite != null) return bombSprite;
        const int S = 24;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2((S - 1) * 0.5f, (S - 1) * 0.5f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (S * 0.5f);
                tex.SetPixel(x, y, d <= 1f ? Color.white : Color.clear);
            }
        tex.Apply();
        bombSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        return bombSprite;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / dropTime);
        float ease = t * t; // 중력 가속 느낌
        transform.position = new Vector3(target.x, Mathf.Lerp(startY, target.y, ease), 0f);
        if (t >= 1f) Detonate();
    }

    void Detonate()
    {
        // 폭발 연출 — 수류탄 프리팹(ExplosionVfx) 재사용, 없으면 코드 VFX
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, target, Quaternion.identity);
            ExplosionVfx ex = fx.GetComponent<ExplosionVfx>();
            if (ex != null) ex.Play(radius);
            Destroy(fx, 1.2f);
        }
        else
        {
            SpellVfx.SpawnRing(target, radius, emberColor, 0.3f, 0.16f);
            SpellParticleVfx.SpawnBurst(target, radius, emberColor, 20, 0.4f);
        }

        // 범위 피해 — ApplyHit 경유(사망 연출/점수 보존)
        Collider2D[] hits = Physics2D.OverlapCircleAll(target, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            IDamageable dmg = hits[i].GetComponent<IDamageable>();
            if (dmg != null) { dmg.ApplyHit(damage); continue; }
            Character ch = hits[i].GetComponent<Character>();
            if (ch != null && !ch.Hit(damage)) hits[i].gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }
}
