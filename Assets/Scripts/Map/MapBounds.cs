using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 맵 경계벽 — 플레이 영역 바깥으로 나가지 못하게 4면에 정적 충돌체를 세운다.
//
// 왜 필요한가: Wall 타일맵에는 장식용 타일 몇 개뿐이라 맵 가장자리에 충돌 형상이 없었다.
// 그래서 적에게 밀리거나 그냥 걸어가는 것만으로도 맵 밖으로 빠져나갈 수 있었다.
//
// 태그를 "Wall"로 다는 이유: 총알 소멸(Bullet/EnemyBullet), 레이저·레일건 사거리 계산이
// 모두 "Wall" 태그를 기준으로 하므로, 경계도 같은 규약을 따르면 전부 자연스럽게 맞물린다.
public class MapBounds : MonoBehaviour
{
    const string GameSceneName = "GameScene";
    const float Thickness = 2f;   // 충분히 두꺼워야 빠른 충돌에도 통과하지 않는다
    const float FallbackHalfW = 22.5f, FallbackHalfH = 12.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (s, _) => { if (s.name == GameSceneName) Create(); };
        if (SceneManager.GetActiveScene().name == GameSceneName) Create();
    }

    static void Create()
    {
        if (FindFirstObjectByType<MapBounds>() != null) return;
        new GameObject("MapBounds").AddComponent<MapBounds>();
    }

    void Start() { Build(); }

    void Build()
    {
        Bounds area = FloorBounds();

        // 안쪽 면이 정확히 바닥 경계에 오도록 두께의 절반만큼 바깥으로 민다
        float halfT = Thickness * 0.5f;
        MakeWall("Bound_Left",   new Vector2(area.min.x - halfT, area.center.y), new Vector2(Thickness, area.size.y + Thickness * 2f));
        MakeWall("Bound_Right",  new Vector2(area.max.x + halfT, area.center.y), new Vector2(Thickness, area.size.y + Thickness * 2f));
        MakeWall("Bound_Bottom", new Vector2(area.center.x, area.min.y - halfT), new Vector2(area.size.x + Thickness * 2f, Thickness));
        MakeWall("Bound_Top",    new Vector2(area.center.x, area.max.y + halfT), new Vector2(area.size.x + Thickness * 2f, Thickness));
    }

    void MakeWall(string name, Vector2 center, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        go.tag = "Wall";           // 총알 소멸·빔 사거리 계산이 이 태그를 본다
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        // Rigidbody 없음 = 정적 충돌체. 움직이지 않으므로 물리 비용이 없다.
    }

    // 바닥 타일맵의 실제 범위. 없으면 보수적인 기본값.
    static Bounds FloorBounds()
    {
        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.name != "Floor") continue;
            tm.CompressBounds();
            Bounds lb = tm.localBounds;
            Vector3 c = tm.transform.TransformPoint(lb.center);
            return new Bounds(c, lb.size);
        }
        return new Bounds(Vector3.zero, new Vector3(FallbackHalfW * 2f, FallbackHalfH * 2f, 1f));
    }
}
