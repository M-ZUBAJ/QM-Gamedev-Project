/*
First Date: 20/10/2026
Date: 2/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. The GameRules Inspector is drawn by it.
Purpose: Shows GameRules as usual, but each dropdown only while the tick box it belongs to is ticked
         (it does nothing while the box is off, so it would only confuse):
           Item Icon Style   -> only while Overwrite Item Icon is ticked
           Notification Side -> only while Show Notifications is ticked
Requirement: GameRulesEditor.cs x-> GameRules.cs
*/

using UnityEditor;

[CustomEditor(typeof(GameRules))]
public class GameRulesEditor : Editor
{
    // ===== Fields =====
    // The field names in GameRules.cs. If you rename a field there, rename it here too.
    // Each pair: { the dropdown, the tick box it belongs to }.
    static readonly string[,] ShownOnlyWhen =
    {
        { "itemIconStyle", "overwriteItemIcon" },
        { "notificationSide", "showNotifications" },
    };

    // ===== Unity messages (Unity calls these) =====

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Walk through every field in order, the same way Unity's normal Inspector does.
        SerializedProperty field = serializedObject.GetIterator();
        bool enterChildren = true;
        while (field.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (IsHidden(field.name)) continue;   // its box is off: hide the dropdown

            using (new EditorGUI.DisabledScope(field.name == "m_Script"))   // the greyed-out "Script" line
            {
                EditorGUILayout.PropertyField(field, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ===== My methods =====

    // True if this field is a dropdown whose tick box is off right now.
    bool IsHidden(string fieldName)
    {
        for (int i = 0; i < ShownOnlyWhen.GetLength(0); i++)
        {
            if (ShownOnlyWhen[i, 0] != fieldName) continue;

            SerializedProperty box = serializedObject.FindProperty(ShownOnlyWhen[i, 1]);
            return box != null && !box.boolValue;
        }
        return false;
    }
}
