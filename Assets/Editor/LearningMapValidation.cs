using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Run from Unity's menu or -executeMethod LearningMapValidation.Run.
public static class LearningMapValidation
{
    [MenuItem("Tools/IGOtower/Validate Learning Maps")]
    public static void Run()
    {
        var root = new GameObject("Learning map validation");
        root.SetActive(false); // Avoid gameplay Awake/Start and progress writes.
        try
        {
            var game = root.AddComponent<GameManager>();
            var board = root.AddComponent<BoardManager>();
            board.gameManager = game;
            var hpField = typeof(GameManager).GetField("<CurrentHp>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            game.setStatus(2, 4);
            hpField.SetValue(game, 2);
            game.Heal(3);
            Require(game.CurrentHp == 5, "Healing must exceed initial HP.");
            game.Heal(-3);
            Require(game.CurrentHp == 5, "Negative healing must not cause damage.");

            int[] expectedHp = { 3, 2, 2, 3, 12, 17 };
            int[] expectedCost = { 2, 4, 3, 2, 5, 5 };
            for (int stage = 1; stage <= 14; stage++)
            {
                var inspectorAllies = new List<AllyUnitData> { Resources.Load<AllyUnitData>("AllyUnit/002") };
                game.allyUnits = inspectorAllies;
                var map = Resources.Load<TextAsset>("Stage/map" + stage);
                Require(map != null, "Missing map" + stage);
                board.ParseMapData(map.text);
                if (stage < 9)
                    Require(ReferenceEquals(game.allyUnits, inspectorAllies), "Legacy map changed Inspector roster.");
                else
                {
                    Require(game.MaxHp == expectedHp[stage - 9], "Incorrect initial HP.");
                    var costField = typeof(GameManager).GetField("_maxCost", BindingFlags.Instance | BindingFlags.NonPublic);
                    Require((int)costField.GetValue(game) == expectedCost[stage - 9], "Incorrect cost.");
                    Require(game.allyUnits.Count == 2 && game.allyUnits[0].unitName == "A" && game.allyUnits[1].unitName == "C", "Expected A/C roster.");
                }
                var data = (int[,])typeof(BoardManager).GetField("boardData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
                foreach (int value in data)
                {
                    if (value < 2) continue;
                    var enemy = Resources.Load<EnemyUnitData>("EnemyUnit/" + value);
                    Require(enemy != null && enemy.prefab != null && enemy.prefab.GetComponent<EnemyUnit>() != null, "Missing enemy data/prefab: " + value);
                }
            }
            bool rejected = false;
            try { board.ParseMapData("2 3 allies=does-not-exist\n1"); }
            catch (FormatException) { rejected = true; }
            Require(rejected, "Invalid roster must be rejected.");
            Debug.Log("LearningMapValidation PASS: 14 maps, A/C restrictions, enemy references, uncapped healing, invalid roster.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
