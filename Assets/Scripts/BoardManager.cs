using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 盤面の生成とマス目(Tile)の管理を行うクラス
/// </summary>
public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("盤面設定")]
    [SerializeField] private int _width = 5;
    [SerializeField] private int _height = 5;
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private EnemyUnit _enemyPrefab;
    
    [Header("カメラ関連")]
    [SerializeField] private Transform _cameraTransform;

    // 座標ごとのマスの参照を保持する辞書
    private Dictionary<Vector2Int, Tile> _tiles;

    private int[,] boardData;

    public GameManager gameManager;

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

        string mapText;
        if (GameSession.EnsureInstance().TryGetSelectedMap(out mapText))
        {
            ParseMapData(mapText);
        }
        else
        {
            LoadStage(1);
        }
    }

    private void Start()
    {
        GenerateGrid();
    }

    public AllyUnit SpawnPreviewAlly(int x, int y, int index)
    {
        AllyUnitData currentData = gameManager.allyUnits[index];

        GameObject prefabToInstantiate = currentData.prefab;

        var go = Instantiate(prefabToInstantiate, new Vector3(x, y, 0), Quaternion.identity);
        var ally = go.GetComponent<AllyUnit>();
        
        // ★ 生成したユニットに「あなたはどのユニットデータか」を教える
        ally.SetupData(currentData);
        
        ally.Init(new Vector2Int(x, y));

        ally.SetAlpha(0.5f);

        return ally;
    }

    public void FinalizeAlly(AllyUnit ally, UnitDirection dir)
    {

        ally.SetDirection(dir);

        ally.SetAlpha(1.0f);

        ally.OnPlaced();
        RuleSystem.Instance.RegisterUnit(ally);

        if (TutorialManager.IsTutorialMode && TutorialManager.Instance != null)
        {
            // 「この座標に置いたよ！」と報告を投げる
            TutorialManager.Instance.ReportAllyPlaced(ally.GridPosition, ally.UnitId, dir);
        }
    }

    public void LoadStage(int stageNumber)
    {
        string filename = "Stage/map" + stageNumber;

        TextAsset mapText = Resources.Load<TextAsset>(filename);

        if(mapText != null)
        {
            ParseMapData(mapText.text);
            Debug.Log(filename + " の読み込みに成功しました。");
        } else
        {
            Debug.LogError(filename + "が見つかりません");
        }
    }

    public void ParseMapData(string text)
    {
        
        string[] lines = text.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
        _height = lines.Length - 1;
        _width = lines[1].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Length;
        boardData = new int[_width, _height];

        string[] value1 = lines[0].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        gameManager.setStatus(int.Parse(value1[0]), int.Parse(value1[1]));

        for (int y = 0; y < _height; y++)
        {
            string[] values = lines[y+1].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != _width)
            {
                throw new FormatException($"マップの横幅が一致していません。行: {y + 2}");
            }

            for(int x = 0; x < _width; x++)
            {
                if (!int.TryParse(values[x], out int cellType))
                {
                    throw new FormatException($"マップのセル指定が不正です: {values[x]}");
                }

                boardData[x, y] = cellType;
            }
        }
    }

    /// <summary>
    /// 指定された幅と高さでグリッド（盤面）を生成する
    /// </summary>
    public void GenerateGrid()
    {
        _tiles = new Dictionary<Vector2Int, Tile>();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if(boardData[x, y] >= 1)
                {
                    // タイルを生成 (X, Yの座標に配置)
                    var spawnedTile = Instantiate(_tilePrefab, new Vector3(x, y, 0), Quaternion.identity, this.transform);
                    spawnedTile.name = $"Tile {x} {y}";

                    // 背景色がきれいに交互になるようにフラグを渡す（市松模様）
                    var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                    spawnedTile.Init(isOffset, new Vector2Int(x, y));

                    // 辞書に登録
                    _tiles[new Vector2Int(x, y)] = spawnedTile;
                }
                // 2以上の数字は敵として扱い、Resources/EnemyUnit/{数字} の敵データを読む。
                if (boardData[x, y] >= 2)
                {
                    SpawnEnemy(x, y, boardData[x, y]);
                }
            }
        }

        // 生成した盤面が画面の中央に来るようにカメラ位置を調整する
        CenterCamera();
    }

    private EnemyUnit SpawnEnemy(int x, int y, int mapValue)
    {
        EnemyUnitData enemyData = GetEnemyData(mapValue);
        if (enemyData == null)
        {
            return null;
        }

        GameObject prefabToInstantiate = GetEnemyPrefab(enemyData, mapValue);

        if (prefabToInstantiate == null)
        {
            return null;
        }

        GameObject go = Instantiate(prefabToInstantiate, new Vector3(x, y, 0), Quaternion.identity);
        EnemyUnit enemy = go.GetComponent<EnemyUnit>();

        if (enemy == null)
        {
            Debug.LogError($"{prefabToInstantiate.name} に EnemyUnit コンポーネントがありません");
            Destroy(go);
            return null;
        }

        if (enemyData != null)
        {
            enemy.SetupData(enemyData);
        }

        enemy.Init(new Vector2Int(x, y));
        RuleSystem.Instance.RegisterUnit(enemy);
        return enemy;
    }

    private EnemyUnitData GetEnemyData(int mapValue)
    {
        string resourcePath = $"EnemyUnit/{mapValue}";
        EnemyUnitData enemyData = Resources.Load<EnemyUnitData>(resourcePath);

        if (enemyData != null)
        {
            return enemyData;
        }

        Debug.LogError($"マップ値 {mapValue} に対応する EnemyUnitData が Resources/{resourcePath}.asset に見つかりません");

        return null;
    }

    private GameObject GetEnemyPrefab(EnemyUnitData enemyData, int mapValue)
    {
        if (enemyData != null && enemyData.prefab != null)
        {
            return enemyData.prefab;
        }

        if (_enemyPrefab == null)
        {
            Debug.LogError($"{enemyData.name} にプレハブが設定されておらず、BoardManagerのデフォルト敵プレハブも未設定です");
            return null;
        }

        Debug.LogWarning($"{enemyData.name} にプレハブが設定されていないため、デフォルト敵プレハブを使います");
        return _enemyPrefab.gameObject;
    }

    /// <summary>
    /// 指定した座標にマスが存在するかどうか（盤面の外側でないか）判定
    /// </summary>
    public bool IsValidPosition(Vector2Int pos)
    {
        return _tiles.ContainsKey(pos);
    }

    /// <summary>
    /// カメラを盤面の中央に移動させるヘルパーメソッド
    /// </summary>
    private void CenterCamera()
    {
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main.transform;
        }

        // カメラ位置をグリッドの真ん中にして、全体が映るようにZ軸を下げる
        _cameraTransform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
    }
}
