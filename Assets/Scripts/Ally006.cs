using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ally006 : AllyUnit
{
    public override void OnPlaced()
    {
        if (_unitData != null)
        {
            Debug.Log($"【入場時スキル】自分のリーダーを回復");
        }
    }
}