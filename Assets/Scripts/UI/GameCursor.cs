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

    // 화면에 그려질 커서 한 변(px). 원본은 64인데 카메라 시야를 5 → 6.5로 넓히면서
    // 화면 요소가 상대적으로 작아져, 커서만 커 보이던 것을 같은 비율로 줄인다.
    // 원본 에셋은 그대로 두고 런타임에 축소본을 만든다 — 크기를 바꾸려면 이 값만 고치면 된다.
    const int CursorSize = 48;

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
        // 터치 기기에는 커서 자체가 없다 — 십자선을 걸어봐야 보이지 않고,
        // 마우스가 붙은 기기에서만 의미가 있다.
        if (sceneName == GameSceneName && !GameInput.TouchMode) SetCrosshair();
        else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 기본 포인터로 복귀
    }

    static void SetCrosshair()
    {
        if (!loaded)
        {
            loaded = true;
            Texture2D src = Resources.Load<Texture2D>(CursorPath);
            if (src == null)
                Debug.LogWarning("[GameCursor] Resources/" + CursorPath + " 를 찾지 못해 기본 커서를 유지합니다.");
            else
                cursorTex = (src.width == CursorSize) ? src : Downscale(src, CursorSize);
        }
        if (cursorTex == null) return;

        // 십자선은 중앙이 조준점 — 핫스팟을 정확히 가운데로 둬야 클릭 지점과 그림이 일치한다.
        Vector2 hotspot = new Vector2(cursorTex.width * 0.5f, cursorTex.height * 0.5f);
        Cursor.SetCursor(cursorTex, hotspot, CursorMode.Auto);
    }

    // 커서 축소본 생성. 원본 임포트 설정(Cursor 타입)은 건드리지 않고 사본만 만든다.
    //
    // 직접 면적 평균(box filter)을 쓰는 이유:
    //   GetPixelBilinear는 반텍셀 오프셋 규약 때문에 64→48처럼 정수배가 아닌 축소에서
    //   좌우 대칭이 깨진다(실측 149px 불일치). 십자선은 비대칭이면 조준점이 어긋나 보인다.
    //   출력 픽셀이 덮는 입력 구간을 그대로 적분하면 구간이 좌우 대칭이라 결과도 대칭이다.
    //
    // RGB는 알파로 가중 평균한다 — 투명 픽셀의 RGB(보통 검정)를 그냥 섞으면 가장자리가 어두워진다.
    static Texture2D Downscale(Texture2D src, int size)
    {
        int n = src.width;
        Color[] sp = src.GetPixels();
        Color[] px = new Color[size * size];
        float step = (float)n / size;

        for (int y = 0; y < size; y++)
        {
            float y0 = y * step, y1 = (y + 1) * step;
            for (int x = 0; x < size; x++)
            {
                float x0 = x * step, x1 = (x + 1) * step;
                float r = 0f, g = 0f, b = 0f, a = 0f, wsum = 0f;

                for (int sy = Mathf.FloorToInt(y0); sy < Mathf.CeilToInt(y1); sy++)
                {
                    float wy = Mathf.Min(y1, sy + 1f) - Mathf.Max(y0, sy);
                    if (wy <= 0f) continue;
                    for (int sx = Mathf.FloorToInt(x0); sx < Mathf.CeilToInt(x1); sx++)
                    {
                        float wx = Mathf.Min(x1, sx + 1f) - Mathf.Max(x0, sx);
                        if (wx <= 0f) continue;
                        Color c = sp[Mathf.Clamp(sy, 0, n - 1) * n + Mathf.Clamp(sx, 0, n - 1)];
                        float w = wx * wy;
                        r += c.r * c.a * w; g += c.g * c.a * w; b += c.b * c.a * w;
                        a += c.a * w; wsum += w;
                    }
                }

                if (wsum <= 0f) { px[y * size + x] = Color.clear; continue; }
                float outA = a / wsum;
                px[y * size + x] = outA > 0.0001f
                    ? new Color(r / a, g / a, b / a, outA)   // 알파 가중 해제
                    : Color.clear;
            }
        }

        Texture2D dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
        dst.filterMode = FilterMode.Bilinear;
        dst.SetPixels(px);
        dst.Apply();
        return dst;
    }
}
