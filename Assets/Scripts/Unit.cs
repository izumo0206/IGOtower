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

    public IEnumerator SlideTo(Vector2 targetWorldPos, float duration)
    {
        Vector2 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Vector3.Lerp は「A地点からB地点までの間を、割合(0.0〜1.0)で移動する」魔法の関数です！
            transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 最後にズレを直してピッタリ目的地に合わせる
        transform.position = targetWorldPos;
    }

    public IEnumerator BumpAttack(Vector2 targetPos, float bumpDistance, float duration)
    {
        Vector2 startPos = transform.position;
        
        // 相手がどっちの方向にいるか（向き）を計算する
        Vector2 direction = (targetPos - startPos).normalized;
        // ちょっとだけ前に出た「踏み込み位置」を計算
        Vector2 bumpPos = startPos + direction * bumpDistance;

        float halfDuration = duration / 2f;
        float elapsed = 0f;

        // 前半：グッと踏み込む！
        while (elapsed < halfDuration)
        {
            transform.position = Vector3.Lerp(startPos, bumpPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        
        // 後半：サッと元の位置に戻る！
        while (elapsed < halfDuration)
        {
            transform.position = Vector3.Lerp(bumpPos, startPos, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
    }
}
