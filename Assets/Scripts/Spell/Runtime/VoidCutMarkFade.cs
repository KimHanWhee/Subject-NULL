using UnityEngine;

// ♠ Void Slash — 벤 자국 하나의 수명 관리.
// 흰 섬광으로 터졌다가 스펠 색으로 식으며 사라진다(그냥 알파만 줄이면 밋밋하다).
public class VoidCutMarkFade : MonoBehaviour
{
    private LineRenderer lr;
    private Color tint;
    private float dur, t;

    public void Init(LineRenderer renderer, Color color, float duration)
    {
        lr = renderer;
        tint = color;
        dur = Mathf.Max(0.01f, duration);
    }

    void Update()
    {
        // unscaled — 시간 감속(Ctrl 선택 등) 중에도 타격감이 늘어지지 않는다
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(t / dur);

        if (lr != null)
        {
            Color c = Color.Lerp(Color.white, tint, k);   // 흰 섬광 → 스펠 색
            c.a = 1f - k * k;                             // 끝에서 빠르게 사라짐
            lr.startColor = c;
            lr.endColor = c;
            lr.widthMultiplier = Mathf.Lerp(0.26f, 0.05f, k); // 얇아지며 스러짐
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
