#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FixInputSetting
{
    [MenuItem("Tools/Fix Input To Both")]
    public static void ApplyBoth()
    {
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (ps.Length > 0)
        {
            var so = new SerializedObject(ps[0]);
            var sp = so.FindProperty("activeInputHandler");
            if (sp != null)
            {
                sp.intValue = 2; // 0 = Old, 1 = New, 2 = Both
                so.ApplyModifiedProperties();
                Debug.Log("✅ Input handler set to BOTH. No restart should be needed.");
            }
        }
    }
}
#endif
