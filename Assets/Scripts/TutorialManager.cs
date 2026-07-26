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

    private Vector2Int? _expectedPos = null;

    public Vector2Int? ExpectedPos
    {
        get { return _expectedPos; }
        private set 
        { 
            _expectedPos = value; 

            Debug.Log($"🕵️ ExpectedPosに代入されたよ！座標: {value} / カーソルはセットされてる？: {(tutorialCursor != null)}");
            
            // ExpectedPosがセットされたら、自動でハイライトを移動して表示する！
            if (_expectedPos.HasValue && tutorialCursor != null)
            {
                // Vector2Int をワールド座標(Vector3)に変換してカーソルを移動
                tutorialCursor.transform.position = new Vector3(_expectedPos.Value.x, _expectedPos.Value.y, 0);
                tutorialCursor.SetActive(true);
                Debug.Log("✨ ハイライトをONにしたよ！");
            }
            // nullになったら（待機が終わったら）ハイライトを消す！
            else if (tutorialCursor != null)
            {
                tutorialCursor.SetActive(false);
            }
        }
    }

    public string ExpectedId { get; private set; }

    public static bool IsTutorialMode = false;



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
        int isCleared = PlayerPrefs.GetInt("IsTutorialCleared", 0);
        if (isCleared == 0)
        {
            IsTutorialMode = true;
            // チュートリアルが必要なら、進行コルーチンをスタート
            StartCoroutine(TutorialSequenceRoutine());
        }
        else
        {
            // 不要なら、このオブジェクトごと消滅させる！
            // これにより、本編プレイ中はチュートリアルのコードが一切メモリに残りません。
            Destroy(gameObject); 
        }
    }

    public void ReportAllyPlaced(Vector2Int placedPos, string unitId)
    {
        // もし待機中の座標があり、かつ「置かれた座標」と「待っていた座標」がピッタリ一致したら！
        if (ExpectedPos != null && ExpectedPos.Value == placedPos &&  ExpectedId != null && ExpectedId == unitId)
        {
            Debug.Log($"大正解！期待通り {placedPos} に配置されました！");
            _isActionCompleted = true; // ここで初めてフラグをオンにする
        }
        else
        {
            Debug.Log($"不正解の場所（{placedPos}）に置かれたので、チュートリアルは進みません。");
            // ※必要であれば、ここに「そこじゃないよ！」という警告ダイアログを出す処理を足すこともできます
        }
    }

    public IEnumerator WaitForPlayerAction(Vector2Int targetPos, string targetId)
    {
        _isActionCompleted = false; 

        ExpectedPos = targetPos;
        ExpectedId = targetId;
        yield return new WaitUntil(() => _isActionCompleted); 

        ExpectedPos = null;
        ExpectedId = null;
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

        yield return StartCoroutine(WaitForPlayerAction(pos1, "001"));

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

        tutorialDialog.Show("ターンを終了しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.EnemyTurn);

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn);





        tutorialDialog.Show("体力が減ってきました。回復スキルを持つ味方を設置しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Show("ターンを終了しましょう");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Hide();

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.EnemyTurn);

        yield return new WaitUntil(() => _gameManager.CurrentPhase == GamePhase.PlayerTurn);



        

        tutorialDialog.Show("体力が高い敵でも、四方を囲むことで破壊することができます。好きな味方を設置して、破壊してみましょう。");

        yield return StartCoroutine(tutorialDialog.WaitForNext());

        tutorialDialog.Show("敵をすべて倒しました！チュートリアルは以上で終了です");

        yield return StartCoroutine(tutorialDialog.WaitForNext());
        
        PlayerPrefs.SetInt("IsTutorialCleared", 1);
        PlayerPrefs.Save();
        FadeManager.Instance.TransitionToScene("SelectStage");
        yield break;
    }
}