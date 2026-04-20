using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 囲碁の「囲み判定」やユニットの配置ルールなどを処理するシステム
/// </summary>
public class RuleSystem : MonoBehaviour
{
    public static RuleSystem Instance { get; private set; }

    // 現在盤面に配置されているすべてのユニットを管理する辞書
    private Dictionary<Vector2Int, Unit> _pieces = new Dictionary<Vector2Int, Unit>();

    // 4方向のベクトル（上、下、左、右）
    private readonly Vector2Int[] _directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EnemyUnit.OnEnemyDestroyed += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        EnemyUnit.OnEnemyDestroyed -= HandleEnemyDestroyed;
    }

    /// <summary>
    /// 盤面にユニットを登録する
    /// </summary>
    public void RegisterUnit(Unit unit)
    {
        _pieces[unit.GridPosition] = unit;
        CheckSurroundings(unit.GridPosition);
    }

    private void HandleEnemyDestroyed(EnemyUnit enemy)
    {
        if (_pieces.ContainsKey(enemy.GridPosition))
        {
            _pieces.Remove(enemy.GridPosition);
        }
    }

    /// <summary>
    /// 特定のマスに味方の駒を配置できるか判定する
    /// </summary>
    public int CanPlaceAlly(Vector2Int pos)
    {
        // 1. 盤面外なら配置不可
        if (!BoardManager.Instance.IsValidPosition(pos)) return 0;

        // 2. すでに何か駒が置いてあれば配置不可
        if (_pieces.ContainsKey(pos)) return 1;

        // 3. 四方を敵に囲まれている場合は配置不可
        int enemyCount = 0;
        foreach (var dir in _directions)
        {
            Vector2Int neighborPos = pos + dir;
            // 盤面の範囲内で、かつ敵ユニットが存在するか
            if (_pieces.TryGetValue(neighborPos, out Unit neighborUnit))
            {
                if (neighborUnit.Type == UnitType.Enemy)
                {
                    enemyCount++;
                }
            }
        }
        
        // 四方が敵 (4つの隣接マスすべてに敵がいる) なら配置不可
        if (enemyCount >= 4)
        {
            return 2;
        }

        return 3;
    }

    /// <summary>
    /// 指定した座標の周囲で「囲みによる破壊」が起きたかチェックする
    /// (新しい駒を置いた直後に呼ぶ)
    /// </summary>
    public void CheckSurroundings(Vector2Int newPiecePos)
    {
        // 新しく置かれた駒の周囲4方向について、敵ユニットが囲まれていないかチェック
        foreach (var dir in _directions)
        {
            Vector2Int checkPos = newPiecePos + dir;

            if (_pieces.TryGetValue(checkPos, out Unit targetUnit))
            {
                if (targetUnit is EnemyUnit enemy)
                {
                    if (IsEnemySurrounded(enemy))
                    {
                        enemy.Captured();
                    }
                }
            }
        }
        
        // （※将来的に味方ユニットも囲まれて破壊される仕様があるなら、味方に対するチェックも追加する）
    }

    /// <summary>
    /// 対象の敵ユニットが完全に囲まれているか（呼吸点がないか）判定する
    /// （※今回は「単体の敵ユニット」にフォーカスしたシンプルな判定になっています。
    ///  複数の敵が連結している場合のグループ呼吸点チェックには後々拡張可能です）
    /// </summary>
    private bool IsEnemySurrounded(EnemyUnit enemy)
    {
        Vector2Int pos = enemy.GridPosition;

        foreach (var dir in _directions)
        {
            Vector2Int neighborPos = pos + dir;

            // 1. 盤面外（壁）は「塞がっている」とみなす
            if (!BoardManager.Instance.IsValidPosition(neighborPos))
            {
                continue;
            }

            // 2. 空きマスがある（呼吸点がある）場合は囲まれていない
            if (!_pieces.ContainsKey(neighborPos))
            {
                return false;
            }

            // 3. 隣接マスに他の敵がいる場合（味方で塞がれていない場合）
            // ※今回「敵同士が繋がっているか」の処理をシンプルにするため、
            // 「隣接が味方」なら塞がっている、「隣接が敵」なら(ここは一旦呼吸点があるとして逃がすか、グループ判定にするか)
            // 現在は単体のみを対象とするため、「空きマス」がないなら全部味方（または壁）とみなすなら
            // すでに空きマスチェックを通過しているので「何かしらの駒で埋まっている」状態
            // もし「味方ユニットで塞がれている」かつ「壁」のみで囲まれていることを条件とするなら
            Unit neighbor = _pieces[neighborPos];
            if (neighbor.Type == UnitType.Enemy)
            {
                // ここは要調整。囲碁のように「連結した敵群全体が囲まれているか」を判定する場合、探索（BFS/DFS）が必要になる。
                // いったん「単体で囲まれたら死亡」というゆるい判定ならこのままfalse(つまり敵の隣なら安全)を返しても良い。
                // 暫定的に、「隣に仲間(敵)がいれば生き残る」としておく
                return false; 
            }
        }

        // 四方向すべてが「壁」か「味方ユニット」だった場合
        return true;
    }
}
