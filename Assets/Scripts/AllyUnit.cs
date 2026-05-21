using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum UnitDirection
{
    Up,
    Right,
    Down,
    Left
}

public class AllyUnit : Unit
{
    [Header("表示設定")]
    [Tooltip("キャラクター画像を表示するSpriteRenderer（子オブジェクトのSquareなど）")]
    [SerializeField] private SpriteRenderer _unitSpriteRenderer;

    [Header("ユニットデータ")]
    [SerializeField] protected AllyUnitData _unitData;

    [Header("現在の状態")]
    public UnitDirection currentDirection = UnitDirection.Up;

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
    }

    /// <summary>
    /// 生成された直後にBoardManagerからユニットデータを渡される
    /// </summary>
    public void SetupData(AllyUnitData data)
    {
        _unitData = data;

        if (_unitData != null)
        {
            CurrentHp = _unitData.maxHp;

            // 画像の差し替え処理
            if (_unitSpriteRenderer == null)
            {
                _unitSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_unitSpriteRenderer != null && _unitData.unitSprite != null)
            {
                _unitSpriteRenderer.sprite = _unitData.unitSprite;

                // 【追加】どんな大きさの画像でも1マス（1x1）に収まるように自動縮小・拡大する
                _unitSpriteRenderer.transform.localScale = Vector3.one; // 一旦スケールをリセット
                Vector2 spriteSize = _unitSpriteRenderer.sprite.bounds.size;
                
                // 縦横で大きい方に合わせてスケールを計算（アスペクト比を維持）
                float maxBound = Mathf.Max(spriteSize.x, spriteSize.y);
                if (maxBound > 0)
                {
                    float scale = 1f / maxBound;
                    // マス目からはみ出ないように少しだけ余白を持たせるなら 0.9f を掛ける
                    _unitSpriteRenderer.transform.localScale = new Vector3(scale * 0.9f, scale * 0.9f, 1f);
                }
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} に UnitData が渡されていません！", this);
        }
    }

    public override void Init(Vector2Int position)
    {
        base.Init(position);
        UpdateStatusText();
    }

    /// <summary>
    /// BoardManagerから配置完了時に呼ばれる処理
    /// </summary>
    public void OnPlaced()
    {
        // ユニットの初期化（配置）が完了したタイミングで、「入場時スキル」を持っていれば発動する
        if (_unitData != null && _unitData.skillTiming == SkillTiming.OnPlaced)
        {
            ExecuteSkill();
        }
    }

    public void PlayPlaceEffect()
    {
        Debug.Log("エフェクト開始！");
        StartCoroutine(ThudAnimation());
    }

    private IEnumerator ThudAnimation()
    {
        // ★追加：最初に自分の「本来のサイズ」を記憶しておく！
        Vector3 baseScale = transform.localScale;

        // 本来のサイズの 1.2倍 にする
        transform.localScale = baseScale * 1.2f;

        float duration = 0.08f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; 

            // 1.2倍のサイズから、元のサイズ(baseScale)へ徐々に戻す
            transform.localScale = Vector3.Lerp(baseScale * 1.2f, baseScale, t);
            
            yield return null;
        }

        // 最後に確実に「記憶しておいた本来のサイズ」に戻す
        transform.localScale = baseScale; 
    }

    /// <summary>
    /// スキルダメージを受ける処理
    /// </summary>

    /// <summary>
    /// GameManagerの ExecuteAllySkills フェーズなどから毎ターン終了時に呼ばれる処理
    /// </summary>
    public void OnTurnEnd()
    {
        // ターン終了時スキルを持っていれば発動する
        if (_unitData != null && _unitData.skillTiming == SkillTiming.OnTurnEnd)
        {
            ExecuteSkill();
        }
    }

    /// <summary>
    /// ユニットデータのスキル種別に基づいた処理の実行
    /// </summary>
    private void ExecuteSkill()
    {
        if (_unitData == null) return;

        switch (_unitData.skillType)
        {
            case SkillType.None:
                // 特になし
                break;

            case SkillType.AttackOneDir:
                // ※方向指定の実装は後日行うため、現在は仮で「上(Up)」方向の1マスを攻撃対象とします
                Vector2Int targetPos = GridPosition + Vector2Int.up;
                switch (currentDirection)
                {
                    case UnitDirection.Up: targetPos = GridPosition + Vector2Int.up; break;
                    case UnitDirection.Right: targetPos = GridPosition + Vector2Int.right; break;
                    case UnitDirection.Down: targetPos = GridPosition + Vector2Int.down; break;
                    case UnitDirection.Left: targetPos = GridPosition + Vector2Int.left; break;
                }
                
                Unit target = RuleSystem.Instance.GetUnitAt(targetPos);
                
                if (target != null && target is EnemyUnit enemy)
                {
                    Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：指定方向({targetPos})の敵に {AttackPower} ダメージ！");
                    enemy.TakeDamage(AttackPower);
                }
                else
                {
                    Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：しかし攻撃方向に敵はいなかった");
                }
                break;

            case SkillType.AttackAllSurround:
                Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：周囲全てに攻撃！");
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var dir in dirs)
                {
                    Unit neighbor = RuleSystem.Instance.GetUnitAt(GridPosition + dir);
                    if (neighbor != null && neighbor is EnemyUnit surroundEnemy)
                    {
                        surroundEnemy.TakeDamage(AttackPower);
                    }
                }
                break;

            case SkillType.HealLeader:
                Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：自分のリーダーを {AttackPower} 回復！");
                if (GameManager.Instance != null)
                {
                    // GameManagerのHeal機能を使ってプレイヤーのHPを攻撃力分回復させる
                    GameManager.Instance.Heal(AttackPower);
                }
                break;

            case SkillType.DebuffOneDir:
                // 指定方向の1マスの敵の攻撃力を下げる
                Vector2Int debuffTargetPos = GridPosition;
                switch (currentDirection)
                {
                    case UnitDirection.Up:    debuffTargetPos = GridPosition + Vector2Int.up;    break;
                    case UnitDirection.Right: debuffTargetPos = GridPosition + Vector2Int.right; break;
                    case UnitDirection.Down:  debuffTargetPos = GridPosition + Vector2Int.down;  break;
                    case UnitDirection.Left:  debuffTargetPos = GridPosition + Vector2Int.left;  break;
                }

                Unit debuffTarget = RuleSystem.Instance.GetUnitAt(debuffTargetPos);
                if (debuffTarget != null && debuffTarget is EnemyUnit debuffEnemy)
                {
                    Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：指定方向({debuffTargetPos})の敵の攻撃力を {AttackPower} 下げる！");
                    debuffEnemy.DebuffAttack(AttackPower);
                }
                else
                {
                    Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：方向に敵はいなかった");
                }
                break;

            case SkillType.DebuffAllSurround:
                // 周囲4方向の敵全員の攻撃力を下げる
                Debug.Log($"【{_unitData.skillTiming}】{_unitData.unitName} のスキル発動：周囲全ての敵の攻撃力を {AttackPower} 下げる！");
                Vector2Int[] debuffDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var dir in debuffDirs)
                {
                    Unit neighbor = RuleSystem.Instance.GetUnitAt(GridPosition + dir);
                    if (neighbor != null && neighbor is EnemyUnit surroundDebuffEnemy)
                    {
                        surroundDebuffEnemy.DebuffAttack(AttackPower);
                    }
                }
                break;
        }
    }

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

    public void SetDirection(UnitDirection newDirection)
    {
        currentDirection = newDirection;

        float zAngle = 0f;
        switch (currentDirection)
        {
            case UnitDirection.Up: zAngle = 0f; break;
            case UnitDirection.Right: zAngle = -90f; break;
            case UnitDirection.Down: zAngle = 180f; break;
            case UnitDirection.Left: zAngle = 90f; break;
        }

        if(_unitSpriteRenderer != null)
        {
            _unitSpriteRenderer.transform.localRotation = Quaternion.Euler(0,0,zAngle);
        }
    }

    public void SetAlpha(float alpha)
    {
        if(_unitSpriteRenderer != null)
        {
            Color currentColor = _unitSpriteRenderer.color;
            currentColor.a = alpha;
            _unitSpriteRenderer.color = currentColor;
        }
    }


}
