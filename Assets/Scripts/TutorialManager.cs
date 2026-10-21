using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    public static bool IsTutorialMode;
    public TutorialDialog tutorialDialog;
    public GameObject tutorialCursor;
    public GameObject tutorialBlockerPanel;
    [SerializeField] private GameObject _skipButtonObj;
    [SerializeField] private Transform[] _targetMarkers;
    [SerializeField] private GameObject _tutorialCursor;

    private GameManager _gameManager;
    private bool _isActionCompleted;
    private bool _isFinishing;
    private bool _waitingForPlacement;
    private bool _waitingForEndTurn;
    private bool _showingDirection;
    private bool _hasFailed;
    private string _placementTitle;
    private string _placementInstruction;
    private Vector2Int? _expectedPos;

    public Vector2Int? ExpectedPos
    {
        get => _expectedPos;
        private set
        {
            _expectedPos = value;
            if (tutorialCursor == null) return;
            if (value.HasValue)
                tutorialCursor.transform.position = new Vector3(value.Value.x, value.Value.y, 0);
            tutorialCursor.SetActive(value.HasValue);
        }
    }

    public string ExpectedId { get; private set; }
    public UnitDirection? ExpectedDir { get; private set; }
    public bool AllowsPlacement => _waitingForPlacement && !_isFinishing
        && tutorialDialog != null && !tutorialDialog.BlocksGameplayInput;
    public bool AllowsEndTurn => _waitingForEndTurn && !_isFinishing
        && tutorialDialog != null && !tutorialDialog.BlocksGameplayInput;

    private void Awake()
    {
        Instance = this;
        IsTutorialMode = GameSession.EnsureInstance().Mode == GameMode.Tutorial;
        _gameManager = FindObjectOfType<GameManager>();
    }

    private void Start()
    {
        if (!IsTutorialMode)
        {
            ClearTutorialState();
            Destroy(gameObject);
            return;
        }
        if (_skipButtonObj != null) _skipButtonObj.SetActive(true);
        StartCoroutine(TutorialSequenceRoutine());
    }

    private void Update()
    {
        // 万一敗北した場合も、既存のリトライ画面を会話で覆わない。
        if (!_hasFailed && IsTutorialMode && _gameManager != null
            && _gameManager.CurrentPhase == GamePhase.GameOver)
        {
            _hasFailed = true;
            _waitingForPlacement = false;
            _waitingForEndTurn = false;
            ExpectedPos = null;
            ExpectedId = null;
            ExpectedDir = null;
            tutorialDialog.Hide();
            StopAllCoroutines();
            return;
        }
        if (!_waitingForPlacement || _gameManager == null) return;
        bool choosingDirection = _gameManager.currentPlacePhase == GameManager.PlacementPhase.SelectingDirection;
        if (_showingDirection == choosingDirection) return;
        _showingDirection = choosingDirection;
        tutorialDialog.ShowInstruction(_placementTitle, choosingDirection
            ? $"向きを <b>{DirectionLabel(ExpectedDir.Value)}</b> にして\n配置を確定してください。"
            : _placementInstruction);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        IsTutorialMode = false;
    }

    public void ReportAllyPlaced(Vector2Int pos, string id, UnitDirection dir)
    {
        if (_waitingForPlacement && ExpectedPos == pos && ExpectedId == id && ExpectedDir == dir)
            _isActionCompleted = true;
    }

    public IEnumerator WaitForPlayerAction(Vector2Int pos, string id, UnitDirection dir)
    {
        _isActionCompleted = false;
        ExpectedPos = pos;
        ExpectedId = id;
        ExpectedDir = dir;
        _waitingForPlacement = true;
        yield return new WaitUntil(() => _isActionCompleted);
        _waitingForPlacement = false;
        ExpectedPos = null;
        ExpectedId = null;
        ExpectedDir = null;
        // 配置・攻撃・囲みの演出を見せてから会話へ進む。
        yield return new WaitForSeconds(1.1f);
    }

    public void SkipTutorial() => CompleteTutorial();

    private void ClearTutorialState()
    {
        _waitingForPlacement = false;
        _waitingForEndTurn = false;
        ExpectedPos = null;
        ExpectedId = null;
        ExpectedDir = null;
        _isActionCompleted = false;
        if (_tutorialCursor != null) _tutorialCursor.SetActive(false);
        if (tutorialDialog != null) tutorialDialog.Hide();
        if (_skipButtonObj != null) _skipButtonObj.SetActive(false);
        IsTutorialMode = false;
    }

    private void CompleteTutorial()
    {
        if (_isFinishing || !IsTutorialMode) return;
        _isFinishing = true;
        StopAllCoroutines();
        ClearTutorialState();
        GameSession.EnsureInstance().MarkTutorialCleared();
        if (FadeManager.Instance != null) FadeManager.Instance.TransitionToScene("SelectStage");
        else SceneManager.LoadScene("SelectStage");
    }

    private IEnumerator Say(string message, NavigatorExpression expression = NavigatorExpression.Speaking)
    {
        tutorialDialog.Show(message, expression);
        yield return tutorialDialog.WaitForNext();
    }

    private IEnumerator Place(int marker, int index, string id, UnitDirection dir, string title)
    {
        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn);
        _gameManager.ChangeIndex(index);
        _placementTitle = title;
        _placementInstruction = $"光っているマスに配置し、\n向きを <b>{DirectionLabel(dir)}</b> にして\n確定してください。";
        _showingDirection = false;
        tutorialDialog.ShowInstruction(title, _placementInstruction);
        Transform target = _targetMarkers[marker];
        yield return WaitForPlayerAction(new Vector2Int(Mathf.RoundToInt(target.position.x),
            Mathf.RoundToInt(target.position.y)), id, dir);
    }

    private IEnumerator EndTurnAndWatch()
    {
        tutorialDialog.ShowInstruction("ターンを終了しよう", "右側の <b>End（ターン終了）</b>\nを押してください。");
        _waitingForEndTurn = true;
        yield return new WaitUntil(() => _gameManager.CurrentPhase != GamePhase.PlayerTurn);
        _waitingForEndTurn = false;
        tutorialDialog.ShowInstruction("HPに注目！", "敵の攻撃でHPが減ります。\n右上のHPを見てみましょう。", focusHp: true);
        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn
            || _gameManager.CurrentPhase == GamePhase.GameClear || _gameManager.CurrentPhase == GamePhase.GameOver);
        // 反撃後の数値を確認してから次の会話へ移る。
        if (_gameManager.CurrentPhase == GamePhase.PlayerTurn) yield return new WaitForSeconds(0.8f);
    }

    private static string DirectionLabel(UnitDirection dir)
    {
        switch (dir)
        {
            case UnitDirection.Up: return "↑ 上";
            case UnitDirection.Down: return "↓ 下";
            case UnitDirection.Left: return "← 左";
            default: return "→ 右";
        }
    }

    private IEnumerator TutorialSequenceRoutine()
    {
        yield return Say("たいへんです！\n宇宙人が攻めてきました！！", NavigatorExpression.Surprised);
        yield return Say("こちらの猫ちゃんたちを出撃させて、\n対応しましょう！\nまずは、攻撃が得意なネコAの出番です。");
        yield return Place(0, 0, "001", UnitDirection.Up, "1 / 4　ネコAを配置しよう");
        yield return Say("できました！ ネコAは配置したときに攻撃します。\n配置を確定すると、ネコごとのコストを消費します。", NavigatorExpression.Happy);
        yield return EndTurnAndWatch();
        yield return Say("ターンを終了すると、敵が攻撃してきます。\nHPが0になる前に、すべての敵を倒しましょう。", NavigatorExpression.Thinking);
        yield return Say("次はネコDです。\nこのネコは、ターン終了時に攻撃します！");
        yield return Place(1, 3, "004", UnitDirection.Up, "2 / 4　ネコDを配置しよう");
        yield return EndTurnAndWatch();
        yield return Say("HPが減ってきましたね。\n回復が得意なネコFを配置してみましょう。", NavigatorExpression.Thinking);
        yield return Place(2, 5, "006", UnitDirection.Up, "3 / 4　ネコFで回復しよう");
        yield return EndTurnAndWatch();
        yield return Say("最後は「囲み」です！\nHPが高い敵も、上下左右を味方で囲むと倒せます。\nネコAで、囲みを完成させましょう！");
        yield return Place(3, 0, "001", UnitDirection.Right, "4 / 4　敵を囲んでみよう");
        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.GameClear);
        yield return Say("やりました！ すべての敵を倒せました！", NavigatorExpression.Happy);
        yield return Say("これで準備はばっちりです。\nステージ選択へ進んで、挑戦してみましょう！", NavigatorExpression.Happy);
        CompleteTutorial();
    }
}
