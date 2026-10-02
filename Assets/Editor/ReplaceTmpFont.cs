using TMPro;
using UnityEditor;
using UnityEngine;

public static class ReplaceTmpFont 
{
    [MenuItem("Tools/Replace TMP Font To Default")]
    public static void ReplaceTmpFontToDefault()
    {
        var font = TMP_Settings.defaultFontAsset;

        var texts = Object.FindObjectsOfType<TMP_Text>(true);
        foreach (var text in texts)
        {
            text.font = font;
            EditorUtility.SetDirty(text);
        }
    }
}
