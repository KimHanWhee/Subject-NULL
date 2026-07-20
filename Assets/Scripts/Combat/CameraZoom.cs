using System.Collections.Generic;
using UnityEngine;

// 카메라 시야 확대 요청 관리 — 여러 버프(스나이퍼/비격진천뢰/레일건)가 동시에 걸려도
// 가장 큰 배율 하나로 수렴하고, 전부 해제되면 원래 크기로 부드럽게 복귀한다.
//
// 사용법: 버프 시작 시 CameraZoom.Request(this, 1.35f), 종료 시 CameraZoom.Release(this).
// Release를 못 부르고 파괴되는 경우(풀링·씬 전환)도 LateUpdate에서 자동 정리한다.
public class CameraZoom : MonoBehaviour
{
    const float LerpSpeed = 4f;      // 클수록 빠르게 줌
    const float SnapEpsilon = 0.001f;

    static readonly Dictionary<Object, float> requests = new Dictionary<Object, float>();
    static readonly List<Object> stale = new List<Object>();
    static CameraZoom driver;

    private Camera cam;
    private float baseSize;

    public static void Request(Object owner, float multiplier)
    {
        if (owner == null) return;
        if (!EnsureDriver()) return;
        requests[owner] = Mathf.Max(1f, multiplier);
    }

    public static void Release(Object owner)
    {
        if (owner == null) return;
        requests.Remove(owner);
    }

    static bool EnsureDriver()
    {
        if (driver != null) return true;
        Camera main = Camera.main;
        if (main == null) return false;
        driver = main.GetComponent<CameraZoom>();
        if (driver == null) driver = main.gameObject.AddComponent<CameraZoom>();
        return true;
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        baseSize = cam.orthographicSize; // 씬의 기본 시야를 기준으로 삼는다
        driver = this;
    }

    void OnDestroy()
    {
        if (driver != this) return;
        driver = null;
        requests.Clear(); // 씬이 바뀌면 이전 씬의 요청은 무의미
    }

    void LateUpdate()
    {
        if (cam == null) return;

        float target = baseSize * MaxMultiplier();
        // 시간 정지(타임스톱·스펠 선택 중)에도 줌은 움직여야 하므로 unscaled
        float k = 1f - Mathf.Exp(-LerpSpeed * Time.unscaledDeltaTime);
        float next = Mathf.Lerp(cam.orthographicSize, target, k);
        cam.orthographicSize = Mathf.Abs(next - target) < SnapEpsilon ? target : next;
    }

    // 파괴된 요청자(버프 컴포넌트)를 걸러내며 최대 배율을 구한다
    static float MaxMultiplier()
    {
        float max = 1f;
        stale.Clear();
        foreach (KeyValuePair<Object, float> kv in requests)
        {
            if (kv.Key == null) { stale.Add(kv.Key); continue; }
            if (kv.Value > max) max = kv.Value;
        }
        for (int i = 0; i < stale.Count; i++) requests.Remove(stale[i]);
        return max;
    }
}
