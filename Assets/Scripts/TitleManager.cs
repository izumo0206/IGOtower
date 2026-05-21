using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // メインモードのボタンが押された時
    public void OnPuzzleModeClicked()
    {
        FadeManager.Instance.TransitionToScene("SelectStage");
    }

    public void OnExtraModeClicked()
    {
        Debug.Log("いつかやる");
    }
}