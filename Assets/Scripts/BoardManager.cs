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
    [SerializeField] private AllyUnit _allyPrefab;
    
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
    }

    private void Start()
    {
        LoadStage(1);
        GenerateGrid();
    }

    public void SetAlly(int x, int y)
    {
        var ally = Instantiate(_allyPrefab, new Vector3(x, y, 0), Quaternion.identity);
        ally.Init(new Vector2Int(x, y));
        RuleSystem.Instance.RegisterUnit(ally);
    }

    public void LoadStage(int stageNumber)
    {
        string filename = "map" + stageNumber;

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

            for(int x = 0; x < _width; x++)
            {
                boardData[x, y] = int.Parse(values[x]);
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
                // ★ もし数字が2だったら、「敵」も生成してタイルの上に配置する
                if (boardData[x, y] == 2)
                {
                    // 敵のプレハブを生成
                    var enemy = Instantiate(_enemyPrefab, new Vector3(x, y, 0), Quaternion.identity);

                    // Unit.cs にある Init() メソッドを使って座標を初期化
                    enemy.Init(new Vector2Int(x, y));
                    // 【とても重要】 RuleSystem に「ここに敵がいるよ」と登録する
                    RuleSystem.Instance.RegisterUnit(enemy);
                }
            }
        }

        // 生成した盤面が画面の中央に来るようにカメラ位置を調整する
        CenterCamera();
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
