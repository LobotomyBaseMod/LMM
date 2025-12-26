#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public class FontReplacer : EditorWindow
{
    private TMP_FontAsset newFont;
    [MenuItem("Tools/AddTextDataLoader")]
    static void AddAllTextDataLoader()
    {
        TextMeshProUGUI[] texts = GameObject.FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            UITextDataLoader TDL = text.gameObject.GetComponent<UITextDataLoader>();
            if (TDL != null) continue;
            TDL = text.gameObject.AddComponent<UITextDataLoader>();
            TDL.id = string.Empty;
            TDL.text = text;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Replace Font")]
    static void Init()
    {
        FontReplacer window = (FontReplacer)EditorWindow.GetWindow(typeof(FontReplacer));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Replace Font", EditorStyles.boldLabel);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace"))
        {
            ReplaceFont();
        }
    }

    void ReplaceFont()
    {
        TextMeshProUGUI[] texts = GameObject.FindObjectsOfType<TextMeshProUGUI>();

        foreach (TextMeshProUGUI text in texts)
        {
            Undo.RecordObject(text, "Replace Font");

            text.font = newFont;

            EditorUtility.SetDirty(text);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif