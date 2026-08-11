using System.Collections.Generic;
using UnityEngine;

// 2D 원샷 사운드 헬퍼 — AudioSource.PlayClipAtPoint는 3D(거리 감쇠)라
// 리스너(카메라)에서 멀면 소리가 급격히 작아진다. 효과음은 이걸로 재생할 것.
//
// ⚠️ 겹침 제어가 핵심이다.
// 같은 소리가 동시에 N개 나면 진폭이 그대로 N배로 더해져 순식간에 귀가 아파진다
// (♠ 아포칼립스처럼 수십 마리가 한 번에 죽을 때 실제로 그랬다).
// 그렇다고 하나만 남기면 대량 처치의 쾌감이 사라지므로, 여러 개는 허용하되
//   ① 동시 재생 수를 제한하고
//   ② 겹칠수록 뒤에 오는 소리를 점점 작게
// 해서 "여러 마리가 죽었다"는 느낌은 남기고 총량만 잡는다.
public static class Sfx
{
    // 같은 클립이 동시에 이만큼까지만 울린다. 넘으면 무시.
    const int MaxVoicesPerClip = 5;

    // 겹칠 때마다 이 비율로 작아진다(1개=100%, 2개=65%, 3개=42% ...).
    // 합계가 발산하지 않도록 1보다 충분히 작게.
    const float StackFalloff = 0.65f;

    // 같은 클립이 이 시간 안에 다시 울리면 한 번에 난 것으로 본다
    const float StackWindow = 0.12f;

    class Voice { public int count; public float lastTime; }
    static readonly Dictionary<AudioClip, Voice> voices = new Dictionary<AudioClip, Voice>();

    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        float vol = Mathf.Clamp01(volume) * StackVolume(clip);
        if (vol <= 0.02f) return; // 이 이하는 들리지도 않으면서 오디오 보이스만 잡아먹는다

        GameObject go = new GameObject("One shot 2D audio");
        AudioSource a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.volume = vol;
        a.spatialBlend = 0f; // 2D — 위치 무관 동일 음량
        a.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }

    // 이번 재생에 줄 음량 배율. 겹침 창 안에서 몇 번째인지로 정한다.
    static float StackVolume(AudioClip clip)
    {
        Voice v;
        if (!voices.TryGetValue(clip, out v)) { v = new Voice(); voices[clip] = v; }

        // 창을 벗어났으면 처음부터 다시 센다(간간이 나는 소리는 항상 제 음량)
        if (Time.unscaledTime - v.lastTime > StackWindow) v.count = 0;
        v.lastTime = Time.unscaledTime;

        if (v.count >= MaxVoicesPerClip) return 0f;
        float mul = Mathf.Pow(StackFalloff, v.count);
        v.count++;
        return mul;
    }
}
