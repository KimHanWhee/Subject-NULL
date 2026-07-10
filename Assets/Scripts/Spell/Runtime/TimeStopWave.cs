using System.Collections.Generic;
using UnityEngine;

// ♣ Time Stop 파동 — 시전자 중심에서 링이 퍼지며, 파장에 닿은 적/적 총알을
// 일그러뜨리고(스쿼시-스트레치) 회색(그레이스케일 머티리얼)으로 만들며 그 자리에 정지시킨다.
// 파동이 다 퍼지면: 색/스케일 원복(정지는 유지) → TimeStopField가 남은 지속시간 동안
// 정지를 유지(파동 이후 스폰된 적/총알 포함).
public class TimeStopWave : MonoBehaviour
{
    class Affected
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Material originalMat;
        public Vector3 baseScale;
        public float phase;      // 개체마다 다른 흔들림 위상
    }

    [Header("Distortion (일그러짐)")]
    public float wobbleAmount = 0.14f; // 스케일 흔들림 비율
    public float wobbleSpeed = 34f;    // 흔들림 속도(rad/s)

    private float startTime;
    private float waveDuration;
    private float stopDuration;        // 파동 완료 후 유지되는 정지 시간
    private float maxRadius;
    private Material grayMat;
    private Color ringColor;

    private readonly List<Affected> affected = new List<Affected>();
    private readonly List<EnemyBullet> stoppedBullets = new List<EnemyBullet>();
    private readonly HashSet<Transform> hitSet = new HashSet<Transform>();
    private SpriteRenderer ring;      // 파동 본체
    private SpriteRenderer ringGlow;  // 뒤따르는 잔광
    private bool completed;

    public static TimeStopWave Spawn(Vector2 center, float radius, float waveDuration, float stopDuration, Material grayMaterial, Color color)
    {
        GameObject go = new GameObject("TimeStopWave");
        go.transform.position = new Vector3(center.x, center.y, 0f);
        TimeStopWave w = go.AddComponent<TimeStopWave>();
        w.maxRadius = Mathf.Max(1f, radius);
        w.waveDuration = Mathf.Max(0.1f, waveDuration);
        w.stopDuration = stopDuration;
        w.grayMat = grayMaterial;
        w.ringColor = color;
        w.startTime = Time.unscaledTime;
        w.ring = w.MakeRing(1f);
        w.ringGlow = w.MakeRing(0.3f);
        return w;
    }

    SpriteRenderer MakeRing(float alphaScale)
    {
        GameObject go = new GameObject("Ring");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpellVfx.RingSprite();
        // 프로젝트의 '최상단' Sorting Layer + 높은 order — 월드 스프라이트에 가려지지 않게(SpellVfx 관례)
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0)
            sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = 32000;
        Color c = ringColor;
        c.a *= alphaScale;
        sr.color = c;
        return sr;
    }

    void Update()
    {
        float t = (Time.unscaledTime - startTime) / waveDuration;
        if (t >= 1f)
        {
            Complete();
            return;
        }

        float ease = 1f - (1f - t) * (1f - t); // easeOutQuad — 초반 빠르게 퍼지고 잦아듦
        float radius = maxRadius * ease;

        // 링 시각: 본체는 파장 선두, 잔광은 살짝 뒤에서 따라옴
        if (ring != null)
        {
            ring.transform.localScale = Vector3.one * (radius * 2f);
            Color c = ringColor;
            c.a = Mathf.Lerp(0.95f, 0.25f, t);
            ring.color = c;
        }
        if (ringGlow != null)
        {
            ringGlow.transform.localScale = Vector3.one * (radius * 2f * 0.85f);
            Color g = ringColor;
            g.a = 0.3f * (1f - t);
            ringGlow.color = g;
        }

        HitScan(radius);

        // 일그러짐: 회색 상태 개체들을 스쿼시-스트레치로 흔듦(파동 완료 시 원복)
        float now = Time.unscaledTime;
        for (int i = 0; i < affected.Count; i++)
        {
            Affected a = affected[i];
            if (a.tr == null) continue;
            float s = Mathf.Sin((now - startTime) * wobbleSpeed + a.phase) * wobbleAmount;
            a.tr.localScale = new Vector3(a.baseScale.x * (1f + s), a.baseScale.y * (1f - s), a.baseScale.z);
        }
    }

    void HitScan(float radius)
    {
        Vector2 center = transform.position;

        foreach (EnemyController ec in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            TryHitEnemy(ec.gameObject, center, radius);
        foreach (RangedEnemyController rc in Object.FindObjectsByType<RangedEnemyController>(FindObjectsSortMode.None))
            TryHitEnemy(rc.gameObject, center, radius);

        // 적 총알: 스크립트 정지(이동+수명 동결) + 회색화. 복구는 TimeStopField가 인계받아 종료 시 수행
        foreach (EnemyBullet b in Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None))
        {
            if (hitSet.Contains(b.transform)) continue;
            if (Vector2.Distance(b.transform.position, center) > radius) continue;
            hitSet.Add(b.transform);
            if (b.enabled)
            {
                b.enabled = false;
                stoppedBullets.Add(b);
            }
            GrayOut(b.gameObject);
        }
    }

    void TryHitEnemy(GameObject enemy, Vector2 center, float radius)
    {
        if (hitSet.Contains(enemy.transform)) return;
        if (Vector2.Distance(enemy.transform.position, center) > radius) return;
        hitSet.Add(enemy.transform);
        // 파동에 맞는 즉시 정지. 남은 파동 시간 + 유지 시간 + 여유(TimeStopField가 주기 갱신으로 이어받음)
        float freezeFor = (startTime + waveDuration - Time.unscaledTime) + stopDuration + 0.3f;
        FreezeStatus.Apply(enemy, freezeFor);
        GrayOut(enemy);
    }

    void GrayOut(GameObject go)
    {
        Affected a = new Affected
        {
            tr = go.transform,
            sr = go.GetComponent<SpriteRenderer>(),
            baseScale = go.transform.localScale,
            phase = Random.value * Mathf.PI * 2f
        };
        if (a.sr != null && grayMat != null)
        {
            a.originalMat = a.sr.sharedMaterial;
            a.sr.material = grayMat;
        }
        affected.Add(a);
    }

    void Complete()
    {
        if (completed) return;
        completed = true;

        RestoreAll(); // 원래 색/스케일로 복귀(정지는 FreezeStatus가 유지)

        // 남은 지속시간 유지 + 파동 이후 스폰 개체 정지. 파동이 멈춘 총알 복구도 인계.
        TimeStopField field = TimeStopField.Spawn(stopDuration);
        field.AdoptStoppedBullets(stoppedBullets);
        stoppedBullets.Clear();

        SpellVfx.SpawnRing(transform.position, maxRadius, Color.white, 0.25f); // 마무리 펄스
        Destroy(gameObject);
    }

    void RestoreAll()
    {
        foreach (Affected a in affected)
        {
            if (a.tr != null) a.tr.localScale = a.baseScale;
            if (a.sr != null && a.originalMat != null) a.sr.material = a.originalMat;
        }
        affected.Clear();
    }

    void OnDestroy()
    {
        // 파동 도중 파괴(씬 전환 등) 안전망 — Complete 경로에선 이미 비워져 있어 무해
        RestoreAll();
        foreach (EnemyBullet b in stoppedBullets)
            if (b != null) b.enabled = true;
        stoppedBullets.Clear();
    }
}
