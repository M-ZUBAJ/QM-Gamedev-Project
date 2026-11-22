/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Items > Build Bow And Arrows (it asks first, and saves your open scenes). Needs GameLair's
              Medieval Fantasy Weapon Pack (Assets/Imported3D/GameLair) and, for the animation, Mixamo's Shooting Arrow
              (Assets/Imported3D/Mixamo/BotShootingArrow.fbx). To use another of the pack's bows (BowTwo, BowThree),
              select it in the Project window first; else BowOne. Run it again any time: everything is made again in
              the same files (the arrows' recipes and damage too). Guide: Docs/Bow Setup.md
Purpose: The bow and its arrows, in one click:
           1. Wooden Bow: Item Data Assets/Config/Items/Tools/WoodenBow (Tool, Kind Bow, Tier Wood; recipe 10 Wood +
              10 Plank) and its prefab Assets/Prefabs/Item/Tools/WoodenBow (the pack's bow; its Model has the bow's grip
              at its middle, upright, shooting along x: ToolInHand puts that in the left fist).
           2. Five arrows: Assets/Config/Items/Arrows/<Stone|Copper|Iron|Gold|Diamond>Arrow (Ammo; Damage 20 / 25 / 30 /
              40 / 50; recipes 2 Wood + 2 Stone = 4 Stone Arrows, then 4 arrows + 1 Copper / Iron / Gold / Diamond =
              4 arrows of the next kind) and their prefabs in Assets/Prefabs/Item/Arrows. All are the pack's ArrowOne
              with only the head recolored: its mesh is copied with the head (the faces at the pointed end in the tip's
              color on the pack's color sheet) as a second part (Assets/Prefabs/Item/Arrows/Meshes), and each kind's
              head gets its material (Assets/Materials/Arrows, made once: change their colors by hand there).
           3. The flying arrow: Assets/Prefabs/Item/Arrows/Arrow Projectile (ArrowProjectile; its numbers are kept
              when the tool runs again).
           4. The bow's animation: Tools > Player > Set Up Player Animations' file work (BotShootingArrow cut into
              BowDraw / BowAim / BowRelease, both controllers).
           5. Turki's crafting list sorted again: the bow joins the Wooden tools, the arrows come after the tools.
           6. The draw ring and arrow count: Assets/Prefabs/UI/Bow/BowCanvas (BowHUD). ToolInHand on the character
              prefab, if it isn't there yet (it holds the bow).
           7. Every game scene in File > Build Settings (GameScene, ProceduralGenerateScene), saved: BowShooter on the
              Player (Arrow Prefab filled), the bow and the arrows in ToolTestKit (GameRules > Tool Test gives them), one
              BowCanvas.
           A missing Gold or Diamond item is made first (the desert's and the snow's tools make the same ones).
Requirement: BowBuilder.cs x-> ItemData.cs, AmmoStats.cs, ToolStats.cs, ItemPickup.cs, ArrowProjectile.cs, BowShooter.cs, BowHUD.cs, ToolTestKit.cs, ToolInHand.cs, PlayerAnimationSetup.cs, CraftingMenuBuilder.cs, DesertBuilder.cs, SnowBuilder.cs, IconArt.cs, MeshBounds.cs, MeshAssetHelper.cs, MaterialBuildHelper.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UIBuildHelper;   // FindPrefabPath, FolderOf, MakeFolders, NewUI, NewCanvas, AddText, CheckTMP

public static class BowBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Bow And Arrows";
    const string ToolItemFolder = "Assets/Config/Items/Tools";
    const string ToolPrefabFolder = "Assets/Prefabs/Item/Tools";
    const string ArrowItemFolder = "Assets/Config/Items/Arrows";
    const string ArrowPrefabFolder = "Assets/Prefabs/Item/Arrows";
    const string ArrowMeshFolder = ArrowPrefabFolder + "/Meshes";
    const string HeadFolder = "Assets/Materials/Arrows";
    const string MaterialFolder = "Assets/Materials/Tools";   // Built-in copies, if the pack's material ever needs one
    const string ProjectileName = "Arrow Projectile";
    const string HudFolder = "Assets/Prefabs/UI/Bow";
    const string HudName = "BowCanvas";
    const int HudSortOrder = 2;          // HUD 0, notifications 5: the ring sits over the HUD's crosshair

    const string BowId = "WoodenBow";
    const string BowShown = "Wooden Bow";
    const string DefaultBow = "BowOne";  // the pack's plain longbow (BowTwo: shorter, wrapped; BowThree: a recurve)
    const string ArrowModel = "ArrowOne";
    const string PackMaterial = "WeaponMaterial";   // the pack's one material (Standard, its color sheet)
    const string ModelName = "Model";    // ToolInHand copies the child with this name into the hand
    const int BowWood = 10, BowPlanks = 10;
    const int ArrowsPerCraft = 4;

    // The arrows, weakest first: each kind's head color (metal, shine) and damage. Stone is made from Wood + Stone,
    // every other kind from 4 arrows of the kind before + 1 of its metal (the item named like it).
    struct ArrowKind
    {
        public string tier;
        public Color head;
        public float metallic, smoothness, damage;
        public ArrowKind(string tier, Color head, float metallic, float smoothness, float damage)
        {
            this.tier = tier;
            this.head = head;
            this.metallic = metallic;
            this.smoothness = smoothness;
            this.damage = damage;
        }
    }
    static readonly ArrowKind[] Kinds =
    {
        new ArrowKind("Stone", new Color(0.52f, 0.52f, 0.54f), 0f, 0.15f, 20f),
        new ArrowKind("Copper", new Color(0.85f, 0.48f, 0.28f), 0.75f, 0.55f, 25f),
        new ArrowKind("Iron", new Color(0.74f, 0.76f, 0.8f), 0.8f, 0.6f, 30f),
        new ArrowKind("Gold", new Color(1f, 0.78f, 0.22f), 0.85f, 0.7f, 40f),
        new ArrowKind("Diamond", new Color(0.55f, 0.95f, 1f), 0.3f, 0.9f, 50f),
    };

    // The arrow model as measured: its mesh copy (head = part 1), the tip and the way it points (in the mesh's own space),
    // and how big it is.
    struct ArrowShape
    {
        public Mesh mesh;
        public Vector3 tip;
        public Vector3 pointsTo;
        public float length;
        public float scale;   // 1, or 0.01 if the file came in centimeters
    }

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Items/Build Bow And Arrows", false, MenuOrder.BuildBowAndArrows)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;
        string bowPath = ChosenBow();
        string arrowPath = ModelPath(ArrowModel);
        if (bowPath == null || arrowPath == null)
        {
            EditorUtility.DisplayDialog(ToolName, "GameLair's Medieval Fantasy Weapon Pack isn't in the project (its BowOne and " +
                                        "ArrowOne models).\n\nImport it (Window > Package Manager > My Assets), then run this again.", "OK");
            return;
        }

        List<string> scenes = GameScenes();
        if (!EditorUtility.DisplayDialog(ToolName,
                "Make the Wooden Bow (from " + Path.GetFileNameWithoutExtension(bowPath) + ") and five arrows:\n\n" +
                "- Wooden Bow: 10 Wood + 10 Plank\n" +
                "- 4 Stone Arrows: 2 Wood + 2 Stone\n" +
                "- 4 Copper / Iron / Gold / Diamond Arrows: 4 arrows of the kind before + 1 Copper / Iron / Gold / Diamond\n\n" +
                "It also sets up the bow's animation (BotShootingArrow), sorts Turki's crafting list, makes the draw ring, " +
                "and puts BowShooter, the test kit and the ring in (and saves): " +
                (scenes.Count > 0 ? string.Join(", ", scenes.ConvertAll(Path.GetFileNameWithoutExtension)) : "no game scene in Build Settings") +
                ".\n\nContinue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;   // unsaved changes: save them, or stop

        List<string> report = new List<string>();
        Material palette = FindPalette(bowPath);
        if (palette == null) report.Add("the pack's " + PackMaterial + " material wasn't found: the bow and arrows have no colors (pink)");
        ItemData bow = BuildBow(bowPath, palette, report);
        List<ItemData> arrows = new List<ItemData>();
        if (SplitArrow(arrowPath, palette, out ArrowShape shape, report))
        {
            arrows = BuildArrows(shape, palette, report);
            BuildProjectile(shape, palette, report);
        }
        AssetDatabase.SaveAssets();

        report.Add(SetUpCharacter());
        report.AddRange(PlayerAnimationSetup.FixAnimationFiles());   // the bow's clips and states (and every other animation put right)
        report.Add("crafting list: " + CraftingMenuBuilder.SortList());
        GameObject hud = BuildHud();
        report.Add(HudFolder + "/" + HudName + ".prefab made (the draw ring and arrow count)");

        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        foreach (string scene in scenes) report.Add(SetUpScene(scene, bow, arrows, hud));
        if (before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
        AssetDatabase.SaveAssets();

        string text = ToolName + ":\n- " + string.Join("\n- ", report.FindAll(line => !string.IsNullOrEmpty(line))) +
                      "\nTest: tick GameRules > Tool Test (a Wooden Bow and 32 of each arrow), Play, select the bow, hold the left " +
                      "mouse button and let go.";
        if (report.Exists(line => line != null && line.Contains("MISSING"))) Debug.LogWarning(text);   // yellow: something to fix
        else Debug.Log(text);
    }

    // ===== My methods =====

    // ----- The pack -----

    // The bow model to use: one of the pack's bows selected in the Project window, else BowOne. Null = none.
    static string ChosenBow()
    {
        Object picked = Selection.activeObject;
        if (picked is GameObject go && go.name.StartsWith("Bow"))
        {
            MeshFilter filter = go.GetComponentInChildren<MeshFilter>(true);
            string path = filter != null && filter.sharedMesh != null ? AssetDatabase.GetAssetPath(filter.sharedMesh) : AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(path) && MeshAssetHelper.MeshIn(path) != null) return path;
        }
        return ModelPath(DefaultBow);
    }

    // A model file by its exact name (e.g. "BowOne"), wherever it is; null if there's none.
    static string ModelPath(string modelName)
    {
        foreach (string guid in AssetDatabase.FindAssets(modelName + " t:Model", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == modelName) return path;
        }
        return null;
    }

    // The pack's material (one color sheet for every weapon), drawable in Built-in.
    static Material FindPalette(string bowPath)
    {
        Material found = FindByName<Material>(PackMaterial, "Material");
        if (found == null)
        {
            foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(bowPath))
            {
                if (part is Material inFile)
                {
                    found = inFile;
                    break;
                }
            }
        }
        return found != null ? MaterialBuildHelper.DrawableCopies(new[] { found }, MaterialFolder)[0] : null;
    }

    // ----- The bow -----

    static ItemData BuildBow(string modelPath, Material palette, List<string> report)
    {
        Mesh mesh = MeshAssetHelper.MeshIn(modelPath);
        if (mesh == null)
        {
            report.Add("no mesh in " + modelPath + ": no bow made");
            return null;
        }
        ItemData item = FindOrMakeItem(ToolItemFolder, BowId);
        GameObject prefab = BuildBowPrefab(mesh, palette, item);
        if (prefab == null)
        {
            report.Add("couldn't save the bow's prefab (see the Console)");
            return item;
        }

        SerializedObject so = new SerializedObject(item);
        so.FindProperty("displayName").stringValue = BowShown;
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("pickup").enumValueIndex = (int)PickupType.Pickup;
        so.FindProperty("icon.mode").enumValueIndex = (int)IconMode.Capture;
        so.FindProperty("itemType").enumValueIndex = (int)ItemType.Tool;
        so.FindProperty("tool.kind").enumValueIndex = (int)ToolKind.Bow;
        so.FindProperty("tool.tier").enumValueIndex = (int)ToolTier.Wood;
        so.FindProperty("minedWith").enumValueIndex = (int)ToolTier.None;
        so.FindProperty("craftAmount").intValue = 1;
        so.FindProperty("craftedAt").enumValueIndex = (int)CraftStation.Crafting;
        string missing = SetRecipe(so, ("Wood", FindItem("Wood"), BowWood), ("Plank", FindItem("Plank"), BowPlanks));
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        report.Add(BowShown + " (" + ToolItemFolder + "/" + BowId + ", from " + Path.GetFileNameWithoutExtension(modelPath) + "): " +
                   (missing == null ? BowWood + " Wood + " + BowPlanks + " Plank" : "no recipe yet, no " + missing + " item"));
        return item;
    }

    // The bow's prefab, emptied and refilled IN PLACE if it exists (so links to it keep working):
    //   WoodenBow (BoxCollider, ItemPickup)
    //     > Model     leaning like a tool icon; its middle = the bow's grip (ToolInHand puts that in the fist)
    //         > Bow   the pack's mesh, upright (y), turned so it shoots along x
    static GameObject BuildBowPrefab(Mesh mesh, Material palette, ItemData item)
    {
        string path = FindPrefabPath(ToolPrefabFolder + "/" + BowId + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(BowId);
        try
        {
            if (existed) Empty(root, null);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            GameObject model = new GameObject(ModelName);
            model.transform.SetParent(root.transform, false);
            GameObject bow = new GameObject("Bow");
            bow.transform.SetParent(model.transform, false);
            bow.AddComponent<MeshFilter>().sharedMesh = mesh;
            bow.AddComponent<MeshRenderer>().sharedMaterial = palette;

            BowShape(mesh, out Vector3 grip, out bool shootsAlongX);
            float scale = mesh.bounds.size.y > 5f ? 0.01f : 1f;   // a file in centimeters
            bow.transform.localRotation = shootsAlongX ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            bow.transform.localScale = Vector3.one * scale;
            bow.transform.localPosition = -(bow.transform.localRotation * grip * scale);

            model.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);   // leaning, like a tool icon (ToolInHand turns its own copy)
            Bounds seen = MeshBounds.Of(model);
            model.transform.localPosition = -seen.center;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = seen.size;

            AddPickup(root, item);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);   // only the saved prefab is kept
            else Object.DestroyImmediate(root);
        }
    }

    // Where the bow is held and which way it shoots, from its mesh (the bow stands along y): the grip is the middle of
    // the bow's height, on the side away from its tips (where the string is tied); an arrow flies from the string
    // through the grip, so it shoots toward +x when the grip is on the +x side of the tips.
    static void BowShape(Mesh mesh, out Vector3 grip, out bool shootsAlongX)
    {
        Vector3[] corners = mesh.vertices;
        Bounds box = mesh.bounds;
        float height = Mathf.Max(0.0001f, box.size.y);

        float tipX = 0f;
        int tips = 0;
        foreach (Vector3 c in corners)
        {
            if (c.y < box.max.y - 0.03f * height) continue;
            tipX += c.x;
            tips++;
        }
        tipX = tips > 0 ? tipX / tips : box.max.x;

        Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
        foreach (Vector3 c in corners)
        {
            if (Mathf.Abs(c.y - box.center.y) > 0.04f * height) continue;   // only the middle of the bow
            if (Mathf.Abs(c.x - tipX) < 0.25f * box.size.x) continue;       // a string corner in the middle (some bows have them)
            min = Vector3.Min(min, c);
            max = Vector3.Max(max, c);
        }
        grip = min.x <= max.x ? new Vector3((min.x + max.x) * 0.5f, box.center.y, (min.z + max.z) * 0.5f) : box.center;
        shootsAlongX = grip.x > tipX;
    }

    // ----- The arrows -----

    // The arrow model's mesh copied with its head as a second part, so each kind can color only its head: the faces
    // near the pointed end (the thinner end) whose color on the pack's color sheet is the tip's. Saved in
    // ArrowMeshFolder. False = the model couldn't be read.
    static bool SplitArrow(string modelPath, Material palette, out ArrowShape shape, List<string> report)
    {
        shape = default;
        Mesh source = MeshAssetHelper.MeshIn(modelPath);
        Vector3[] corners = source != null ? source.vertices : new Vector3[0];
        if (corners.Length == 0)
        {
            report.Add("couldn't read the arrow model " + modelPath + ": no arrows made");
            return false;
        }
        Vector2[] uvs = source.uv;
        int[] faces = source.triangles;
        Bounds box = source.bounds;
        int axis = box.size.x >= box.size.y && box.size.x >= box.size.z ? 0 : box.size.y >= box.size.z ? 1 : 2;   // its length
        float low = box.min[axis], length = Mathf.Max(0.0001f, box.size[axis]);

        // The pointed end: the end whose last 3 % is thinner.
        bool tipHigh = Thickness(corners, axis, box.center, low + length * 0.97f, float.MaxValue) <=
                       Thickness(corners, axis, box.center, float.MinValue, low + length * 0.03f);
        int tipCorner = 0;
        for (int i = 1; i < corners.Length; i++)
        {
            if (tipHigh ? corners[i][axis] > corners[tipCorner][axis] : corners[i][axis] < corners[tipCorner][axis]) tipCorner = i;
        }

        // The head's color: the color under a face at the tip.
        Texture2D sheet = uvs.Length == corners.Length ? ReadPicture(palette) : null;
        bool readColors = sheet != null;   // kept for the report: the sheet is destroyed below, and then it reads as null
        Color headColor = Color.clear;
        if (sheet != null)
        {
            for (int f = 0; f + 2 < faces.Length; f += 3)
            {
                if (faces[f] != tipCorner && faces[f + 1] != tipCorner && faces[f + 2] != tipCorner) continue;
                Vector2 middle = (uvs[faces[f]] + uvs[faces[f + 1]] + uvs[faces[f + 2]]) / 3f;
                headColor = sheet.GetPixelBilinear(middle.x, middle.y);
                break;
            }
        }

        List<int> body = new List<int>(), head = new List<int>();
        int usedPass = 0;
        for (int pass = 0; pass < 2 && head.Count == 0; pass++)   // pass 1 = no color match: the faces in the tip's 12 %
        {
            usedPass = pass;
            body.Clear();
            for (int f = 0; f + 2 < faces.Length; f += 3)
            {
                int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                float along = ((corners[a][axis] + corners[b][axis] + corners[c][axis]) / 3f - low) / length;
                float fromTip = tipHigh ? 1f - along : along;
                bool isHead;
                if (pass == 0 && sheet != null)
                {
                    Vector2 middle = (uvs[a] + uvs[b] + uvs[c]) / 3f;
                    Color color = sheet.GetPixelBilinear(middle.x, middle.y);
                    float apart = Mathf.Abs(color.r - headColor.r) + Mathf.Abs(color.g - headColor.g) + Mathf.Abs(color.b - headColor.b);
                    isHead = fromTip < 0.35f && apart < 0.08f;
                }
                else isHead = fromTip < 0.12f;
                List<int> into = isHead ? head : body;
                into.Add(a);
                into.Add(b);
                into.Add(c);
            }
        }
        if (readColors) Object.DestroyImmediate(sheet);

        MakeFolders(ArrowMeshFolder);
        Mesh split = SaveSplitMesh(source, body, head, ArrowMeshFolder + "/" + Path.GetFileNameWithoutExtension(modelPath) + " Split.asset");
        Vector3 pointsTo = Vector3.zero;
        pointsTo[axis] = tipHigh ? 1f : -1f;
        float scale = length > 5f ? 0.01f : 1f;   // a file in centimeters
        shape = new ArrowShape { mesh = split, tip = corners[tipCorner], pointsTo = pointsTo, length = length * scale, scale = scale };
        report.Add("arrow model " + Path.GetFileNameWithoutExtension(modelPath) + ": " + (length * scale).ToString("0.00") + " m long, head = " +
                   head.Count / 3 + " of " + faces.Length / 3 + " faces" +
                   (!readColors ? " (by shape: the color sheet couldn't be read)"
                    : usedPass > 0 ? " (by shape: no face near the tip had the head's color)" : " (by its color)"));
        return true;
    }

    // How far the corners between "from" and "to" along the length reach out from the arrow's middle line.
    static float Thickness(Vector3[] corners, int axis, Vector3 center, float from, float to)
    {
        float most = 0f;
        foreach (Vector3 c in corners)
        {
            if (c[axis] < from || c[axis] > to) continue;
            Vector3 off = c - center;
            off[axis] = 0f;
            most = Mathf.Max(most, off.magnitude);
        }
        return most;
    }

    // A material's picture read from its file (a readable copy, whatever the import settings), or null.
    static Texture2D ReadPicture(Material material)
    {
        Texture picture = material != null ? material.mainTexture : null;
        string path = picture != null ? AssetDatabase.GetAssetPath(picture) : null;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        Texture2D copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (copy.LoadImage(File.ReadAllBytes(path))) return copy;
        Object.DestroyImmediate(copy);
        return null;
    }

    // The arrow mesh with two parts (0 = shaft and feathers, 1 = head), saved at "path" (an existing file is refilled, so
    // links to it stay).
    static Mesh SaveSplitMesh(Mesh source, List<int> body, List<int> head, string path)
    {
        Mesh copy = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = copy == null;
        if (isNew) copy = new Mesh();
        else copy.Clear();

        copy.name = Path.GetFileNameWithoutExtension(path);
        copy.indexFormat = source.indexFormat;
        Vector3[] corners = source.vertices;
        copy.vertices = corners;
        Vector3[] normals = source.normals;
        if (normals.Length == corners.Length) copy.normals = normals;
        Vector2[] uvs = source.uv;
        if (uvs.Length == corners.Length) copy.uv = uvs;
        Vector4[] tangents = source.tangents;
        if (tangents.Length == corners.Length) copy.tangents = tangents;
        Color32[] colors = source.colors32;
        if (colors.Length == corners.Length) copy.colors32 = colors;
        copy.subMeshCount = 2;
        copy.SetTriangles(body, 0, false);
        copy.SetTriangles(head, 1, false);
        if (normals.Length != corners.Length) copy.RecalculateNormals();
        copy.RecalculateBounds();

        if (isNew) AssetDatabase.CreateAsset(copy, path);
        else EditorUtility.SetDirty(copy);
        return copy;
    }

    static List<ItemData> BuildArrows(ArrowShape shape, Material palette, List<string> report)
    {
        List<ItemData> made = new List<ItemData>();
        List<string> recipes = new List<string>();
        ItemData before = null;   // the kind before (its arrows make this one)
        foreach (ArrowKind kind in Kinds)
        {
            string id = kind.tier + "Arrow";
            ItemData item = FindOrMakeItem(ArrowItemFolder, id);
            Material head = HeadMaterial(kind);
            GameObject prefab = BuildArrowPrefab(id, shape, palette, head, item);

            SerializedObject so = new SerializedObject(item);
            so.FindProperty("displayName").stringValue = kind.tier + " Arrow";
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("pickup").enumValueIndex = (int)PickupType.Pickup;
            so.FindProperty("icon.mode").enumValueIndex = (int)IconMode.Capture;
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Ammo;
            so.FindProperty("ammo.damage").floatValue = kind.damage;
            so.FindProperty("ammo.head").objectReferenceValue = head;
            so.FindProperty("minedWith").enumValueIndex = (int)ToolTier.None;
            so.FindProperty("craftAmount").intValue = ArrowsPerCraft;
            so.FindProperty("craftedAt").enumValueIndex = (int)CraftStation.Crafting;
            string missing = before == null
                ? SetRecipe(so, ("Wood", FindItem("Wood"), 2), ("Stone", FindItem("Stone"), 2))
                : SetRecipe(so, (before.name, before, ArrowsPerCraft), (kind.tier, CostItem(kind.tier, report), 1));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);

            recipes.Add(kind.tier + " " + kind.damage + (missing == null ? "" : " (no recipe yet: no " + missing + " item)"));
            made.Add(item);
            before = item;
        }
        report.Add(made.Count + " arrows in " + ArrowItemFolder + ", damage " + string.Join(", ", recipes) +
                   " (4 per craft: 2 Wood + 2 Stone, then 4 of the kind before + 1 metal)");
        return made;
    }

    // An arrow's prefab, emptied and refilled IN PLACE if it exists:
    //   <Kind>Arrow (BoxCollider, ItemPickup)
    //     > Model     leaning like a tool icon, centered
    //         > Arrow the split mesh, tip up; part 0 the pack's color sheet, part 1 this kind's head
    static GameObject BuildArrowPrefab(string id, ArrowShape shape, Material palette, Material head, ItemData item)
    {
        string path = FindPrefabPath(ArrowPrefabFolder + "/" + id + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(id);
        try
        {
            if (existed) Empty(root, null);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            GameObject model = new GameObject(ModelName);
            model.transform.SetParent(root.transform, false);
            GameObject arrow = MakeArrowLook("Arrow", model.transform, shape, palette, head, Vector3.up);
            arrow.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);   // leaning, like a tool icon
            Bounds seen = MeshBounds.Of(model);
            model.transform.localPosition = -seen.center;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = seen.size;

            AddPickup(root, item);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // The arrow model under "parent", turned so it points along "pointTo" (in the parent's space), sized in meters.
    static GameObject MakeArrowLook(string name, Transform parent, ArrowShape shape, Material palette, Material head, Vector3 pointTo)
    {
        GameObject arrow = new GameObject(name);
        arrow.transform.SetParent(parent, false);
        arrow.AddComponent<MeshFilter>().sharedMesh = shape.mesh;
        arrow.AddComponent<MeshRenderer>().sharedMaterials = new[] { palette, head };
        arrow.transform.localRotation = Quaternion.FromToRotation(shape.pointsTo, pointTo);
        arrow.transform.localScale = Vector3.one * shape.scale;
        return arrow;
    }

    // This kind's head material in HeadFolder: made once (Standard, its color, metal and shine), then kept as it is.
    static Material HeadMaterial(ArrowKind kind)
    {
        MakeFolders(HeadFolder);
        string path = HeadFolder + "/" + kind.tier + " Arrow Head.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find("Standard")) { name = kind.tier + " Arrow Head", color = kind.head };
        material.SetFloat("_Metallic", kind.metallic);
        material.SetFloat("_Glossiness", kind.smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // The metal an arrow kind costs (the item named like it); a missing Gold or Diamond item is made first.
    static ItemData CostItem(string tier, List<string> report)
    {
        ItemData item = FindItem(tier);
        if (item != null) return item;
        if (tier == "Gold") return DesertBuilder.MakeGold(report);
        if (tier == "Diamond") return SnowBuilder.MakeDiamond(report);
        return null;
    }

    // ----- The flying arrow -----

    // Arrow Projectile, refilled IN PLACE (its ArrowProjectile and the numbers on it are kept):
    //   Arrow Projectile (ArrowProjectile: Look, Length)
    //     > Look      forward (z) = the way it points, the tip at its middle (ToolInHand copies it onto the string)
    //         > Arrow the split mesh (part 1 gets each shot's head)
    static void BuildProjectile(ArrowShape shape, Material palette, List<string> report)
    {
        string path = FindPrefabPath(ArrowPrefabFolder + "/" + ProjectileName + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(ProjectileName);
        try
        {
            if (existed) Empty(root, typeof(ArrowProjectile));
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            GameObject look = new GameObject("Look");
            look.transform.SetParent(root.transform, false);
            GameObject arrow = MakeArrowLook("Arrow", look.transform, shape, palette, HeadMaterial(Kinds[0]), Vector3.forward);
            arrow.transform.localPosition = -(arrow.transform.localRotation * (shape.tip * shape.scale));   // the tip on Look's middle

            ArrowProjectile flying = root.GetComponent<ArrowProjectile>();
            if (flying == null) flying = root.AddComponent<ArrowProjectile>();
            SerializedObject so = new SerializedObject(flying);
            so.FindProperty("look").objectReferenceValue = look;
            so.FindProperty("length").floatValue = shape.length;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            report.Add(path + (saved ? " made (the flying arrow)" : " couldn't be saved (see the Console)"));
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // ----- The draw ring -----

    // BowCanvas: the ring around the crosshair and, under it, the arrow and how many (BowHUD runs it):
    //   BowCanvas (Canvas, Sort Order 2, BowHUD)
    //     > Bow (CanvasGroup: it fades in while a bow is held)
    //         > RingBack, DrawRing (the ring, faint / filling round)
    //         > Arrows (a dark rounded box) > ArrowIcon, ArrowCount
    static GameObject BuildHud()
    {
        MakeFolders(HudFolder);
        GameObject root = NewCanvas(HudName, HudSortOrder);
        try
        {
            Object.DestroyImmediate(root.GetComponent<GraphicRaycaster>());   // nothing on it is clicked

            GameObject bow = NewUI("Bow", root.transform);
            Place(bow, Vector2.zero, new Vector2(300f, 300f));
            bow.AddComponent<CanvasGroup>().alpha = 0f;

            Sprite ringPicture = IconArt.DrawRing();
            Image back = Picture("RingBack", bow.transform, ringPicture, new Color(1f, 1f, 1f, 0.22f), Vector2.zero, new Vector2(60f, 60f));
            back.type = Image.Type.Simple;
            Image ring = Picture("DrawRing", bow.transform, ringPicture, Color.white, Vector2.zero, new Vector2(60f, 60f));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;

            GameObject pill = NewUI("Arrows", bow.transform);
            Place(pill, new Vector2(0f, -62f), new Vector2(110f, 40f));
            Image box = pill.AddComponent<Image>();
            box.sprite = IconArt.RoundedBox();
            box.type = Image.Type.Sliced;
            box.color = new Color(0f, 0f, 0f, 0.45f);
            box.raycastTarget = false;
            HorizontalLayoutGroup row = pill.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 12, 4, 4);
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            ContentSizeFitter fit = pill.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image icon = Picture("ArrowIcon", pill.transform, null, Color.white, Vector2.zero, new Vector2(30f, 30f));
            icon.preserveAspect = true;
            LayoutElement iconSize = icon.gameObject.AddComponent<LayoutElement>();
            iconSize.preferredWidth = iconSize.preferredHeight = 30f;

            TextMeshProUGUI count = AddText(NewUI("ArrowCount", pill.transform), "32", 24f, TextAlignmentOptions.MidlineLeft);
            count.color = Color.white;
            count.fontStyle = FontStyles.Bold;
            count.enableWordWrapping = false;

            root.AddComponent<BowHUD>();
            return PrefabUtility.SaveAsPrefabAsset(root, HudFolder + "/" + HudName + ".prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static Image Picture(string name, Transform parent, Sprite sprite, Color color, Vector2 at, Vector2 size)
    {
        GameObject go = NewUI(name, parent);
        Place(go, at, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // In the middle of its parent, moved by "at", this big.
    static void Place(GameObject go, Vector2 at, Vector2 size)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = size;
    }

    // ----- The character -----

    // ToolInHand on the player's character prefab (the scene's Character and the first-person hands, a Prefab Variant of
    // it, both get it): it puts the bow in the left fist and the arrow on the string. Build Tier Tools adds it too.
    static string SetUpCharacter()
    {
        string path = FindPrefabPath("Assets/Prefabs/Player/PlayerCharacter.prefab");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            return "no PlayerCharacter.prefab yet: run Tools > Player > Add Character Model, then this again, to see the bow in the hands";
        }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            GameObject holder = animator != null ? animator.gameObject : root;
            if (holder.GetComponent<ToolInHand>() != null) return "the character holds the bow (ToolInHand)";
            holder.AddComponent<ToolInHand>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return "ToolInHand added to " + path + " (the bow in the left fist, the arrow on the string)";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ----- The game scenes -----

    // The game scenes: the ones in File > Build Settings with a player (PlayerController) in them.
    static List<string> GameScenes()
    {
        List<string> found = new List<string>();
        string[] scripts = AssetDatabase.FindAssets("PlayerController t:MonoScript");
        string guid = scripts.Length > 0 ? scripts[0] : null;
        if (string.IsNullOrEmpty(guid)) return found;
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;
            if (File.ReadAllText(entry.path).Contains(guid)) found.Add(entry.path);
        }
        return found;
    }

    // Opens a game scene: BowShooter on the Player with the flying arrow, the bow and arrows in the test kit, one
    // BowCanvas; saves it. Returns a report.
    static string SetUpScene(string path, ItemData bow, List<ItemData> arrows, GameObject hud)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player == null) return scene.name + ": no Player (PlayerInventory), skipped";

        BowShooter shooter = player.GetComponent<BowShooter>();
        if (shooter == null) shooter = player.gameObject.AddComponent<BowShooter>();
        ArrowProjectile projectile = FlyingArrow();   // loaded now, after the scene opened (see FlyingArrow)
        SerializedObject so = new SerializedObject(shooter);
        if (projectile != null) so.FindProperty("arrowPrefab").objectReferenceValue = projectile;
        so.ApplyModifiedPropertiesWithoutUndo();
        bool arrowSet = new SerializedObject(shooter).FindProperty("arrowPrefab").objectReferenceValue != null;   // what it really holds

        ToolTestKit kit = player.GetComponent<ToolTestKit>();
        if (kit == null) kit = player.gameObject.AddComponent<ToolTestKit>();
        SerializedObject kitSo = new SerializedObject(kit);
        AddToList(kitSo.FindProperty("tools"), bow);
        foreach (ItemData arrow in arrows) AddToList(kitSo.FindProperty("arrows"), arrow);
        kitSo.ApplyModifiedPropertiesWithoutUndo();

        bool kept = false;
        foreach (BowHUD found in Object.FindObjectsByType<BowHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(found.gameObject);
            bool ours = root != null && PrefabUtility.GetCorrespondingObjectFromSource(root) == hud;
            if (ours && !kept)
            {
                kept = true;
                continue;
            }
            Object.DestroyImmediate(root != null ? root : found.gameObject);   // a second one, or one made by hand
        }
        if (!kept && hud != null)
        {
            GameObject made = (GameObject)PrefabUtility.InstantiatePrefab(hud, scene);
            made.name = hud.name;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.name + ": BowShooter (Arrow Prefab " + (arrowSet ? "set" : "MISSING: drag " + ArrowPrefabFolder + "/" + ProjectileName +
                                                             " into it") + "), the bow and " +
               arrows.Count + " arrows in ToolTestKit, " + (kept ? "its BowCanvas updated" : "BowCanvas added") + " (saved)";
    }

    // The flying arrow's script, loaded from its prefab file right when a scene needs it. It isn't kept from when the
    // prefab was made: held from that early in the tool (before the animation files were reimported and the scenes were
    // opened), it came back empty, and the first version of this tool saved "Arrow Prefab: None" in both scenes
    // (22/11/2026). The bow item, the arrows and the ring (kept the same way) were fine; only this script on a prefab wasn't.
    static ArrowProjectile FlyingArrow()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(ArrowPrefabFolder + "/" + ProjectileName + ".prefab"));
        return prefab != null ? prefab.GetComponent<ArrowProjectile>() : null;
    }

    static void AddToList(SerializedProperty list, Object item)
    {
        if (list == null || item == null) return;
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return;
        }
        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
    }

    // ----- Small helpers -----

    static ItemData FindOrMakeItem(string folder, string id)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(folder + "/" + id + ".asset");
        if (item == null) item = FindItem(id);   // moved somewhere else
        if (item != null) return item;

        MakeFolders(folder);
        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, folder + "/" + id + ".asset");
        return item;
    }

    static ItemData FindItem(string id) => FindByName<ItemData>(id, "ItemData");

    // Writes a recipe into an Item Data (applied by the caller). Returns the name of a missing item (then the recipe is
    // left empty: not craftable yet), or null.
    static string SetRecipe(SerializedObject so, params (string name, ItemData item, int amount)[] parts)
    {
        SerializedProperty recipe = so.FindProperty("recipe");
        recipe.ClearArray();
        foreach ((string name, ItemData item, int amount) in parts)
        {
            if (item == null) return name;
        }
        foreach ((string name, ItemData item, int amount) in parts)
        {
            recipe.InsertArrayElementAtIndex(recipe.arraySize);
            SerializedProperty entry = recipe.GetArrayElementAtIndex(recipe.arraySize - 1);
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("amount").intValue = amount;
        }
        return null;
    }

    static void AddPickup(GameObject root, ItemData item)
    {
        ItemPickup pickup = root.AddComponent<ItemPickup>();
        SerializedObject so = new SerializedObject(pickup);
        so.FindProperty("data").objectReferenceValue = item;
        so.FindProperty("amount").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Removes everything from an opened prefab except its top object's Transform (and a "keep" script with its numbers).
    static void Empty(GameObject root, System.Type keep)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in root.GetComponents<MonoBehaviour>())
        {
            if (script != null && (keep == null || script.GetType() != keep)) Object.DestroyImmediate(script);
        }
        foreach (Component part in root.GetComponents<Component>())
        {
            if (part != null && !(part is Transform) && (keep == null || part.GetType() != keep)) Object.DestroyImmediate(part);
        }
    }

    // An asset by its exact file name (e.g. "Plank"), of this type ("ItemData", "Material"); null if there's none.
    static T FindByName<T>(string name, string type) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:" + type, new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != name) continue;
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
        }
        return null;
    }
}
