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

        int isCleared = PlayerPrefs.GetInt("IsTutorialCleared", 0);

        if (isCleared == 0)
        {
            TextAsset stageData = Resources.Load<TextAsset>("Stage/map1");
            GameManager.selectedStageText = stageData.text;
            FadeManager.Instance.TransitionToScene("MainScene");
        }
        else
        {
            FadeManager.Instance.TransitionToScene("SelectStage");
        }
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