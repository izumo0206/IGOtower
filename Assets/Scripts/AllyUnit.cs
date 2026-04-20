using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AllyUnit : Unit
{
    [Header("表示設定")]
    [Tooltip("キャラクター画像を表示するSpriteRenderer（子オブジェクトのSquareなど）")]
    [SerializeField] private SpriteRenderer _unitSpriteRenderer;

    [Header("ユニットデータ")]
    [SerializeField] protected AllyUnitData _unitData;

    public int CurrentHp { get; private set; }
    
    // データから各種ステータスを参照（データがない場合の対策付き）
    public int AttackPower => _unitData != null ? _unitData.attackPower : 0;
    public int Cost => _unitData != null ? _unitData.cost : 0;
    
    // データからIDやスキル種別を取得できるようにプロパティを公開
    public string UnitId => _unitData != null ? _unitData.unitId : "";
    public SkillType Skill => _unitData != null ? _unitData.skillType : SkillType.None;

    public static event Action<AllyUnit> OnAllyDestroyed;

    private void Awake()
    {
        _unitType = UnitType.Ally;
        
        // データが設定されているかチェック
        if (_unitData != null)
        {
            CurrentHp = _unitData.maxHp;

            // 画像の差し替え処理
            if (_unitSpriteRenderer == null)
            {
                // インスペクターでアタッチし忘れた時のための自動取得
                _unitSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_unitSpriteRenderer != null && _unitData.unitSprite != null)
            {
                _unitSpriteRenderer.sprite = _unitData.unitSprite;
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} に UnitData がアタッチされていません！", this);
        }
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

    public virtual void OnPlaced() { }

    // ターン開始時
    public virtual void OnTurnStart() { }

    // ターン終了時
    public virtual void OnTurnEnd() { }

    /// <summary>
    /// ユニットデータのスキル種別に基づいた特別な挙動（例）
    /// 実際のゲーム内の攻撃フェーズ等から呼び出して使います。
    /// </summary>
}
