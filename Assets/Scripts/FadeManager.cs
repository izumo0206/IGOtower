using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance; // どこからでも呼べるようにする
    public CanvasGroup canvasGroup;     // さっきの Canvas Group を紐付ける
    public float fadeDuration = 0.5f;   // 暗転にかかる時間

    private void Awake()
    {
        // シーンが変わってもこのマネージャーが消えないようにする魔法（シングルトン）
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

    // 外部から呼ぶ関数：指定したシーンへフワッと移動する
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        canvasGroup.blocksRaycasts = true;
        // 1. フワッと暗くする（Fade Out）
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        // 2. シーンを裏で読み込む
        yield return SceneManager.LoadSceneAsync(sceneName);

        // 3. フワッと明るくする（Fade In）
        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1 - (elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.blocksRaycasts = false;
    }
}