/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > Items > Build Plate Armor (it asks first), and save the scene afterwards.
              Run it again any time: the items, their prefabs and the Gear page are refilled in place, and recipes you
              changed stay. Guide: Docs/Armor Setup.md
Purpose: Blink's plate armor (the PlateSet1 parts already inside the player's character, switched off) as the iron
         set: 6 items the player can craft, wear and see, in one click:
           - Assets/Config/Items/Armor/<Id>.asset: Iron Helmet, Iron Shoulders, Iron Chestplate, Iron Gloves, Iron
             Leggings, Iron Boots. Item Type Armor: the slot, Protection 1.5 % each and 2.5 % for the chestplate (10 %
             off every hit for all six), Set = Iron (all six: stamina recharges 50 % faster, PlayerArmor's Set
             Bonuses), the character's part to show and what it hides; a photo icon; a recipe of Iron (5 / 4 / 8 / 3 /
             7 / 4, only for a new item or one without a recipe); made at the Crafting page. They were called Plate...
             before: their old files are renamed (so everything linked to them stays linked);
           - Assets/Prefabs/Item/Armor/<Id>.prefab (+ Meshes/<Id>.asset): the piece frozen in the character's pose, for
             its icon and for when it's dropped;
           - ArmorLook on Assets/Prefabs/Player/PlayerCharacter, so the body, the first-person hands and the Gear page's
             preview show the worn pieces, with the skin under the clothes (so nothing flickers): Blink's rows (the
             skin each piece covers from every side, switched off) and trimmed copies of the skin a piece covers only
             partly (Assets/Prefabs/Player/SkinUnderClothes: the arms under the chestplate and the hands in the gloves,
             without the faces under them; for the body and for the first-person hands);
           - PlayerArmor on the Player (the worn armor: it takes its share off every hit), and the armor in the
             Player's ToolTestKit (GameRules Tool Test gives it too);
           - the armor in Turki's crafting list, after the tools (Tools > UI > Build Crafting Menu's sorting);
           - Tools > UI > Build Gear Page: the inventory's Gear page (the character on the left, the slots on the right);
           - the Armor row on the inventory's Info tab (how much the armor protects, the set bonus in gold).
Requirement: ArmorBuilder.cs x-> ItemData.cs, ArmorStats.cs, IconSource.cs, ItemPickup.cs, ArmorLook.cs, FirstPersonHands.cs, PlayerArmor.cs, PlayerInventory.cs, ToolTestKit.cs, MeshAssetHelper.cs, CraftingMenuBuilder.cs, GearPageBuilder.cs, InventoryUIBuilder.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders, CheckTMP, AddToPlayer

public static class ArmorBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Plate Armor";
    const string ItemFolder = "Assets/Config/Items/Armor";
    const string PrefabFolder = "Assets/Prefabs/Item/Armor";
    const string MeshFolder = PrefabFolder + "/Meshes";
    const string CharacterPath = "Assets/Prefabs/Player/PlayerCharacter.prefab";
    const string HandsPath = "Assets/Prefabs/Player/FirstPersonHands.prefab";   // the first-person hands (found by name if moved)
    const string SkinFolder = "Assets/Prefabs/Player/SkinUnderClothes";         // the trimmed skin copies
    const string BodyName = "Body";   // Blink's skin parts are under Mesh > Body (Arms, Chest, Feet, Hands, Head, Legs, Neck)
    const string ModelName = "Model";

    // The skin under the clothes, as shares of the character's height (0.0006 = about 1 mm on a 1.8 m body). Skin lies
    // ON a piece where its corners are within OnSkin of the piece's surface: two surfaces that close flicker. A skin
    // part gets a trimmed copy when at least MinOnShare (1 %) of it lies on the piece, and the copy leaves out every
    // face within CutSkin (about 3 mm, so also where an animation moves the two a little apart).
    const float OnSkin = 0.0006f;
    const float CutSkin = 0.0017f;
    const float MinOnShare = 0.01f;

    // One piece: its Item Id (and the one it had before), its name, the character's part, where it's worn, how much of
    // a hit it takes off (percent), how much Iron it costs and what it hides on the character while worn.
    struct Piece
    {
        public string id, oldId, name, part;
        public ArmorSlot slot;
        public float protection;
        public int iron;
        public string[] hides;
    }

    const string SetName = "Iron";   // the set the six pieces make (PlayerArmor's Set Bonuses: Iron = stamina 50 % faster)

    // 1.5 % a piece and 2.5 % for the chestplate: 10 % off every hit for the whole set.
    static readonly Piece[] Pieces =
    {
        new Piece { id = "IronHelmet",     oldId = "PlateHelmet",     name = "Iron Helmet",     part = "PlateSet1_Helmet",    slot = ArmorSlot.Helmet,    protection = 1.5f, iron = 5, hides = new[] { "Hair" } },
        new Piece { id = "IronShoulders",  oldId = "PlateShoulders",  name = "Iron Shoulders",  part = "PlateSet1_Shoulders", slot = ArmorSlot.Shoulders, protection = 1.5f, iron = 4, hides = new string[0] },
        new Piece { id = "IronChestplate", oldId = "PlateChestplate", name = "Iron Chestplate", part = "PlateSet1_Chest",     slot = ArmorSlot.Chest,     protection = 2.5f, iron = 8, hides = new[] { "Starter_Chest" } },
        new Piece { id = "IronGloves",     oldId = "PlateGloves",     name = "Iron Gloves",     part = "PlateSet1_Gloves",    slot = ArmorSlot.Gloves,    protection = 1.5f, iron = 3, hides = new string[0] },
        new Piece { id = "IronLeggings",   oldId = "PlateLeggings",   name = "Iron Leggings",   part = "PlateSet1_Pants",     slot = ArmorSlot.Legs,      protection = 1.5f, iron = 7, hides = new[] { "Starter_Pants" } },
        new Piece { id = "IronBoots",      oldId = "PlateBoots",      name = "Iron Boots",      part = "PlateSet1_Boots",     slot = ArmorSlot.Boots,     protection = 1.5f, iron = 4, hides = new[] { "Starter_Boots" } },
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Items/Build Plate Armor", false, MenuOrder.BuildPlateArmor)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;

        string characterPath = FindPrefabPath(CharacterPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(characterPath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no PlayerCharacter prefab yet. Run Tools > Player > Add Character Model first.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(ToolName,
                "This makes Blink's plate armor wearable, as the iron set:\n\n" +
                "- 6 armor items (helmet, shoulders, chestplate, gloves, leggings, boots) in " + ItemFolder + ":\n" +
                "  1.5 % protection each, 2.5 % for the chestplate (10 % for all six); all six = stamina recharges 50 % faster\n" +
                "- ArmorLook on the PlayerCharacter prefab, PlayerArmor on the Player\n" +
                "- the skin under the clothes hidden or trimmed, so the shoulders, gloves and boots don't flicker\n" +
                "- the armor in Turki's crafting list (recipes of Iron) and in the Tool Test kit\n" +
                "- the inventory's Gear page, built again: the character on the left, the armor slots on the right\n" +
                "- the Armor row on the inventory's Info tab (the set bonus in gold)\n\n" +
                "Open GameScene first, and save it afterwards. Continue?", "Build", "Cancel"))
            return;

        int renamed = RenameOldFiles();   // Plate... -> Iron...
        ItemData iron = FindItem("Iron");
        List<ItemData> items = new List<ItemData>();
        List<string> missing = new List<string>();
        List<ArmorLook.SkinUnder> skin;
        List<string> trimmed = new List<string>();
        float height;
        GameObject source = PrefabUtility.LoadPrefabContents(characterPath);   // to freeze each piece from (never saved)
        try
        {
            foreach (Piece piece in Pieces)
            {
                SkinnedMeshRenderer part = FindPart(source, piece.part);
                if (part == null)
                {
                    missing.Add(piece.part);
                    continue;
                }
                ItemData item = FindOrMakeItem(piece.id);
                GameObject prefab = BuildPrefab(piece, part, item);
                FillItem(item, piece, prefab, iron);
                items.Add(item);
            }
            skin = SkinRows(source);
            height = AddTrims(source, skin, 0f, trimmed);   // the body's skin
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
        }
        AddFirstPersonTrims(skin, height, trimmed);           // the first-person hands' own arms and hands
        skin.RemoveAll(row => (row.hides == null || row.hides.Length == 0) && row.trims.Count == 0);   // rows that do nothing
        AssetDatabase.SaveAssets();

        string look = SetUpArmorLook(characterPath, skin);
        string player = SetUpPlayer(items);
        string crafting = CraftingMenuBuilder.SortList();
        string gear = GearPageBuilder.BuildPage();
        string info = InventoryUIBuilder.AddArmorRowToCanvas();
        AssetDatabase.SaveAssets();

        Debug.Log(ToolName + ": " + items.Count + " iron armor items in " + ItemFolder +
                  (renamed > 0 ? " (" + renamed + " old Plate files renamed to Iron)" : "") +
                  (missing.Count > 0 ? " (not found on the character: " + string.Join(", ", missing) + ")" : "") +
                  (iron == null ? " (no Iron item: no recipes)" : "") +
                  "; " + look + " (skin under the clothes: " +
                  (trimmed.Count > 0 ? "trimmed copies " + string.Join(", ", trimmed) : "no trimmed copies needed") + ");" +
                  player + " Crafting: " + crafting + " Gear page: " + gear + " Info tab: " + info + "." +
                  " Save the scene, then Play: craft or get the armor (Tool Test), Tab > Gear.");
        string clashes = Clashes(skin);
        if (clashes != null) Debug.LogWarning(ToolName + ": " + clashes);
    }

    // ===== My methods =====

    // The pieces were called Plate... at first (20/11/2026). Their files (Item Data, prefab, frozen mesh) get the Iron
    // names, and a rename keeps everything that links to them: Turki's crafting list, the Tool Test kit, the prefab's
    // mesh. Nothing happens once they're renamed, or if an Iron file is there already. Returns how many were renamed.
    static int RenameOldFiles()
    {
        int renamed = 0;
        foreach (Piece piece in Pieces)
        {
            ItemData old = FindItem(piece.oldId);
            if (old != null && FindItem(piece.id) == null) renamed += Rename(AssetDatabase.GetAssetPath(old), piece.id);
            renamed += Rename(FindPrefabPath(PrefabFolder + "/" + piece.oldId + ".prefab"), piece.id);
            renamed += Rename(MeshFolder + "/" + piece.oldId + ".asset", piece.id);
        }
        if (renamed > 0) AssetDatabase.SaveAssets();
        return renamed;
    }

    // Renames the file at "path" to "newName" (same folder and extension) if it's there and that name is free. 1 = done.
    static int Rename(string path, string newName)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.LoadMainAssetAtPath(path) == null) return 0;
        string target = FolderOf(path) + "/" + newName + Path.GetExtension(path);
        if (AssetDatabase.LoadMainAssetAtPath(target) != null) return 0;   // the new file is there already: both stay
        string problem = AssetDatabase.RenameAsset(path, newName);
        if (string.IsNullOrEmpty(problem)) return 1;
        Debug.LogWarning(ToolName + ": couldn't rename " + path + " to " + newName + ": " + problem);
        return 0;
    }

    // The character's part with this name (e.g. PlateSet1_Helmet), switched on or off, or null.
    static SkinnedMeshRenderer FindPart(GameObject character, string partName)
    {
        foreach (SkinnedMeshRenderer part in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (part.name == partName) return part;
        }
        return null;
    }

    static ItemData FindItem(string id)
    {
        foreach (string guid in AssetDatabase.FindAssets(id + " t:ItemData", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == id) return AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }
        return null;
    }

    static ItemData FindOrMakeItem(string id)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemFolder + "/" + id + ".asset");
        if (item == null) item = FindItem(id);   // moved somewhere else
        if (item != null) return item;

        MakeFolders(ItemFolder);
        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, ItemFolder + "/" + id + ".asset");
        return item;
    }

    // The piece's item prefab, emptied and refilled IN PLACE if it exists: a box to aim at (F picks it up), ItemPickup,
    // and Model = the frozen piece with the character's armor material.
    static GameObject BuildPrefab(Piece piece, SkinnedMeshRenderer part, ItemData item)
    {
        Mesh mesh = FreezeMesh(piece, part);
        if (mesh == null) return null;

        string path = FindPrefabPath(PrefabFolder + "/" + piece.id + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(piece.id);
        try
        {
            if (existed) Empty(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            GameObject model = new GameObject(ModelName);
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterials = part.sharedMaterials;

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;

            SerializedObject so = new SerializedObject(root.AddComponent<ItemPickup>());   // the names in quotes must match ItemPickup's fields
            so.FindProperty("data").objectReferenceValue = item;
            so.FindProperty("amount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // The piece frozen in the character's saved pose, turned and sized as it is on the character, with its middle at
    // (0, 0, 0). Saved (refilled) as Meshes/<Id>.asset.
    static Mesh FreezeMesh(Piece piece, SkinnedMeshRenderer part)
    {
        Mesh posed = PosedMesh(part);
        MakeFolders(MeshFolder);
        Mesh saved = MeshAssetHelper.SaveCopy(posed, posed.bounds.center, 1f, MeshFolder + "/" + piece.id + ".asset");
        Object.DestroyImmediate(posed);
        return saved;
    }

    // A new mesh of the part as it stands in the character's saved pose, in the character's space (destroy it when done).
    static Mesh PosedMesh(SkinnedMeshRenderer part)
    {
        part.gameObject.SetActive(true);   // only in an opened copy, which is never saved
        Mesh posed = new Mesh();
        part.BakeMesh(posed, true);        // true = without the part's own scale: its matrix below adds it (else it'd be twice)

        // Into the character's space: everything the part's object does (move, turn, size) relative to the character.
        Matrix4x4 toCharacter = part.transform.root.worldToLocalMatrix * part.transform.localToWorldMatrix;
        Vector3[] corners = posed.vertices;
        Vector3[] normals = posed.normals;
        Vector4[] tangents = posed.tangents;
        for (int i = 0; i < corners.Length; i++) corners[i] = toCharacter.MultiplyPoint3x4(corners[i]);
        for (int i = 0; i < normals.Length; i++) normals[i] = toCharacter.MultiplyVector(normals[i]).normalized;
        for (int i = 0; i < tangents.Length; i++)
        {
            Vector3 along = toCharacter.MultiplyVector(tangents[i]).normalized;
            tangents[i] = new Vector4(along.x, along.y, along.z, tangents[i].w);
        }
        posed.vertices = corners;
        if (normals.Length == corners.Length) posed.normals = normals;
        if (tangents.Length == corners.Length) posed.tangents = tangents;
        posed.RecalculateBounds();
        return posed;
    }

    // Removes everything from an opened prefab except its top object's Transform: children, scripts, then the rest.
    static void Empty(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in root.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(script);
        foreach (Component part in root.GetComponents<Component>())
        {
            if (part != null && !(part is Transform)) Object.DestroyImmediate(part);
        }
    }

    // The Item Data's settings: name, prefab, Pickup, a photo icon, Item Type Armor with its Armor section (slot,
    // Protection, Set, Piece, Hides), and a recipe of Iron only when it has none (so a recipe you changed stays).
    static void FillItem(ItemData item, Piece piece, GameObject prefab, ItemData iron)
    {
        SerializedObject so = new SerializedObject(item);   // the names in quotes must match ItemData's fields
        so.FindProperty("displayName").stringValue = piece.name;
        if (prefab != null) so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("pickup").enumValueIndex = (int)PickupType.Pickup;
        so.FindProperty("icon.mode").enumValueIndex = (int)IconMode.Capture;
        so.FindProperty("itemType").enumValueIndex = (int)ItemType.Armor;
        so.FindProperty("armor.slot").enumValueIndex = (int)piece.slot;
        so.FindProperty("armor.protection").floatValue = piece.protection;
        so.FindProperty("armor.set").stringValue = SetName;
        so.FindProperty("armor.piece").stringValue = piece.part;
        SerializedProperty hides = so.FindProperty("armor.hides");
        hides.ClearArray();
        foreach (string covered in piece.hides)
        {
            hides.InsertArrayElementAtIndex(hides.arraySize);
            hides.GetArrayElementAtIndex(hides.arraySize - 1).stringValue = covered;
        }
        so.FindProperty("craftAmount").intValue = 1;
        so.FindProperty("craftedAt").enumValueIndex = (int)CraftStation.Crafting;

        SerializedProperty recipe = so.FindProperty("recipe");
        if (recipe.arraySize == 0 && iron != null)
        {
            recipe.InsertArrayElementAtIndex(0);
            SerializedProperty line = recipe.GetArrayElementAtIndex(0);
            line.FindPropertyRelative("item").objectReferenceValue = iron;
            line.FindPropertyRelative("amount").intValue = piece.iron;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    // ArmorLook on the PlayerCharacter prefab's top object (the Animator's), so every copy of the character has it, with
    // its Skin Under Clothes rows. Returns a few words for the Console.
    static string SetUpArmorLook(string characterPath, List<ArmorLook.SkinUnder> rows)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(characterPath);
        try
        {
            ArmorLook look = root.GetComponent<ArmorLook>();
            bool added = look == null;
            if (added) look = root.AddComponent<ArmorLook>();
            WriteSkinRows(look, rows);
            PrefabUtility.SaveAsPrefabAsset(root, characterPath);
            return "ArmorLook " + (added ? "added to" : "updated on") + " PlayerCharacter";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ArmorLook's Skin Under Clothes rows to keep: each clothing part and its Hides as set on the prefab (Blink's when
    // there's no ArmorLook yet), Blink's rows it's missing, and an empty row for each armor piece without one (a trimmed
    // copy needs a row). The Trims are always made again (AddTrims).
    static List<ArmorLook.SkinUnder> SkinRows(GameObject character)
    {
        List<ArmorLook.SkinUnder> rows = new List<ArmorLook.SkinUnder>();
        ArmorLook look = character.GetComponentInChildren<ArmorLook>(true);
        if (look != null)
        {
            foreach (ArmorLook.SkinUnder row in look.SkinUnderClothes)
            {
                if (row == null || string.IsNullOrEmpty(row.clothing) || HasRow(rows, row.clothing)) continue;
                rows.Add(new ArmorLook.SkinUnder(row.clothing, row.hides != null ? (string[])row.hides.Clone() : new string[0]));
            }
        }
        foreach (ArmorLook.SkinUnder row in ArmorLook.BlinkSkin())
        {
            if (!HasRow(rows, row.clothing)) rows.Add(row);
        }
        foreach (Piece piece in Pieces)
        {
            if (!HasRow(rows, piece.part)) rows.Add(new ArmorLook.SkinUnder(piece.part));
        }
        return rows;
    }

    static bool HasRow(List<ArmorLook.SkinUnder> rows, string clothing) => rows.Exists(row => row.clothing == clothing);

    // Trimmed copies (ArmorLook.TrimmedSkin) for every row's clothing: each skin part it doesn't switch off, where at
    // least MinOnShare of it lies on the clothing. On the first-person hands (they have FirstPersonHands) only the parts
    // that copy draws: its arms, hands and gloves. height = the character's height (0 = measure it on this one's skin);
    // returns it, so the first-person hands (only arms) can use the body's.
    static float AddTrims(GameObject character, List<ArmorLook.SkinUnder> rows, float height, List<string> made)
    {
        Transform body = FindDeep(character.transform, BodyName);
        if (body == null) return height;
        FirstPersonHands hands = character.GetComponentInChildren<FirstPersonHands>(true);

        Dictionary<SkinnedMeshRenderer, Mesh> skins = new Dictionary<SkinnedMeshRenderer, Mesh>();   // each skin part and its posed copy
        try
        {
            foreach (SkinnedMeshRenderer part in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (part.sharedMesh != null && (hands == null || hands.Draws(part.name))) skins[part] = PosedMesh(part);
            }
            if (height <= 0f) height = Height(skins.Values);

            foreach (ArmorLook.SkinUnder row in rows)
            {
                SkinnedMeshRenderer cloth = FindPart(character, row.clothing);
                if (cloth == null || cloth.sharedMesh == null || (hands != null && !hands.Draws(cloth.name))) continue;
                Mesh clothPosed = PosedMesh(cloth);
                Vector3[] clothCorners = clothPosed.vertices;
                int[] clothFaces = clothPosed.triangles;
                Object.DestroyImmediate(clothPosed);

                foreach (KeyValuePair<SkinnedMeshRenderer, Mesh> skin in skins)
                {
                    if (row.hides != null && System.Array.IndexOf(row.hides, skin.Key.name) >= 0) continue;   // switched off anyway
                    if (row.trims.Exists(trim => trim.full == skin.Key.sharedMesh)) continue;                  // that mesh is done
                    Mesh trimmed = Trim(skin.Key.sharedMesh, skin.Value, clothCorners, clothFaces, row.clothing, height, out float share);
                    if (trimmed == null) continue;
                    row.trims.Add(new ArmorLook.TrimmedSkin { part = skin.Key.name, full = skin.Key.sharedMesh, trimmed = trimmed });
                    made.Add(trimmed.name + " (" + Mathf.RoundToInt(share * 100f) + " % of it lay on it)");
                }
            }
        }
        finally
        {
            foreach (Mesh posed in skins.Values) Object.DestroyImmediate(posed);
        }
        return height;
    }

    // The first-person hands' own trimmed copies, added to the same rows: that copy's arms and hands have meshes of their
    // own (FirstPersonArmsHelper), and ArmorLook only uses a copy on a part that has the mesh it was made from.
    static void AddFirstPersonTrims(List<ArmorLook.SkinUnder> rows, float height, List<string> made)
    {
        string path = FindPrefabPath(HandsPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;   // no first-person hands yet
        GameObject copy = PrefabUtility.LoadPrefabContents(path);             // never saved
        try
        {
            AddTrims(copy, rows, height, made);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(copy);
        }
    }

    // Lowest to highest corner of these posed meshes (the character's height), or 1.8 m if there are none.
    static float Height(IEnumerable<Mesh> posed)
    {
        float low = float.MaxValue, high = float.MinValue;
        foreach (Mesh mesh in posed)
        {
            foreach (Vector3 corner in mesh.vertices)
            {
                low = Mathf.Min(low, corner.y);
                high = Mathf.Max(high, corner.y);
            }
        }
        return high > low ? high - low : 1.8f;
    }

    // A copy of the skin's mesh without its faces lying under the clothing (all three corners within CutSkin of the
    // clothing's surface), saved as SkinFolder/<mesh> under <clothing>.asset. Null = less than MinOnShare of the skin
    // lies ON the clothing (within OnSkin), so the two don't flicker and no copy is needed. share = how much of it does.
    static Mesh Trim(Mesh mesh, Mesh posed, Vector3[] cloth, int[] clothFaces, string clothing, float height, out float share)
    {
        float on = height * OnSkin, cut = height * CutSkin;
        Vector3[] corners = posed.vertices;
        float[] distance = DistanceTo(cloth, clothFaces, corners, cut);

        float all = 0f, lying = 0f;   // the skin's whole surface, and the part of it lying on the clothing
        int cutFaces = 0;
        List<int>[] kept = new List<int>[posed.subMeshCount];
        for (int sub = 0; sub < kept.Length; sub++)
        {
            kept[sub] = new List<int>();
            int[] faces = posed.GetTriangles(sub);
            for (int i = 0; i + 2 < faces.Length; i += 3)
            {
                int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                float area = Vector3.Cross(corners[b] - corners[a], corners[c] - corners[a]).magnitude;   // twice its area: only shares count
                float farthest = Mathf.Max(distance[a], Mathf.Max(distance[b], distance[c]));
                all += area;
                if (farthest <= on) lying += area;
                if (farthest <= cut)
                {
                    cutFaces++;
                    continue;
                }
                kept[sub].Add(a);
                kept[sub].Add(b);
                kept[sub].Add(c);
            }
        }
        share = all > 0f ? lying / all : 0f;
        if (share < MinOnShare || cutFaces == 0) return null;

        MakeFolders(SkinFolder);
        return MeshAssetHelper.SaveSkinnedCopy(mesh, kept, SkinFolder + "/" + mesh.name + " under " + clothing + ".asset");
    }

    // How far each corner is from the clothing's surface (its faces); farther than "limit" = float.MaxValue.
    static float[] DistanceTo(Vector3[] cloth, int[] faces, Vector3[] corners, float limit)
    {
        int count = faces.Length / 3;
        Bounds[] near = new Bounds[count];   // each clothing face's box, grown by "limit" on every side (a quick first check)
        for (int f = 0; f < count; f++)
        {
            Bounds box = new Bounds(cloth[faces[f * 3]], Vector3.zero);
            box.Encapsulate(cloth[faces[f * 3 + 1]]);
            box.Encapsulate(cloth[faces[f * 3 + 2]]);
            box.Expand(limit * 2f);
            near[f] = box;
        }

        float[] distance = new float[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 corner = corners[i];
            float best = float.MaxValue;
            for (int f = 0; f < count; f++)
            {
                if (!near[f].Contains(corner)) continue;
                Vector3 closest = ClosestOnFace(corner, cloth[faces[f * 3]], cloth[faces[f * 3 + 1]], cloth[faces[f * 3 + 2]]);
                best = Mathf.Min(best, Vector3.Distance(corner, closest));
            }
            distance[i] = best <= limit ? best : float.MaxValue;
        }
        return distance;
    }

    // The point of the face (a, b, c) closest to p: inside it, on one of its edges or at a corner (Ericson's way, from
    // "Real-Time Collision Detection").
    static Vector3 ClosestOnFace(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));

        float sum = va + vb + vc;
        if (sum <= 0f) return a;   // a flat face with no area
        return a + ab * (vb / sum) + ac * (vc / sum);
    }

    static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Writes the rows into ArmorLook's Skin Under Clothes list (the names in quotes must match ArmorLook's fields).
    static void WriteSkinRows(ArmorLook look, List<ArmorLook.SkinUnder> rows)
    {
        SerializedObject so = new SerializedObject(look);
        SerializedProperty list = so.FindProperty("skinUnderClothes");
        list.ClearArray();
        for (int r = 0; r < rows.Count; r++)
        {
            list.InsertArrayElementAtIndex(r);
            SerializedProperty row = list.GetArrayElementAtIndex(r);
            row.FindPropertyRelative("clothing").stringValue = rows[r].clothing;

            SerializedProperty hides = row.FindPropertyRelative("hides");
            hides.ClearArray();
            foreach (string part in rows[r].hides ?? new string[0])
            {
                hides.InsertArrayElementAtIndex(hides.arraySize);
                hides.GetArrayElementAtIndex(hides.arraySize - 1).stringValue = part;
            }

            SerializedProperty trims = row.FindPropertyRelative("trims");
            trims.ClearArray();
            foreach (ArmorLook.TrimmedSkin trim in rows[r].trims)
            {
                trims.InsertArrayElementAtIndex(trims.arraySize);
                SerializedProperty line = trims.GetArrayElementAtIndex(trims.arraySize - 1);
                line.FindPropertyRelative("part").stringValue = trim.part;
                line.FindPropertyRelative("full").objectReferenceValue = trim.full;
                line.FindPropertyRelative("trimmed").objectReferenceValue = trim.trimmed;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A body part trimmed under two pieces uses only one copy at a time (the later row's) while both show. A sentence
    // naming such parts, or null.
    static string Clashes(List<ArmorLook.SkinUnder> rows)
    {
        Dictionary<Mesh, List<string>> under = new Dictionary<Mesh, List<string>>();
        foreach (ArmorLook.SkinUnder row in rows)
        {
            foreach (ArmorLook.TrimmedSkin trim in row.trims)
            {
                if (!under.TryGetValue(trim.full, out List<string> clothes)) under[trim.full] = clothes = new List<string>();
                clothes.Add(row.clothing);
            }
        }
        List<string> clashes = new List<string>();
        foreach (KeyValuePair<Mesh, List<string>> part in under)
        {
            if (part.Value.Count > 1) clashes.Add(part.Key.name + " (under " + string.Join(" and ", part.Value) + ")");
        }
        if (clashes.Count == 0) return null;
        return "these skin parts lie on more than one piece: " + string.Join(", ", clashes) + ". While both pieces show, " +
               "only the later one's trimmed copy is used, so a little flicker may stay there.";
    }

    // PlayerArmor on the Player in the open scene, and the armor in its ToolTestKit (Ctrl + Z undoes it).
    // Returns a sentence for the Console.
    static string SetUpPlayer(List<ItemData> items)
    {
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();
        if (inventory == null) return " No Player in the open scene: open GameScene and run it again (for PlayerArmor).";

        AddToPlayer<PlayerArmor>();
        string kitReport = "";
        ToolTestKit kit = inventory.GetComponent<ToolTestKit>();
        if (kit != null)
        {
            SerializedObject so = new SerializedObject(kit);
            SerializedProperty list = so.FindProperty("tools");   // must match ToolTestKit's field
            int added = 0;
            foreach (ItemData item in items)
            {
                bool there = false;
                for (int i = 0; i < list.arraySize && !there; i++) there = list.GetArrayElementAtIndex(i).objectReferenceValue == item;
                if (there) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                added++;
            }
            so.ApplyModifiedProperties();   // with Undo
            kitReport = added > 0 ? " " + added + " armor items added to the Tool Test kit." : "";
        }
        EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);
        return " PlayerArmor on the Player." + kitReport;
    }
}
