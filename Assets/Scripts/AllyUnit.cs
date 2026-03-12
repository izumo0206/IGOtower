using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AllyUnit : Unit
{
    [Header("味方ステータス")]
    [SerializeField] private int _attackPower = 2;
    [SerializeField] private int _maxHp = 2;

    public int CurrentHp { get; private set; }
    public int AttackPower => _attackPower;

    public static event Action<AllyUnit> OnAllyDestroyed;

    private void Awake()
    {
        _unitType = UnitType.Ally;
        CurrentHp = _maxHp;
    }

    public override void Init(Vector2Int position)
    {
        base.Init(position);
        UpdateStatusText();
    }

    /// <summary>
    /// スキルダメージを受ける処理
    /// </summary>

    /// <summary>
    /// 囲碁のルール（4方向囲み）によって破壊される処理
    /// </summary>
    public void Captured()
    {
        Debug.Log("味方が四方を囲まれて破壊された！");
        Die();
    }

    private void Die()
    {
        // 破壊イベントを発行（盤面からの登録解除や勝敗判定などに使う）
        OnAllyDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    private void UpdateStatusText()
    {
    
    }
}
