using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Browser local storage schema. Keep this serializable and versioned so a later
/// release can migrate player data without relying on scene-specific keys.
/// </summary>
[Serializable]
public class GameProfile
{
    public const int CurrentVersion = 2;

    public int version = CurrentVersion;
    public bool tutorialCleared;
    public List<string> clearedStages = new List<string>();
    public List<string> seenUnitIntroductions = new List<string>();
    public List<string> seenStageTutorials = new List<string>();

    public bool HasStageClear(string stageName) => clearedStages.Contains(stageName);
    public bool HasSeenUnitIntroduction(string unitId) => seenUnitIntroductions.Contains(unitId);
    public bool HasSeenStageTutorial(string tutorialId) => seenStageTutorials.Contains(tutorialId);
}

public static class GameProfileStore
{
    public const string SaveKey = "IGOtower.Profile.v1";

    public static GameProfile Load()
    {
        GameProfile profile = null;
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                profile = JsonUtility.FromJson<GameProfile>(PlayerPrefs.GetString(SaveKey));
            }
            catch (ArgumentException)
            {
                // A malformed browser value should never prevent the title screen
                // from opening. The player can reset it from Settings.
                profile = null;
            }
        }

        // Missing profiles may still have progress in the original map keys.
        profile = profile ?? new GameProfile { version = 1 };
        Normalize(profile);
        if (profile.version < 2)
        {
            ImportLegacyProgress(profile);
            var renumbered = new List<string>();
            foreach (string stageName in profile.clearedStages)
            {
                if (stageName != null && stageName.StartsWith("map", StringComparison.Ordinal)
                    && int.TryParse(stageName.Substring(3), out int number)
                    && number >= 9 && number <= 23)
                {
                    AddIfMissing(renumbered, "map" + (number - 8));
                }
            }
            profile.clearedStages = renumbered;
        }
        Save(profile);
        return profile;
    }

    public static GameProfile CreateDefault()
    {
        return new GameProfile();
    }

    public static void Save(GameProfile profile)
    {
        Normalize(profile);
        profile.version = GameProfile.CurrentVersion;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(profile));
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey("IsTutorialCleared");
        for (int i = 1; i <= 23; i++)
        {
            PlayerPrefs.DeleteKey("map" + i + "_Cleared");
        }
        PlayerPrefs.Save();
    }

    private static void Normalize(GameProfile profile)
    {
        profile.clearedStages = profile.clearedStages ?? new List<string>();
        profile.seenUnitIntroductions = profile.seenUnitIntroductions ?? new List<string>();
        profile.seenStageTutorials = profile.seenStageTutorials ?? new List<string>();
    }

    private static void ImportLegacyProgress(GameProfile profile)
    {
        if (PlayerPrefs.GetInt("IsTutorialCleared", 0) == 1)
        {
            profile.tutorialCleared = true;
        }

        for (int i = 1; i <= 23; i++)
        {
            string stageName = "map" + i;
            if (PlayerPrefs.GetInt(stageName + "_Cleared", 0) == 1)
            {
                AddIfMissing(profile.clearedStages, stageName);
            }
        }
    }

    private static void AddIfMissing(List<string> values, string value)
    {
        if (!values.Contains(value)) values.Add(value);
    }
}
