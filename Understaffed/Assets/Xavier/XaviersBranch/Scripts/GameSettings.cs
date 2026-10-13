using UnityEngine;
using UnityEngine.Audio;

public static class GameSettings
{
    private const string MasterKey = "Settings_Master";
    private const string MusicKey = "Settings_Music";
    private const string FxKey = "Settings_Fx";

    //match names in the mixer
    private const string MasterParam = "Master";
    private const string MusicParam = "Music";
    private const string FxParam = "FX";

    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 1f;
    public static float Fx { get; private set; } = 1f;

    private static AudioMixer mixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

    //Find Mixer and load settings from PlayerPrefs
    private static void Init()
    {
        mixer = Resources.Load<AudioMixer>("MasterAudio");   // Assets/Resources/MainMixer.mixer
        Master = PlayerPrefs.GetFloat(MasterKey, 1f);
        Music = PlayerPrefs.GetFloat(MusicKey, 1f);
        Fx = PlayerPrefs.GetFloat(FxKey, 1f);
        Apply();
    }

    //Convert float to decibels
    private static float ToDb(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;

    public static void Apply()
    {
        if (mixer == null) return;
        mixer.SetFloat(MasterParam, ToDb(Master));
        mixer.SetFloat(MusicParam, ToDb(Music));
        mixer.SetFloat(FxParam, ToDb(Fx));
    }

    public static void SetMaster(float v) { Master = Mathf.Clamp01(v); Apply(); }
    public static void SetMusic(float v) { Music = Mathf.Clamp01(v); Apply(); }
    public static void SetFx(float v) { Fx = Mathf.Clamp01(v); Apply(); }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, Master);
        PlayerPrefs.SetFloat(MusicKey, Music);
        PlayerPrefs.SetFloat(FxKey, Fx);
        PlayerPrefs.Save();
    }
}