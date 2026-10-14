/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. Every Item Data file's Inspector is drawn by it.
Purpose: Shows Item Data's fields as usual, but the Consumable section (Health / Hunger / Stamina) only
         when Item Type = Consumable, so a Material like Wood never shows food settings.
Requirement: ItemDataEditor.cs x-> ItemData.cs
*/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
[CanEditMultipleObjects]   // still works with several Item Data files selected
public class ItemDataEditor : Editor
{
    // ===== Fields =====
    // The field names in ItemData.cs. If you rename a field there, rename it here too.
    const string TypeField = "itemType";
    const string ConsumableField = "consumable";

    // ===== Unity messages (Unity calls these) =====

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty type = serializedObject.FindProperty(TypeField);
        // Show the section if the type is Consumable (or the selected files don't all agree).
        bool showConsumable = type == null || type.hasMultipleDifferentValues
                              || type.enumValueIndex == (int)ItemType.Consumable;

        // Walk through every field in order, the same way Unity's normal Inspector does.
        SerializedProperty field = serializedObject.GetIterator();
        bool enterChildren = true;
        while (field.NextVisible(enterChildren))
        {
            enterChildren = false;   // fields inside a field (like Consumable's stats) draw themselves

            if (field.name == ConsumableField)
            {
                if (!showConsumable) continue;   // a Material: hide the food settings
                field.isExpanded = true;         // open, so the stats show without clicking the arrow
            }

            using (new EditorGUI.DisabledScope(field.name == "m_Script"))   // the greyed-out "Script" line
            {
                EditorGUILayout.PropertyField(field, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
