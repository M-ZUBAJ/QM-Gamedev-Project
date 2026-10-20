/*
First Date: 20/10/2026
Date: 20/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. The GameRules Inspector is drawn by it.
Purpose: Shows GameRules as usual, but the Item Icon Style dropdown only while Overwrite Item Icon is ticked
         (it does nothing while the box is off, so it would only confuse).
Requirement: GameRulesEditor.cs x-> GameRules.cs
*/

using UnityEditor;

[CustomEditor(typeof(GameRules))]
public class GameRulesEditor : Editor
{
    // ===== Fields =====
    // The field names in GameRules.cs. If you rename a field there, rename it here too.
    const string OverwriteField = "overwriteItemIcon";
    const string StyleField = "itemIconStyle";

    // ===== Unity messages (Unity calls these) =====

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty overwrite = serializedObject.FindProperty(OverwriteField);
        bool showStyle = overwrite == null || overwrite.boolValue;

        // Walk through every field in order, the same way Unity's normal Inspector does.
        SerializedProperty field = serializedObject.GetIterator();
        bool enterChildren = true;
        while (field.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (field.name == StyleField && !showStyle) continue;   // box off: hide the dropdown

            using (new EditorGUI.DisabledScope(field.name == "m_Script"))   // the greyed-out "Script" line
            {
                EditorGUILayout.PropertyField(field, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
