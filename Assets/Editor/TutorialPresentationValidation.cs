using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 構造と参照の検証。画面操作・描画の確認はPlayモードで別途行う。
public static class TutorialPresentationValidation
{
    [MenuItem("Tools/IGOtower/Validate Tutorial Presentation")]
    public static void Run()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (!alreadyLoaded)
            scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            foreach (var image in roots.SelectMany(r => r.GetComponentsInChildren<Image>(true)))
                RequireValidSprite(image);
            var dialog = roots.SelectMany(r => r.GetComponentsInChildren<TutorialDialog>(true)).Single();
            var data = new SerializedObject(dialog);
            foreach (string field in new[] { "messageText", "conversationPanel", "navigatorImage", "advanceButton",
                "instructionPanel", "instructionTitle", "instructionText", "hpFocusTarget", "hpFocusOverlay",
                "unitOverviewPanel", "unitOverviewImage", "unitOverviewTitle", "unitOverviewStats", "unitOverviewAdvice",
                "speechBubble", "navigatorName", "continuePrompt" })
                Require(data.FindProperty(field).objectReferenceValue != null, "Missing " + field);
            var expressions = data.FindProperty("expressions");
            Require(expressions.arraySize == 5, "Expected five navigator expressions");
            for (int i = 0; i < expressions.arraySize; i++)
                Require(expressions.GetArrayElementAtIndex(i).objectReferenceValue is Sprite, "Missing expression Sprite");
            var canvas = dialog.GetComponent<Canvas>();
            Require(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder == 200,
                "Tutorial presentation must render above gameplay UI");
            Require(dialog.transform.parent == null, "Tutorial UI must be outside the gameplay CanvasGroup");
            var menu = roots.SelectMany(r => r.GetComponentsInChildren<InGameMenuController>(true)).Single();
            var menuData = new SerializedObject(menu);
            var menuPanel = (GameObject)menuData.FindProperty("menuPanel").objectReferenceValue;
            var menuCanvas = menuPanel.GetComponentInParent<Canvas>();
            Require(menuCanvas != null && menuCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                && menuCanvas.sortingLayerID == canvas.sortingLayerID && menuCanvas.sortingOrder > canvas.sortingOrder
                && menuCanvas.sortingOrder < 999, "Menu must render above tutorial UI and below the scene fade");
            Require(menuCanvas.GetComponent<GraphicRaycaster>() != null, "Menu must receive pointer input");
            var conversationPanel = (GameObject)data.FindProperty("conversationPanel").objectReferenceValue;
            var conversationCanvas = conversationPanel.GetComponent<Canvas>();
            Require(conversationCanvas != null && conversationCanvas.overrideSorting
                && conversationCanvas.sortingLayerID == menuCanvas.sortingLayerID
                && conversationCanvas.sortingOrder > menuCanvas.sortingOrder && conversationCanvas.sortingOrder < 999,
                "Conversation must cover the menu while instructions remain below it");
            Require(conversationCanvas.GetComponent<GraphicRaycaster>() != null,
                "Conversation must intercept taps above the menu");
            var unitCard = (GameObject)data.FindProperty("unitOverviewPanel").objectReferenceValue;
            Require(!unitCard.activeSelf && unitCard.GetComponentInParent<Canvas>() == conversationCanvas,
                "Unit card must start hidden inside the navigator conversation canvas");
            var introduction = roots.SelectMany(r => r.GetComponentsInChildren<StageUnitIntroduction>(true)).Single();
            var introductionData = new SerializedObject(introduction);
            var introductions = introductionData.FindProperty("introductions");
            string[] introductionStages = { "map7", "map8", "map9", "map10", "map16", "map17", "map19", "map20" };
            string[] introductionUnits = { "002", "011", "006", "015", "007", "008", "004", "005" };
            Require(introductions.arraySize == introductionStages.Length, "Missing new-unit introduction");
            for (int i = 0; i < introductions.arraySize; i++)
            {
                var entry = introductions.GetArrayElementAtIndex(i);
                Require(entry.FindPropertyRelative("stageName").stringValue == introductionStages[i],
                    "Introduction must target the unit's first main-game stage");
                var unit = (AllyUnitData)entry.FindPropertyRelative("unit").objectReferenceValue;
                Require(unit != null && unit.unitId == introductionUnits[i] && unit.unitSprite != null,
                    "Introduction must reference the real unit and its visual");
                Require(!string.IsNullOrWhiteSpace(entry.FindPropertyRelative("advice").stringValue),
                    "Missing navigator advice");
            }
            Require(introductionData.FindProperty("dialog").objectReferenceValue == dialog,
                "Introduction must reuse the tutorial navigator UI");
            var stageTutorials = introductionData.FindProperty("stageTutorials");
            Require(stageTutorials.arraySize == 1, "Missing placement-rule tutorial");
            var placementTutorial = stageTutorials.GetArrayElementAtIndex(0);
            Require(placementTutorial.FindPropertyRelative("stageName").stringValue == "map12"
                && placementTutorial.FindPropertyRelative("tutorialId").stringValue == "surrounded-placement"
                && !string.IsNullOrWhiteSpace(placementTutorial.FindPropertyRelative("message").stringValue),
                "Surrounded-placement explanation must target map12 and have its own read-state ID");
            var game = roots.SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).Single();
            Require(game.tutorialDialog == dialog, "Gameplay must respect the introduction input blocker");
            var closeMenu = (Button)menuData.FindProperty("closeButton").objectReferenceValue;
            Require(closeMenu != null && closeMenu.GetComponentInParent<Canvas>() == menuCanvas,
                "Close button must be on the foreground menu canvas");
            var guide = (GameObject)data.FindProperty("instructionPanel").objectReferenceValue;
            Require(!guide.activeSelf, "Guide must start hidden");
            var bubble = dialog.transform.Find("ConversationOverlay/NavigatorSpeechBubble");
            Require(bubble != null, "Missing speech bubble background");
            RequireWhitePanel(bubble.GetComponent<Image>());
            RequireWhitePanel(guide.GetComponent<Image>());
            var focus = (RectTransform)data.FindProperty("hpFocusOverlay").objectReferenceValue;
            var target = (RectTransform)data.FindProperty("hpFocusTarget").objectReferenceValue;
            Require(target.name == "HpText", "HP focus must track the actual HP display");
            Require(!focus.gameObject.activeSelf && focus.parent == dialog.transform,
                "HP focus must start hidden on the tutorial canvas");
            var shades = data.FindProperty("hpFocusShades");
            Require(shades.arraySize == 4, "HP focus needs four surrounding shades");
            for (int i = 0; i < shades.arraySize; i++)
            {
                var shade = (RectTransform)shades.GetArrayElementAtIndex(i).objectReferenceValue;
                Require(shade != null && shade.parent == focus, "Missing HP focus shade");
                var image = shade.GetComponent<Image>();
                Require(image != null && !image.raycastTarget && image.color.a > 0 && image.color.a <= 0.5f,
                    "HP shades must dim gently without intercepting input");
            }
            var advance = (Button)data.FindProperty("advanceButton").objectReferenceValue;
            Require(advance.GetComponent<Image>().raycastTarget, "Conversation must consume screen taps");
            Require(advance.onClick.GetPersistentEventCount() == 0, "Advance listener must only be registered once");
            var manager = roots.SelectMany(r => r.GetComponentsInChildren<TutorialManager>(true)).Single();
            Require(manager.tutorialDialog == dialog, "Manager/dialog mismatch");
            var managerData = new SerializedObject(manager);
            var markers = managerData.FindProperty("_targetMarkers");
            Require(markers.arraySize == 4, "Keep all four original placement exercises");
            for (int i = 0; i < markers.arraySize; i++)
                Require(markers.GetArrayElementAtIndex(i).objectReferenceValue != null, "Missing target marker");
            var skip = (GameObject)managerData.FindProperty("_skipButtonObj").objectReferenceValue;
            Require(skip != null && skip.transform.parent == advance.transform,
                "Skip must belong to the conversation overlay so it stays hidden during gameplay and HP focus");
            RequireWhitePanel(skip.GetComponent<Image>());
            Debug.Log("TutorialPresentationValidation PASS: expressions, scene wiring, draw order, raycasts, guide, skip, markers.");
        }
        finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void RequireWhitePanel(Image image)
    {
        Require(image != null && image.enabled, "Missing or disabled background Image");
        Require(image.color == Color.white, "Background must be opaque white");
        Require(image.sprite != null && AssetDatabase.GetAssetPath(image.sprite)
            == "Assets/Art/TutorialNavigator/white_panel.png", "Background must use the explicit white panel Sprite");
        Require(image.type == Image.Type.Sliced && image.fillCenter && image.hasBorder,
            "Background must draw its center as well as its rounded border");
        Require(image.rectTransform.sizeDelta.x > 0 && image.rectTransform.sizeDelta.y > 0,
            "Background must have a visible size");
    }

    private static void RequireValidSprite(Image image)
    {
        // 名前だけ存在する画像ではなく、Unityが解決した実際のSpriteを確認する。
        string location = image.gameObject.name;
        var property = new SerializedObject(image).FindProperty("m_Sprite");
        if (property.objectReferenceValue == null)
        {
            Require(property.objectReferenceInstanceIDValue == 0, "Missing Sprite on Image: " + location);
            return; // SpriteなしのImageは、単色パネルとして正当。
        }
        var sprite = property.objectReferenceValue as Sprite;
        Require(sprite != null, "Image references an asset which is not a Sprite: " + location);
        try
        {
            Require(sprite.texture != null, "Missing Sprite texture on Image: " + location);
            var border = sprite.border; // GenerateSlicedSpriteと同じ参照を検証する。
            Require(border.x >= 0 && border.y >= 0 && border.z >= 0 && border.w >= 0,
                "Invalid Sprite border on Image: " + location);
        }
        catch (MissingReferenceException exception)
        {
            throw new InvalidOperationException("Destroyed Sprite reference on Image: " + location, exception);
        }
    }
}
