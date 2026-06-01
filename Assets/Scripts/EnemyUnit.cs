using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Data; // ステータス表示用

/// <summary>
/// 敵ユニット（攻撃力と体力を持ち、毎ターンダメージを与えてくる）
/// </summary>
public class EnemyUnit : Unit
{
    [Header("敵ステータス")]
    [SerializeField] private int _attackPower = 2;
    [SerializeField] private int _maxHp = 2;

    [Header("UI参照")]
    [SerializeField] private TextMeshPro _statusATKText;
    [SerializeField] private TextMeshPro _statusHPText;

    public int CurrentHp { get; private set; }
    public int AttackPower => _attackPower;

    [Header("演出用")]
    public GameObject dieEffectPrefab;

    /// <summary>
    /// 攻撃力を指定した数値だけ下げる（デバフ）
    /// 下限は0（マイナスにはならない）
    /// </summary>
    public void DebuffAttack(int amount)
    {
        _attackPower = Mathf.Max(0, _attackPower - amount);
        Debug.Log($"敵の攻撃力が {amount} 下がった。現在の攻撃力: {_attackPower}");
        UpdateStatusText();
    }

    public void Pushed(int amount, Vector2Int direction)
    {
        Vector2Int position = GridPosition + amount * direction;
        if(RuleSystem.Instance.CanPlaceAlly(position) == 3){
            StartCoroutine(this.SlideTo(position, 0.2f));
            RuleSystem.Instance.RemoveUnit(this);
            Init(position);
            RuleSystem.Instance.RegisterUnit(this);
        }
    }

    public void Pulled(int amount, Vector2Int direction)
    {
        Vector2Int position = GridPosition - amount * direction;
        if(RuleSystem.Instance.CanPlaceAlly(position) == 3){
            StartCoroutine(this.SlideTo(position, 0.2f));
            RuleSystem.Instance.RemoveUnit(this);
            Init(position);
            RuleSystem.Instance.RegisterUnit(this);
        }
    }

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
        if (dieEffectPrefab != null)
        {
            Vector3Int effectPos = new Vector3Int(this.GridPosition.x, this.GridPosition.y, 0);
            // 敵の座標（targetEnemy.transform.position）にエフェクトを生成
            GameObject effect = Instantiate(dieEffectPrefab, effectPos, Quaternion.identity);

            Destroy(effect, 1.0f); 
        }
        OnEnemyDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    private void UpdateStatusText()
    {
        if (_statusATKText != null)
        {
            _statusATKText.text = _attackPower.ToString();
        }

        if (_statusHPText != null)
        {
            _statusHPText.text = CurrentHp.ToString();
        }
    }
}
