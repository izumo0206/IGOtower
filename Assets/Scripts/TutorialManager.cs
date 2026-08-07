using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    private bool _isActionCompleted = false;

    [Header("チュートリアル用のUIやカーソル")]
    public TutorialDialog tutorialDialog;
    public GameObject tutorialCursor;
    public GameObject tutorialBlockerPanel;

    [SerializeField] private GameObject _skipButtonObj;

    private Vector2Int? _expectedPos = null;

    public Vector2Int? ExpectedPos
    {
        get { return _expectedPos; }
        private set 
        { 
            _expectedPos = value; 
            
            // ExpectedPosがセットされたら、自動でハイライトを移動して表示する！
            if (_expectedPos.HasValue && tutorialCursor != null)
            {
                // Vector2Int をワールド座標(Vector3)に変換してカーソルを移動
                tutorialCursor.transform.position = new Vector3(_expectedPos.Value.x, _expectedPos.Value.y, 0);
                tutorialCursor.SetActive(true);
            }
            // nullになったら（待機が終わったら）ハイライトを消す！
            else if (tutorialCursor != null)
            {
                tutorialCursor.SetActive(false);
            }
        }
    }

    public string ExpectedId { get; private set; }

    public UnitDirection? ExpectedDir { get; private set; }

    public static bool IsTutorialMode = false;

    private Coroutine _tutorialCoroutine;



    // バトルマネージャーへの参照（命令を出すため）
    private GameManager _gameManager;

    [SerializeField] private Transform[] _targetMarkers;
    [SerializeField] private GameObject _tutorialCursor;

    private void Awake()
    {
        Instance = this;
        _gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        bool isCleared = GameSession.EnsureInstance().Profile.tutorialCleared;
        if (!isCleared)
        {
            IsTutorialMode = true;

            if (_skipButtonObj != null) _skipButtonObj.SetActive(true);
            // チュートリアルが必要なら、進行コルーチンをスタート
            _tutorialCoroutine = StartCoroutine(TutorialSequenceRoutine());
        }
        else
        {
            // 不要なら、このオブジェクトごと消滅させる！
            // これにより、本編プレイ中はチュートリアルのコードが一切メモリに残りません。
            Destroy(gameObject); 
        }
    }

    public void ReportAllyPlaced(Vector2Int placedPos, string unitId, UnitDirection targetDir)
    {
        // もし待機中の座標があり、かつ「置かれた座標」と「待っていた座標」がピッタリ一致したら！
        if (ExpectedPos != null && ExpectedPos.Value == placedPos &&  ExpectedId != null && ExpectedId == unitId && ExpectedDir != null && ExpectedDir == targetDir)
        {
            _isActionCompleted = true; // ここで初めてフラグをオンにする
        }
        else
        {
        }
    }

    public IEnumerator WaitForPlayerAction(Vector2Int targetPos, string targetId, UnitDirection targetDir)
    {
        _isActionCompleted = false; 

        ExpectedPos = targetPos;
        ExpectedId = targetId;
        ExpectedDir = targetDir;
        yield return new WaitUntil(() => _isActionCompleted); 

        ExpectedPos = null;
        ExpectedId = null;
        ExpectedDir = null;
    }

    public void SkipTutorial()
    {
        // 1. 動いている台本（コルーチン）を強制ストップ！
        if (_tutorialCoroutine != null)
        {
            StopCoroutine(_tutorialCoroutine);
        }

        // 2. チュートリアル用のUI（カーソルや光る床など）を隠す
        ExpectedPos = null;
        ExpectedDir = null;
        if (_tutorialCursor != null)
        {
            _tutorialCursor.SetActive(false);
        }

        // 3. チュートリアルのテキストやウィンドウを非表示にする
        tutorialDialog.Hide();

        // 4. クリアフラグを立てて保存する（次回から出ないようにする）
        GameSession.EnsureInstance().MarkTutorialCleared();
        

        // 5. チュートリアルモードをOFF！
        IsTutorialMode = false;
        
        Debug.Log("⏩ チュートリアルをスキップしました！");

        if (_skipButtonObj != null) _skipButtonObj.SetActive(false);

        // 6. 通常のゲーム開始処理を呼ぶ
        FadeManager.Instance.TransitionToScene("SelectStage");
    }

    private IEnumerator TutorialSequenceRoutine()
    {
        tutorialDialog.Show("ようこそ！");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Show("敵を攻撃して倒してみましょう。攻撃スキルを持つ味方を設置してください");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        Vector2Int pos1 = new Vector2Int(
            Mathf.RoundToInt(_targetMarkers[0].position.x),
            Mathf.RoundToInt(_targetMarkers[0].position.y)
        );

        _gameManager.ChangeIndex(0);

        yield return StartCoroutine(WaitForPlayerAction(pos1, "001", UnitDirection.Up));

        tutorialDialog.Show("味方は設置するときに消費コストが決められています。設置を確定したときに消費されます");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Show("ターンを終了しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.EnemyTurn);

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn);

        tutorialDialog.Show("プレイヤーのターンが終わると、敵が攻撃してきます。体力が0にならないように、敵をすべて倒すとステージクリアです。");

        yield return StartCoroutine(tutorialDialog.WaitForNext());




        tutorialDialog.Show("ターン終了時にスキルが発動する味方もいます。設置してみましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        Vector2Int pos2 = new Vector2Int(
            Mathf.RoundToInt(_targetMarkers[1].position.x),
            Mathf.RoundToInt(_targetMarkers[1].position.y)
        );

        _gameManager.ChangeIndex(3);

        yield return StartCoroutine(WaitForPlayerAction(pos2, "004", UnitDirection.Up));

        tutorialDialog.Show("ターンを終了しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.EnemyTurn);

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn);





        tutorialDialog.Show("体力が減ってきました。回復スキルを持つ味方を設置しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        Vector2Int pos3 = new Vector2Int(
            Mathf.RoundToInt(_targetMarkers[2].position.x),
            Mathf.RoundToInt(_targetMarkers[2].position.y)
        );

        _gameManager.ChangeIndex(5);

        yield return StartCoroutine(WaitForPlayerAction(pos3, "006", UnitDirection.Up));

        tutorialDialog.Show("ターンを終了しましょう");

        tutorialDialog.Show("ターンを終了しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.EnemyTurn);

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn);



        

        tutorialDialog.Show("体力が高い敵でも、四方を囲むことで破壊することができます。好きな味方を設置して、破壊してみましょう。");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        Vector2Int pos4 = new Vector2Int(
            Mathf.RoundToInt(_targetMarkers[3].position.x),
            Mathf.RoundToInt(_targetMarkers[3].position.y)
        );

        _gameManager.ChangeIndex(0);

        yield return StartCoroutine(WaitForPlayerAction(pos4, "001", UnitDirection.Right));

        tutorialDialog.Show("ターンを終了しましょう");

        tutorialDialog.Show("敵をすべて倒しました！チュートリアルは以上で終了です");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        if (_skipButtonObj != null) _skipButtonObj.SetActive(false);
        
        GameSession.EnsureInstance().MarkTutorialCleared();
        FadeManager.Instance.TransitionToScene("SelectStage");
        yield break;
    }
}
