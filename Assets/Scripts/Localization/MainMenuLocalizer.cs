using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 메인 메뉴의 씬 배치 버튼 라벨을 현재 언어로 교체한다.
// 이 버튼들은 씬에 한국어 텍스트가 박혀 있어(에셋), 코드에서 런타임으로 덮어써야 한다.
// 언어를 바꾸면 즉시 다시 칠한다.
public class MainMenuLocalizer : MonoBehaviour
{
    const string Scene = "MainMenuScene";

    // 씬 오브젝트 이름 → 번역 키
    static readonly string[,] Map =
    {
        { "GameStart",    "menu.start" },
        { "DeckBuilding", "menu.deck" },
        { "Gacha",        "menu.gacha" },
        { "HowToPlay",    "menu.howto" },
        { "RankingButton","menu.ranking" },
        { "SettingsButton","menu.settings" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (s, _) => { if (s.name == Scene) Create(); };
        if (SceneManager.GetActiveScene().name == Scene) Create();
    }

    static void Create()
    {
        if (FindFirstObjectByType<MainMenuLocalizer>() != null) return;
        new GameObject("MainMenuLocalizer").AddComponent<MainMenuLocalizer>();
    }

    void Start()
    {
        Loc.OnChanged += Apply;
        StartCoroutine(ApplyWhenReady());
    }

    void OnDestroy() { Loc.OnChanged -= Apply; }

    // 랭킹/설정 버튼은 코루틴으로 나중에 생성되므로 잠시 재시도한다.
    IEnumerator ApplyWhenReady()
    {
        float deadline = Time.unscaledTime + 6f;
        while (Time.unscaledTime < deadline)
        {
            Apply();
            yield return null;
        }
    }

    void Apply()
    {
        for (int i = 0; i < Map.GetLength(0); i++)
        {
            Transform t = FindInScene(Map[i, 0]);
            if (t == null) continue;
            TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) continue;
            string want = Loc.T(Map[i, 1]);
            if (label.text != want) label.text = want;
        }
    }

    // 활성 씬 루트에서만 탐색 — LoadingOverlay(DontDestroyOnLoad)를 잡지 않게
    static Transform FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindDeep(root.transform, name);
            if (t != null) return t;
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
}
