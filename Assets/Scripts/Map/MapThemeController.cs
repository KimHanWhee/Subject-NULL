using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

// 맵 테마 적용기 — GameScene 로드 시 스스로 생성되어(씬 편집 불필요)
// Resources/MapThemes의 테마 중 하나를 랜덤 적용한다. F9로 순환 전환(테스트/연출 확인용).
// 적용 요소: Floor/Wall 타일맵 틴트 · 카메라 배경색 · 화면 오버레이 · 환경 파티클 · 타일 교체 · BGM
public class MapThemeController : MonoBehaviour
{
    const string ScenePlayable = "GameScene";
    const int OverlayOrder = 30000;  // 스펠 VFX(32000)보다 아래, 월드 스프라이트보다 위
    const int ParticleOrder = 31000; // 오버레이 위(분위기막에 눌리지 않는 또렷한 입자)

    private MapTheme[] themes;
    private MapTheme current;
    private static int lastIndex = -1; // 씬 재시작 시 직전 테마 반복 방지

    private Tilemap floorMap, wallMap;
    private Color floorColor0 = Color.white, wallColor0 = Color.white;
    private Camera cam;
    private Color bgColor0;
    private SpriteRenderer overlay;
    private ParticleSystem ambient;
    private AudioSource bgmSource;

    // ⚠️ 맵 테마(연구실/불/눈/풀/던전)는 비활성화되어 있다.
    //    틴트·오버레이·환경 파티클이 배경과 적의 대비를 떨어뜨려 적이 잘 안 보였다.
    //    되살리려면 아래 [RuntimeInitializeOnLoadMethod] 주석을 해제하면 된다
    //    (스크립트와 Resources/MapThemes 에셋은 그대로 보존해 둠).
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, _) => TryCreate(scene);
        TryCreate(SceneManager.GetActiveScene());
    }

    static void TryCreate(Scene scene)
    {
        if (scene.name != ScenePlayable) return;
        if (FindFirstObjectByType<MapThemeController>() != null) return;
        new GameObject("MapThemeController").AddComponent<MapThemeController>();
    }

    void Awake()
    {
        themes = Resources.LoadAll<MapTheme>("MapThemes");

        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.name == "Floor") floorMap = tm;
            else if (tm.name == "Wall") wallMap = tm;
        }
        if (floorMap != null) floorColor0 = floorMap.color;
        if (wallMap != null) wallColor0 = wallMap.color;

        cam = Camera.main;
        if (cam != null) bgColor0 = cam.backgroundColor;
    }

    void Start()
    {
        if (themes == null || themes.Length == 0) return;

        // 실험실 고정 배경 — Lab 테마가 있으면 항상 적용(스토리: 실험체 능력 평가장)
        for (int k = 0; k < themes.Length; k++)
            if (themes[k] != null && themes[k].name == "Lab") { Apply(k); return; }

        // 폴백: 기존 랜덤 테마
        int i;
        do { i = Random.Range(0, themes.Length); }
        while (themes.Length > 1 && i == lastIndex);
        Apply(i);
    }

    void Update()
    {
        // F9: 테마 순환(플레이 중 분위기 확인용)
        if (Keyboard.current != null && Keyboard.current[Key.F9].wasPressedThisFrame && themes.Length > 0)
            Apply((lastIndex + 1) % themes.Length);
    }

    void LateUpdate()
    {
        // Shift 선택 모드의 시야 확대(orthographicSize 변경)에도 오버레이가 항상 화면을 덮도록 매 프레임 보정
        if (overlay != null && overlay.gameObject.activeSelf && cam != null)
        {
            float h = cam.orthographicSize * 2f;
            overlay.transform.localScale = new Vector3(h * cam.aspect + 2f, h + 2f, 1f);
        }
    }

    // 환경 파티클 방출 영역 — 카메라가 아닌 맵(Floor 타일맵) 전체 기준.
    // 카메라 크기 기준이면 Shift 줌아웃 시 방출 영역 경계(네모 박스)가 화면에 드러난다.
    Bounds MapWorldBounds()
    {
        if (floorMap != null)
        {
            floorMap.CompressBounds();
            Bounds lb = floorMap.localBounds;
            Vector3 c = floorMap.transform.TransformPoint(lb.center);
            Vector3 s = Vector3.Scale(lb.size, floorMap.transform.lossyScale);
            return new Bounds(c, s);
        }
        // 폴백: 카메라 시야의 2.5배(최대 줌아웃에도 경계가 안 보일 만큼)
        if (cam != null)
        {
            float h = cam.orthographicSize * 2f;
            return new Bounds(cam.transform.position, new Vector3(h * cam.aspect * 2.5f, h * 2.5f, 1f));
        }
        return new Bounds(Vector3.zero, new Vector3(50f, 30f, 1f));
    }

    // 조커 발동 등에서 호출 — 현재와 다른 테마를 랜덤 적용.
    public void RandomizeTheme()
    {
        if (themes == null || themes.Length == 0) return;
        int i;
        do { i = Random.Range(0, themes.Length); }
        while (themes.Length > 1 && i == lastIndex);
        Apply(i);
    }

    public static void RandomizeCurrent()
    {
        MapThemeController c = FindFirstObjectByType<MapThemeController>();
        if (c != null) c.RandomizeTheme();
    }

    public void Apply(int index)
    {
        MapTheme theme = themes[index];
        if (theme == null) return;

        RevertTileSwap(); // 이전 테마의 타일 교체 원복(F9 전환 대비)
        lastIndex = index;
        current = theme;

        if (floorMap != null) floorMap.color = floorColor0 * theme.floorTint;
        if (wallMap != null) wallMap.color = wallColor0 * theme.wallTint;
        if (cam != null) cam.backgroundColor = theme.overrideBackground ? theme.backgroundColor : bgColor0;

        ApplyTileSwap(theme);
        ApplyOverlay(theme);
        ApplyAmbient(theme);
        ApplyBgm(theme);
    }

    // ---- 타일 교체 ----

    void ApplyTileSwap(MapTheme theme)
    {
        if (theme.fromTiles == null || theme.toTiles == null) return;
        int n = Mathf.Min(theme.fromTiles.Length, theme.toTiles.Length);
        for (int i = 0; i < n; i++)
        {
            if (theme.fromTiles[i] == null || theme.toTiles[i] == null) continue;
            if (floorMap != null) floorMap.SwapTile(theme.fromTiles[i], theme.toTiles[i]);
            if (wallMap != null) wallMap.SwapTile(theme.fromTiles[i], theme.toTiles[i]);
        }
    }

    void RevertTileSwap()
    {
        if (current == null || current.fromTiles == null || current.toTiles == null) return;
        int n = Mathf.Min(current.fromTiles.Length, current.toTiles.Length);
        for (int i = 0; i < n; i++)
        {
            if (current.fromTiles[i] == null || current.toTiles[i] == null) continue;
            if (floorMap != null) floorMap.SwapTile(current.toTiles[i], current.fromTiles[i]);
            if (wallMap != null) wallMap.SwapTile(current.toTiles[i], current.fromTiles[i]);
        }
    }

    // ---- 화면 오버레이 ----

    void ApplyOverlay(MapTheme theme)
    {
        if (theme.overlayColor.a <= 0f)
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
            return;
        }
        if (overlay == null)
        {
            GameObject go = new GameObject("ThemeOverlay");
            go.transform.SetParent(cam != null ? cam.transform : transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 10f); // 카메라 앞(월드 z=0 평면)
            overlay = go.AddComponent<SpriteRenderer>();
            overlay.sprite = WhiteSprite();
            SetTopLayer(overlay, OverlayOrder);
        }
        overlay.gameObject.SetActive(true);
        overlay.color = theme.overlayColor;
        if (cam != null)
        {
            float h = cam.orthographicSize * 2f;
            overlay.transform.localScale = new Vector3(h * cam.aspect + 2f, h + 2f, 1f);
        }
    }

    // ---- 환경 파티클 ----

    void ApplyAmbient(MapTheme theme)
    {
        if (ambient != null) Destroy(ambient.gameObject); // 스타일이 달라 재구성이 단순·확실
        ambient = null;
        if (theme.ambient == MapTheme.AmbientStyle.None || cam == null) return;

        // 맵 전체를 덮는 월드 고정 방출 영역 — 카메라 부착이면 Shift 줌아웃 시 경계 박스가 보인다
        Bounds area = MapWorldBounds();
        float halfH = area.extents.y + 2f;
        float halfW = area.extents.x + 2f;

        GameObject go = new GameObject("ThemeAmbient");
        go.transform.position = new Vector3(area.center.x, area.center.y, 10f); // 월드 고정(카메라 비부착)
        ambient = go.AddComponent<ParticleSystem>();
        ambient.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ambient.main;
        main.playOnAwake = false;
        main.loop = true;
        main.prewarm = true; // 시작/전환 즉시 화면에 입자가 차 있도록(누적 대기 없음)
        main.gravityModifier = 0f;
        main.startColor = theme.ambientColor;
        main.maxParticles = 4000; // 맵 전체 커버(수명 = 맵 높이/낙하속도)라 기본 1000으론 부족
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 카메라가 움직여도 입자는 월드에 남음

        var shape = ambient.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;

        var em = ambient.emission;
        var col = ambient.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        switch (theme.ambient)
        {
            // 주의: velocityOverLifetime의 TwoConstants(랜덤 범위)가 이 버전에서 적용되지 않아
            // "셰이프 회전 + startSpeed(랜덤 범위 정상 동작)"로 방출 방향을 준다. 흩날림은 noise 담당.
            case MapTheme.AmbientStyle.Snow:
            case MapTheme.AmbientStyle.Ash:
            {
                bool snow = theme.ambient == MapTheme.AmbientStyle.Snow;
                float fall = snow ? 1.6f : 0.9f;
                shape.position = new Vector3(0f, halfH, 0f);      // 화면 위 가장자리
                shape.scale = new Vector3(halfW * 2f, 0.5f, 1f);
                shape.rotation = new Vector3(90f, 0f, 0f);        // 박스 +Z 방출 → 아래(-Y)
                main.startLifetime = (halfH * 2f + 1f) / fall;
                main.startSpeed = new ParticleSystem.MinMaxCurve(fall * 0.85f, fall * 1.15f);
                main.startSize = new ParticleSystem.MinMaxCurve(snow ? 0.1f : 0.08f, snow ? 0.24f : 0.18f);
                var noise = ambient.noise;
                noise.enabled = true;
                noise.strength = 0.3f;   // 좌우 흩날림
                noise.frequency = 0.5f;
                em.rateOverTime = halfW * (snow ? 2.2f : 1.4f);
                break;
            }
            case MapTheme.AmbientStyle.Embers:
            {
                shape.position = new Vector3(0f, -halfH, 0f);     // 화면 아래 가장자리
                shape.scale = new Vector3(halfW * 2f, 0.5f, 1f);
                shape.rotation = new Vector3(-90f, 0f, 0f);       // 박스 +Z 방출 → 위(+Y)
                main.startLifetime = (halfH * 2f + 1f) / 1.2f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 1.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                var noise = ambient.noise;
                noise.enabled = true;
                noise.strength = 0.35f;
                noise.frequency = 0.4f;
                em.rateOverTime = halfW * 1.8f;
                break;
            }
            case MapTheme.AmbientStyle.Fireflies:
            {
                shape.scale = new Vector3(halfW * 2f, halfH * 2f, 1f); // 화면 전체
                main.startLifetime = 4.5f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
                var noise = ambient.noise;
                noise.enabled = true;
                noise.strength = 0.5f;   // 느린 무작위 표류
                noise.frequency = 0.25f;
                var sz = ambient.sizeOverLifetime; // 깜빡이듯 커졌다 작아짐
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.2f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.2f)));
                em.rateOverTime = halfW * halfH * 0.16f;
                break;
            }
        }

        var r = ambient.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = SpellParticleVfx.SharedMaterial(); // 부드러운 원형 Additive(발광) 재사용
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) r.sortingLayerID = layers[layers.Length - 1].id;
        r.sortingOrder = ParticleOrder;

        ambient.Play();
    }

    // ---- BGM ----

    void ApplyBgm(MapTheme theme)
    {
        if (theme.bgm == null)
        {
            if (bgmSource != null) bgmSource.Stop();
            return;
        }
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }
        bgmSource.clip = theme.bgm;
        bgmSource.volume = theme.bgmVolume;
        bgmSource.Play();
    }

    // ---- 공용 ----

    static void SetTopLayer(SpriteRenderer sr, int order)
    {
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = order;
    }

    static Sprite whiteSprite;
    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); // 1x1 유닛
        return whiteSprite;
    }
}
