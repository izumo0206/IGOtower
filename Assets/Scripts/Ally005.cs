using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ally005 : AllyUnit
{
    public override void OnTurnEnd()
    {
        if (_unitData != null)
        {
            Debug.Log($"【終了時スキル】周囲全てに攻撃");
        }
    }
}