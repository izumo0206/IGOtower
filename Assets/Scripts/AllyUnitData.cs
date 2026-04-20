using UnityEngine;

// 後に拡張しやすいように列挙型(Enum)でスキルの種類を定義します
public enum SkillType
{
    None,       // スキルなし（基本ユニット）
    EnDa1,     // [入場時]指定方向1マスに攻撃
    EnDaAll,     // [入場時]周囲すべてに攻撃
    EnHe,     // [入場時]自分のリーダーを回復
    ExDa1,     // [終了時]指定方向1マスに攻撃
    ExDaAll,     // [終了時]周囲すべてに攻撃
    ExHe      // [終了時]自分のリーダーを回復
}

[CreateAssetMenu(fileName = "NewAllyUnitData", menuName = "UnitData/AllyUnitData")]
public class AllyUnitData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("ユニットの固有ID")]
    public string unitId;
    [Tooltip("画面に表示する名前")]
    public string unitName;
    [Tooltip("キャラクターの画像")]
    public Sprite unitSprite;
    [Tooltip("配置コスト")]
    public int cost = 1;

    [Header("ステータス")]
    [Tooltip("最大HP")]
    public int maxHp = 2;
    [Tooltip("攻撃力")]
    public int attackPower = 2;

    [Header("挙動設定")]
    [Tooltip("このユニットの機能・スキル種別")]
    public SkillType skillType = SkillType.None;

    public AllyUnit prefab;
    
    // 必要周辺データ（プレハブなど）もここに追加できます
    // public GameObject prefab;
}
