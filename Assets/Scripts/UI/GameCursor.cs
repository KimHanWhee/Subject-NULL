using UnityEngine;
using UnityEngine.SceneManagement;

// 마우스 커서 교체 — 게임 화면에서는 조준용 십자선, 메뉴에서는 기본 포인터.
//
// 게임에서만 바꾸는 이유: 십자선은 "쏘는 곳"을 가리키는 도구라 버튼을 누르는 메뉴에서는
// 오히려 클릭 지점이 헷갈린다. 메뉴는 OS 기본 포인터가 가장 정확하다.
//
// 씬 배선 불필요(프로젝트의 런타임 UI 생성 관례).
public static class GameCursor
{
    const string CursorPath = "UI/Cursor";   // Resources/UI/Cursor.png
    const string GameSceneName = "GameScene";

    static Texture2D cursorTex;
    static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) { Apply(s.name); }

    static void Apply(string sceneName)
    {
        if (sceneName == GameSceneName) SetCrosshair();
        else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 기본 포인터로 복귀
    }

    static void SetCrosshair()
    {
        if (!loaded)
        {
            loaded = true;
            cursorTex = Resources.Load<Texture2D>(CursorPath);
            if (cursorTex == null)
                Debug.LogWarning("[GameCursor] Resources/" + CursorPath + " 를 찾지 못해 기본 커서를 유지합니다.");
        }
        if (cursorTex == null) return;

        // 십자선은 중앙이 조준점 — 핫스팟을 정확히 가운데로 둬야 클릭 지점과 그림이 일치한다.
        Vector2 hotspot = new Vector2(cursorTex.width * 0.5f, cursorTex.height * 0.5f);
        Cursor.SetCursor(cursorTex, hotspot, CursorMode.Auto);
    }
}
