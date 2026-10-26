using System.Collections;
using UnityEngine;

/// <summary>本編の新しい駒とルールを、初回だけナビゲーターの会話UIで紹介する。</summary>
public sealed class StageUnitIntroduction : MonoBehaviour
{
    [SerializeField] private TutorialDialog dialog;
    [System.Serializable]
    private sealed class Introduction
    {
        public string stageName;
        public AllyUnitData unit;
        [TextArea(2, 4)] public string advice;
    }

    [SerializeField] private Introduction[] introductions;

    [System.Serializable]
    private sealed class StageTutorial
    {
        public string tutorialId;
        public string stageName;
        [TextArea(2, 5)] public string message;
    }

    [SerializeField] private StageTutorial[] stageTutorials;

    private IEnumerator Start()
    {
        // 通常マップでTutorialManagerがUIを閉じるStart処理の後に表示する。
        yield return null;
        GameSession session = GameSession.EnsureInstance();
        if (session.Mode != GameMode.Puzzle || dialog == null)
            yield break;

        if (stageTutorials != null)
        {
            foreach (StageTutorial tutorial in stageTutorials)
            {
                if (tutorial == null || tutorial.stageName != session.SelectedStageName
                    || string.IsNullOrEmpty(tutorial.tutorialId) || string.IsNullOrEmpty(tutorial.message)
                    || session.Profile.HasSeenStageTutorial(tutorial.tutorialId))
                    continue;

                dialog.Show(tutorial.message);
                yield return dialog.WaitForNext();
                // 駒の紹介とは別に、説明を読み終えたルールを記録する。
                session.MarkStageTutorialSeen(tutorial.tutorialId);
                dialog.Hide();
            }
        }

        if (introductions == null) yield break;
        foreach (Introduction introduction in introductions)
        {
            if (introduction == null || introduction.stageName != session.SelectedStageName
                || introduction.unit == null
                || session.Profile.HasSeenUnitIntroduction(introduction.unit.unitId))
                continue;

            dialog.ShowUnitIntroduction(introduction.unit, introduction.advice);
            yield return dialog.WaitForNext();
            // 読み終えて閉じた時点で保存。途中退出時は次回も説明を見られる。
            session.MarkUnitIntroductionSeen(introduction.unit.unitId);
            dialog.Hide();
        }
    }
}
