using UnityEngine;

// 대시 잔상 1장 — 시전 순간의 플레이어 스프라이트를 복사해 제자리에 두고 페이드아웃 후 파괴.
// 대시 경로를 따라 여러 장 생성하면 트레일이 된다(생성 주기는 PlayerController.dashGhostInterval).
public class DashGhost : MonoBehaviour
{
    private SpriteRenderer sr;
    private float startTime;
    private float lifetime;
    private Color startColor;

    public static void Spawn(SpriteRenderer source, Color tint, float lifetime = 0.25f)
    {
        if (source == null || source.sprite == null) return;

        GameObject go = new GameObject("DashGhost");
        go.transform.position = source.transform.position;
        go.transform.rotation = source.transform.rotation;
        go.transform.localScale = source.transform.lossyScale;

        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = source.sprite;
        r.flipX = source.flipX;
        r.flipY = source.flipY;
        r.sortingLayerID = source.sortingLayerID;
        r.sortingOrder = source.sortingOrder - 1; // 플레이어 바로 뒤에 깔림
        r.color = tint;

        DashGhost ghost = go.AddComponent<DashGhost>();
        ghost.sr = r;
        ghost.startColor = tint;
        ghost.lifetime = Mathf.Max(0.05f, lifetime);
        ghost.startTime = Time.time;
    }

    void Update()
    {
        float t = (Time.time - startTime) / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }
        Color c = startColor;
        c.a = startColor.a * (1f - t); // 선형 페이드아웃
        sr.color = c;
    }
}
