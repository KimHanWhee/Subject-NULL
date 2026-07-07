using System;
using UnityEngine;
using UnityEngine.UI;

// Design Ref: §5.1/§5.4 — 우측 하단 손패 5 표시(슈트 아이콘 + 등급 색 테두리 + 리필 dim).
// Plan SC: FR-05(손패5 표시) / FR-03(등급 색)
public class SpellHandHUD : MonoBehaviour
{
    [Serializable]
    public class SlotView
    {
        public RectTransform root;  // 드래그 히트 판정/상승 대상
        public Image icon;          // 마블 아이콘
        public Image border;        // 등급 색 테두리
        public Image flash;         // 생성 시 흰색 플래시 오버레이(선택 — 아이콘 위에 배치)
        public CanvasGroup group;   // dim(빈 슬롯) — 선택
    }

    public SpellCaster caster;
    public SlotView[] slots;        // 인스펙터에 5개 배치
    public MarbleSkinTable skinTable; // 슈트×등급 → 벨트 구슬 스프라이트(비우면 marble.icon 사용)

    [Header("Spawn Anim (리필 생성 연출)")]
    public float spawnDuration = 0.25f;                     // 생성 애니 길이(초, unscaled)
    [Range(0.05f, 1f)] public float spawnStartScale = 0.3f; // 시작 스케일(작게 팝업)

    [NonSerialized] public int suppressedSlot = -1;         // 드래그로 집어든 슬롯(아이콘 숨김, 런타임 전용)
    private bool[] filledPrev;   // 직전 채움 여부(빈→참 전이로 생성 애니 트리거)
    private float[] spawnStart;  // 생성 애니 시작 시각(unscaled), <0=비활성
    private bool[] flashAuto;    // 해당 슬롯 flash를 코드가 자동 생성했는지

    // 벨트에 뜰 구슬 스프라이트 결정: 마블 개별 icon 우선, 없으면 테이블(슈트×등급).
    public Sprite ResolveIcon(SpellMarble m)
    {
        if (m == null) return null;
        if (m.icon != null) return m.icon;                 // 개별 오버라이드 우선
        return skinTable != null ? skinTable.Get(m.suit, m.grade) : null;
    }

    void OnEnable()
    {
        EnsureAnimState();
        if (caster != null) caster.OnHandChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (caster != null) caster.OnHandChanged -= Refresh;
    }

    void EnsureAnimState()
    {
        int n = slots != null ? slots.Length : 0;
        if (filledPrev != null && filledPrev.Length == n) return;
        filledPrev = new bool[n];
        spawnStart = new float[n];
        flashAuto = new bool[n];
        for (int i = 0; i < n; i++) spawnStart[i] = -1f;
    }

    void Update()
    {
        if (caster == null || slots == null) return;
        EnsureAnimState();
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView v = slots[i];
            if (v == null) continue;

            // 빈 슬롯 리필 진행도를 dim으로 표현
            SpellMarble m = i < caster.Slots.Count ? caster.Slots[i] : null;
            if (m == null && v.group != null)
                v.group.alpha = Mathf.Lerp(0.25f, 0.6f, caster.RefillProgress(i));

            // 생성 애니 진행
            if (spawnStart[i] >= 0f) TickSpawn(i, v);
        }
    }

    public void Refresh()
    {
        if (caster == null || slots == null) return;
        EnsureAnimState();
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView v = slots[i];
            if (v == null) continue;
            SpellMarble m = i < caster.Slots.Count ? caster.Slots[i] : null;

            if (m != null)
            {
                Sprite s = ResolveIcon(m);                 // 슈트×등급 룩업(또는 개별 icon)
                bool show = s != null && i != suppressedSlot; // 집어든 슬롯은 잠시 숨김
                if (v.icon != null) { v.icon.sprite = s; v.icon.enabled = show; }
                if (v.border != null) v.border.color = GradePalette.ColorOf(m.grade);
                if (v.group != null) v.group.alpha = 1f;

                if (!filledPrev[i] && show) StartSpawn(i, v); // 빈→채움 전이 시 생성 연출
            }
            else
            {
                if (v.icon != null) { v.icon.enabled = false; ResetIconScale(v); }
                if (v.flash != null) v.flash.enabled = false;
                if (v.group != null) v.group.alpha = 0.25f;
                spawnStart[i] = -1f;
            }

            filledPrev[i] = (m != null);
        }
    }

    // 드래그로 집어든 슬롯을 벨트에서 숨김(-1이면 해제) 후 즉시 반영
    public void SetDragSuppressed(int slot)
    {
        suppressedSlot = slot;
        Refresh();
    }

    void StartSpawn(int i, SlotView v)
    {
        spawnStart[i] = Time.unscaledTime;
        if (v.icon != null) v.icon.rectTransform.localScale = Vector3.one * spawnStartScale;

        EnsureFlash(i, v);   // flash 미지정 시 아이콘 자식으로 흰 오버레이 자동 생성
        if (v.flash != null)
        {
            if (flashAuto[i] && v.icon != null) v.flash.sprite = v.icon.sprite; // 자동 플래시는 현재 마블 모양으로
            v.flash.enabled = true;
            Color c = v.flash.color; c.a = 1f; v.flash.color = c;   // 처음엔 새하얗게
        }
    }

    void TickSpawn(int i, SlotView v)
    {
        float p = (Time.unscaledTime - spawnStart[i]) / Mathf.Max(0.0001f, spawnDuration);
        bool done = p >= 1f;
        p = Mathf.Clamp01(p);
        float ease = 1f - (1f - p) * (1f - p);             // easeOutQuad — 톡 튀어나오는 느낌
        float scale = Mathf.Lerp(spawnStartScale, 1f, ease);

        // 아이콘만 스케일(자동 flash는 아이콘 자식이라 함께 커짐 — 이중 적용 방지)
        if (v.icon != null) v.icon.rectTransform.localScale = Vector3.one * scale;
        if (v.flash != null)
        {
            Color c = v.flash.color; c.a = 1f - p; v.flash.color = c;   // 흰색이 서서히 사라지며 아이콘 드러남
            if (done) v.flash.enabled = false;
        }
        if (done)
        {
            if (v.icon != null) v.icon.rectTransform.localScale = Vector3.one;
            spawnStart[i] = -1f;
        }
    }

    // flash 미지정 슬롯에 흰색 오버레이를 아이콘 자식으로 런타임 생성(배선 불필요)
    void EnsureFlash(int i, SlotView v)
    {
        if (v.flash != null || v.icon == null) return;
        GameObject go = new GameObject("AutoFlash", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(v.icon.rectTransform, false);   // 아이콘과 동일한 위치·크기·스케일
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;
        img.enabled = false;                         // StartSpawn에서 켬
        rt.SetAsLastSibling();                        // 아이콘 위에 덧그림
        v.flash = img;
        flashAuto[i] = true;
    }

    void ResetIconScale(SlotView v)
    {
        if (v.icon != null) v.icon.rectTransform.localScale = Vector3.one;
    }

    public RectTransform GetSlotRect(int i) =>
        (slots != null && i >= 0 && i < slots.Length && slots[i] != null) ? slots[i].root : null;

    public int SlotCount => slots != null ? slots.Length : 0;
}
