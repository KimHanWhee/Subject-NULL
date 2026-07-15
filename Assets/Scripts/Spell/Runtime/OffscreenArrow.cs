using UnityEngine;

// 목표(대숙청 안전지대 등)가 카메라 시야 밖이면 화면 가장자리에 방향 화살표를 띄운다.
// 시야 안에 들어오면 자동으로 숨김. 카메라를 따라 매 프레임 갱신.
public class OffscreenArrow : MonoBehaviour
{
    private Vector2 target;
    private SpriteRenderer sr;
    private static Sprite arrowSprite;

    public static OffscreenArrow Show(Vector2 target, Color color)
    {
        var go = new GameObject("OffscreenArrow");
        var a = go.AddComponent<OffscreenArrow>();
        a.target = target;
        a.sr = go.AddComponent<SpriteRenderer>();
        a.sr.sprite = ArrowSprite();
        a.sr.color = color;
        go.transform.localScale = Vector3.one * 0.7f;
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) a.sr.sortingLayerID = layers[layers.Length - 1].id;
        a.sr.sortingOrder = 32050;
        return a;
    }

    public void SetTarget(Vector2 t) { target = t; }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) { sr.enabled = false; return; }

        Vector3 vp = cam.WorldToViewportPoint(target);
        bool onScreen = vp.z > 0f && vp.x > 0.06f && vp.x < 0.94f && vp.y > 0.06f && vp.y < 0.94f;
        sr.enabled = !onScreen;
        if (onScreen) return;

        Vector2 camCenter = cam.transform.position;
        Vector2 d = (target - camCenter);
        if (d.sqrMagnitude < 0.0001f) { sr.enabled = false; return; }
        d.Normalize();

        float halfH = cam.orthographicSize * 0.86f;      // 가장자리 살짝 안쪽
        float halfW = cam.orthographicSize * cam.aspect * 0.9f;
        float scale = Mathf.Min(
            halfW / Mathf.Max(Mathf.Abs(d.x), 0.0001f),
            halfH / Mathf.Max(Mathf.Abs(d.y), 0.0001f));
        Vector2 pos = camCenter + d * scale;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg); // 스프라이트 = +x 방향

        // 은은한 맥동(주의 환기)
        float s = 0.62f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f);
        transform.localScale = Vector3.one * s;
    }

    // 오른쪽(+x)을 가리키는 화살표 삼각형
    static Sprite ArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;
        const int w = 64, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (h - 1) * 0.5f;
        for (int y = 0; y < h; y++)
        {
            float ty = Mathf.Abs(y - c) / c;         // 0(중앙)~1(가장자리)
            float maxX = (w - 1) * (1f - ty);        // 중앙에서 apex(오른쪽)까지
            for (int x = 0; x < w; x++)
            {
                float a = x <= maxX ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        arrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        return arrowSprite;
    }
}
