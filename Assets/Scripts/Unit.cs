using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Enemy,
    Ally
}

/// <summary>
/// 盤面に配置されるすべての駒（ユニット）の基底クラス
/// </summary>
public abstract class Unit : MonoBehaviour
{
    [Header("基本ステータス")]
    [SerializeField] protected UnitType _unitType;
    
    public Vector2Int GridPosition { get; protected set; }
    public UnitType Type => _unitType;

    /// <summary>
    /// ユニットの初期化
    /// </summary>
    public virtual void Init(Vector2Int position)
    {
        GridPosition = position;
        // マスの座標に合わせてUnity上の座標も更新
        transform.position = new Vector3(position.x, position.y, 0f);
    }
}
