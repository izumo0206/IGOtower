using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 盤面の各マスの状態を管理するクラス
/// </summary>
public class Tile : MonoBehaviour
{
    [Header("マスの見た目")]
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Color _baseColor;
    [SerializeField] private Color _offsetColor;

    // マスの座標
    public Vector2Int GridPosition { get; private set; }

    /// <summary>
    /// マスを初期化する
    /// </summary>
    public void Init(bool isOffset, Vector2Int gridPos)
    {
        GridPosition = gridPos;
        
        // 視認性を良くするために市松模様にする（色分け）
        _renderer.color = isOffset ? _offsetColor : _baseColor;
    }

    /// <summary>
    /// ユニットがこのマスに配置可能か判定する（後で拡張）
    /// </summary>
    public bool CanPlaceUnit()
    {
        // TODO: 「すでにユニットがいるか」「四方が敵に囲まれているか」の判定を入れる
        return true; 
    }
}
