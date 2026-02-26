using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // ステータス表示用

/// <summary>
/// 敵ユニット（攻撃力と体力を持ち、毎ターンダメージを与えてくる）
/// </summary>
public class EnemyUnit : Unit
{
    [Header("敵ステータス")]
    [SerializeField] private int _attackPower = 2;
    [SerializeField] private int _maxHp = 2;

    [Header("UI参照")]
    [SerializeField] private TextMeshPro _statusText; // 「2/2」のように表示するテキスト

    public int CurrentHp { get; private set; }
    public int AttackPower => _attackPower;

    public static event Action<EnemyUnit> OnEnemyDestroyed;

    private void Awake()
    {
        _unitType = UnitType.Enemy;
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
    public void TakeDamage(int damage)
    {
        CurrentHp -= damage;
        UpdateStatusText();

        Debug.Log($"敵が {damage} ダメージを受けた。残りHP: {CurrentHp}");

        if (CurrentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 囲碁のルール（4方向囲み）によって破壊される処理
    /// </summary>
    public void Captured()
    {
        Debug.Log("敵が四方を囲まれて破壊された！");
        Die();
    }

    private void Die()
    {
        // 破壊イベントを発行（盤面からの登録解除や勝敗判定などに使う）
        OnEnemyDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    private void UpdateStatusText()
    {
        if (_statusText != null)
        {
            _statusText.text = $"{_attackPower} / {CurrentHp}";
        }
    }
}
