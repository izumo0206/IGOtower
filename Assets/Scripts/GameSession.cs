using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Puzzle,
    Tutorial
}

/// <summary>
/// パズルモードとチュートリアルのシーン間データを保持します。
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameProfile Profile { get; private set; }
    public GameMode Mode { get; private set; } = GameMode.Puzzle;
    public string SelectedMapText { get; private set; }
    public string SelectedStageName { get; private set; }

    public bool HasSelectedMap => !string.IsNullOrEmpty(SelectedMapText);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeSceneLoad()
    {
        EnsureInstance();
    }

    public static GameSession EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameSession existing = FindObjectOfType<GameSession>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject sessionObject = new GameObject("GameSession");
        Instance = sessionObject.AddComponent<GameSession>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Profile = GameProfileStore.Load();
    }

    public void BeginTutorial(string mapText)
    {
        Mode = GameMode.Tutorial;
        SelectedMapText = mapText;
        SelectedStageName = "tutorial";
    }

    public void BeginPuzzleStage(string stageName, string mapText)
    {
        Mode = GameMode.Puzzle;
        SelectedStageName = stageName;
        SelectedMapText = mapText;
    }

    public bool TryGetSelectedMap(out string mapText)
    {
        mapText = SelectedMapText;
        return !string.IsNullOrEmpty(mapText);
    }

    public void ConsumeSelectedMap()
    {
        // MainSceneの初期化中にも参照するため、メニューへ戻るまで保持します。
    }

    public void MarkTutorialCleared()
    {
        Profile.tutorialCleared = true;
        PlayerPrefs.SetInt("IsTutorialCleared", 1);
        SaveProfile();
    }

    public void MarkPuzzleStageCleared(string stageName)
    {
        if (string.IsNullOrEmpty(stageName)) return;

        if (!Profile.clearedStages.Contains(stageName))
            Profile.clearedStages.Add(stageName);

        PlayerPrefs.SetInt(stageName + "_Cleared", 1);
        SaveProfile();
    }

    public bool IsPuzzleStageCleared(string stageName)
    {
        return Profile.HasStageClear(stageName);
    }

    public void HandleBattleClear()
    {
        if (Mode == GameMode.Tutorial)
            MarkTutorialCleared();
        else
            MarkPuzzleStageCleared(SelectedStageName);
    }

    public void ReturnToTitle()
    {
        ClearActiveSession();
        TransitionTo("TitleScene");
    }

    public void ResetAllProgress()
    {
        GameProfileStore.Reset();
        Profile = GameProfileStore.Load();
        ClearActiveSession();
    }

    public void ClearActiveSession()
    {
        SelectedMapText = null;
        SelectedStageName = null;
        Mode = GameMode.Puzzle;
    }

    private void SaveProfile()
    {
        GameProfileStore.Save(Profile);
    }

    private static void TransitionTo(string sceneName)
    {
        if (FadeManager.Instance != null)
            FadeManager.Instance.TransitionToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
