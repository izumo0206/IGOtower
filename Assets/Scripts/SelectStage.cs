using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshProを使うために必要
using UnityEngine.SceneManagement;
using UnityEngine.UI; // シーン移動のために必要

public class SelectStage : MonoBehaviour
{
    public GameObject stageButtonPrefab;
    public Transform buttonContainer;
    // Start is called before the first frame update
    void Start()
    {
        TextAsset[] stageFiles = Resources.LoadAll<TextAsset>("Stage");

        foreach (TextAsset stageFile in stageFiles)
        {
            GameObject newButton = Instantiate(stageButtonPrefab, buttonContainer);
            StageButton stageBtn = newButton.GetComponent<StageButton>();

            if (stageBtn != null)
            {
                string saveKey = stageFile.name + "_Cleared";
                bool isCleared = PlayerPrefs.GetInt(saveKey, 0) == 1;

                stageBtn.Setup(stageFile.name, isCleared, () =>
                {
                    GameManager.currentStageName = stageFile.name; 
                    OnStageButtonClicked(stageFile.text);
                });
            }
        }
    }

    private void OnStageButtonClicked(string mapData)
    {
        GameManager.selectedStageText = mapData;

        FadeManager.Instance.TransitionToScene("MainScene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
