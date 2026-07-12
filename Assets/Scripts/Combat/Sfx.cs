using UnityEngine;

// 2D 원샷 사운드 헬퍼 — AudioSource.PlayClipAtPoint는 3D(거리 감쇠)라
// 리스너(카메라)에서 멀면 소리가 급격히 작아진다. 효과음은 이걸로 재생할 것.
public static class Sfx
{
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        GameObject go = new GameObject("One shot 2D audio");
        AudioSource a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.volume = Mathf.Clamp01(volume);
        a.spatialBlend = 0f; // 2D — 위치 무관 동일 음량
        a.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }
}
