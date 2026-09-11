using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialDialog : MonoBehaviour
{
    [Header("文字を表示するテキストUI")]
    public TextMeshProUGUI messageText;


    // ボタンが押されたかを記憶する秘密のフラグ
    private bool _isNextPressed = false;

    public void Show(string message)
    {
        messageText.text = message;
        gameObject.SetActive(true);
        _isNextPressed = false; // ★ 表示するたびに、毎回フラグを「まだ押されてない」状態にリセットする
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Appear()
    {
        gameObject.SetActive(true);
    }


    // ① インスペクターから「次へ」ボタンを押した時に呼び出す関数
    public void OnClickNextButton()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSE();
        }
        // 押されたらフラグをオンにする！
        _isNextPressed = true;
    }

    // ② 呼び出し側（BattleManager）が「押されるまで待つ」ためのコルーチン
    public IEnumerator WaitForNext()
    {
        // _isNextPressed が true になる（ボタンが押される）まで、ここで延々と待ち続ける
        while (!_isNextPressed)
        {
            yield return null; // 1フレーム待ってからループし直す
        }
    }
}