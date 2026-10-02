using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("よく使う共通の効果音")]
    [SerializeField] private AudioClip _buttonSound;
    [SerializeField] private AudioClip _cancelSound;
    [SerializeField] private AudioClip _errorSound;
    [SerializeField] private AudioClip _switchSound;

    [SerializeField] private AudioSource _bgmSource; // BGMを鳴らす用のAudioSource

    // フェード処理が重複しないように覚えておく箱
    private Coroutine _fadeCoroutine;


    private const string MasterVolumeKey = "IGOtower.MasterVolume";

    public static SoundManager Instance;

    private static float _masterVolume = 1f;
    private static bool _volumeLoaded;

    private Queue<AudioClip> _seQueue = new Queue<AudioClip>();

    private bool _isPlayingQueue = false;

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

    public void PlayClip(AudioClip clip)
    {
        if (clip != null && seAudioSource != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
    }

    public void PlaySE(AudioClip clip)
    {
        _seQueue.Enqueue(clip);

        if (!_isPlayingQueue)
        {
            StartCoroutine(ProcessSEQueueRoutine());
        }
    }

    private IEnumerator ProcessSEQueueRoutine()
    {
        _isPlayingQueue = true;

        while(_seQueue.Count > 0)
        {
            AudioClip nextClip = _seQueue.Dequeue();

            PlayClip(nextClip);

            yield return new WaitForSeconds(0.15f);
        }
        _isPlayingQueue = false;
    }

    public void PlayBGMWithFadeIn(AudioClip bgmClip, float fadeDuration = 1.0f, bool isLoop = true)
    {
        if (_bgmSource.clip == bgmClip && _bgmSource.isPlaying)
        {
            return; 
        }
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        _bgmSource.clip = bgmClip;
        
        _bgmSource.loop = isLoop; 
        
        _bgmSource.Play();
        
        _fadeCoroutine = StartCoroutine(FadeAudioRoutine(_bgmSource, 0f, 1.0f, fadeDuration));
    }

    private IEnumerator FadeAudioRoutine(AudioSource targetSource, float startVol, float targetVol, float duration)
    {
        float time = 0f;

        // 指定した時間（duration）が経過するまでループ
        while (time < duration)
        {
            time += Time.deltaTime;
            // Lerp：スタート値とゴール値の間を、時間の割合（time / duration）で滑らかに繋ぐ魔法の式
            targetSource.volume = Mathf.Lerp(startVol, targetVol, time / duration);
            
            yield return null; // 1フレーム待つ（これがないと一瞬で終わっちゃう）
        }

        // 最後に確実にターゲットの音量にする
        targetSource.volume = targetVol;

        // フェードアウト（音量が0になった）なら、完全に再生を止める！
        if (targetVol <= 0f)
        {
            targetSource.Stop();
            targetSource.volume = 1.0f; // 次に再生する時のために音量を1に戻しておく
        }
    }

    public void PlayButtonSE()
    {
        PlaySE(_buttonSound);
    }

    public void PlayCancelSE()
    {
        PlaySE(_cancelSound);
    }

    public void PlayErrorSE()
    {
        PlaySE(_errorSound);
    }
    public void PlaySwitchSE()
    {
        PlaySE(_switchSound);
    }
}
