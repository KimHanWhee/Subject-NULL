using System.Collections.Generic;
using UnityEngine;

// 플레이어 이동속도 배율의 단일 관리자.
//
// 왜 필요한가: 예전에는 각 버프(Sniper·Adrenaline·GhostStep·SpeedBoost)가
//   "현재 pc.speed"를 원본으로 캐시하고 끝날 때 되돌렸다. 두 버프가 겹치면
//   나중에 시작한 쪽이 "이미 감소된 speed"를 원본으로 기억해 버려서,
//   해제 순서에 따라 이동속도가 영구히 낮아지는 버그가 있었다.
//   (예: 스나이퍼 → 고스트스텝 순으로 걸면 최종 speed가 절반으로 고정)
//
// 해결: pc.speed는 씬의 원본값 그대로 두고, 각 버프는 여기에 "배율"만 등록한다.
//   최종 이동속도 = speed × 모든 배율의 곱. 등록/해제 순서와 무관하게 항상 정확하다.
//   CameraZoom과 동일한 요청/해제 패턴.
public static class PlayerSpeedModifiers
{
    static readonly Dictionary<Object, float> mults = new Dictionary<Object, float>();
    static readonly List<Object> stale = new List<Object>();

    public static void Set(Object owner, float multiplier)
    {
        if (owner == null) return;
        mults[owner] = Mathf.Max(0.01f, multiplier);
    }

    public static void Clear(Object owner)
    {
        if (owner == null) return;
        mults.Remove(owner);
    }

    // 판이 바뀌면 남은 배율을 모두 버린다(씬 전환 안전망).
    public static void ResetAll() { mults.Clear(); }

    // 파괴된 등록자를 걸러내며 곱을 구한다.
    public static float Current
    {
        get
        {
            float m = 1f;
            stale.Clear();
            foreach (KeyValuePair<Object, float> kv in mults)
            {
                if (kv.Key == null) { stale.Add(kv.Key); continue; }
                m *= kv.Value;
            }
            for (int i = 0; i < stale.Count; i++) mults.Remove(stale[i]);
            return m;
        }
    }
}
