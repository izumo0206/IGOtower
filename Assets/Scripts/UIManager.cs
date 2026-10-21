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
    [SerializeField] private Outline _endTurnHighlightFrame;


    private void Start()
    {
        // ターン終了ボタンに処理を登録する
        if (_endTurnButton != null)
        {
            _endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }
        RefreshEndTurnButton();
    }

    private void LateUpdate()
    {
        RefreshEndTurnButton();
    }

    private void RefreshEndTurnButton()
    {
        bool canEndTurn = GameManager.Instance != null && GameManager.Instance.CanEndPlayerTurn;
        if (_endTurnButton != null)
            _endTurnButton.interactable = canEndTurn;

        if (_endTurnHighlightFrame != null)
        {
            bool showFrame = _endTurnButton != null && _endTurnButton.isActiveAndEnabled
                && _endTurnButton.IsInteractable();
            if (_endTurnHighlightFrame.enabled != showFrame)
                _endTurnHighlightFrame.enabled = showFrame;
        }
    }

    private void OnDestroy()
    {
        if (_endTurnButton != null)
            _endTurnButton.onClick.RemoveListener(OnEndTurnButtonClicked);
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
        if (GameManager.Instance == null || !GameManager.Instance.CanEndPlayerTurn
            || _endTurnButton == null || !_endTurnButton.IsInteractable()) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSE();
        }
        // 配置前・配置後のどちらからでもターンを終了できる。
        GameManager.Instance.EndPlayerTurn();
        RefreshEndTurnButton();
    }

}
