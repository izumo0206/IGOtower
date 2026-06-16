using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("効果音")]
    public AudioClip buttonSound;
    public AudioClip switchSound;
    // メインモードのボタンが押された時
    public void OnPuzzleModeClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(switchSound);
        }
        FadeManager.Instance.TransitionToScene("SelectStage");
    }

    public void OnExtraModeClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(buttonSound);
        }
        Debug.Log("いつかやる");
    }
}