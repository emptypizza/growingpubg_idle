#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;
using TMPro;

public static class SetupMobileUI
{
    [MenuItem("Tools/Setup Mobile UI")]
    public static void Setup()
    {
        // Find GameCanvas
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null)
        {
            Debug.LogError("GameCanvas not found!");
            return;
        }

        // Remove old mobile input objects if they exist
        DestroyIfExists("MobileInputSystem");
        DestroyIfExists("GameCanvas/JoystickBG");
        DestroyIfExists("GameCanvas/FireBtn");

        var canvasT = canvas.transform;

        // ═══════════════════════════════════════════
        //  1. JOYSTICK (bottom-left)
        // ═══════════════════════════════════════════

        // Background circle
        var joyBG = new GameObject("JoystickBG", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
        joyBG.transform.SetParent(canvasT, false);
        var joyBGRect = joyBG.GetComponent<RectTransform>();
        joyBGRect.anchorMin = new Vector2(0, 0);
        joyBGRect.anchorMax = new Vector2(0, 0);
        joyBGRect.pivot = new Vector2(0.5f, 0.5f);
        joyBGRect.anchoredPosition = new Vector2(160, 160);
        joyBGRect.sizeDelta = new Vector2(200, 200);
        var joyBGImg = joyBG.GetComponent<Image>();
        joyBGImg.color = new Color(1f, 1f, 1f, 0.25f);
        joyBGImg.raycastTarget = true;

        // Handle (inner circle)
        var joyHandle = new GameObject("JoystickHandle", typeof(RectTransform), typeof(Image));
        joyHandle.transform.SetParent(joyBG.transform, false);
        var handleRect = joyHandle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(80, 80);
        var handleImg = joyHandle.GetComponent<Image>();
        handleImg.color = new Color(1f, 1f, 1f, 0.6f);
        handleImg.raycastTarget = false;

        // Wire joystick handle reference
        var joystick = joyBG.GetComponent<VirtualJoystick>();
        var jso = new SerializedObject(joystick);
        jso.FindProperty("handle").objectReferenceValue = handleRect;
        jso.FindProperty("deadZone").floatValue = 0.1f;
        jso.ApplyModifiedProperties();

        // ═══════════════════════════════════════════
        //  2. FIRE BUTTON (bottom-right)
        // ═══════════════════════════════════════════

        var fireBtn = new GameObject("FireBtn", typeof(RectTransform), typeof(Image));
        fireBtn.transform.SetParent(canvasT, false);
        var fireBtnRect = fireBtn.GetComponent<RectTransform>();
        fireBtnRect.anchorMin = new Vector2(1, 0);
        fireBtnRect.anchorMax = new Vector2(1, 0);
        fireBtnRect.pivot = new Vector2(0.5f, 0.5f);
        fireBtnRect.anchoredPosition = new Vector2(-140, 140);
        fireBtnRect.sizeDelta = new Vector2(140, 140);
        var fireBtnImg = fireBtn.GetComponent<Image>();
        fireBtnImg.color = new Color(0.9f, 0.2f, 0.2f, 0.5f);
        fireBtnImg.raycastTarget = true;

        // Fire button text label
        var fireTxtGO = new GameObject("FireText", typeof(RectTransform));
        fireTxtGO.transform.SetParent(fireBtn.transform, false);
        var fireTxtRect = fireTxtGO.GetComponent<RectTransform>();
        fireTxtRect.anchorMin = Vector2.zero;
        fireTxtRect.anchorMax = Vector2.one;
        fireTxtRect.offsetMin = Vector2.zero;
        fireTxtRect.offsetMax = Vector2.zero;

        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null) tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.ugui");
        var fireTMP = fireTxtGO.AddComponent(tmpType);
        var tmpSo = new SerializedObject(fireTMP);
        tmpSo.FindProperty("m_text").stringValue = "FIRE";
        tmpSo.FindProperty("m_fontSize").floatValue = 28f;
        tmpSo.FindProperty("m_fontColor").colorValue = Color.white;
        tmpSo.FindProperty("m_HorizontalAlignment").intValue = 2; // center
        tmpSo.FindProperty("m_VerticalAlignment").intValue = 512; // middle
        tmpSo.ApplyModifiedProperties();
        // Set raycastTarget false so clicks pass to parent
        var tmpGraphic = fireTMP as Graphic;
        if (tmpGraphic != null) tmpGraphic.raycastTarget = false;

        // Add EventTrigger to fire button for PointerDown/PointerUp
        var trigger = fireBtn.AddComponent<EventTrigger>();

        // ═══════════════════════════════════════════
        //  3. MOBILE INPUT SYSTEM (scene root)
        // ═══════════════════════════════════════════

        // Find or create MobileInput GO
        var mobileInputGO = GameObject.Find("MobileInputSystem");
        if (mobileInputGO == null)
        {
            mobileInputGO = new GameObject("MobileInputSystem");
        }
        var mobileInput = mobileInputGO.GetComponent<MobileInput>();
        if (mobileInput == null)
            mobileInput = mobileInputGO.AddComponent<MobileInput>();

        // Wire references
        var miso = new SerializedObject(mobileInput);
        miso.FindProperty("moveJoystick").objectReferenceValue = joystick;
        miso.FindProperty("fireButton").objectReferenceValue = fireBtn;
        miso.ApplyModifiedProperties();

        // ═══════════════════════════════════════════
        //  4. Wire EventTrigger for fire button → MobileInput
        // ═══════════════════════════════════════════

        // We need to set up EventTrigger entries programmatically
        // PointerDown → MobileInput.OnFireDown()
        // PointerUp → MobileInput.OnFireUp()
        var downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        var downCall = new EventTrigger.TriggerEvent();
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(downCall, new UnityEngine.Events.UnityAction(mobileInput.OnFireDown));
        downEntry.callback = downCall;
        trigger.triggers.Add(downEntry);

        var upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        var upCall = new EventTrigger.TriggerEvent();
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(upCall, new UnityEngine.Events.UnityAction(mobileInput.OnFireUp));
        upEntry.callback = upCall;
        trigger.triggers.Add(upEntry);

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✅ Mobile UI setup complete! Joystick (bottom-left) + Fire button (bottom-right)");
    }

    private static void DestroyIfExists(string path)
    {
        var go = GameObject.Find(path);
        if (go != null) Object.DestroyImmediate(go);
    }
}
#endif
