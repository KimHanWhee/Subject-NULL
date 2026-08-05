using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HP바 아래 활성 버프 표시 — 플레이어의 IBuffDisplay 컴포넌트를 모아 아이콘 + 숫자(남은 초 / ×횟수)로 표기.
// 코드 생성 UI(프로젝트 관례). Canvas 자식으로 배치, 좌상단 앵커. skinTable은 아이콘 폴백용(인스펙터 연결).
public class PlayerBuffHUD : MonoBehaviour
{
    public MarbleSkinTable skinTable;   // 마블 아이콘 폴백(벨트와 동일)
    public int maxChips = 8;
    public float chipSize = 42f;
    public float chipGap = 48f;

    static Font uiFont;
    static Sprite whiteSprite;

    GameObject playerGo;
    RectTransform root;
    readonly List<Chip> chips = new List<Chip>();
    readonly List<IBuffDisplay> buf = new List<IBuffDisplay>();

    class Chip
    {
        public GameObject go;
        public Image icon;
        public Text number;
    }

    void Start()
    {
        root = GetComponent<RectTransform>();
        if (root == null) return;
        for (int i = 0; i < maxChips; i++) chips.Add(BuildChip(i));
    }

    void Update()
    {
        if (root == null) return;
        if (playerGo == null)
        {
            playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null) { HideFrom(0); return; }
        }

        buf.Clear();
        playerGo.GetComponents<IBuffDisplay>(bufTmp);
        for (int i = 0; i < bufTmp.Count; i++)
        {
            IBuffDisplay b = bufTmp[i];
            if (b == null) continue;
            // 패시브는 전용 HUD(PassiveHUD)가 따로 표시한다. 여기 끼면 중복이고,
            // 상시 패시브는 지속시간이 무한이라 남은 초 표기도 의미가 없다.
            if (b.BuffMarble != null && b.BuffMarble.isPassive) continue;
            bool active = b.BuffTimed ? b.BuffRemaining > 0.05f : b.BuffCharges > 0;
            if (active) buf.Add(b);
        }

        int n = Mathf.Min(buf.Count, chips.Count);
        for (int i = 0; i < n; i++)
        {
            Chip c = chips[i];
            IBuffDisplay b = buf[i];
            if (!c.go.activeSelf) c.go.SetActive(true);
            c.icon.sprite = ResolveIcon(b.BuffMarble);
            c.icon.enabled = c.icon.sprite != null;
            if (b.BuffTimed) c.number.text = Mathf.CeilToInt(b.BuffRemaining).ToString();
            else c.number.text = "x" + b.BuffCharges;
        }
        HideFrom(n);
    }

    readonly List<IBuffDisplay> bufTmp = new List<IBuffDisplay>();

    void HideFrom(int from)
    {
        for (int i = from; i < chips.Count; i++)
            if (chips[i].go.activeSelf) chips[i].go.SetActive(false);
    }

    Sprite ResolveIcon(SpellMarble m)
    {
        if (m == null) return null;
        if (m.icon != null) return m.icon;
        if (m.ability != null && m.ability.icon != null) return m.ability.icon;
        return skinTable != null ? skinTable.Get(m.suit, m.grade) : null;
    }

    Chip BuildChip(int index)
    {
        GameObject go = new GameObject("Buff" + index, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(chipSize, chipSize);
        rt.anchoredPosition = new Vector2(index * chipGap, 0f);

        Image bg = AddImage(rt, "BG", new Vector2(chipSize, chipSize), new Color(0f, 0f, 0f, 0.38f));
        bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bg.rectTransform.anchoredPosition = Vector2.zero;

        Image icon = AddImage(rt, "Icon", new Vector2(chipSize - 8f, chipSize - 8f), Color.white);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, 1f);
        icon.preserveAspect = true;

        // 숫자 배경 pill(가독성) + 텍스트
        Image pill = AddImage(rt, "Pill", new Vector2(chipSize, 16f), new Color(0f, 0f, 0f, 0.72f));
        pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        pill.rectTransform.pivot = new Vector2(0.5f, 0f);
        pill.rectTransform.anchoredPosition = new Vector2(0f, -2f);

        Text number = AddText(rt, "Num", new Vector2(chipSize, 18f), 15, TextAnchor.MiddleCenter, "", Color.white);
        number.fontStyle = FontStyle.Bold;
        number.rectTransform.anchorMin = number.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        number.rectTransform.pivot = new Vector2(0.5f, 0f);
        number.rectTransform.anchoredPosition = new Vector2(0f, -3f);

        go.SetActive(false);
        return new Chip { go = go, icon = icon, number = number };
    }

    // ── UI 프리미티브 ──
    static Font UiFont()
    {
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Pretendard-Regular"); // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 필수
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return uiFont;
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    static Image AddImage(RectTransform parent, string name, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text AddText(RectTransform parent, string name, Vector2 size, int fontSize, TextAnchor align, string text, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        Text t = go.AddComponent<Text>();
        t.font = UiFont(); t.fontSize = fontSize; t.alignment = align; t.supportRichText = true;
        t.text = text; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
