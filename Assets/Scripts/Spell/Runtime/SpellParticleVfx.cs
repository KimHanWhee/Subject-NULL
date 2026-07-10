using UnityEngine;

// Design Ref: spell-marble §8.5 — 전 능력 공용 코드 파티클 VFX(ExplosionVfx의 절차적 구성 선례를 일반화).
// 프리팹 없이 런타임에 ParticleSystem을 구성해 5종 프리미티브를 제공:
//  Burst(방사 폭발) / Implode(중심 흡수) / Rise(상승 반짝임) / Orbit(대상 궤도) / Field(장판 잔불)
// - useUnscaledTime: 선택 슬로우와 무관하게 실시간 재생(SpellVfx/ExplosionVfx 규약)
// - 최상단 Sorting Layer + order 32000 → 월드 스프라이트에 가려지지 않음
// - 수명 관리: 방출 duration 경과 시 방출 중단 → 입자 잔존 후 자체 Destroy(풀 불필요, 소량 오브젝트)
public class SpellParticleVfx : MonoBehaviour
{
    // HDR 색 부스트: URP Bloom(threshold 0.8)이 입자를 발광시키도록 1.0 초과 색으로 방출
    const float hdrBoost = 2f;

    private ParticleSystem ps;
    private Transform follow;   // null이면 위치 고정
    private float duration;     // 방출 지속(초, unscaled). 0이면 버스트 1회
    private float lifeAfter;    // 방출 종료 후 입자 잔존 여유(초)
    private float startTime;
    private bool stopped;

    // 방사형 스파크 폭발 — 타격/발동 임팩트. 입자가 lifetime 동안 radius 가장자리에 도달.
    public static SpellParticleVfx SpawnBurst(Vector2 pos, float radius, Color color, int count = 24, float lifetime = 0.45f)
    {
        SpellParticleVfx fx = Create("SpellBurst", pos, null, color);
        var main = fx.ps.main;
        main.startLifetime = lifetime;
        main.startSpeed = radius / Mathf.Max(0.05f, lifetime);
        main.startSize = Mathf.Max(0.07f, radius * 0.15f);
        SetCircleShape(fx.ps, 0.05f, 1f);
        SetBurst(fx.ps, count);
        fx.duration = 0f;
        fx.stopped = true;
        fx.lifeAfter = lifetime + 0.1f;
        fx.ps.Play();
        return fx;
    }

    // 가장자리에서 중심으로 빨려드는 입자 — 블랙홀/텔레포트 도착/시간 정지.
    // duration = 0: 1회 버스트, > 0: 지속 방출(블랙홀처럼 오래 빨아들이는 연출).
    // follow != null: 대상 추종(레전드 차징). Local 시뮬레이션이라 이미 방출된 입자도 함께 이동해
    //                 항상 "현재 플레이어 위치"로 수렴한다(World면 시전 시점 위치에 고정됨).
    public static SpellParticleVfx SpawnImplode(Vector2 pos, float radius, Color color, float duration = 0f, int count = 26, float lifetime = 0.5f, Transform follow = null)
    {
        SpellParticleVfx fx = Create("SpellImplode", pos, follow, color);
        var main = fx.ps.main;
        if (follow != null) main.simulationSpace = ParticleSystemSimulationSpace.Local; // SpawnOrbit과 동일 추종 규약
        main.startLifetime = lifetime;
        main.startSpeed = -(radius * 0.95f) / Mathf.Max(0.05f, lifetime); // 음수 = 셰이프 안쪽으로
        main.startSize = Mathf.Max(0.07f, radius * 0.12f);
        SetCircleShape(fx.ps, radius, 0f); // 가장자리에서만 방출
        if (duration > 0f)
        {
            var em = fx.ps.emission;
            em.rateOverTime = Mathf.Clamp(count / Mathf.Max(0.2f, lifetime), 8f, 60f);
        }
        else
        {
            SetBurst(fx.ps, count);
            fx.stopped = true;
        }
        fx.duration = duration;
        fx.lifeAfter = lifetime + 0.1f;
        fx.ps.Play();
        return fx;
    }

    // 대상 주변에서 위로 떠오르는 반짝임 — 회복/축복 계열. World 시뮬레이션이라 이동 시 궤적이 남음.
    public static SpellParticleVfx SpawnRise(Transform follow, Color color, float duration, float radius = 0.5f)
    {
        SpellParticleVfx fx = Create("SpellRise", follow != null ? (Vector2)follow.position : Vector2.zero, follow, color);
        var main = fx.ps.main;
        main.startLifetime = 0.7f;
        main.startSpeed = 0f;
        main.startSize = 0.18f;
        SetCircleShape(fx.ps, Mathf.Max(0.1f, radius), 1f);
        var em = fx.ps.emission;
        em.rateOverTime = 14f;
        var vol = fx.ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.World;
        vol.y = new ParticleSystem.MinMaxCurve(1.3f); // 위로 떠오름
        fx.duration = Mathf.Max(0.05f, duration);
        fx.lifeAfter = 0.8f;
        fx.ps.Play();
        return fx;
    }

    // 대상 둘레를 도는 입자 링 — 버프/실드 계열(지속 표시). Local 시뮬레이션으로 대상을 따라다님.
    public static SpellParticleVfx SpawnOrbit(Transform follow, float radius, Color color, float duration)
    {
        SpellParticleVfx fx = Create("SpellOrbit", follow != null ? (Vector2)follow.position : Vector2.zero, follow, color);
        var main = fx.ps.main;
        main.startLifetime = 0.8f;
        main.startSpeed = 0f;
        main.startSize = 0.16f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // 대상 추종 궤도
        SetCircleShape(fx.ps, Mathf.Max(0.15f, radius), 0f);        // 가장자리에서만 방출
        var em = fx.ps.emission;
        em.rateOverTime = 13f;
        var vol = fx.ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(3f); // 중심 축 공전
        fx.duration = Mathf.Max(0.05f, duration);
        fx.lifeAfter = 0.9f;
        fx.ps.Play();
        return fx;
    }

    // 반경 전체에서 은은히 피어오르는 잔불/입김 — 장판(화염/감속/빙결) 지속 표시.
    public static SpellParticleVfx SpawnField(Vector2 pos, float radius, Color color, float duration)
    {
        SpellParticleVfx fx = Create("SpellField", pos, null, color);
        var main = fx.ps.main;
        main.startLifetime = 0.9f;
        main.startSpeed = 0f;
        main.startSize = 0.17f;
        SetCircleShape(fx.ps, Mathf.Max(0.2f, radius), 1f); // 반경 내부 전체에서 방출
        var em = fx.ps.emission;
        em.rateOverTime = Mathf.Clamp(radius * radius * 5f, 8f, 40f); // 면적 비례(상한)
        var vol = fx.ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.World;
        vol.y = new ParticleSystem.MinMaxCurve(0.45f); // 살짝 떠오름
        fx.duration = Mathf.Max(0.05f, duration);
        fx.lifeAfter = 1f;
        fx.ps.Play();
        return fx;
    }

    void Update()
    {
        if (follow != null) transform.position = follow.position; // 파괴된 대상은 null 판정 → 제자리 페이드

        float t = Time.unscaledTime - startTime;
        if (!stopped && t >= duration)
        {
            stopped = true;
            var em = ps.emission;
            em.enabled = false; // 방출만 중단, 남은 입자는 수명대로 소멸
        }
        if (t >= duration + lifeAfter) Destroy(gameObject);
    }

    static SpellParticleVfx Create(string name, Vector2 pos, Transform follow, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.position = follow != null ? follow.position : new Vector3(pos.x, pos.y, 0f);
        SpellParticleVfx fx = go.AddComponent<SpellParticleVfx>();
        fx.follow = follow;
        fx.ps = BuildSystem(go, color);
        fx.startTime = Time.unscaledTime;
        return fx;
    }

    // ExplosionVfx.BuildSystem과 동일 규약의 기본 시스템(버스트/레이트는 각 프리미티브가 설정)
    static ParticleSystem BuildSystem(GameObject go, Color color)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.useUnscaledTime = true;
        main.playOnAwake = false;
        main.loop = true;                 // 방출 중단은 Update가 duration으로 제어
        main.gravityModifier = 0f;
        main.startColor = color * hdrBoost; // HDR: Bloom 발광 유도
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white * hdrBoost, 0f), new GradientColorKey(color * hdrBoost, 0.35f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = SharedMaterial();
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0)
            r.sortingLayerID = layers[layers.Length - 1].id;
        r.sortingOrder = 32000;

        return ps;
    }

    static void SetCircleShape(ParticleSystem ps, float radius, float thickness)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = thickness; // 0=가장자리만, 1=내부 전체
    }

    static void SetBurst(ParticleSystem ps, int count)
    {
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, count)) });
    }

    // 부드러운 원형 입자용 Additive 머티리얼(1회 생성 공유) — ExplosionVfx와 동일 방식
    static Material sharedMat;
    public static Material SharedMaterial()
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
                float d = Mathf.Sqrt(dx * dx + dy * dy) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                softDot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        softDot.Apply();
        return softDot;
    }
}
