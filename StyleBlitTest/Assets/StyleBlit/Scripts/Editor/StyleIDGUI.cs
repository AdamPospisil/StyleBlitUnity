using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StyleIDScript))]
public class StyleIDGUI : Editor
{
    public const string STYLE_TEXTURE_FOLDER = "Assets/StyleBlit/Material/StyleTextures/";
    public const string STYLE_TEXTURE_PREFIX = "source_";

    private SerializedProperty StyleIdProp;
    private Texture texture;

    void OnEnable()
    {
        StyleIdProp = serializedObject.FindProperty("StyleId");
        texture = (Texture)AssetDatabase.LoadAssetAtPath(STYLE_TEXTURE_FOLDER + STYLE_TEXTURE_PREFIX + StyleIdProp.intValue + ".png", typeof(Texture));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUIContent buttonContent = new GUIContent(texture, "Choose style texture");

        var centeredStyle = GUI.skin.GetStyle("Label");
        centeredStyle.alignment = TextAnchor.MiddleLeft;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Style texture", "Button", centeredStyle);

        if (GUILayout.Button(buttonContent, GUILayout.Width(100), GUILayout.Height(100)))
        {
            EditorGUIUtility.ShowObjectPicker<Texture2D>(texture, false, STYLE_TEXTURE_PREFIX, EditorGUIUtility.GetControlID(FocusType.Passive) + 100);
        }

        if (Event.current.commandName == "ObjectSelectorClosed")
        {
            texture = (Texture)EditorGUIUtility.GetObjectPickerObject();
            int textureID = ExtractTextureID(texture);

            if (AssetDatabase.FindAssets(STYLE_TEXTURE_PREFIX + StyleIdProp.intValue).Length != 0)
                StyleIdProp.intValue = textureID;
        }

        EditorGUILayout.EndHorizontal();
        serializedObject.ApplyModifiedProperties();
    }

    private int ExtractTextureID(Texture texture)
    {
        string index = texture.name.Substring(texture.name.LastIndexOf(STYLE_TEXTURE_PREFIX) + STYLE_TEXTURE_PREFIX.Length);
        return int.Parse(index);
    }

}
