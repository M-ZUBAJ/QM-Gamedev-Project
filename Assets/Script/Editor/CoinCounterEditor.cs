/*
First Date: 18/10/2026
Date: 18/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. Every CoinCounter's Inspector is drawn by it.
Purpose: Shows CoinCounter's fields as usual, but only the ones the chosen Look needs:
           Auto Icon     -> nothing extra
           Static Icon   -> Static Icon (the picture)
           Animated Icon -> Aseprite and Aseprite Tag
           Text Name     -> Text Name (the word)
         Text Color and the Icon / Text slots always show.
Requirement: CoinCounterEditor.cs x-> CoinCounter.cs
*/

using UnityEditor;

[CustomEditor(typeof(CoinCounter))]
[CanEditMultipleObjects]   // still works with several counters selected
public class CoinCounterEditor : Editor
{
    // ===== Unity messages (Unity calls these) =====

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty lookProp = serializedObject.FindProperty("look");   // must match the field name in CoinCounter
        bool mixed = lookProp.hasMultipleDifferentValues;                     // several selected that don't agree: show all
        CoinLook look = (CoinLook)lookProp.enumValueIndex;

        // Walk through every field in order, the same way Unity's normal Inspector does.
        SerializedProperty field = serializedObject.GetIterator();
        bool enterChildren = true;
        while (field.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!mixed && !Needed(field.name, look)) continue;

            using (new EditorGUI.DisabledScope(field.name == "m_Script"))   // the greyed-out "Script" line
            {
                EditorGUILayout.PropertyField(field, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ===== My methods =====

    // Does this Look use this field? The names in quotes must match the field names in CoinCounter.
    static bool Needed(string fieldName, CoinLook look)
    {
        switch (fieldName)
        {
            case "staticIcon":  return look == CoinLook.StaticIcon;
            case "aseprite":
            case "asepriteTag": return look == CoinLook.AnimatedIcon;
            case "textName":    return look == CoinLook.TextName;
            default:            return true;   // look, textColor, icon, text
        }
    }
}
