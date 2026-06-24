using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyUnitData", menuName = "UnitData/EnemyUnitData")]
public class EnemyUnitData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("敵ユニットの固有ID")]
    public string unitId;
    [Tooltip("画面やログに表示する名前")]
    public string unitName;
    [Tooltip("敵の画像")]
    public Sprite unitSprite;
    [Tooltip("生成用プレハブ")]
    public GameObject prefab;

    [Header("ステータス")]
    [Tooltip("最大HP")]
    public int maxHp = 2;
    [Tooltip("攻撃力")]
    public int attackPower = 2;
}
