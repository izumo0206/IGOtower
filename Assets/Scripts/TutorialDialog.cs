using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum NavigatorExpression { Normal, Speaking, Surprised, Happy, Thinking }

// 表示だけを担当し、進行条件はTutorialManagerが管理する。
public class TutorialDialog : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    [SerializeField] private GameObject conversationPanel;
    [SerializeField] private Image navigatorImage;
    [SerializeField] private Sprite[] expressions;
    [SerializeField] private Button advanceButton;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TextMeshProUGUI instructionTitle;
    [SerializeField] private TextMeshProUGUI instructionText;
    [Header("HPへの注目")]
    [SerializeField] private RectTransform hpFocusTarget;
    [SerializeField] private RectTransform hpFocusOverlay;
    [SerializeField] private RectTransform[] hpFocusShades;
    [SerializeField] private Vector2 hpFocusPadding = new Vector2(12f, 12f);

    private readonly Vector3[] _hpCorners = new Vector3[4];
    private Canvas _hpCanvas;

    private bool _isNextPressed;
    private int _shownFrame;
    private int _lastAdvanceFrame = -10;
    public bool IsConversationVisible => conversationPanel != null && conversationPanel.activeInHierarchy;
    // 閉じたタップが同フレーム・次フレームの盤面操作に使われるのを防ぐ。
    public bool BlocksGameplayInput => IsConversationVisible || Time.frameCount <= _lastAdvanceFrame + 1;

    private void Awake()
    {
        if (advanceButton != null) advanceButton.onClick.AddListener(OnClickNextButton);
    }

    private void OnDestroy()
    {
        if (advanceButton != null) advanceButton.onClick.RemoveListener(OnClickNextButton);
    }

    public void Show(string message, NavigatorExpression expression = NavigatorExpression.Speaking)
    {
        SetHpFocus(false);
        gameObject.SetActive(true);
        instructionPanel.SetActive(false);
        conversationPanel.SetActive(true);
        messageText.text = message;
        int index = (int)expression;
        if (navigatorImage != null && expressions != null && index < expressions.Length)
            navigatorImage.sprite = expressions[index];
        _isNextPressed = false;
        _shownFrame = Time.frameCount;
    }

    public void ShowInstruction(string title, string instruction, bool focusHp = false)
    {
        gameObject.SetActive(true);
        conversationPanel.SetActive(false);
        instructionPanel.SetActive(true);
        instructionTitle.text = title;
        instructionText.text = instruction;
        SetHpFocus(focusHp);
    }

    public void Hide()
    {
        SetHpFocus(false);
        conversationPanel.SetActive(false);
        instructionPanel.SetActive(false);
    }

    public void OnClickNextButton()
    {
        if (!IsConversationVisible || _isNextPressed || Time.frameCount <= _shownFrame) return;
        _isNextPressed = true;
        _lastAdvanceFrame = Time.frameCount;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonSE();
    }

    public IEnumerator WaitForNext()
    {
        yield return new WaitUntil(() => _isNextPressed);
    }

    private void SetHpFocus(bool visible)
    {
        if (hpFocusOverlay == null) return;
        bool ready = hpFocusTarget != null && hpFocusShades != null && hpFocusShades.Length == 4;
        hpFocusOverlay.gameObject.SetActive(visible && ready);
        if (visible && ready) UpdateHpFocus();
    }

    private void LateUpdate()
    {
        if (hpFocusOverlay != null && hpFocusOverlay.gameObject.activeInHierarchy) UpdateHpFocus();
    }

    private void UpdateHpFocus()
    {
        if (_hpCanvas == null) _hpCanvas = hpFocusTarget.GetComponentInParent<Canvas>();
        Camera camera = _hpCanvas != null && _hpCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _hpCanvas.worldCamera : null;
        hpFocusTarget.GetWorldCorners(_hpCorners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in _hpCorners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hpFocusOverlay, screen, null, out Vector2 local))
                return;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        Rect rect = hpFocusOverlay.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        float left = Mathf.Clamp01((min.x - hpFocusPadding.x - rect.xMin) / rect.width);
        float right = Mathf.Clamp01((max.x + hpFocusPadding.x - rect.xMin) / rect.width);
        float bottom = Mathf.Clamp01((min.y - hpFocusPadding.y - rect.yMin) / rect.height);
        float top = Mathf.Clamp01((max.y + hpFocusPadding.y - rect.yMin) / rect.height);
        // 上下左右の4枚だけを描き、HP表示の矩形には何も重ねない。
        SetShade(hpFocusShades[0], 0, 0, left, 1);
        SetShade(hpFocusShades[1], right, 0, 1, 1);
        SetShade(hpFocusShades[2], left, 0, right, bottom);
        SetShade(hpFocusShades[3], left, top, right, 1);
    }

    private static void SetShade(RectTransform shade, float left, float bottom, float right, float top)
    {
        shade.anchorMin = new Vector2(left, bottom);
        shade.anchorMax = new Vector2(right, top);
        shade.offsetMin = Vector2.zero;
        shade.offsetMax = Vector2.zero;
    }
}
