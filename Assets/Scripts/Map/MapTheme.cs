using UnityEngine;
using UnityEngine.Tilemaps;

// 맵 분위기(테마) 정의 — 같은 맵 레이아웃에 "옷"만 갈아입히는 데이터.
// 타일 아트 없이도 틴트+오버레이+환경 파티클+BGM만으로 분위기 전환이 가능하고,
// 테마 전용 타일셋이 생기면 fromTiles/toTiles 쌍으로 교체까지 확장한다.
// 에셋 위치: Assets/Resources/MapThemes/ (MapThemeController가 LoadAll로 수집)
[CreateAssetMenu(fileName = "MapTheme", menuName = "SubjectNull/Map Theme")]
public class MapTheme : ScriptableObject
{
    // 환경 파티클 스타일 — MapThemeController가 절차 생성(프리팹 불필요)
    public enum AmbientStyle
    {
        None = 0,
        Snow = 1,      // 위→아래로 흩날리는 눈
        Embers = 2,    // 아래→위로 떠오르는 불씨
        Fireflies = 3, // 화면 전체를 느리게 떠다니는 반딧불
        Ash = 4        // 위→아래로 천천히 지는 재
    }

    [Header("Info")]
    public string displayName = "테마";

    [Header("Tint (white = 변경 없음)")]
    public Color floorTint = Color.white; // Floor 타일맵 곱연산 틴트
    public Color wallTint = Color.white;  // Wall 타일맵 곱연산 틴트

    [Header("Camera Background")]
    public bool overrideBackground = false;
    public Color backgroundColor = Color.black;

    [Header("Screen Overlay (alpha 0 = 없음)")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0f); // 화면 전체 분위기막(적/플레이어 포함 톤 통일)

    [Header("Ambient Particles")]
    public AmbientStyle ambient = AmbientStyle.None;
    public Color ambientColor = Color.white;

    [Header("Tile Swap (선택 — 테마 전용 타일셋이 생기면 쌍으로 등록)")]
    public TileBase[] fromTiles; // 기본 타일
    public TileBase[] toTiles;   // 교체 타일(같은 인덱스끼리 쌍)

    [Header("BGM (선택)")]
    public AudioClip bgm;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
}
