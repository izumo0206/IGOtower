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

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if(buttonText != null)
            {
                buttonText.text = stageFile.name;
            }

            string mapData = stageFile.text;
            newButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnStageButtonClicked(mapData);
            });
        }
    }

    private void OnStageButtonClicked(string mapData)
    {
        GameManager.selectedStageText = mapData;

        SceneManager.LoadScene("MainScene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
