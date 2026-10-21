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

            int[] expectedHp = { 3, 2, 2, 3, 12, 17, 8, 10, 6, 17, 12, 11, 18, 22, 28, 6, 10, 28, 35, 37, 76, 30 };
            int[] expectedCost = { 2, 4, 3, 2, 5, 5, 3, 4, 5, 7, 5, 3, 5, 7, 7, 3, 4, 9, 7, 7, 7, 9 };
            string[] expectedAllies = { "AC", "AC", "AC", "AC", "AC", "AC",
                "ABC", "ACK", "ACF", "ACO", "AC", "ABC", "ABCK", "ACFO", "ABCFKO", "ACG", "ACH", "ACGH",
                "ACD", "ACE", "ACDE", "ACDEGH" };
            var unitO = Resources.Load<AllyUnitData>("AllyUnit/015");
            Require(unitO != null && unitO.unitName == "O" && unitO.cost == 2 && unitO.maxHp == 2
                && unitO.attackPower == 3 && unitO.skillTiming == SkillTiming.OnPlaced
                && unitO.skillType == SkillType.AttackOneDir && unitO.howToPut == HowToPut.None
                && unitO.prefab != null, "Incorrect O data.");
            for (int stage = 1; stage <= expectedHp.Length; stage++)
            {
                var inspectorAllies = new List<AllyUnitData> { Resources.Load<AllyUnitData>("AllyUnit/002") };
                game.allyUnits = inspectorAllies;
                var map = Resources.Load<TextAsset>("Stage/map" + stage);
                Require(map != null, "Missing map" + stage);
                board.ParseMapData(map.text);
                {
                    Require(game.MaxHp == expectedHp[stage - 1], "Incorrect initial HP.");
                    var costField = typeof(GameManager).GetField("_maxCost", BindingFlags.Instance | BindingFlags.NonPublic);
                    Require((int)costField.GetValue(game) == expectedCost[stage - 1], "Incorrect cost.");
                    string actualAllies = string.Concat(game.allyUnits.ConvertAll(unit => unit.unitName));
                    Require(actualAllies == expectedAllies[stage - 1], "Incorrect roster for map" + stage);
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
            Debug.Log("LearningMapValidation PASS: 22 maps, per-map rosters, O, enemy references, uncapped healing, invalid roster.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
