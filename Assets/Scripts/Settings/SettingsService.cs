using UnityEngine;

// 게임 설정(음량·언어) — PlayerPrefs에 저장하고 시작 시 자동 적용.
//
// 음량은 AudioListener.volume 하나로 처리한다. 이 게임은 효과음이
// Sfx.Play2D(임시 AudioSource) · 적/플레이어 개별 AudioSource · BGM(MapThemeController)로
// 흩어져 있어서, 리스너 단계에서 한 번에 거는 것이 누락 없이 확실하다.
public static class SettingsService
{
    const string VolumeKey = "Settings.MasterVolume";
    const string LangKey = "Settings.Language";

    public static float MasterVolume { get; private set; } = 1f;

    static bool loaded;

    // 씬 로드 전에 적용 — 첫 화면의 소리부터 설정이 반영되도록.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        AudioListener.volume = MasterVolume;

        int lang = PlayerPrefs.GetInt(LangKey, (int)Language.Ko);
        Loc.SetLanguage(lang == (int)Language.En ? Language.En : Language.Ko);
    }

    public static void SetMasterVolume(float v)
    {
        MasterVolume = Mathf.Clamp01(v);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        PlayerPrefs.Save();
    }

    public static void SetLanguage(Language lang)
    {
        Loc.SetLanguage(lang);
        PlayerPrefs.SetInt(LangKey, (int)lang);
        PlayerPrefs.Save();
    }
}
