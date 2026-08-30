#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Adds a button to DialogueHud's Inspector: "Build Dialogue Canvas".
/// Clicking it creates a real, editable Canvas hierarchy (Text/Image/Button GameObjects
/// you can see, drag, and resize like any normal UI), wires them into DialogueHud's
/// fields, and hooks up the button OnClick events — all in one click, as a one-time
/// starting point. After that, edit the generated objects by hand like anything else;
/// this tool never runs again unless you click the button again.
///
/// This does NOT run at runtime and is not included in game builds (wrapped in
/// UNITY_EDITOR). It only exists to save you from placing ~9 UI elements by hand.
/// </summary>
[CustomEditor(typeof(DialogueHud))]
public class DialogueHudEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        DialogueHud hud = (DialogueHud)target;

        if (GUILayout.Button("Build Dialogue Canvas", GUILayout.Height(32)))
        {
            BuildCanvas(hud);
        }

        EditorGUILayout.HelpBox(
            "Creates a real Canvas with Text/Image/Button objects as siblings under a new " +
            "'DialogueCanvas', wires them into the fields above, and hooks up button clicks. " +
            "Safe to click once, then adjust everything by hand afterward. Re-running will " +
            "create a second canvas if the fields are already filled — clear the fields first " +
            "if you want to regenerate.",
            MessageType.Info);
    }

    private static void BuildCanvas(DialogueHud hud)
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        var canvasGo = new GameObject("DialogueCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create DialogueCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        TextMeshProUGUI scratchpadText = MakeText(canvasGo.transform, "ScratchpadText", new Vector2(0.02f, 0.55f), new Vector2(0.28f, 0.96f), 22, TextAlignmentOptions.TopLeft);
        
        TextMeshProUGUI clockHoursText = MakeText(canvasGo.transform, "ClockHoursText", new Vector2(0.32f, 0.90f), new Vector2(0.42f, 0.99f), 32, TextAlignmentOptions.Center);
        clockHoursText.fontStyle = FontStyles.Bold;
        
        TextMeshProUGUI clockMinutesText = MakeText(canvasGo.transform, "ClockMinutesText", new Vector2(0.44f, 0.90f), new Vector2(0.54f, 0.99f), 32, TextAlignmentOptions.Center);
        clockMinutesText.fontStyle = FontStyles.Bold;
        
        TextMeshProUGUI clockAmPmText = MakeText(canvasGo.transform, "ClockAmPmText", new Vector2(0.56f, 0.90f), new Vector2(0.66f, 0.99f), 32, TextAlignmentOptions.Center);
        clockAmPmText.fontStyle = FontStyles.Bold;
        
        TextMeshProUGUI speakerText = MakeText(canvasGo.transform, "SpeakerText", new Vector2(0.32f, 0.78f), new Vector2(0.72f, 0.88f), 26, TextAlignmentOptions.BottomLeft);
        speakerText.fontStyle = FontStyles.Bold;
        TextMeshProUGUI bodyText = MakeText(canvasGo.transform, "BodyText", new Vector2(0.32f, 0.22f), new Vector2(0.72f, 0.78f), 22, TextAlignmentOptions.TopLeft);

        var portraitGo = new GameObject("Portrait");
        Undo.RegisterCreatedObjectUndo(portraitGo, "Create Portrait");
        portraitGo.transform.SetParent(canvasGo.transform, false);
        Image portrait = portraitGo.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.color = Color.white;
        Stretch(portraitGo.GetComponent<RectTransform>(), new Vector2(0.74f, 0.22f), new Vector2(0.98f, 0.88f));
        portrait.enabled = false;

        Button continueButton = MakeButton(canvasGo.transform, "ContinueButton", "continue", new Vector2(0.72f, 0.08f), new Vector2(0.96f, 0.18f));
        Button followButton = MakeButton(canvasGo.transform, "FollowButton", "follow", new Vector2(0.32f, 0.08f), new Vector2(0.54f, 0.18f));
        Button ignoreButton = MakeButton(canvasGo.transform, "IgnoreButton", "ignore", new Vector2(0.56f, 0.08f), new Vector2(0.70f, 0.18f));

        var flashGo = new GameObject("FlashOverlay");
        Undo.RegisterCreatedObjectUndo(flashGo, "Create FlashOverlay");
        flashGo.transform.SetParent(canvasGo.transform, false);
        Image flash = flashGo.AddComponent<Image>();
        flash.color = Color.clear;
        Stretch(flashGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        flash.raycastTarget = false;
        flashGo.SetActive(false);
        flashGo.transform.SetAsLastSibling();

        // Wire OnClick events to the DialogueHud's public methods.
        UnityEditor.Events.UnityEventTools.AddPersistentListener(continueButton.onClick, hud.OnContinue);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(followButton.onClick, hud.OnFollow);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(ignoreButton.onClick, hud.OnIgnore);

        // Assign all fields on DialogueHud via SerializedObject (fields are private).
        var so = new SerializedObject(hud);
        so.FindProperty("npcSpeakerText").objectReferenceValue = speakerText;
        so.FindProperty("npcBodyText").objectReferenceValue = bodyText;
        so.FindProperty("scratchpadText").objectReferenceValue = scratchpadText;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.FindProperty("followButton").objectReferenceValue = followButton;
        so.FindProperty("ignoreButton").objectReferenceValue = ignoreButton;
        so.FindProperty("flash").objectReferenceValue = flash;
        so.FindProperty("npcPortrait").objectReferenceValue = portrait;
        so.FindProperty("clockHoursText").objectReferenceValue = clockHoursText;
        so.FindProperty("clockMinutesText").objectReferenceValue = clockMinutesText;
        so.FindProperty("clockAmPmText").objectReferenceValue = clockAmPmText;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = canvasGo;
        EditorUtility.SetDirty(hud);
        Debug.Log("[DialogueHudEditor] Canvas built and wired. Adjust positions/sizes by hand as needed.");
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        Stretch(go.GetComponent<RectTransform>(), min, max);
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        Stretch(go.GetComponent<RectTransform>(), min, max);

        var child = new GameObject("Label");
        child.transform.SetParent(go.transform, false);
        var t = child.AddComponent<TextMeshProUGUI>();
        t.fontSize = 24;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.text = label;
        Stretch(child.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        return btn;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
