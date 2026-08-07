using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private const string MasterVolumeKey = "IGOtower.MasterVolume";

    public static SoundManager Instance;

    private static float _masterVolume = 1f;
    private static bool _volumeLoaded;

    public static float MasterVolume
    {
        get
        {
            LoadMasterVolume();
            return _masterVolume;
        }
    }

    [Header("SE再生用スピーカー")]
    public AudioSource seAudioSource;

    private void Awake()
    {
        // 自分が唯一の存在になるように設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplySavedMasterVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        _volumeLoaded = true;
        AudioListener.volume = _masterVolume;
        PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolume);
        PlayerPrefs.Save();
    }

    private static void ApplySavedMasterVolume()
    {
        LoadMasterVolume();
        AudioListener.volume = _masterVolume;
    }

    private static void LoadMasterVolume()
    {
        if (_volumeLoaded) return;

        _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        _volumeLoaded = true;
    }

    public void PlaySE(AudioClip clip)
    {
        if (clip != null && seAudioSource != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
    }
}
