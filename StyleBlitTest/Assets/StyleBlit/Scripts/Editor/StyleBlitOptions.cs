using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class StyleBlitOptions : EditorWindow
{
    string customLUTPath;

    Texture2D defaultSourceNormals;

    [MenuItem("StyleBlit/LUT generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(StyleBlitOptions));
    }

    public void OnInspectorUpdate()
    {
        this.Repaint();
    }

    private void OnEnable()
    {
        customLUTPath = Application.dataPath + "/custom_lut.png";
        string sourceNormalsPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("s_normals")[0]);
        defaultSourceNormals = (Texture2D)AssetDatabase.LoadAssetAtPath(sourceNormalsPath, typeof(Texture));
    }

    void OnGUI()
    {
        GUILayout.Label("Render custom LUT", EditorStyles.boldLabel);

        GUILayout.Label("If you want to use different guidance source map, generate a new LUT and feed it to shader.", EditorStyles.helpBox);

        defaultSourceNormals = (Texture2D)EditorGUILayout.ObjectField("Source normal map", defaultSourceNormals, typeof(Texture2D), false);

        GUILayout.Label("Save to file:");

        customLUTPath = GUILayout.TextField(customLUTPath);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
        {
            customLUTPath = EditorUtility.SaveFilePanel("Load png Textures", "Assets", "custom_lut", "png");
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Render", GUILayout.ExpandWidth(false)))
        {
            bool render = true;
            try
            {
                string dir = Path.GetDirectoryName(customLUTPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch (Exception e)
            {
                render = false;
                Debug.LogError(e.Message);
                EditorUtility.DisplayDialog("Path invalid", "The save location is invalid.", "OK");
            }

            if (render && defaultSourceNormals)
                RenderLUT.createTexture(customLUTPath, defaultSourceNormals);
        }

        GUILayout.EndHorizontal();

    }

}
