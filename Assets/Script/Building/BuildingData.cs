/*
First Date: 5/10/2026
Date: 23/11/2026
Author: Tony
Use by: This is an ASSET (a config file in the Project window), not a script you put on an object.
Unity Editor: Easiest: select the building (in the scene, or its prefab) > menu Tools > Building > Make Building From Selection,
              then fill in its Recipe. By hand:
              1. First make the building prefab (the finished building, made from shapes),
                 and an Item Data config file for every material it needs (see Docs/Item Setup.md).
              2. Project window: open a folder (e.g. Assets/Building), right click > Create > Building > Building Data.
                 Rename the new file, e.g. "Furnace".
              3. Click the file and fill in the Inspector:
                 Display Name = the name on the build button, e.g. "Furnace".
                 Description  = what it does.
                 Prefab       = drag the building PREFAB from the Project window (what it looks like when built).
                 Icon         = choose ONE: Capture (a photo of the prefab), Static (drag in a PNG sprite)
                                or Animated (drag in an .aseprite file). Only that one's field shows.
                 Recipe       = click + for each material: drag its Item Data (Assets/Config/Items) into "Item", set "Amount".
              4. It appears in the build menu (B) by itself (BuildMenuAutoList).
              Piece Type (Snapping) says how it snaps to other pieces while placing (Snap mode, middle click):
              Auto = guessed from the file name ("...Floor..." = Floor, "...Wall..." = Wall, "...Roof..." = Roof,
              "...Trap..." = Trap, anything else = Other = furniture). Choose it by hand if the name doesn't say.
              Category (Build menu) = which tab of the build menu it's in: Auto = from its Piece Type (Trap = Traps,
              anything else = Building). Upgrade Of = it's built ON a placed building of that kind and replaces it
              (e.g. the Iron Bear Trap on a Copper Bear Trap): its Recipe is only the extra. Description shows in the build menu's info box (rest the mouse on the
              building for 1 second), with the numbers its prefab's scripts give (IBuildingInfo, e.g. a trap's damage).
              Full steps: Docs/Build Menu Setup.md
Purpose: One thing the player can build: its name, description, picture, prefab and recipe.
         The recipe is what it costs, in items from the inventory (each line points to the material's Item Data).
         It doesn't have to match the shapes of the prefab.
         Its Piece Type decides how it snaps to built pieces (BuildSnap): floors side by side, walls on floors and
         next to / on top of walls, roofs and second floors on top of a floor's walls, furniture on floors and
         against walls, traps on the ground or on a floor (since 23/11/2026: the ground under a trap doesn't count as
         in the way, and nothing snaps onto a trap).
Requirement: BuildingData.cs x-> IconCapture.cs, IconSource.cs, Recipe.cs, PlayerInventory.cs, IBuildingInfo.cs
             BuildingData.cs <-x Blueprint.cs, BlueprintPlacer.cs, BuildingButton.cs, BuildingTools.cs, BuildMenu.cs, BuildMenuAutoList.cs, BuildMenuUIBuilder.cs, BuildSnap.cs, BuiltPiece.cs, ItemTools.cs, PlayerStats.cs, TrapBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

// How a building snaps to other built pieces while placing (Snap mode). New kinds of pieces get a new line here,
// and then rules in BuildSnap.
public enum PieceType
{
    Auto,    // guessed from the file name: "...Floor...", "...Wall...", "...Roof...", else Other
    Floor,   // next to floors; over a floor with walls, on top of the walls (a ceiling / second floor)
    Wall,    // on floors (along an edge or across the middle), next to floors, on top of and next to walls (L, T)
    Roof,    // over a floor with walls, on top of the walls; next to other roofs
    Other,   // furniture (e.g. a crafting table): on floors and furniture, against walls, next to floors and furniture
    Trap,    // lies on the ground or on a floor (23/11/2026, e.g. the iron trap): the ground under it isn't "in the way", nothing snaps onto it
}

// Which tab of the build menu a building is in (23/11/2026). Add new ones at the end (the files save the number).
public enum BuildCategory
{
    Auto,       // from the Piece Type: Trap = Traps, anything else = Building
    Building,   // floors, walls, roofs, furniture
    Traps,      // traps (e.g. the iron trap)
}

// CreateAssetMenu adds this to the right click > Create menu in the Project window.
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    // ===== Fields =====
    [Tooltip("Name shown to the player. Leave empty to use the asset's file name.")]
    [SerializeField] string displayName = "";

    [TextArea(2, 5)]
    [Tooltip("What it does.")]
    [SerializeField] string description = "";

    [Tooltip("The finished building (what it looks like when built).")]
    [SerializeField] GameObject prefab;

    [Tooltip("The build menu picture. Capture = a photo of the prefab, Static = a PNG, Animated = an .aseprite file. " +
             "Static with no Image = no picture, only the name.")]
    [SerializeField] IconSource icon = new IconSource();

    [Header("Recipe")]
    [Tooltip("What it costs. Click + for each material, drag in its Item Data (Assets/Config/Items) and set the amount. " +
             "Empty = free.")]
    [SerializeField] List<Ingredient> recipe = new List<Ingredient>();

    [Header("Snapping")]
    [Tooltip("How it snaps to other built pieces in Snap mode (middle click while placing). " +
             "Auto = from the file name: Floor / Wall / Roof if the name says so, else Other " +
             "(furniture: sits on floors and against walls).")]
    [SerializeField] PieceType pieceType = PieceType.Auto;

    [Header("Build menu")]
    [Tooltip("Which tab of the build menu it's in. Auto = from its Piece Type: Trap = Traps, anything else = Building.")]
    [SerializeField] BuildCategory category = BuildCategory.Auto;

    [Header("Upgrade")]
    [Tooltip("Built ON a placed building of this kind, which it replaces (like upgrading a tool), e.g. the Iron Bear " +
             "Trap on a Copper Bear Trap. Its Recipe is then only the extra. Empty = it's built anywhere.")]
    [SerializeField] BuildingData upgradeOf;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public GameObject Prefab => prefab;
    public IReadOnlyList<Ingredient> Recipe => recipe;
    public IconSource Icon => icon;   // where the picture comes from (Capture / Static / Animated)

    public BuildingData UpgradeOf => upgradeOf;   // null = not an upgrade (built anywhere)

    // Its name with "a" or "an" in front (by its first letter): "a Copper Bear Trap", "an Iron Bear Trap".
    public string NameWithArticle
    {
        get
        {
            string shown = DisplayName;
            bool vowel = !string.IsNullOrEmpty(shown) && "AEIOU".IndexOf(char.ToUpperInvariant(shown[0])) >= 0;
            return (vowel ? "an " : "a ") + shown;
        }
    }

    // The build menu tab it's in: the one chosen, or (Auto) Traps for a trap and Building for anything else.
    public BuildCategory Category => category != BuildCategory.Auto ? category
                                   : Piece == PieceType.Trap ? BuildCategory.Traps : BuildCategory.Building;

    // The piece type really used: the one chosen, or (Auto) the one the file name says.
    public PieceType Piece => pieceType == PieceType.Auto ? GuessPiece(name) : pieceType;

    // ===== Unity messages (Unity calls these) =====

    // Unity calls this when a value is changed in the Inspector: read the .aseprite frames if Icon is Animated.
    void OnValidate()
    {
#if UNITY_EDITOR
        if (icon == null) icon = new IconSource();
        icon.EditorRefresh(this);
#endif
    }

    // ===== My methods =====

    // The picture to show (for an animated icon: its first frame; UIAnimatedImage.ShowIcon plays the rest).
    // Capture = a photo of the prefab (taken once, then remembered).
    public Sprite GetIcon()
    {
        if (!icon.IsCapture) return icon.Still;
        return prefab != null ? IconCapture.GetIcon("Building|" + name, prefab) : null;
    }

    // The recipe checks (shared with Item Data, see Recipe).
    public bool CanAfford(PlayerInventory inv) => global::Recipe.CanAfford(recipe, inv);
    public string Missing(PlayerInventory inv) => global::Recipe.Missing(recipe, inv);   // e.g. "3 Stone", null = enough
    public bool TakeFrom(PlayerInventory inv) => global::Recipe.TakeFrom(recipe, inv);

    // The lines for the build menu's info box from its prefab's own scripts (IBuildingInfo, e.g. a trap's "Damage 20"),
    // one per line; "" = none.
    public string InfoLines()
    {
        if (prefab == null) return "";
        List<string> lines = new List<string>();
        if (upgradeOf != null) lines.Add("Built on " + upgradeOf.NameWithArticle + " (it replaces it)");
        foreach (IBuildingInfo info in prefab.GetComponentsInChildren<IBuildingInfo>(true))
        {
            string text = info.InfoLines();
            if (!string.IsNullOrEmpty(text)) lines.Add(text);
        }
        return string.Join("\n", lines);
    }

    // Floor / Wall / Roof / Trap from a name like "WoodenWall", "Stone Floor" or "Bear Trap"; anything else = Other.
    public static PieceType GuessPiece(string buildingName)
    {
        string n = buildingName.ToLowerInvariant();
        if (n.Contains("floor") || n.Contains("foundation")) return PieceType.Floor;
        if (n.Contains("wall")) return PieceType.Wall;
        if (n.Contains("roof")) return PieceType.Roof;
        if (n.Contains("trap")) return PieceType.Trap;
        return PieceType.Other;
    }
}
