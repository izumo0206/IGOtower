using System;
using UnityEngine;
using UnityEngine.Video;

public class TitleManager : MonoBehaviour
{

    public AudioClip _titleBGM;
    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMWithFadeIn(_titleBGM, 1.0f);
        }
    }

    // メインモードのボタンが押された時
    public void OnPuzzleModeClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySwitchSE();
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
