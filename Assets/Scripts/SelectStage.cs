using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro; // TextMeshProを使うために必要
using UnityEngine.SceneManagement;
using UnityEngine.UI; // シーン移動のために必要

public class SelectStage : MonoBehaviour
{


    public GameObject stageButtonPrefab;
    public Transform buttonContainer;
    // Start is called before the first frame update
    public AudioClip _selectBGM;
    void Start()
    {
        TextAsset[] stageFiles = Resources.LoadAll<TextAsset>("Stage")
            .OrderBy(stage => ExtractStageNumber(stage.name))
            .ToArray();

        foreach (TextAsset stageFile in stageFiles)
        {
            GameObject newButton = Instantiate(stageButtonPrefab, buttonContainer);
            StageButton stageBtn = newButton.GetComponent<StageButton>();

            if (stageBtn != null)
            {
                bool isCleared = GameSession.EnsureInstance().IsPuzzleStageCleared(stageFile.name);

                stageBtn.Setup(stageFile.name, isCleared, () =>
                {
                    OnStageButtonClicked(stageFile.name, stageFile.text);
                });
            }
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMWithFadeIn(_selectBGM, 1.0f, true);
        }
    }

    private void OnStageButtonClicked(string stageName, string mapData)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySwitchSE();
        }
        GameSession.EnsureInstance().BeginPuzzleStage(stageName, mapData);

        FadeManager.Instance.TransitionToScene("MainScene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private static int ExtractStageNumber(string stageName)
    {
        int value;
        return int.TryParse(stageName.Replace("map", string.Empty), out value) ? value : int.MaxValue;
    }
}
