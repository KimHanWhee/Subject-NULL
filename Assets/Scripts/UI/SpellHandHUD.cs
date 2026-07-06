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
        public CanvasGroup group;   // dim(빈 슬롯) — 선택
    }

    public SpellCaster caster;
    public SlotView[] slots;        // 인스펙터에 5개 배치

    void OnEnable()
    {
        if (caster != null) caster.OnHandChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (caster != null) caster.OnHandChanged -= Refresh;
    }

    void Update()
    {
        // 빈 슬롯 리필 진행도를 dim으로 표현
        if (caster == null || slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView v = slots[i];
            if (v == null || v.group == null) continue;
            SpellMarble m = i < caster.Slots.Count ? caster.Slots[i] : null;
            if (m == null) v.group.alpha = Mathf.Lerp(0.25f, 0.6f, caster.RefillProgress(i));
        }
    }

    public void Refresh()
    {
        if (caster == null || slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView v = slots[i];
            if (v == null) continue;
            SpellMarble m = i < caster.Slots.Count ? caster.Slots[i] : null;

            if (m != null)
            {
                if (v.icon != null) { v.icon.sprite = m.icon; v.icon.enabled = m.icon != null; }
                if (v.border != null) v.border.color = GradePalette.ColorOf(m.grade);
                if (v.group != null) v.group.alpha = 1f;
            }
            else
            {
                if (v.icon != null) v.icon.enabled = false;
                if (v.group != null) v.group.alpha = 0.25f;
            }
        }
    }

    public RectTransform GetSlotRect(int i) =>
        (slots != null && i >= 0 && i < slots.Length && slots[i] != null) ? slots[i].root : null;

    public int SlotCount => slots != null ? slots.Length : 0;
}
