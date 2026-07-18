using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// HP바 노이즈/글리치 연출(연구실 모니터 느낌) — HeartPoint가 런타임에 부착.
// 4~9초마다 0.1~0.28초 버스트: fill 지터 + 알파 플리커 + 스캔라인 오버레이 + 프레임 시안 틴트.
// 시각 연출 전용(unscaled time) — 일시정지 중에도 살아있는 계기판처럼 보이게.
public class HpBarGlitchFx : MonoBehaviour
{
    Image frame, fill;
    RectTransform fillRt;
    Image scan;
    Color frameBase, fillBase;
    static Sprite scanSprite;

    public void Init(Image frameImg, Image fillImg)
    {
        frame = frameImg;
        fill = fillImg;
        fillRt = fillImg != null ? fillImg.rectTransform : null;
        if (frame != null) frameBase = frame.color;
        if (fill != null) fillBase = fill.color;
        BuildScanlines();
        StartCoroutine(Loop());
    }

    // 가로 스캔라인 오버레이(2px 주기 줄무늬를 세로로 스트레치) — 평소엔 꺼둠.
    void BuildScanlines()
    {
        var go = new GameObject("Scanlines", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        scan = go.GetComponent<Image>();
        scan.sprite = ScanSprite();
        scan.color = new Color(0.6f, 1f, 0.95f, 0f);
        scan.raycastTarget = false;
        var rt = scan.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling();
    }

    static Sprite ScanSprite()
    {
        if (scanSprite != null) return scanSprite;
        const int H = 64;
        var tex = new Texture2D(1, H, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < H; y++)
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, (y & 2) == 0 ? 1f : 0f));
        tex.Apply();
        scanSprite = Sprite.Create(tex, new Rect(0, 0, 1, H), new Vector2(0.5f, 0.5f));
        return scanSprite;
    }

    IEnumerator Loop()
    {
        var cyan = new Color(0.5f, 0.95f, 1f);
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(4f, 9f));
            if (fill == null || fillRt == null) yield break;

            float dur = Random.Range(0.1f, 0.28f);
            float t = 0f;
            Vector2 basePos = fillRt.anchoredPosition;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                fillRt.anchoredPosition = basePos + new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-1.2f, 1.2f));
                var c = fillBase; c.a = Random.Range(0.5f, 1f);
                fill.color = c;
                if (frame != null) frame.color = Color.Lerp(frameBase, cyan, Random.Range(0f, 0.35f));
                if (scan != null)
                {
                    var sc = scan.color; sc.a = Random.Range(0.06f, 0.2f);
                    scan.color = sc;
                }
                yield return null;
            }
            // 원복
            fillRt.anchoredPosition = basePos;
            fill.color = fillBase;
            if (frame != null) frame.color = frameBase;
            if (scan != null) { var sc = scan.color; sc.a = 0f; scan.color = sc; }
        }
    }
}
