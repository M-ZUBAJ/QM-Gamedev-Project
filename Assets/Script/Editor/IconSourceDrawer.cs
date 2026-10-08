/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. Every IconSource field (the "Icon" of an Item Data or Building Data) is drawn by it.
Purpose: Draws an IconSource in the Inspector as a dropdown (Capture / Static / Animated) followed by ONLY the field
         that choice needs, so a developer can't fill in the wrong one:
           Capture  -> a short note (nothing to fill in)
           Static   -> Image (a PNG sprite)
           Animated -> Aseprite (.aseprite file) and Aseprite Tag
Requirement: IconSourceDrawer.cs x-> IconSource.cs
*/

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(IconSource))]
public class IconSourceDrawer : PropertyDrawer
{
    // ===== Fields =====
    static float Line => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    // ===== Unity messages (Unity calls these) =====

    // How tall the whole thing is: the dropdown plus the lines of the chosen option.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return Line * (1 + ExtraLines(Mode(property)));
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // The choice. The names in quotes must match the fields in IconSource.
        SerializedProperty mode = property.FindPropertyRelative("mode");
        EditorGUI.PropertyField(row, mode, label);

        EditorGUI.indentLevel++;
        switch (Mode(property))
        {
            case IconMode.Capture:
                row.y += Line;
                EditorGUI.LabelField(row, " ", "A photo of the prefab, taken by itself.", EditorStyles.miniLabel);
                break;

            case IconMode.Static:
                row.y += Line;
                EditorGUI.PropertyField(row, property.FindPropertyRelative("image"), new GUIContent("Image (PNG)"));
                break;

            case IconMode.Animated:
                row.y += Line;
                SerializedProperty file = property.FindPropertyRelative("aseprite");
                file.objectReferenceValue = EditorGUI.ObjectField(row, new GUIContent("Aseprite", "The .aseprite file."),
                                                                  file.objectReferenceValue, typeof(Object), false);
                row.y += Line;
                EditorGUI.PropertyField(row, property.FindPropertyRelative("asepriteTag"), new GUIContent("Aseprite Tag"));
                break;
        }
        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    // ===== My methods =====

    static IconMode Mode(SerializedProperty property)
    {
        return (IconMode)property.FindPropertyRelative("mode").enumValueIndex;
    }

    static int ExtraLines(IconMode mode)
    {
        return mode == IconMode.Animated ? 2 : 1;
    }
}
