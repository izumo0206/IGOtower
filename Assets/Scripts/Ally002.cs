using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ally002 : AllyUnit
{
    public override void OnPlaced()
    {
        if (_unitData != null)
        {
            Debug.Log($"【入場時スキル】周囲全てに攻撃");
        }
    }
}