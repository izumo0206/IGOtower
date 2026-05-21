using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButton : MonoBehaviour
{
    [Header("紐付け用")]
    public TextMeshProUGUI stageNameText; // ステージ名のテキスト
    public GameObject clearMark;          // クリアマークの画像オブジェクト

    // Managerから呼ばれる「ボタンの初期設定」関数
    public void Setup(string stageName, bool isCleared, UnityEngine.Events.UnityAction onClickAction)
    {
        // 1. テキストを設定
        stageNameText.text = stageName;

        // 2. クリア状況に応じて、マークを表示/非表示にする
        clearMark.SetActive(isCleared);

        // 3. クリックされた時の処理を登録
        Button button = GetComponent<Button>();
        button.onClick.AddListener(onClickAction);
    }
}
