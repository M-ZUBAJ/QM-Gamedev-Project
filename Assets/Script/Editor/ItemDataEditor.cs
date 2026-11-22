/*
First Date: 11/10/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. Every Item Data file's Inspector is drawn by it.
Purpose: Shows Item Data's fields as usual, but each type's own section only for that type, so a Material like
         Wood never shows food or tool settings:
         - Consumable section (Health / Hunger / Stamina): Item Type = Consumable;
         - Tool section (Kind / Tier): Item Type = Tool;
         - Bow section (Damage / Draw Seconds / Arrow Speed): Item Type = Tool with Tool > Kind = Bow;
         - Armor section (Slot / Protection / Set / Piece / Hides): Item Type = Armor;
         - Ammo section (Damage / Head): Item Type = Ammo (an arrow);
         - Mined With (which pickaxe a rock dropping it needs, or which axe a tree): Item Type = Material.
Requirement: ItemDataEditor.cs x-> ItemData.cs, ToolStats.cs
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
    const string ToolField = "tool";
    const string BowField = "bow";
    const string ArmorField = "armor";
    const string AmmoField = "ammo";
    const string MinedWithField = "minedWith";

    // ===== Unity messages (Unity calls these) =====

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty type = serializedObject.FindProperty(TypeField);
        // Show the section if the type is Consumable (or the selected files don't all agree).
        bool showConsumable = type == null || type.hasMultipleDifferentValues
                              || type.enumValueIndex == (int)ItemType.Consumable;
        bool showTool = type == null || type.hasMultipleDifferentValues || type.enumValueIndex == (int)ItemType.Tool;
        SerializedProperty kind = serializedObject.FindProperty(ToolField + ".kind");   // a tool's Kind (Bow = it shoots)
        bool showBow = showTool && (kind == null || kind.hasMultipleDifferentValues || kind.enumValueIndex == (int)ToolKind.Bow);
        bool showArmor = type == null || type.hasMultipleDifferentValues || type.enumValueIndex == (int)ItemType.Armor;
        bool showAmmo = type == null || type.hasMultipleDifferentValues || type.enumValueIndex == (int)ItemType.Ammo;
        bool showMining = type == null || type.hasMultipleDifferentValues || type.enumValueIndex == (int)ItemType.Material;

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
            if (field.name == ToolField)
            {
                if (!showTool) continue;         // not a tool: hide Kind / Tier
                field.isExpanded = true;
            }
            if (field.name == BowField)
            {
                if (!showBow) continue;          // not a bow: hide Damage / Draw Seconds / Arrow Speed
                field.isExpanded = true;
            }
            if (field.name == ArmorField)
            {
                if (!showArmor) continue;        // not armor: hide Slot / Protection / Set / Piece / Hides
                field.isExpanded = true;
            }
            if (field.name == AmmoField)
            {
                if (!showAmmo) continue;         // not an arrow: hide Damage / Head
                field.isExpanded = true;
            }
            if (field.name == MinedWithField && !showMining) continue;   // only materials come out of rocks and trees

            using (new EditorGUI.DisabledScope(field.name == "m_Script"))   // the greyed-out "Script" line
            {
                EditorGUILayout.PropertyField(field, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
