using UnityEngine;

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

        GameSession session = GameSession.EnsureInstance();

        if (!session.Profile.tutorialCleared)
        {
            TextAsset stageData = Resources.Load<TextAsset>("Tutorial/map0");
            if (stageData == null) return;
            session.BeginTutorial(stageData.text);
            FadeManager.Instance.TransitionToScene("MainScene");
        }
        else
        {
            FadeManager.Instance.TransitionToScene("SelectStage");
        }
    }

}
