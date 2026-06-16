using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("SE再生用スピーカー")]
    public AudioSource seAudioSource;

    private void Awake()
    {
        // 自分が唯一の存在になるように設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySE(AudioClip clip)
    {
        if (clip != null && seAudioSource != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
    }
}