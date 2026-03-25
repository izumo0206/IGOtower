using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// ゲームの進行状態（フェーズ）を表す列挙型
/// </summary>
public enum GamePhase
{
    Init,               // ゲーム開始直後の初期化
    PlayerTurn,         // プレイヤーが駒を置くフェーズ
    ExecuteAllySkills,  // ターン終了時スキルなどの処理
    EnemyTurn,          // 敵のダメージ計算など
    GameOver,           // 負け
    GameClear           // 勝ち
}

/// <summary>
/// ゲーム全体の進行、ルール判定、プレイヤーのステータス管理を行うクラス
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("プレイヤーステータス")]
    [SerializeField] private int _maxHp = 10;
    [SerializeField] private int _maxCost = 7;

    public int CurrentHp { get; private set; }
    public int RemainingCost { get; private set; }

    public bool AlreadySet = false;

    public GamePhase CurrentPhase { get; private set; }

    public GameObject testPrefab;

    public BoardManager boardManager;

    public GameObject highlightSquareAble;
    public GameObject highlightSquareUnable; 
    public Collider boardCollider;

    public GameObject cutInPanel;
    public TextMeshProUGUI cutInText;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        HighLight();
        if(CurrentPhase == GamePhase.PlayerTurn){
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit))
                {
                    if(AlreadySet == false)
                    {
                        if(RemainingCost >= 2/*選択したコマのコスト(仮)*/)
                        {
                            int x = (int)Mathf.Round(hit.point.x);
                            int y = (int)Mathf.Round(hit.point.y);

                            Vector2Int pos = new Vector2Int(x, y);

                            // RuleSystemを使って「ここに味方を置けるか？」をチェック
                            if (RuleSystem.Instance.CanPlaceAlly(pos))
                            {
                                TryConsumeCost(2/*選択したコマのコスト(仮)*/);
                                boardManager.SetAlly(x, y);
                                Debug.Log($"({x}, {y}) に味方を配置しました！");
                                AlreadySet = true;
                                
                                // ※ もし配置後にターンを終了させるならここで EndPlayerTurn() を呼びます
                                // EndPlayerTurn();
                            }
                            else
                            {
                                Debug.Log("そこには配置できません（盤面外、または既に何かが置かれています）");
                            }
                        } 
                        else
                        {
                            Debug.Log("コストが足りません");
                        }
                    } 
                    else
                    {
                        Debug.Log("すでにコマを配置しています");
                    }
                }
            }
        }
    }

    public void HighLight()
    {
        if(CurrentPhase == GamePhase.PlayerTurn)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (boardCollider.Raycast(ray, out hit, 100f))
            {
                float x = Mathf.Round(hit.point.x);
                float y = Mathf.Round(hit.point.y);

                Vector2Int pos = new Vector2Int((int)x, (int)y);

                if(AlreadySet == false && RemainingCost >= 2 && RuleSystem.Instance.CanPlaceAlly(pos)){

                    highlightSquareAble.transform.position = new Vector2(x, y);

                    if (!highlightSquareAble.activeSelf)
                    {
                        highlightSquareAble.SetActive(true);
                    }
                    if (highlightSquareUnable.activeSelf)
                    {
                        highlightSquareUnable.SetActive(false);
                    }
                } else
                {
                    highlightSquareUnable.transform.position = new Vector2(x, y);

                    if (!highlightSquareUnable.activeSelf)
                    {
                        highlightSquareUnable.SetActive(true);
                    }
                    if (highlightSquareAble.activeSelf)
                    {
                        highlightSquareAble.SetActive(false);
                    }
                }
            } 
            else
            {
                if (highlightSquareAble.activeSelf)
                {
                    highlightSquareAble.SetActive(false);
                }

                if (highlightSquareUnable.activeSelf)
                {
                    highlightSquareUnable.SetActive(false);
                }
            }
        }
    }

    public void setStatus(int maxHP, int maxCost)
    {
        _maxHp = maxHP;
        _maxCost = maxCost;
    }

    


    /// <summary>
    /// ゲーム開始時の初期化処理
    /// </summary>
    public void InitializeGame()
    {
        CurrentPhase = GamePhase.Init;
        CurrentHp = _maxHp;
        RemainingCost = _maxCost;

        Debug.Log($"ゲーム開始！ HP: {CurrentHp}, コスト: {RemainingCost}");
        
        // 開発用：少し待ってからプレイヤーターンへ
        StartCoroutine(StartPlayerTurnDelayed());
    }

    private IEnumerator StartPlayerTurnDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        ChangePhase(GamePhase.PlayerTurn);
    }

    private IEnumerator ShowCutInRoutine()
    {
        cutInPanel.SetActive(true);
        
        // アニメーションが終わるまで待つ（例: 2秒）
        yield return new WaitForSeconds(2.0f);
        
        cutInPanel.SetActive(false);
    }

    /// <summary>
    /// フェーズを遷移させる
    /// </summary>
    public void ChangePhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
        Debug.Log($"[Phase] {CurrentPhase} に移行しました");

        switch (CurrentPhase)
        {
            case GamePhase.PlayerTurn:
                // TODO: プレイヤーの入力（駒の配置）を受け付ける状態にする
                cutInText.text = "YOUR TURN";
                cutInText.color = Color.white;
                StartCoroutine(ShowCutInRoutine());
                AlreadySet = false;
                
                break;

            case GamePhase.ExecuteAllySkills:
                // TODO: 「ターン終了時効果」を持つ味方ユニットのスキルを一斉発動する
                // 発動完了後、EnemyTurnへ
                ChangePhase(GamePhase.EnemyTurn);
                break;

            case GamePhase.EnemyTurn:
                // 敵の合計攻撃力を計算してプレイヤーにダメージを与える
                ProcessEnemyAttacks();
                break;

            case GamePhase.GameClear:
                Debug.Log("GAME CLEAR!!");
                break;

            case GamePhase.GameOver:
                Debug.Log("GAME OVER...");
                break;
        }
    }

    /// <summary>
    /// プレイヤーがユニットを配置し、ターンを終了する処理
    /// </summary>
    public void EndPlayerTurn()
    {
        if (CurrentPhase != GamePhase.PlayerTurn) return;

        // 味方のスキル発動フェーズへ移行（今回は仮で直接敵ターンに行くなどの処理はChangePhaseに委譲）
        ChangePhase(GamePhase.ExecuteAllySkills);
    }

    /// <summary>
    /// 敵ターン（ダメージ計算）の処理
    /// </summary>
    private void ProcessEnemyAttacks()
    {
        // TODO: 盤面にいる全敵ユニットを取得し、攻撃力の合計を出す
        int totalEnemyDamage = CalculateTotalEnemyDamage();

        TakeDamage(totalEnemyDamage);

        // 勝敗・進行の判定
        if (CurrentHp <= 0)
        {
            ChangePhase(GamePhase.GameOver);
        }
        else if (CheckAllEnemiesDefeated())
        {
            ChangePhase(GamePhase.GameClear);
        }
        else
        {
            // まだ敵がいれば、次のプレイヤーターンへ
            ChangePhase(GamePhase.PlayerTurn);
        }
    }

    public void TakeDamage(int damage)
    {
        CurrentHp -= damage;
        Debug.Log($"プレイヤーは {damage} のダメージを受けた。残りHP: {CurrentHp}");
    }

    public void Heal(int amount)
    {
        CurrentHp += amount;
        // 最大HPを超えないようにするならここでCapする
        Debug.Log($"プレイヤーは {amount} 回復した。残りHP: {CurrentHp}");
    }

    /// <summary>
    /// コストを消費可能か確認し、可能なら消費する
    /// </summary>
    public bool TryConsumeCost(int cost)
    {
        if (RemainingCost >= cost)
        {
            RemainingCost -= cost;
            Debug.Log($"コスト {cost} 消費。残りコスト: {RemainingCost}");
            return true;
        }
        return false;
    }

    // ---------------- 以下、スタブ（後でUnitクラスと連携） ----------------

    private int CalculateTotalEnemyDamage()
    {
        // 盤面にいる全EnemyUnitを取得
        var enemies = FindObjectsOfType<EnemyUnit>();
        int total = 0;
        foreach (var enemy in enemies)
        {
            total += enemy.AttackPower;
        }
        return total;
    }

    private bool CheckAllEnemiesDefeated()
    {
        // 敵リストの数が0かチェック
        var enemies = FindObjectsOfType<EnemyUnit>();
        return enemies.Length == 0; 
    }
}
