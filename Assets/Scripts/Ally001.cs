using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ally001 : AllyUnit
{
    public override void OnPlaced()
    {
        if (_unitData != null)
        {
            Debug.Log($"【入場時スキル】指定方向１マスに攻撃");
        }
    }
}
