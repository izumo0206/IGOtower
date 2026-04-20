using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ally004 : AllyUnit
{
    public override void OnTurnEnd()
    {
        if (_unitData != null)
        {
            Debug.Log($"【終了時スキル】指定方向１マスに攻撃");
        }
    }
}