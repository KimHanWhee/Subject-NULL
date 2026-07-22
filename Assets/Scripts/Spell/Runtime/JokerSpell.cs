using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 조커 스펠 — 덱의 스펠마블을 전부 소모하면(셔플백 1바퀴) 과부하가 걸려
// 여러 기믹 중 하나가 랜덤 발동한다. SpellCaster.DrawNext의 재셔플 시점에서 Trigger됨.
// 기믹: ①대숙청(안전지대 밖 전멸) ②번개 폭풍(0.5초 경고 낙뢰 연타)
public class JokerSpell : MonoBehaviour
{
    const int GimmickCount = 2;

    // 기믹 효과음(SpellCaster가 인스펙터 클립을 주입) — null이면 무음(Sfx.Play2D가 안전 처리)
    private AudioClip thunderSound;   // 번개 폭풍 — 낙뢰가 떨어질 때마다
    private AudioClip purgeSound;     // 대숙청 — 폭발(심판) 순간

    // forceIndex: 테스트/디버그용 강제 선택(-1 = 랜덤)
    // onGimmickStart: 3초 경고가 끝나고 기믹이 실제 발동하는 순간 호출(SpellCaster의 덱 리셋 훅)
    // warnSound: 경고 사이렌(점멸 동안 루프 재생, 종료 시 정지)
    // thunder/purge: 각 기믹 효과음
    public static void Trigger(GameObject player, int forceIndex = -1, System.Action onGimmickStart = null, AudioClip warnSound = null,
        AudioClip thunderSound = null, AudioClip purgeSound = null)
    {
        JokerSpell j = new GameObject("JokerSpell").AddComponent<JokerSpell>();
        j.thunderSound = thunderSound;
        j.purgeSound = purgeSound;
        j.StartCoroutine(j.Sequence(player, forceIndex, onGimmickStart, warnSound));
    }

    // 경고(3초, 화면 적색 점멸 + 문구 + 사이렌) → 기믹 발동
    IEnumerator Sequence(GameObject player, int forceIndex, System.Action onGimmickStart, AudioClip warnSound)
    {
        const float warnTime = 3f;
        Camera cam = Camera.main;

        // 사이렌 — 점멸 구간 동안 루프, 경고 종료와 동시에 정지
        AudioSource siren = null;
        if (warnSound != null)
        {
            siren = gameObject.AddComponent<AudioSource>();
            siren.clip = warnSound;
            siren.loop = true;
            siren.spatialBlend = 0f; // 2D
            siren.volume = 0.8f;
            siren.Play();
        }

        // 전체 화면 오버레이(카메라 자식) — 검정 딤(배경 어둡게) + 적색 점멸
        SpriteRenderer dark = null;
        SpriteRenderer overlay = null;
        TMPro.TextMeshPro label = null;
        // 위험 사선 띠(폴리스 라인 테이프) — 위/아래에서 슬라이드 진입 + 스트라이프 스크롤
        Transform tapeTop = null, tapeBot = null;
        Material tapeTopMat = null, tapeBotMat = null;
        float topTargetY = 0f, botTargetY = 0f;
        const float bandH = 1.1f;                 // 띠 두께(월드 유닛)
        const float hideDist = bandH + 0.9f;      // 화면 밖 은신 거리(슬라이드 인/아웃)
        if (cam != null)
        {
            float h = cam.orthographicSize * 2f;
            Vector3 fullScreen = new Vector3(h * cam.aspect + 2f, h + 2f, 1f);

            // 검정 딤 백드롭 — 경고 동안 배경을 어둡게(붉은 점멸 오버레이 뒤)
            GameObject dg = new GameObject("JokerWarnDim");
            dg.transform.SetParent(cam.transform, false);
            dg.transform.localPosition = new Vector3(0f, 0f, 10f);
            dark = dg.AddComponent<SpriteRenderer>();
            dark.sprite = WhiteSprite();
            dark.color = Color.clear; // 기본색(불투명 흰색) 노출 금지 — 페이드 루프가 색을 입힌다
            dg.transform.localScale = fullScreen;
            SetTopLayer(dark, 30450); // 붉은 오버레이(30500)보다 뒤

            GameObject og = new GameObject("JokerWarnOverlay");
            og.transform.SetParent(cam.transform, false);
            og.transform.localPosition = new Vector3(0f, 0f, 10f);
            overlay = og.AddComponent<SpriteRenderer>();
            overlay.sprite = WhiteSprite();
            overlay.color = Color.clear;
            og.transform.localScale = fullScreen;
            SetTopLayer(overlay, 30500); // 테마 오버레이(30000) 위, 파티클(31000) 아래

            // 테이프 밴드 2줄 — 화면보다 넓게(기울임 여백), 위/아래 가장자리. 반대 방향으로 흐름.
            float bw = h * cam.aspect + 4f;
            topTargetY = cam.orthographicSize - bandH * 0.55f;
            botTargetY = -cam.orthographicSize + bandH * 0.55f;
            tapeTop = MakeBand(cam.transform, bw, bandH, topTargetY, 4f, 30800, out tapeTopMat);
            tapeBot = MakeBand(cam.transform, bw, bandH, botTargetY, -4f, 30800, out tapeBotMat);

            GameObject lg = new GameObject("JokerWarnLabel");
            lg.transform.SetParent(cam.transform, false);
            lg.transform.localPosition = new Vector3(0f, 1.6f, 10f);
            label = lg.AddComponent<TMPro.TextMeshPro>();
            TMPro.TMP_FontAsset kf = FloatingText.KoreanFont();
            if (kf != null) label.font = kf; // 한글 글리프(기본 폰트엔 없음)
            label.text = "!! 스펠 마블 과부하 !!";
            label.fontSize = 6f;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            MeshRenderer mr = lg.GetComponent<MeshRenderer>();
            SortingLayer[] layers = SortingLayer.layers;
            if (mr != null && layers != null && layers.Length > 0) mr.sortingLayerID = layers[layers.Length - 1].id;
            if (mr != null) mr.sortingOrder = 31500;
        }

        // 경고 동안 게임 슬로우(Ctrl 선택 슬로우와 동일 감각) — 화면을 가려도 위협이 거의 멈춰 안전.
        // unscaled 기준 자동 만료라 경고(3초) 뒤 기믹은 정상 속도로 진행된다.
        const float warnSlowScale = 0.05f;
        if (TimeController.Instance != null)
            TimeController.Instance.Pulse(warnSlowScale, warnTime);

        float t = 0f;
        while (t < warnTime)
        {
            t += Time.unscaledDeltaTime; // 사이렌(실시간)과 동기 — 슬로우 무관하게 3초 유지
            float pulse = Mathf.PingPong(t * 2.4f, 1f); // 붉어졌다 돌아왔다
            // 시작·끝 0.4초 페이드(배경 딤·오버레이·테이프 슬라이드 공용)
            float vis = Mathf.Min(Mathf.Clamp01(t / 0.4f), Mathf.Clamp01((warnTime - t) / 0.4f));
            if (dark != null) dark.color = new Color(0f, 0f, 0f, vis * 0.6f);             // 배경 어둡게(우세)
            if (overlay != null) overlay.color = new Color(1f, 0.15f, 0.15f, vis * pulse * 0.12f); // 은은한 붉은 점멸
            if (label != null) label.color = new Color(1f, 0.32f, 0.32f, vis * (0.55f + pulse * 0.45f));
            if (tapeTopMat != null) tapeTopMat.mainTextureOffset = new Vector2(-t * 1.3f, 0f);
            if (tapeBotMat != null) tapeBotMat.mainTextureOffset = new Vector2(t * 1.3f, 0f);
            if (tapeTop != null) tapeTop.localPosition = new Vector3(0f, topTargetY + (1f - vis) * hideDist, 10f);
            if (tapeBot != null) tapeBot.localPosition = new Vector3(0f, botTargetY - (1f - vis) * hideDist, 10f);
            yield return null;
        }
        if (dark != null) Destroy(dark.gameObject);
        if (overlay != null) Destroy(overlay.gameObject);
        if (label != null) Destroy(label.gameObject);
        if (tapeTop != null) Destroy(tapeTop.gameObject);
        if (tapeBot != null) Destroy(tapeBot.gameObject);
        if (tapeTopMat != null) Destroy(tapeTopMat); // 런타임 생성 머티리얼 누수 방지
        if (tapeBotMat != null) Destroy(tapeBotMat);
        if (siren != null) { siren.Stop(); Destroy(siren); }

        onGimmickStart?.Invoke(); // 덱 재셔플·리필·Ctrl 잠금 해제(SpellCaster)
        // 조커 시 랜덤 테마 전환은 제거 — 불/눈/풀 테마의 틴트·오버레이가
        // 배경 대비를 떨어뜨려 적이 잘 보이지 않는 문제가 있었다.
        Run(player, forceIndex);
    }

    
    void Run(GameObject player, int forceIndex)
    {
        int pick = forceIndex >= 0 ? forceIndex % GimmickCount : Random.Range(0, GimmickCount);
        switch (pick)
        {
            case 0: StartCoroutine(Purge(player)); break;
            default: StartCoroutine(LightningStorm(player)); break;
        }
    }

    // 공통 발동 연출 — 과부하 폭발 느낌(무지개빛 = 레전드 팔레트 차용)
    void Overload(GameObject player, string label, Color color)
    {
        Vector2 p = player != null ? (Vector2)player.transform.position : Vector2.zero;
        SpellVfx.SpawnRing(p, 6f, color, 0.6f);
        SpellParticleVfx.SpawnBurst(p, 3f, color, 40, 0.6f);
        FloatingText.Show(p + Vector2.up * 1.2f, "JOKER! " + label, color);
    }

    // ---- ① 대숙청: 안전지대 밖 모든 생명체 사망 ----
    // 대숙청 경고 중에는 적의 공격이 플레이어에게 통하지 않는다.
    // 제한 시간 안에 안전지대까지 가는 것 자체가 과제인데 적 공격까지 피해야 하면
    // 회피가 사실상 불가능했다. 심판(원 밖 즉사) 직전에 반드시 해제된다.
    public static bool EnemyAttacksSuppressed { get; private set; }

    // 안전망 — 씬 전환·재시작 등으로 코루틴이 중간에 끊겨도 무적 상태가 남지 않게 한다.
    void OnDestroy() { EnemyAttacksSuppressed = false; }

    IEnumerator Purge(GameObject player)
    {
        const float warnTime = 3.5f;
        const float safeRadius = 2.6f;
        // 맵 전체 랜덤(내부 x±18, y±8 — 안전원이 벽에 걸치지 않게)
        Vector2 safe = new Vector2(Random.Range(-18f, 18f), Random.Range(-8f, 8f));

        Overload(player, "대숙청", new Color(1f, 0.25f, 0.25f));

        // 안전지대 원 표시(지속) + 주기 펄스 링으로 시선 유도 + 시야 밖이면 방향 화살표
        LineRenderer circle = DrawCircle(safe, safeRadius, new Color(0.4f, 1f, 0.55f, 0.9f));
        OffscreenArrow arrow = OffscreenArrow.Show(safe, new Color(0.4f, 1f, 0.55f, 1f));
        EnemyAttacksSuppressed = true;   // 이동에만 집중할 수 있게 — 심판 직전 해제
        float t = 0f;
        while (t < warnTime)
        {
            SpellVfx.SpawnRing(safe, safeRadius, new Color(0.4f, 1f, 0.55f, 1f), 0.5f);
            yield return new WaitForSeconds(0.6f);
            t += 0.6f;
        }
        EnemyAttacksSuppressed = false;  // 대숙청 자체의 피해는 정상 적용되어야 하므로 여기서 해제
        Destroy(circle.gameObject);
        if (arrow != null) Destroy(arrow.gameObject);

        // 심판 — 원 밖 적 전멸 + 원 밖 플레이어 치명타(피해 수정 체인·부활 마블은 정상 개입)
        SpellVfx.SpawnRing(safe, 14f, new Color(1f, 0.3f, 0.3f, 1f), 0.5f);
        SpellParticleVfx.SpawnBurst(safe, 10f, new Color(1f, 0.4f, 0.3f, 1f), 70, 0.7f);
        Sfx.Play2D(purgeSound, 0.9f); // 대숙청 폭발음
        foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>())
        {
            if (!e.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, safe) > safeRadius)
                e.ApplyHit(9999f);
        }
        if (player != null && Vector2.Distance(player.transform.position, safe) > safeRadius)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.ApplyRangedHit(99f, gameObject);
        }
        Destroy(gameObject, 1f);
    }

    // ---- ② 번개 폭풍: N초간 낙뢰 연타. 각 지점에 0.5초 수렴 링 경고 후 즉발 —
    // 반응 속도(이동/대시)로 피해야 한다. 모든 생명체 타격(적 3딜, 플레이어 1딜). ----
    IEnumerator LightningStorm(GameObject player)
    {
        const float duration = 5f;       // 폭풍 지속
        const float strikeInterval = 0.3f; // 낙뢰 생성 간격
        Color c = new Color(1f, 0.95f, 0.4f);
        Overload(player, "번개 폭풍", c);

        float end = Time.time + duration;
        while (Time.time < end)
        {
            // 절반은 맵 랜덤, 절반은 플레이어 주변 — 순수 랜덤만으론 위협이 안 됨
            Vector2 pos;
            if (player != null && Random.value < 0.5f)
                pos = (Vector2)player.transform.position + Random.insideUnitCircle * 2.5f;
            else
                pos = new Vector2(Random.Range(-8f, 8f), Random.Range(-4f, 4f));
            StartCoroutine(Strike(pos, player));
            yield return new WaitForSeconds(strikeInterval);
        }
        Destroy(gameObject, 1.5f); // 마지막 낙뢰 코루틴 종료 여유
    }

    // 낙뢰 1발: 0.5초 경고(수렴 링) → 번개 시각 + 반경 내 모든 생명체 타격
    IEnumerator Strike(Vector2 pos, GameObject player)
    {
        const float warnTime = 0.5f;
        const float hitRadius = 0.9f;
        Color warn = new Color(1f, 0.9f, 0.3f, 1f);
        SpellVfx.SpawnConverge(pos, hitRadius * 1.6f, warn, warnTime, false, null);
        yield return new WaitForSeconds(warnTime);

        // 번개 시각: 하늘에서 내리꽂는 지그재그 볼트(잠깐 표시)
        LineRenderer bolt = DrawBolt(pos);
        SpellParticleVfx.SpawnBurst(pos, hitRadius, new Color(1f, 1f, 0.7f, 1f), 14, 0.3f);
        Sfx.Play2D(thunderSound, 0.55f); // 낙뢰음(타격마다) — 연타 겹침 대비 볼륨 절제

        foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>())
        {
            if (!e.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, pos) <= hitRadius)
                e.ApplyHit(3f);
        }
        if (player != null && Vector2.Distance(player.transform.position, pos) <= hitRadius)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.ApplyRangedHit(1f, gameObject); // 대시 무적으로 회피 가능
        }

        yield return new WaitForSeconds(0.12f);
        if (bolt != null) Destroy(bolt.gameObject);
    }

    // 지그재그 낙뢰 라인(임팩트 지점 위 7유닛에서 내리꽂음)
    LineRenderer DrawBolt(Vector2 impact)
    {
        GameObject go = new GameObject("JokerBolt");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        const int seg = 6;
        lr.positionCount = seg;
        lr.useWorldSpace = true;
        lr.startWidth = 0.16f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 1f, 0.75f, 1f);
        lr.endColor = new Color(1f, 0.95f, 0.4f, 1f);
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) lr.sortingLayerID = layers[layers.Length - 1].id;
        lr.sortingOrder = 29600;
        Vector2 top = impact + Vector2.up * 7f;
        for (int i = 0; i < seg; i++)
        {
            float t = (float)i / (seg - 1);
            Vector2 p = Vector2.Lerp(top, impact, t);
            if (i > 0 && i < seg - 1) p.x += Random.Range(-0.45f, 0.45f); // 지그재그
            lr.SetPosition(i, p);
        }
        return lr;
    }

    // ---- 연출 헬퍼 ----

    // 지속 표시용 원 둘레(LineRenderer 루프). 최상단 레이어 강제(VFX 규약).
    LineRenderer DrawCircle(Vector2 center, float radius, Color color)
    {
        GameObject go = new GameObject("JokerSafeZone");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        const int seg = 48;
        lr.positionCount = seg;
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) lr.sortingLayerID = layers[layers.Length - 1].id;
        lr.sortingOrder = 29500;
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            lr.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
        return lr;
    }

    // 1x1 흰 스프라이트(오버레이용)
    static Sprite whiteSprite;
    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    static void SetTopLayer(SpriteRenderer sr, int order)
    {
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = order;
    }

    // 경고 테이프 밴드 1줄 — 카메라 자식 쿼드 + 스크롤용 머티리얼(Unlit/Transparent는 offset 반영됨).
    Transform MakeBand(Transform parent, float width, float height, float yLocal, float tiltDeg, int order, out Material mat)
    {
        GameObject go = new GameObject("JokerHazardBand");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, yLocal, 10f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
        go.transform.localScale = new Vector3(width, height, 1f);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = QuadMesh();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        // 빌드에는 참조 없는 셰이더가 스트리핑될 수 있음(WebGL에서 실제 발생) — fallback 체인 + 실패 시 띠 생략
        Shader bandShader = Shader.Find("Unlit/Transparent");
        if (bandShader == null) bandShader = Shader.Find("Sprites/Default");
        if (bandShader == null) { mat = null; Destroy(go); return null; }
        mat = new Material(bandShader);
        mat.mainTexture = HazardTexture();
        mat.mainTextureScale = new Vector2(width / Mathf.Max(0.01f, height), 1f); // 정사각 텍셀 → 45° 유지
        mr.sharedMaterial = mat;
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) mr.sortingLayerID = layers[layers.Length - 1].id;
        mr.sortingOrder = order;
        return go.transform;
    }

    // 1x1 중심 쿼드(UV 0~1). 테이프 밴드 공용.
    static Mesh quadMesh;
    static Mesh QuadMesh()
    {
        if (quadMesh != null) return quadMesh;
        Mesh m = new Mesh();
        m.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        m.uv = new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        m.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        m.RecalculateBounds();
        quadMesh = m;
        return quadMesh;
    }

    // 위험 사선 스트라이프(노랑/검정) 텍스처 — Repeat 랩으로 타일링·스크롤.
    static Texture2D hazardTex;
    static Texture2D HazardTexture()
    {
        if (hazardTex != null) return hazardTex;
        const int S = 64;
        const int stripe = 16; // 대각선 줄 두께(px) — S가 2*stripe의 배수라 이음매 없이 반복
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        Color yellow = new Color(1f, 0.82f, 0f, 0.92f);
        Color black = new Color(0.06f, 0.06f, 0.06f, 0.92f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, (((x + y) / stripe) & 1) == 0 ? yellow : black); // 45° 대각
        tex.Apply();
        hazardTex = tex;
        return hazardTex;
    }
}

// 월드 플로팅 텍스트 — 위로 떠오르며 페이드아웃. 조커 안내/과부하 알림 등 공용.
public class FloatingText : MonoBehaviour
{
    // 한글 지원 폰트(Resources/Fonts/KoreanSDF, 맑은고딕 동적 아틀라스) — 없으면 TMP 기본
    static TMPro.TMP_FontAsset koreanFont;
    static bool fontSearched;
    public static TMPro.TMP_FontAsset KoreanFont()
    {
        if (!fontSearched)
        {
            fontSearched = true;
            koreanFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/KoreanSDF");
        }
        return koreanFont;
    }

    public static void Show(Vector2 pos, string text, Color color, float size = 5f, float life = 1.8f)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = pos;
        TMPro.TextMeshPro tmp = go.AddComponent<TMPro.TextMeshPro>();
        TMPro.TMP_FontAsset kf = KoreanFont();
        if (kf != null) tmp.font = kf;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        SortingLayer[] layers = SortingLayer.layers;
        if (mr != null && layers != null && layers.Length > 0) mr.sortingLayerID = layers[layers.Length - 1].id;
        if (mr != null) mr.sortingOrder = 31500;
        FloatingText ft = go.AddComponent<FloatingText>();
        ft.StartCoroutine(ft.FloatAndFade(tmp, life));
    }

    System.Collections.IEnumerator FloatAndFade(TMPro.TextMeshPro tmp, float life)
    {
        float t = 0f;
        Color c0 = tmp.color;
        while (t < life && tmp != null)
        {
            t += Time.unscaledDeltaTime;
            tmp.transform.position += Vector3.up * (0.6f * Time.unscaledDeltaTime);
            Color c = c0;
            c.a = Mathf.Lerp(1f, 0f, t / life);
            tmp.color = c;
            yield return null;
        }
        if (tmp != null) Destroy(tmp.gameObject);
    }
}
