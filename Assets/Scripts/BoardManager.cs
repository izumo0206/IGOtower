using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    [Header("カメラ関連")]
    [SerializeField] private Transform _cameraTransform;

    // 座標ごとのマスの参照を保持する辞書
    private Dictionary<Vector2Int, Tile> _tiles;

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
        GenerateGrid();
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
                // タイルを生成 (X, Yの座標に配置)
                var spawnedTile = Instantiate(_tilePrefab, new Vector3(x, y, 0), Quaternion.identity, this.transform);
                spawnedTile.name = $"Tile {x} {y}";

                // 背景色がきれいに交互になるようにフラグを渡す（市松模様）
                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(isOffset, new Vector2Int(x, y));

                // 辞書に登録
                _tiles[new Vector2Int(x, y)] = spawnedTile;
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
