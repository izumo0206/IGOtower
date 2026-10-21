using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム画面のミニメニューを管理します。
/// UIの見た目はCanvas側で作成し、必要な参照だけInspectorから設定してください。
/// </summary>
public sealed class InGameMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI currentStageNameText;
    [SerializeField] private Slider volumeSlider;

    [Header("Buttons (optional)")]
    [SerializeField] private Button menuButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button returnToMenuButton;
    [Header("Title debug (optional)")]
    [SerializeField] private Button clearCacheButton;
    [SerializeField] private TextMeshProUGUI cacheClearStatusText;

    private void Awake()
    {
        if (menuButton != null)
            menuButton.onClick.AddListener(OpenMenu);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);

        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        if (clearCacheButton != null)
            clearCacheButton.onClick.AddListener(ClearCache);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.wholeNumbers = false;
            volumeSlider.SetValueWithoutNotify(SoundManager.MasterVolume);
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        RefreshStageName();
        SetMenuVisible(false);
    }

    private void OnDestroy()
    {
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OpenMenu);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseMenu);

        if (returnToMenuButton != null)
            returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
        if (clearCacheButton != null)
            clearCacheButton.onClick.RemoveListener(ClearCache);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
    }

    public void OpenMenu()
    {
        RefreshStageName();

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(SoundManager.MasterVolume);

        SetMenuVisible(true);
    }

    public void CloseMenu()
    {
        SetMenuVisible(false);
    }

    public void ToggleMenu()
    {
        SetMenuVisible(menuPanel != null && !menuPanel.activeSelf);
    }

    public void ReturnToMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToStageSelect();
            return;
        }

        GameSession.EnsureInstance().ReturnToTitle();
    }

    public void SetMasterVolume(float volume)
    {
        SoundManager.SetMasterVolume(volume);
    }

    // 画像や音量設定ではなく、チュートリアル・ステージの進行データを初期化する。
    public void ClearCache()
    {
        GameSession.EnsureInstance().ResetAllProgress();
        if (cacheClearStatusText != null)
            cacheClearStatusText.text = "初期化しました。\nゲーム開始でチュートリアルに戻ります。";
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonSE();
    }

    public void RefreshStageName()
    {
        if (currentStageNameText != null)
            currentStageNameText.text = GetCurrentStageDisplayName();
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuPanel != null)
            menuPanel.SetActive(visible);
    }

    private static string GetCurrentStageDisplayName()
    {
        GameSession session = GameSession.EnsureInstance();

        if (session.Mode == GameMode.Tutorial)
            return "チュートリアル";

        string stageName = session.SelectedStageName;
        if (string.IsNullOrWhiteSpace(stageName))
            return "ステージ未選択";

        if (stageName.StartsWith("map", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(stageName.Substring(3), out int stageNumber))
        {
            return $"ステージ {stageNumber}";
        }

        return stageName;
    }
}
