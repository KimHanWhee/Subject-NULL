using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 진입 연출 — 4개 버튼이 오른쪽에서 위→아래 순서로 슬라이드 인.
// 동시에 각 버튼(+계정)에 MenuButtonFx(호버 글로우/사운드)를 런타임 부착.
// 씬엔 이 컴포넌트를 가진 GameObject 하나만 있으면 된다(버튼은 이름으로 탐색).
public class MainMenuIntro : MonoBehaviour
{
    // 슬라이드 순서(위 → 아래). 씬 버튼 오브젝트 이름과 일치해야 함.
    static readonly string[] Order = { "GameStart", "DeckBuilding", "Gacha", "HowToPlay" };

    [Header("Slide")]
    public float slideDistance = 1200f; // 시작 오프셋(오른쪽 화면 밖)
    public float slideDuration = 0.42f;
    public float stagger = 0.11f;        // 버튼 간 등장 시간차

    AudioClip hoverClip;

    void Start()
    {
        hoverClip = Resources.Load<AudioClip>("Sounds/UIHover");

        for (int i = 0; i < Order.Length; i++)
        {
            Transform tf = FindInScene(Order[i]);
            if (tf == null) continue;
            AddFx(tf.gameObject);
            StartCoroutine(SlideIn((RectTransform)tf, i * stagger));
        }

        // 계정 버튼: 호버 FX만(슬라이드 제외)
        Transform acc = FindInScene("AccountButton");
        if (acc != null) AddFx(acc.gameObject);
    }

    // 활성 씬의 루트에서만 탐색.
    // ⚠️ Canvas 기준으로 찾으면 안 된다 — 재방문 시 LoadingOverlay(DontDestroyOnLoad)의 캔버스가
    //    먼저 잡혀 메뉴 버튼을 하나도 못 찾고, 호버 FX가 통째로 사라진다.
    static Transform FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindDeep(root.transform, name);
            if (t != null) return t;
        }
        return null;
    }

    void AddFx(GameObject go)
    {
        MenuButtonFx fx = go.GetComponent<MenuButtonFx>();
        if (fx == null) fx = go.AddComponent<MenuButtonFx>();
        fx.Init(hoverClip);
    }

    IEnumerator SlideIn(RectTransform rt, float delay)
    {
        Vector2 target = rt.anchoredPosition;
        Vector2 start = target + new Vector2(slideDistance, 0f);
        rt.anchoredPosition = start; // 즉시 화면 밖으로 숨김(첫 프레임 깜빡임 방지)

        float t = 0f;
        while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }

        t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / slideDuration);
            k = 1f - Mathf.Pow(1f - k, 3f); // ease-out cubic
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        rt.anchoredPosition = target;
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
}
