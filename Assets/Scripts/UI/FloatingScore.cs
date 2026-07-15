using UnityEngine;

// 적 사망 위치에 뜨는 "+점수" 팝업 — 위로 떠오르며 페이드아웃(월드 스페이스 TextMesh).
public class FloatingScore : MonoBehaviour
{
    private float life = 0.8f;
    private float age;
    private float rise = 1.4f;      // 초당 상승 유닛
    private TextMesh tm;
    private MeshRenderer mr;
    private Color baseColor;

    public static void Spawn(Vector3 worldPos, int amount, int combo)
    {
        var go = new GameObject("FloatingScore");
        go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.4f, 0f);
        var f = go.AddComponent<FloatingScore>();
        f.Init(amount, combo);
    }

    void Init(int amount, int combo)
    {
        tm = gameObject.AddComponent<TextMesh>();
        tm.text = "+" + amount;
        tm.characterSize = 0.035f;
        tm.fontSize = 90;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 내장 폰트(숫자용)
        if (tm.font == null) tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font != null ? tm.font.material : mr.sharedMaterial;
        // 콤보가 높을수록 금색/크게
        baseColor = combo >= 3 ? new Color(1f, 0.82f, 0.2f) : Color.white;
        tm.color = baseColor;
        transform.localScale = Vector3.one * (combo >= 3 ? 1.25f : 1f);
        // 최상단 정렬(월드 스프라이트/적 위)
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) mr.sortingLayerID = layers[layers.Length - 1].id;
        mr.sortingOrder = 32000;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (rise * Time.deltaTime);
        float t = age / life;
        if (tm != null)
        {
            var c = baseColor;
            c.a = Mathf.Clamp01(1f - t);
            tm.color = c;
        }
        if (age >= life) Destroy(gameObject);
    }
}
