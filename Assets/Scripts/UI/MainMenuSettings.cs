using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 메인 메뉴 우상단에 "설정" 버튼을 런타임으로 추가(랭킹 버튼 바로 아래).
// 씬 배선 없이 동작하도록 MainMenuRanking과 동일한 패턴을 따른다.
public class MainMenuSettings : MonoBehaviour
{
    const string Scene = "MainMenuScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (s, _) => { if (s.name == Scene) Create(); };
        if (SceneManager.GetActiveScene().name == Scene) Create();
    }

    static void Create()
    {
        if (FindFirstObjectByType<MainMenuSettings>() != null) return;
        new GameObject("MainMenuSettings").AddComponent<MainMenuSettings>();
    }

    void Start() { StartCoroutine(AttachWhenReady()); }

    // 계정/랭킹 버튼이 준비된 뒤에 붙는다(생성 순서에 의존하지 않게 재시도).
    IEnumerator AttachWhenReady()
    {
        float deadline = Time.unscaledTime + 6f;
        GameObject anchor = FindByName("AccountButton");
        while (anchor == null && Time.unscaledTime < deadline)
        {
            yield return null;
            anchor = FindByName("AccountButton");
        }
        if (anchor == null) yield break;
        BuildButton(anchor);
    }

    // 활성 씬의 루트에서만 탐색 — LoadingOverlay(DontDestroyOnLoad) 캔버스를 잡지 않게.
    static GameObject FindByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindDeep(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    void BuildButton(GameObject anchor)
    {
        GameObject go = new GameObject("SettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(anchor.transform.parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(220f, 64f);
        rt.anchoredPosition = new Vector2(-30f, -178f); // 랭킹(-30,-104, h64) 바로 아래

        Image img = go.GetComponent<Image>();
        Sprite chrome = Resources.Load<Sprite>("UI/SubjectNullButton");
        if (chrome != null) { img.sprite = chrome; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.2f, 0.24f, 0.34f, 0.95f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(SettingsUI.Open);

        go.AddComponent<MenuButtonFx>().Init(Resources.Load<AudioClip>("Sounds/UIHover"));

        GameObject tg = new GameObject("Label", typeof(RectTransform));
        tg.transform.SetParent(rt, false);
        TextMeshProUGUI t = tg.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
        if (f != null) t.font = f;
        t.fontSize = 26;
        t.color = new Color(0.88f, 0.95f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        RectTransform trt = t.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = rt.sizeDelta;
        trt.anchoredPosition = Vector2.zero;

        // 언어 전환 시 라벨도 함께 바뀌도록
        System.Action refresh = () => { if (t != null) t.text = Loc.T("menu.settings"); };
        Loc.OnChanged += refresh;
        LocBinder.Attach(go, refresh);
        refresh();
    }
}

// 오브젝트가 파괴될 때 Loc.OnChanged 구독을 해제해 주는 작은 도우미.
// (해제를 잊으면 파괴된 UI를 갱신하려다 예외가 난다)
public class LocBinder : MonoBehaviour
{
    System.Action handler;

    public static void Attach(GameObject go, System.Action handler)
    {
        LocBinder b = go.AddComponent<LocBinder>();
        b.handler = handler;
    }

    void OnDestroy() { if (handler != null) Loc.OnChanged -= handler; }
}
