using UnityEngine;

// Design Ref: spell-marble §8.5 — Grenade(TargetedBurst, ♠ 공격) 폭발 VFX.
// 코드로 ParticleSystem 2종(코어 플래시 + 파편 스파크)을 구성하고 blast radius에 맞춰 재생한다.
// - 선택 슬로우와 무관하게 실시간 재생(useUnscaledTime) → 설계 원칙 준수.
// - 종료 후 자동 정리: 풀 오브젝트는 SetActive(false)로 반환(ObjectPool 규약), 비풀은 호출측이 Destroy.
// - 기존 SpellVfx의 "코드 기반 절차적 VFX + 최상단 Sorting" 선례를 따라 에셋 의존을 최소화.
[DisallowMultipleComponent]
public class ExplosionVfx : MonoBehaviour
{
    [Header("Look")]
    public Color coreColor = new Color(1f, 0.75f, 0.25f, 1f);   // 밝은 주황-노랑(화구)
    public Color debrisColor = new Color(1f, 0.45f, 0.12f, 1f); // 주황(파편)
    public int coreCount = 10;
    public int debrisCount = 28;
    public float coreLifetime = 0.28f;
    public float debrisLifetime = 0.45f;

    [Header("Cleanup")]
    [Tooltip("재생 후 정리까지 여유(초, unscaled)")]
    public float tail = 0.1f;

    private ParticleSystem core;
    private ParticleSystem debris;
    private float returnTime;
    private bool playing;

    void Awake()
    {
        EnsureBuilt();
    }

    // 자식 파티클(코어/파편)을 1회 생성. Awake 미실행 순서(풀 초기화/에디터)에서도 안전.
    void EnsureBuilt()
    {
        if (core == null) core = BuildSystem("Core", coreColor);
        if (debris == null) debris = BuildSystem("Debris", debrisColor);
    }

    // blast radius에 맞춰 파티클 파라미터를 스케일한 뒤 재생. 종료 시 자동 정리.
    public void Play(float radius)
    {
        EnsureBuilt();
        radius = Mathf.Max(0.1f, radius);
        Configure(radius);

        core.Clear(true); core.Play(true);
        debris.Clear(true); debris.Play(true);

        returnTime = Time.unscaledTime + Mathf.Max(coreLifetime, debrisLifetime) + Mathf.Max(0f, tail);
        playing = true;
    }

    void Update()
    {
        if (playing && Time.unscaledTime >= returnTime)
        {
            playing = false;
            gameObject.SetActive(false); // 풀 반환(ObjectPool은 SetActive(false)만으로 반환). 비풀은 호출측 Destroy.
        }
    }

    void OnDisable() { playing = false; }

    // radius에 맞춰 크기/속도/개수 반영. 파편은 lifetime 동안 radius 가장자리에 도달.
    void Configure(float radius)
    {
        var cm = core.main;
        cm.startSize = radius * 1.4f;
        cm.startLifetime = coreLifetime;
        cm.startSpeed = 0f;
        var ce = core.emission;
        ce.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, coreCount)) });

        var dm = debris.main;
        dm.startSize = Mathf.Max(0.04f, radius * 0.12f);
        dm.startLifetime = debrisLifetime;
        dm.startSpeed = radius / Mathf.Max(0.05f, debrisLifetime); // lifetime 동안 radius 이동
        var de = debris.emission;
        de.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, debrisCount)) });
    }

    ParticleSystem BuildSystem(string childName, Color color)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.useUnscaledTime = true;        // 슬로우 무관 실시간 재생
        main.playOnAwake = false;
        main.loop = false;
        main.gravityModifier = 0f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 폭발은 발생 위치 고정

        var emission = ps.emission;
        emission.rateOverTime = 0f;          // 버스트만 사용(Configure에서 설정)

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;                // 점에 가까운 지점에서 방출

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.35f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = ParticleMaterial();
        // 프로젝트 최상단 Sorting Layer + 큰 order → 어떤 월드 스프라이트에도 안 가려짐(SpellVfx 선례)
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0)
            r.sortingLayerID = layers[layers.Length - 1].id;
        r.sortingOrder = 32000;

        return ps;
    }

    // 부드러운 원형 입자용 Additive 머티리얼(1회 생성 공유). 텍스처는 코드로 생성한 소프트 도트.
    static Material sharedMat;
    static Material ParticleMaterial()
    {
        if (sharedMat != null) return sharedMat;

        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        sharedMat = new Material(sh);
        Texture2D dot = SoftDotTexture();
        if (sharedMat.HasProperty("_MainTex")) sharedMat.SetTexture("_MainTex", dot);
        if (sharedMat.HasProperty("_BaseMap")) sharedMat.SetTexture("_BaseMap", dot);
        return sharedMat;
    }

    static Texture2D softDot;
    static Texture2D SoftDotTexture()
    {
        if (softDot != null) return softDot;

        const int size = 64;
        softDot = new Texture2D(size, size, TextureFormat.RGBA32, false);
        softDot.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / c; // 0(중심)~1(가장자리)
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                                    // 중심에 밀도 집중
                softDot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        softDot.Apply();
        return softDot;
    }
}
