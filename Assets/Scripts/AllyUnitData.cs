using UnityEngine;

// スキルの発動タイミング
public enum SkillTiming
{
    None,
    OnPlaced,   // 配置した瞬間に発動
    OnTurnEnd   // ターン終了時に発動
}

// スキルの種類（Ally001〜006の機能を統合）
public enum SkillType
{
    None,
    AttackOneDir,       // 指定方向１マスに攻撃
    AttackAllSurround,  // 周囲全てに攻撃
    HealLeader          // 自分のリーダーを回復
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
    [Tooltip("生成用プレハブ")]
    public GameObject prefab;

    [Header("ステータス")]
    [Tooltip("最大HP")]
    public int maxHp = 2;
    [Tooltip("攻撃力")]
    public int attackPower = 2;

    [Header("挙動設定")]
    [Tooltip("スキルの発動タイミング")]
    public SkillTiming skillTiming = SkillTiming.None;
    [Tooltip("スキルの種類")]
    public SkillType skillType = SkillType.None;

    /// <summary>
    /// UI表示用の詳細テキストを生成して返す
    /// </summary>
    public string GetDescription()
    {
        // 攻撃力の数字を別の色（オレンジ色等）にするためのTextMeshProリッチテキストタグ
        string powText = $"<color=#FF7F50>{attackPower}</color>";

        string desc = $"コスト: {cost}   HP: {maxHp}   POWER: {powText}\n";
        
        if (skillType == SkillType.None)
        {
            desc += "スキル: なし";
            return desc;
        }

        string timingStr = skillTiming == SkillTiming.OnPlaced ? "【入場時】" : "【ターン終了時】";
        string effectStr = "";
        switch (skillType)
        {
            case SkillType.AttackOneDir: 
                effectStr = $"指定方向1マスの敵に {powText} ダメージ"; 
                break;
            case SkillType.AttackAllSurround: 
                effectStr = $"周囲の敵すべてに {powText} ダメージ"; 
                break;
            case SkillType.HealLeader: 
                // 回復スキルの数字も色を変える場合（例：緑色）
                string healText = $"<color=#32CD32>{attackPower}</color>";
                effectStr = $"プレイヤーのHPを {healText} 回復"; 
                break;
        }

        desc += $"スキル: {timingStr}\n{effectStr}";
        return desc;
    }
}
