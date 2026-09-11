using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DirectionUIController : MonoBehaviour
{
    [Header("効果音")]

    [Header("チュートリアル用の色設定")]
    [SerializeField] private Button _upButton;
    [SerializeField] private Button _downButton;
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;

    [SerializeField] private Color _highlightColor = Color.yellow; 
    [SerializeField] private Color _normalColor = Color.white;

    private void OnEnable()
    {
        // チュートリアル中なら、TutorialManagerの「期待する方向」を見に行って色を変える！
        if (TutorialManager.IsTutorialMode && TutorialManager.Instance != null)
        {
            UpdateDirectionButtonColors(TutorialManager.Instance.ExpectedDir);
        }
        else
        {
            // 通常モードなら、全部通常の色に戻す
            UpdateDirectionButtonColors(null);
        }
    }

    private void SetButtonColor(Button btn, Color newColor)
    {
        if (btn == null) return;
        ColorBlock colors = btn.colors;
        colors.normalColor = newColor;
        colors.selectedColor = newColor;
        btn.colors = colors;
    }

    private void UpdateDirectionButtonColors(UnitDirection? targetDir)
    {
        // 一旦リセット
        SetButtonColor(_upButton, _normalColor);
        SetButtonColor(_downButton, _normalColor);
        SetButtonColor(_leftButton, _normalColor);
        SetButtonColor(_rightButton, _normalColor);

        // 正解のボタンだけ色を付ける
        if (targetDir.HasValue)
        {
            if (targetDir.Value == UnitDirection.Up) SetButtonColor(_upButton, _highlightColor);
            else if (targetDir.Value == UnitDirection.Down) SetButtonColor(_downButton, _highlightColor);
            else if (targetDir.Value == UnitDirection.Left) SetButtonColor(_leftButton, _highlightColor);
            else if (targetDir.Value == UnitDirection.Right) SetButtonColor(_rightButton, _highlightColor);
        }
    }


    public void OnClickUp()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSE();
        }
        SendDirection(UnitDirection.Up);
    }
    public void OnClickRight()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSE();
        }
        SendDirection(UnitDirection.Right);
    }
    public void OnClickDown()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSE();
        }
        SendDirection(UnitDirection.Down);
    }
    public void OnClickLeft()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSE();
        }
        SendDirection(UnitDirection.Left);
    }

    public void OnClickCancel()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayCancelSE();
        }
        GameManager gm = FindObjectOfType<GameManager>();
        if(gm != null)
        {
            gm.CancelPlacement();
        }
    }

    private void SendDirection(UnitDirection dir)
    {
        GameManager gm = FindObjectOfType<GameManager>();

        if(gm != null)
        {
            gm.OnDirectionSelected(dir);
        }
    }
}
