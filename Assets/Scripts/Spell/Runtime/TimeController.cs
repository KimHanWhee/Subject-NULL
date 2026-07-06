using System.Collections.Generic;
using UnityEngine;

// Design Ref: §4.1 — Time.timeScale 단일 진실원.
// 홀드형 소스(선택 모드)와 펄스형 소스(니어미스)의 최소 scale을 적용, 소스가 비면 1.0로 복구.
// 이로써 니어미스 슬로우와 선택 슬로우가 충돌 없이 공존(Plan §6.2 리스크 해결).
// Plan SC: FR-08(홀드 슬로우) / FR-07(확실한 복구)
public class TimeController : MonoBehaviour
{
    public static TimeController Instance { get; private set; }

    private float defaultFixedDelta;
    private readonly Dictionary<int, float> holds = new Dictionary<int, float>();
    private int nextHandle = 1;

    private struct PulseEntry { public float scale; public float endUnscaled; }
    private readonly List<PulseEntry> pulses = new List<PulseEntry>();

    private float appliedScale = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        defaultFixedDelta = Time.fixedDeltaTime;
    }

    // 홀드형 슬로우 등록(선택 모드). 반환 핸들로 해제.
    public int PushHold(float scale)
    {
        int handle = nextHandle++;
        holds[handle] = scale;
        Apply();
        return handle;
    }

    public void PopHold(int handle)
    {
        if (holds.Remove(handle)) Apply();
    }

    // 시간제 슬로우(니어미스). unscaled 기준으로 자동 만료.
    public void Pulse(float scale, float duration)
    {
        pulses.Add(new PulseEntry { scale = scale, endUnscaled = Time.unscaledTime + duration });
        Apply();
    }

    void Update()
    {
        if (pulses.Count == 0) return;

        bool changed = false;
        for (int i = pulses.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime >= pulses[i].endUnscaled)
            {
                pulses.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) Apply();
    }

    // 활성 소스들의 최소 scale 적용(가장 강한 슬로우 우선). 없으면 1.0.
    void Apply()
    {
        float scale = 1f;
        foreach (var s in holds.Values)
            if (s < scale) scale = s;
        for (int i = 0; i < pulses.Count; i++)
            if (pulses[i].scale < scale) scale = pulses[i].scale;

        if (Mathf.Approximately(scale, appliedScale)) return;
        appliedScale = scale;
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDelta * scale; // 물리도 비례(끊김 방지)
    }

    // 안전망: 비활성/씬 전환 시 시간 정상화.
    void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        if (Instance == this) Instance = null;
    }
}
