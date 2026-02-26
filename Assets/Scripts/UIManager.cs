using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使用していると仮定

/// <summary>
/// 画面上のUI（体力やコスト、ターン進行ボタンなど）を管理するクラス
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Button _endTurnButton;

    private void Start()
    {
        // ターン終了ボタンに処理を登録する
        if (_endTurnButton != null)
        {
            _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }
    }

    private void Update()
    {
        // 毎フレーム GameManager の値を参照して画面を更新する
        if (GameManager.Instance != null && _hpText != null && _costText != null)
        {
            _hpText.text = $"HP: {GameManager.Instance.CurrentHp}";
            _costText.text = $"残コスト: {GameManager.Instance.RemainingCost}";
        }
    }

    /// <summary>
    /// ターン終了ボタンが押されたときの処理
    /// </summary>
    private void OnEndTurnButtonClicked()
    {
        if (GameManager.Instance != null)
        {
             // プレイヤーの配置が終わり、ターン終了を宣言して敵フェーズへ
            GameManager.Instance.EndPlayerTurn();
        }
    }
}
