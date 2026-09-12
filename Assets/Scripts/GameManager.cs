using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Data;
using Unity.VisualScripting;

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

    public enum PlacementPhase
    {
        SelectingCell,      // 1. 場所を選んでいる状態
        SelectingDirection  // 2. 向きを選んでいる状態
    }

    [Header("配置システム")]
    public PlacementPhase currentPlacePhase = PlacementPhase.SelectingCell;
    
    // 仮置き中のコマを覚えておく変数
    private AllyUnit pendingUnit; 

    // 出現させる矢印UIのプレハブ
    public GameObject directionUIPrefab; 
    private GameObject currentDirectionUI;

    public int CurrentHp { get; private set; }
    public int RemainingCost { get; private set; }
    public int MaxHp => _maxHp;

    public bool AlreadySet = false;

    public GamePhase CurrentPhase { get; private set; }

    public GameObject testPrefab;

    public BoardManager boardManager;

    public GameObject highlightSquareAble;
    public GameObject highlightSquareUnable; 
    public Collider boardCollider;

    public GameObject cutInPanel;
    public TextMeshProUGUI cutInText;

    [Header("リザルト画面")]
    public GameObject gameClearPanel;
    public GameObject gameOverPanel;

    private int currentIndex = 0;

    public TextMeshProUGUI nameDisplay;

    public Image iconDisplay;

    public TextMeshProUGUI infoDisplay; // 追加：スキル・ステータス詳細表示用

    public List<AllyUnitData> allyUnits = new List<AllyUnitData>();

    public CanvasGroup mainUICanvasGroup;

    private Vector2Int _pendingGridPos;
    public TutorialDialog tutorialDialog;

    public AudioClip _gameBGM;
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

    private void OnEnable()
    {
        EnemyUnit.OnEnemyDestroyed += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        EnemyUnit.OnEnemyDestroyed -= HandleEnemyDestroyed;
    }

    private void Start()
    {
        InitializeGame();
        UpdateSelectionUI();
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMWithFadeIn(_gameBGM, 1.0f);
        }
    }

    private void Update()
    {
        HighLight();
        if(CurrentPhase == GamePhase.PlayerTurn){
            if (Input.GetMouseButtonDown(0))
            {
                if (currentPlacePhase == PlacementPhase.SelectingDirection) return;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit))
                {
                    if(AlreadySet == false)
                    {
                        if (TutorialManager.IsTutorialMode && TutorialManager.Instance != null)
                        {
                            int x = (int)Mathf.Round(hit.point.x);
                            int y = (int)Mathf.Round(hit.point.y);
                            Vector2Int pos = new Vector2Int(x, y);

                            // もし「期待されている座標」が設定されていて、クリックした場所がそれと違ったら…
                            if ((TutorialManager.Instance.ExpectedPos.HasValue && 
                                TutorialManager.Instance.ExpectedPos.Value != pos)
                                || TutorialManager.Instance.ExpectedId != allyUnits[currentIndex].unitId)
                            {

                                return; // ⛔ ここで処理を強制終了して、下の「置く処理」を絶対に実行させない！
                            }
                        }

                        if(RemainingCost >= GetSelectedPlacementCost())
                        {
                            int x = (int)Mathf.Round(hit.point.x);
                            int y = (int)Mathf.Round(hit.point.y);

                            Vector2Int pos = new Vector2Int(x, y);

                            switch (allyUnits[currentIndex].howToPut){
                            case HowToPut.None:
                                // RuleSystemを使って「ここに味方を置けるか？」をチェック
                                if (RuleSystem.Instance.CanPlaceAlly(pos) == 3)
                                {
                                    if(currentPlacePhase == PlacementPhase.SelectingCell){
                                        
                                        pendingUnit = boardManager.SpawnPreviewAlly(x,y,currentIndex);

                                        ShowDirectionUI(new Vector3(x, y, 0));
                                        currentPlacePhase = PlacementPhase.SelectingDirection;

                                        if (mainUICanvasGroup != null)
                                        {
                                            mainUICanvasGroup.interactable = false;   // ボタンなどを一斉に無効化
                                            mainUICanvasGroup.blocksRaycasts = false; // クリックの当たり判定を無くす
                                            mainUICanvasGroup.alpha = 0.5f;           // 全体を半透明（暗く）する
                                        }
                                    }


                                    
                                    // ※ もし配置後にターンを終了させるならここで EndPlayerTurn() を呼びます
                                    // EndPlayerTurn();
                                }
                                else if(RuleSystem.Instance.CanPlaceAlly(pos) != 0)
                                {
                                    Debug.Log("そこには配置できません");
                                }
                                break;
                            case HowToPut.Sacrifice:
                                    if (RuleSystem.Instance.CanSacrifice(pos))
                                    {
                                        if(currentPlacePhase == PlacementPhase.SelectingCell){
                                        
                                            pendingUnit = boardManager.SpawnPreviewAlly(x,y,currentIndex);

                                            ShowDirectionUI(new Vector3(x, y, 0));
                                            currentPlacePhase = PlacementPhase.SelectingDirection;

                                            _pendingGridPos = pos;

                                            if (mainUICanvasGroup != null)
                                            {
                                                mainUICanvasGroup.interactable = false;   // ボタンなどを一斉に無効化
                                                mainUICanvasGroup.blocksRaycasts = false; // クリックの当たり判定を無くす
                                                mainUICanvasGroup.alpha = 0.5f;           // 全体を半透明（暗く）する
                                            }
                                        }
                                    }else
                                    {
                                        Debug.Log("そこには配置できません");
                                    }
                                break;
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
        if (currentPlacePhase == PlacementPhase.SelectingDirection) return;
        if(CurrentPhase == GamePhase.PlayerTurn)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (boardCollider.Raycast(ray, out hit, 100f))
            {
                float x = Mathf.Round(hit.point.x);
                float y = Mathf.Round(hit.point.y);

                Vector2Int pos = new Vector2Int((int)x, (int)y);

                if(RuleSystem.Instance.CanPlaceAlly(pos) == 0){
                    if (highlightSquareAble.activeSelf)
                    {
                        highlightSquareAble.SetActive(false);
                    }

                    if (highlightSquareUnable.activeSelf)
                    {
                        highlightSquareUnable.SetActive(false);
                    }
                    return;
                }

                if(AlreadySet == false && RemainingCost >= GetSelectedPlacementCost() && RuleSystem.Instance.CanPlaceAlly(pos) == 3){

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

    private int GetSelectedPlacementCost()
    {
        if (allyUnits == null || allyUnits.Count == 0) return int.MaxValue;
        return Mathf.Max(0, allyUnits[currentIndex].cost);
    }

    private void ShowDirectionUI(Vector3 position)
    {
        HideDirectionUI();

        currentDirectionUI = Instantiate(directionUIPrefab, position, Quaternion.identity);
    }

    private void HideDirectionUI()
    {
        if(currentDirectionUI != null)
        {
            Destroy(currentDirectionUI);
            currentDirectionUI = null;
        }
    }

    public void OnDirectionSelected(UnitDirection dir)
    {
        if(pendingUnit != null)
        {
            

            if (TutorialManager.IsTutorialMode && TutorialManager.Instance != null)
            {
                // マネージャーが「この向きを待ってるよ」と言っているのに、違う向きにしようとしたら…
                if (TutorialManager.Instance.ExpectedDir.HasValue && 
                    TutorialManager.Instance.ExpectedDir.Value != dir)
                {
                    
                    return; // ⛔ ブロック！向きを確定させない
                }
            }

            if(allyUnits[currentIndex].howToPut == HowToPut.Sacrifice)
            {
                Unit target = RuleSystem.Instance.GetUnitAt(_pendingGridPos);
                AllyUnit ally = target as AllyUnit;
                if (ally == null)
                {
                    CancelPlacement();
                    return;
                }
                ally.Sacrificed();
            }

            boardManager.FinalizeAlly(pendingUnit, dir);

            pendingUnit.PlayPlaceEffect(); 

            HideDirectionUI();
            pendingUnit = null;

            TryConsumeCost(GetSelectedPlacementCost());
            AlreadySet = true;


            if (mainUICanvasGroup != null)
            {
                mainUICanvasGroup.interactable = true;
                mainUICanvasGroup.blocksRaycasts = true;
                mainUICanvasGroup.alpha = 1.0f; // 透明度を100%（くっきり）に戻す
            }
                
            

            currentPlacePhase = PlacementPhase.SelectingCell;


        }
    }

    public void CancelPlacement()
    {
        if(pendingUnit != null)
        {
            Destroy(pendingUnit.gameObject);
            pendingUnit = null;
        }

        HideDirectionUI();

        if(mainUICanvasGroup != null)
        {
            mainUICanvasGroup.interactable = true;
            mainUICanvasGroup.blocksRaycasts = true;
            mainUICanvasGroup.alpha = 1.0f;
        }

        currentPlacePhase = PlacementPhase.SelectingCell;
    }

    


    /// <summary>
    /// ゲーム開始時の初期化処理
    /// </summary>
    public void InitializeGame()
    {
        CurrentPhase = GamePhase.Init;
        CurrentHp = _maxHp;
        RemainingCost = _maxCost;

        // UIパネルを非表示にしておく
        if (gameClearPanel != null) gameClearPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

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
        if (PlayerPrefs.GetInt("IsTutorialCleared", 0) == 0)
        {
            yield break;
        }
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
                if (!TutorialManager.IsTutorialMode){
                    if (cutInText != null) cutInText.text = "YOUR TURN";
                    if (cutInText != null) cutInText.color = Color.white;
                    if (cutInPanel != null) StartCoroutine(ShowCutInRoutine());
                }
                AlreadySet = false;
                
                break;

            case GamePhase.ExecuteAllySkills:
                // TODO: 「ターン終了時効果」を持つ味方ユニットのスキルを一斉発動する
                var allies = FindObjectsOfType<AllyUnit>();

                foreach (var ally in allies)
                {
                    if (ally == null) continue;

                    ally.OnTurnEnd();

                    if (CurrentPhase == GamePhase.GameClear || CurrentPhase == GamePhase.GameOver)
                    {
                        return;
                    }
                }

                // 発動完了後、EnemyTurnへ
                ChangePhase(GamePhase.EnemyTurn);
                break;

            case GamePhase.EnemyTurn:
                // 敵の合計攻撃力を計算してプレイヤーにダメージを与える
                var onEnemyTurnAllies = FindObjectsOfType<AllyUnit>();
                var enemies = FindObjectsOfType<EnemyUnit>();

                StartCoroutine(EnemyTurnSequenceRoutine(enemies, onEnemyTurnAllies));

                break;

            case GamePhase.GameClear:
                Debug.Log("GAME CLEAR!!");
                GameSession.EnsureInstance().HandleBattleClear();
                if (!TutorialManager.IsTutorialMode){
                    if (gameClearPanel != null) gameClearPanel.SetActive(true);
                }
                break;

            case GamePhase.GameOver:
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(0.5f, 0.5f);
                }
                Debug.Log("GAME OVER...");
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 現在のステージをもう一度やり直す（UIボタンから呼び出す用）
    /// </summary>
    public void RetryStage()
    {
        // 現在開いているシーンを再度読み込む
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// ステージ選択画面に戻る（UIボタンから呼び出す用）
    /// </summary>
    public void ReturnToStageSelect()
    {
        SceneManager.LoadScene("SelectStage");
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
    private void ProcessEnemyAttacks(int damage)
    {
        // TODO: 盤面にいる全敵ユニットを取得し、攻撃力の合計を出す
        // int totalEnemyDamage = CalculateTotalEnemyDamage();

        TakeDamage(damage);

        // 勝敗・進行の判定
        if (CurrentHp <= 0)
        {
            
            ChangePhase(GamePhase.GameOver);
        }
        else if (CheckAllEnemiesDefeated())
        {
            ChangePhase(GamePhase.GameClear);
        }
    }

    private void ActivateSkill()
    {
        

    }

    private void HandleEnemyDestroyed(EnemyUnit enemy)
    {
        if (CurrentPhase == GamePhase.GameClear || CurrentPhase == GamePhase.GameOver)
        {
            return;
        }

        if (CheckAllEnemiesDefeated())
        {
            ChangePhase(GamePhase.GameClear);
        }
    }

    public void TakeDamage(int damage)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        Debug.Log($"プレイヤーは {damage} のダメージを受けた。残りHP: {CurrentHp}");
    }

    public void Heal(int amount)
    {
        // ステージのHPは初期値。回復によって初期HPを超えられる。
        CurrentHp += Mathf.Max(0, amount);
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

    public void NextPiece()
    {
        if (!TutorialManager.IsTutorialMode && allyUnits.Count > 0){
            currentIndex = (currentIndex + 1) % allyUnits.Count;
            UpdateSelectionUI();
        }
    }
    public void PrevPiece()
    {
        if (!TutorialManager.IsTutorialMode && allyUnits.Count > 0){
            currentIndex--;
            if(currentIndex < 0)
            {
                currentIndex = allyUnits.Count - 1;
            }
            UpdateSelectionUI();
        }
    }


    void UpdateSelectionUI()
    {
        if (allyUnits == null || allyUnits.Count == 0) return;
        // ScriptableObject自体の名前(.name)ではなく、設定した unitName を使うと綺麗です
        nameDisplay.text = allyUnits[currentIndex].unitName;

        if (iconDisplay != null && allyUnits[currentIndex].unitSprite != null)
        {
            iconDisplay.sprite = allyUnits[currentIndex].unitSprite;
        }

        if (infoDisplay != null)
        {
            infoDisplay.text = allyUnits[currentIndex].GetDescription();
        }
    }

    public int GetSelected()
    {
        return currentIndex;
    }

    public void ChangeIndex(int index)
    {
        if (allyUnits == null || allyUnits.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, allyUnits.Count - 1);
        UpdateSelectionUI();
    }


    // ---------------- 以下、スタブ（後でUnitクラスと連携） ----------------


    // 敵のターンを順番に処理するコルーチン
    // 引数が2つに増えました！
    private IEnumerator EnemyTurnSequenceRoutine(EnemyUnit[] enemies, AllyUnit[] onEnemyTurnAllies)
    {
        Debug.Log("敵のターン開始");
        yield return new WaitForSeconds(0.5f);

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.CurrentHp <= 0) continue;


            int damage = enemy.AttackPower; 


            // 味方のシールドで順番に受ける
            foreach (var ally in onEnemyTurnAllies)
            {
                // 🚨 【超重要】ここで味方が生きているかチェック！
                if (ally == null || ally.CurrentHp <= 0) continue;

                damage = ally.Shield(damage);
            }

            if (damage > 0)
            {
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.1f);
                ProcessEnemyAttacks(damage); 

                if (CurrentPhase == GamePhase.GameOver || CurrentPhase == GamePhase.GameClear)
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.4f);
        }

        if (CurrentPhase == GamePhase.EnemyTurn)
        {
            ChangePhase(GamePhase.PlayerTurn);
        }

    }

    private bool CheckAllEnemiesDefeated()
    {
        // 撃破処理中の敵は、Destroyが完了する前でも倒れているものとして扱う
        var enemies = FindObjectsOfType<EnemyUnit>();

        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.IsDefeated)
            {
                return false;
            }
        }

        return true; 
    }
}
