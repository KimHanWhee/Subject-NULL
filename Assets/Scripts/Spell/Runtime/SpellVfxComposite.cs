using UnityEngine;

// Grenade(ExplosionVfx)의 "코어 플래시 + 파편" 다층 구성을 일반화한 조립형 코드 VFX.
// 프리팹에서 레이어를 인스펙터로 조합해 각 SpellAbility.effectPrefab에 연결한다.
// - 원샷 레이어(Flash/Burst/Spark/Implode): 발동 임팩트. 즉시 1회 재생(delay로 2단 연출 가능)
// - 지속 레이어(Rise/Orbit/Field): 버프 지속 표시. 오브젝트가 파괴될 때까지 방출
// 수명은 호출측(각 Ability의 Destroy(fx, duration))이 관리 — 기존 effectPrefab 규약 그대로.
// useUnscaledTime + 최상단 정렬 + HDR 색(Bloom 발광) — SpellParticleVfx와 동일 규약.
public class SpellVfxComposite : MonoBehaviour
{
    public enum LayerMode
    {
        Flash,   // 중심 대형 섬광 1발(임팩트의 핵심)
        Burst,   // 방사형 파편
        Spark,   // 방사형 스파크(속도 방향으로 길게 늘어남)
        Implode, // 가장자리→중심 흡수(1회)
        Rise,    // 위로 떠오르는 반짝임(지속, World=이동 궤적)
        Orbit,   // 둘레를 도는 입자(지속, 대상 추종)
        Field    // 반경 전체 잔불(지속)
    }

    [System.Serializable]
    public class Layer
    {
        public LayerMode mode = LayerMode.Burst;
        public Color color = Color.white;
        public float radius = 1f;
        [Tooltip("원샷=버스트 개수, 지속=초당 방출 수")] public float count = 20f;
        public float size = 0.15f;
        public float lifetime = 0.4f;
        [Tooltip("시작 지연(초) — 2단 임팩트 연출")] public float delay = 0f;
    }

    const float hdrBoost = 2f; // URP Bloom 발광 유도(SpellParticleVfx와 동일)

    public Layer[] layers;

    private bool built;

    void OnEnable()
    {
        if (built) return; // 풀 재사용 시 중복 생성 방지(자식 시스템 유지)
        built = true;
        if (layers == null) return;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] != null) BuildLayer(layers[i], i);
    }

    void BuildLayer(Layer l, int index)
    {
        GameObject go = new GameObject("L" + index + "_" + l.mode);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        bool continuous = l.mode == LayerMode.Rise || l.mode == LayerMode.Orbit || l.mode == LayerMode.Field;

        var main = ps.main;
        main.useUnscaledTime = true;
        main.playOnAwake = false;
        main.loop = continuous;
        main.gravityModifier = 0f;
        main.startColor = l.color * hdrBoost;
        main.startDelay = l.delay;
        main.startLifetime = l.lifetime;
        main.startSize = l.size;
        main.startSpeed = 0f;
        // Flash/Orbit은 대상(부모) 추종이 자연스러움 → Local, 나머지는 World(궤적/잔상)
        main.simulationSpace = (l.mode == LayerMode.Flash || l.mode == LayerMode.Orbit)
            ? ParticleSystemSimulationSpace.Local
            : ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.radiusThickness = 1f;

        switch (l.mode)
        {
            case LayerMode.Flash:
                main.startSize = l.radius * 2f; // 반경을 채우는 대형 글로우 1발
                SetBurst(ps, 1);
                break;

            case LayerMode.Burst:
                main.startSpeed = l.radius / Mathf.Max(0.05f, l.lifetime);
                SetBurst(ps, Mathf.RoundToInt(l.count));
                break;

            case LayerMode.Spark:
                main.startSpeed = l.radius / Mathf.Max(0.05f, l.lifetime);
                SetBurst(ps, Mathf.RoundToInt(l.count));
                break;

            case LayerMode.Implode:
                shape.radius = l.radius;
                shape.radiusThickness = 0f; // 가장자리 방출
                main.startSpeed = -(l.radius * 0.95f) / Mathf.Max(0.05f, l.lifetime);
                SetBurst(ps, Mathf.RoundToInt(l.count));
                break;

            case LayerMode.Rise:
                shape.radius = Mathf.Max(0.1f, l.radius);
                emission.rateOverTime = l.count;
                var volR = ps.velocityOverLifetime;
                volR.enabled = true;
                volR.space = ParticleSystemSimulationSpace.World;
                volR.y = new ParticleSystem.MinMaxCurve(1.3f);
                break;

            case LayerMode.Orbit:
                shape.radius = Mathf.Max(0.15f, l.radius);
                shape.radiusThickness = 0f;
                emission.rateOverTime = l.count;
                var volO = ps.velocityOverLifetime;
                volO.enabled = true;
                volO.space = ParticleSystemSimulationSpace.Local;
                volO.orbitalZ = new ParticleSystem.MinMaxCurve(3f);
                break;

            case LayerMode.Field:
                shape.radius = Mathf.Max(0.2f, l.radius);
                emission.rateOverTime = l.count;
                var volF = ps.velocityOverLifetime;
                volF.enabled = true;
                volF.space = ParticleSystemSimulationSpace.World;
                volF.y = new ParticleSystem.MinMaxCurve(0.45f);
                break;
        }

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white * hdrBoost, 0f), new GradientColorKey(l.color * hdrBoost, 0.35f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = l.mode == LayerMode.Spark
            ? ParticleSystemRenderMode.Stretch // 속도 방향으로 늘어난 스파크
            : ParticleSystemRenderMode.Billboard;
        if (l.mode == LayerMode.Spark) { r.lengthScale = 4f; r.velocityScale = 0.05f; }
        r.material = SpellParticleVfx.SharedMaterial();
        SortingLayer[] sortingLayers = SortingLayer.layers;
        if (sortingLayers != null && sortingLayers.Length > 0)
            r.sortingLayerID = sortingLayers[sortingLayers.Length - 1].id;
        r.sortingOrder = 32000;

        ps.Play();
    }

    static void SetBurst(ParticleSystem ps, int count)
    {
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, count)) });
    }
}
