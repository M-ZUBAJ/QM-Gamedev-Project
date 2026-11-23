/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Building > Build Traps (needs Assets/Imported3D/CGTraderBearTrap/beartrap.fbx). Run it
              again any time: the traps are rebuilt in the same files and no scene changes. Then press B while
              playing: the 4 Bear Traps are in the build menu's Traps tab. Each trap's numbers and recipe come from
              the Tiers table below and are written every run: change them there.
              Guide: Docs/Trap Setup.md
Purpose: Makes the 4 Bear Traps the player builds with B, in one click (23/11/2026):
             Copper Bear Trap    4 Copper                       1 use    damage 10   stun 2 s
             Iron Bear Trap      Copper Bear Trap + 3 Iron      1 use    damage 20   stun 3 s
             Gold Bear Trap      Iron Bear Trap + 4 Gold        2 uses   damage 25   stun 3 s
             Diamond Bear Trap   Gold Bear Trap + 4 Diamond     2 uses   damage 30   stun 4 s
         Iron, Gold and Diamond are upgrades (Building Data Upgrade Of = the trap before it): each is built ON a placed
         trap of the tier before it (Iron on Copper, Gold on Iron, Diamond on Gold), which it replaces
         (BlueprintPlacer), so its recipe is only the extra metal. Each trap hurts the player who steps on it by its
         damage too.
           - the model is read from the FBX (its bones in their resting pose), centered with its bottom on the ground
             and sized to Trap Width (0.7 m), and cut into 3 meshes all four share, saved in
             Assets/Prefabs/Building/Traps/Meshes as Bear Trap Base / Jaw Front / Jaw Back: the Base (the middle plate
             and the bar across it) and the Front and Back Jaw (the two halves of the ring of teeth around it: faces
             whose corners all lie out on the ring, split by which side of the long middle line they're on). The FBX's
             own bones can't close it: the 4 jaw bones sit at the ring's edge, not on the hinge, and its 2
             "animations" are single still frames;
           - per trap a material Assets/Materials/Traps/<Tier> Bear Trap.mat in its metal's color (only made if
             missing, so your color changes stay);
           - per trap a prefab Assets/Prefabs/Building/Traps/<Tier> Bear Trap.prefab (rebuilt in place): its top object
             on the Ignore Raycast layer with a trigger Box Collider (the catching box), BearTrap (its numbers, and the
             jaws filled in: each jaw's pivot is on the hinge, the trap's long middle line at the ring's bottom, and
             the way each turns to close is worked out) and StandOnGround (it lies flat on the ground, on a slope
             too); Model > Base, Jaw Front, Jaw Back;
           - per trap the Building Data Assets/Config/Building/<Tier> Bear Trap.asset (Piece Type Trap = the Traps tab,
             a photo of the trap as its picture, its description for the info box, its recipe and Upgrade Of = the
             Building Data of the trap it's built on), so it
             shows in the B menu by itself; then the build menu's list gets the 4 in this order (Copper first).
         The first trap was the Iron Trap (and before that the Bear Trap): its files are renamed to the Iron Bear Trap
         and its meshes to Bear Trap ..., keeping their links (the build menu's list, the prefab's meshes).
Requirement: TrapBuilder.cs x-> BearTrap.cs, BuildingData.cs, BuildMenu.cs, ItemData.cs, StandOnGround.cs, DesertBuilder.cs, SnowBuilder.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIBuildHelper;   // MakeFolders, FindPrefabPath

public static class TrapBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Traps";
    const string Kind = "Bear Trap";   // each trap is "<Tier> Bear Trap"
    const string ModelPath = "Assets/Imported3D/CGTraderBearTrap/beartrap.fbx";
    const string PrefabFolder = "Assets/Prefabs/Building/Traps";
    const string MeshFolder = PrefabFolder + "/Meshes";
    const string MaterialFolder = "Assets/Materials/Traps";
    const string DataFolder = "Assets/Config/Building";
    const string BuildMenuPath = "Assets/Prefabs/UI/Building/BuildMenu/BuildMenuCanvas.prefab";

    // Its old names: the first trap's files are renamed to the Iron Bear Trap (links kept).
    static readonly string[] OldNames = { "Iron Trap", "Bear Trap" };
    const string OldMeshName = "Iron Trap";   // "Iron Trap Base.asset" -> "Bear Trap Base.asset"

    // The info box's words (BuildingData Description). The numbers come from BearTrap itself.
    const string Catching = "It lies on the ground and snaps shut on a monster that walks over it, hurting it and " +
                            "holding it still. Careful: it hurts you too if you step on it.";
    // Earlier texts of this tool: replaced by the new ones (a description you wrote yourself stays).
    static readonly string[] OldDescriptions =
    {
        "Snaps shut on a monster that steps on it: hurts it and holds it still for a few seconds.",
        "One time use. It lies on the ground and snaps shut on a monster that walks over it, hurting it and holding " +
        "it still. Careful: it hurts you too if you step on it.",
        Catching + " Build an Iron, Gold or Diamond Bear Trap on it to make it stronger.",   // when all 3 went on copper
    };

    const float TrapWidth = 0.7f;        // meters along its long side (a real bear trap is about 0.5 m; a little bigger is easier to see and step on)
    const float RingShare = 0.88f;       // a face is part of the jaws when all its corners lie this far out (1 = the ring's outline)
    const float CatchHeight = 0.5f;      // meters: how tall the catching box is
    const int IgnoreRaycastLayer = 2;    // the crosshair, attacks and E look through the trap's box

    // One trap of the table.
    class Tier
    {
        public readonly string name;                     // "Copper": the trap is "Copper Bear Trap"
        public readonly Color color;                     // its material (only when the material is made)
        public readonly float metallic;
        public readonly float smoothness;
        public readonly string builtOn;                  // an upgrade: the tier it's built on (and replaces); null = built anywhere
        public readonly (string id, int amount)[] cost;  // its recipe (an upgrade: only the extra)
        public readonly int uses;                        // snaps before it's used up (BearTrap Uses)
        public readonly float damage;                    // to a monster, and to the player who steps on it
        public readonly float stun;                      // seconds the monster stands still (BearTrap Hold Seconds)

        public Tier(string name, Color color, float metallic, float smoothness, string builtOn,
                    (string id, int amount)[] cost, int uses, float damage, float stun)
        {
            this.name = name;
            this.color = color;
            this.metallic = metallic;
            this.smoothness = smoothness;
            this.builtOn = builtOn;
            this.cost = cost;
            this.uses = uses;
            this.damage = damage;
            this.stun = stun;
        }

        public string Title => name + " " + Kind;
        public bool IsUpgrade => !string.IsNullOrEmpty(builtOn);
    }

    // The traps, each upgrade after the tier it's built on (Built On). Tony's numbers (23/11/2026).
    static readonly Tier[] Tiers =
    {
        new Tier("Copper", new Color(0.85f, 0.48f, 0.28f), 0.75f, 0.55f, null, new[] { ("Copper", 4) }, 1, 10f, 2f),
        new Tier("Iron", new Color(0.36f, 0.35f, 0.37f), 0.75f, 0.4f, "Copper", new[] { ("Iron", 3) }, 1, 20f, 3f),
        new Tier("Gold", new Color(1f, 0.78f, 0.22f), 0.85f, 0.7f, "Iron", new[] { ("Gold", 4) }, 2, 25f, 3f),
        new Tier("Diamond", new Color(0.55f, 0.95f, 1f), 0.3f, 0.9f, "Gold", new[] { ("Diamond", 4) }, 2, 30f, 4f),
    };

    // One of the 3 pieces the model is cut into.
    class Part
    {
        public string name;
        public readonly List<Vector3> positions = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector4> tangents = new List<Vector4>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public List<int>[] faces;
        readonly Dictionary<int, int> map = new Dictionary<int, int>();

        public Part(string partName, int subMeshes)
        {
            name = partName;
            faces = new List<int>[subMeshes];
            for (int i = 0; i < subMeshes; i++) faces[i] = new List<int>();
        }

        public int FaceCount
        {
            get
            {
                int n = 0;
                foreach (List<int> f in faces) n += f.Count / 3;
                return n;
            }
        }

        // Adds one corner of the source (once), returns its number in this part.
        public int Corner(int source, Model model)
        {
            if (map.TryGetValue(source, out int index)) return index;
            index = positions.Count;
            map[source] = index;
            positions.Add(model.positions[source]);
            normals.Add(source < model.normals.Length ? model.normals[source] : Vector3.up);
            if (model.tangents.Length == model.positions.Length) tangents.Add(model.tangents[source]);
            if (model.uvs.Length == model.positions.Length) uvs.Add(model.uvs[source]);
            return index;
        }
    }

    // The model read from the FBX, in the trap's space (centered, bottom at 0, sized).
    class Model
    {
        public Vector3[] positions;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector2[] uvs;
        public int[][] faces;          // per sub-mesh
        public bool longIsX;           // the long side runs along X (else along Z)
        public float halfLong;
        public float halfShort;
    }

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Building/Build Traps", false, MenuOrder.BuildTraps)]
    static void FromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        GameObject fbx = FindModel();
        if (fbx == null)
        {
            EditorUtility.DisplayDialog(ToolName, "The bear trap model isn't in the project: put beartrap.fbx in " +
                                        "Assets/Imported3D/CGTraderBearTrap (see Docs/Assets We Use.md for its link).", "OK");
            return;
        }

        List<string> table = new List<string>();
        foreach (Tier tier in Tiers) table.Add(tier.Title + ": " + RecipeText(tier) + ", " + Numbers(tier));
        if (!EditorUtility.DisplayDialog(ToolName,
                "Build the " + Tiers.Length + " bear traps from " + AssetDatabase.GetAssetPath(fbx) + "?\n\n" +
                string.Join("\n", table) + "\n\n" +
                "Each upgrade is built on a placed trap of the tier before it (Iron on Copper, Gold on Iron, Diamond on " +
                "Gold), which it replaces. They show in the B menu's Traps tab by themselves. Their numbers and recipes are written every run (change them in " +
                "TrapBuilder.cs's Tiers table); other trap settings and the colors stay.",
                "Build", "Cancel")) return;

        string report = Build(fbx);
        Debug.Log(ToolName + ": " + report);
        EditorUtility.DisplayDialog(ToolName, report + "\n\nTest: Play, press B, open the Traps tab, build a " +
                                    Tiers[0].Title + ", then pick an Iron Bear Trap and aim at it.", "OK");
    }

    // ===== My methods =====

    // Builds everything from the model. Returns what it did.
    public static string Build(GameObject fbx)
    {
        List<string> report = new List<string>();
        RenameOldFiles(report);
        Model model = Read(fbx, out string problem);
        if (model == null) return "nothing done: " + problem;

        Part[] parts = Cut(model, out float hingeY);
        if (parts[1].FaceCount == 0 || parts[2].FaceCount == 0)
            return "nothing done: no ring of teeth found to make the jaws from (is it the bear trap model?)";

        MakeFolders(MeshFolder);
        MakeFolders(MaterialFolder);
        MakeFolders(DataFolder);
        Mesh[] meshes = new Mesh[parts.Length];
        for (int i = 0; i < parts.Length; i++) meshes[i] = SaveMesh(parts[i], MeshFolder + "/" + Kind + " " + parts[i].name + ".asset");
        report.Add("meshes made (" + TrapWidth.ToString("0.00") + " m wide; base " + parts[0].FaceCount + " faces, jaws " +
                   parts[1].FaceCount + " + " + parts[2].FaceCount + ")");

        Vector3 axis = model.longIsX ? Vector3.right : Vector3.forward;
        float frontTurn = TurnToClose(parts[1], axis);
        float backTurn = TurnToClose(parts[2], axis);

        Dictionary<string, BuildingData> byTier = new Dictionary<string, BuildingData>();   // the traps made so far, by tier name
        List<BuildingData> made = new List<BuildingData>();
        foreach (Tier tier in Tiers)
        {
            BuildingData builtOn = null;   // an upgrade: the Building Data of the trap it's built on
            if (tier.IsUpgrade && !byTier.TryGetValue(tier.builtOn, out builtOn))
            {
                report.Add(tier.Title + " NOT made: the " + TitleOf(tier.builtOn) + " it's built on wasn't made");
                continue;
            }
            Material look = MaterialFor(tier);
            GameObject prefab = SavePrefab(tier, model, meshes, look, hingeY, axis, frontTurn, backTurn);
            if (prefab == null)
            {
                report.Add(tier.Title + " NOT made: its prefab couldn't be saved (the Console says why)");
                continue;
            }
            BuildingData data = SaveBuildingData(tier, prefab, builtOn, report);
            byTier[tier.name] = data;
            made.Add(data);
        }
        AssetDatabase.SaveAssets();
        SortBuildMenu(made, report);
        return string.Join("\n", report);
    }

    // The first trap's files (the Iron Trap, before that the Bear Trap) renamed to the Iron Bear Trap, and its meshes
    // to the shared Bear Trap ... names. Unity keeps their links (the build menu's list, the prefab's meshes). Only
    // while the new name isn't taken.
    static void RenameOldFiles(List<string> report)
    {
        List<(string from, string to)> renames = new List<(string, string)>();
        foreach (string old in OldNames)
        {
            renames.Add((PrefabFolder + "/" + old + ".prefab", PrefabFolder + "/Iron " + Kind + ".prefab"));
            renames.Add((DataFolder + "/" + old + ".asset", DataFolder + "/Iron " + Kind + ".asset"));
            renames.Add((MaterialFolder + "/" + old + " Metal.mat", MaterialFolder + "/Iron " + Kind + ".mat"));
        }
        foreach (string part in new[] { "Base", "Jaw Front", "Jaw Back" })
            renames.Add((MeshFolder + "/" + OldMeshName + " " + part + ".asset", MeshFolder + "/" + Kind + " " + part + ".asset"));

        int count = 0;
        foreach ((string from, string to) in renames)
        {
            if (AssetDatabase.LoadMainAssetAtPath(from) == null || AssetDatabase.LoadMainAssetAtPath(to) != null) continue;
            string error = AssetDatabase.RenameAsset(from, Path.GetFileNameWithoutExtension(to));
            if (string.IsNullOrEmpty(error)) count++;
            else Debug.LogWarning(ToolName + ": couldn't rename " + from + ": " + error);
        }
        if (count > 0) report.Add(count + " files of the old Iron Trap renamed (links kept)");
    }

    // The bear trap FBX: at its usual place, else any model called beartrap.
    static GameObject FindModel()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model != null) return model;
        foreach (string guid in AssetDatabase.FindAssets("beartrap t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == "beartrap") return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // ----- Reading and cutting the model -----

    // The model's mesh as it rests (its bones in their resting pose), in the trap's space: centered, bottom at 0,
    // Trap Width along its long side. Null (and why) if it has no mesh.
    static Model Read(GameObject fbx, out string problem)
    {
        problem = null;
        GameObject copy = Object.Instantiate(fbx);
        copy.hideFlags = HideFlags.HideAndDontSave;
        copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Mesh baked = null;   // a copy to read from, destroyed at the end
        try
        {
            Mesh source;
            Matrix4x4 toTrap;
            SkinnedMeshRenderer skin = copy.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin != null && skin.sharedMesh != null)
            {
                baked = new Mesh();
                skin.BakeMesh(baked, true);   // true: the renderer's scale isn't baked in (localToWorldMatrix adds it once)
                source = skin.sharedMesh;
                toTrap = copy.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
            }
            else
            {
                MeshFilter shape = copy.GetComponentInChildren<MeshFilter>(true);
                if (shape == null || shape.sharedMesh == null)
                {
                    problem = "the model has no mesh";
                    return null;
                }
                source = shape.sharedMesh;
                baked = Object.Instantiate(source);
                toTrap = copy.transform.worldToLocalMatrix * shape.transform.localToWorldMatrix;
            }

            Vector3[] corners = baked.vertices;
            if (corners.Length == 0)
            {
                problem = "the model's mesh can't be read";
                return null;
            }
            Vector3[] normals = baked.normals;
            Vector4[] tangents = baked.tangents;
            Vector2[] uvs = baked.uv.Length == corners.Length ? baked.uv : source.uv;
            for (int i = 0; i < corners.Length; i++) corners[i] = toTrap.MultiplyPoint3x4(corners[i]);
            for (int i = 0; i < normals.Length; i++) normals[i] = toTrap.MultiplyVector(normals[i]).normalized;
            for (int i = 0; i < tangents.Length; i++)
            {
                Vector3 t = toTrap.MultiplyVector(tangents[i]).normalized;
                tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
            }

            // Centered, bottom on the ground, sized.
            Bounds box = new Bounds(corners[0], Vector3.zero);
            foreach (Vector3 c in corners) box.Encapsulate(c);
            bool longIsX = box.size.x >= box.size.z;
            float scale = TrapWidth / Mathf.Max(longIsX ? box.size.x : box.size.z, 0.0001f);
            Vector3 foot = new Vector3(box.center.x, box.min.y, box.center.z);
            for (int i = 0; i < corners.Length; i++) corners[i] = (corners[i] - foot) * scale;

            Model model = new Model
            {
                positions = corners,
                normals = normals,
                tangents = tangents,
                uvs = uvs,
                faces = new int[source.subMeshCount][],
                longIsX = longIsX,
                halfLong = (longIsX ? box.size.x : box.size.z) * scale * 0.5f,
                halfShort = (longIsX ? box.size.z : box.size.x) * scale * 0.5f,
            };
            for (int s = 0; s < source.subMeshCount; s++) model.faces[s] = source.GetTriangles(s);
            return model;
        }
        finally
        {
            Object.DestroyImmediate(copy);
            if (baked != null) Object.DestroyImmediate(baked);
        }
    }

    // Cuts the model into the Base (0), the Front Jaw (1, on the -side of the long middle line) and the Back Jaw (2):
    // a face whose corners all lie out on the ring of teeth belongs to the jaw on its side. hingeY = the ring's bottom
    // (where the jaws turn).
    static Part[] Cut(Model model, out float hingeY)
    {
        int subs = model.faces.Length;
        Part[] parts = { new Part("Base", subs), new Part("Jaw Front", subs), new Part("Jaw Back", subs) };
        List<int>[] chosen = { new List<int>(), new List<int>(), new List<int>() };   // source corners per part (to find the hinge first)
        hingeY = float.MaxValue;

        for (int s = 0; s < subs; s++)
        {
            int[] faces = model.faces[s];
            for (int f = 0; f + 2 < faces.Length; f += 3)
            {
                int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                int part = 0;
                if (OnRing(model, a) && OnRing(model, b) && OnRing(model, c))
                {
                    float side = Across(model, a) + Across(model, b) + Across(model, c);
                    part = side < 0f ? 1 : 2;
                    hingeY = Mathf.Min(hingeY, model.positions[a].y, model.positions[b].y, model.positions[c].y);
                }
                chosen[part].Add(s);
                chosen[part].Add(a);
                chosen[part].Add(b);
                chosen[part].Add(c);
            }
        }
        if (hingeY == float.MaxValue) hingeY = 0f;

        for (int p = 0; p < parts.Length; p++)
        {
            List<int> list = chosen[p];   // 4 numbers per face: its sub-mesh, then its 3 corners
            for (int i = 0; i + 3 < list.Count; i += 4)
            {
                int s = list[i];
                parts[p].faces[s].Add(parts[p].Corner(list[i + 1], model));
                parts[p].faces[s].Add(parts[p].Corner(list[i + 2], model));
                parts[p].faces[s].Add(parts[p].Corner(list[i + 3], model));
            }
        }

        // The jaws' corners are kept from their hinge point (each jaw turns around its own origin).
        Vector3 hinge = new Vector3(0f, hingeY, 0f);
        for (int p = 1; p < parts.Length; p++)
        {
            for (int i = 0; i < parts[p].positions.Count; i++) parts[p].positions[i] -= hinge;
        }
        return parts;
    }

    // Out on the ring of teeth: its distance from the middle, measured on the trap's oval (1 = the outline).
    static bool OnRing(Model model, int corner)
    {
        Vector3 p = model.positions[corner];
        float along = (model.longIsX ? p.x : p.z) / Mathf.Max(model.halfLong, 0.0001f);
        float across = (model.longIsX ? p.z : p.x) / Mathf.Max(model.halfShort, 0.0001f);
        return along * along + across * across >= RingShare * RingShare;
    }

    // Which side of the long middle line (the hinge) a corner is on: below 0 = front.
    static float Across(Model model, int corner)
    {
        Vector3 p = model.positions[corner];
        return model.longIsX ? p.z : p.x;
    }

    // +1 or -1: the way a jaw turns around the hinge to stand up (its middle goes up, not into the ground).
    static float TurnToClose(Part jaw, Vector3 axis)
    {
        Vector3 middle = Vector3.zero;
        foreach (Vector3 p in jaw.positions) middle += p;
        middle /= Mathf.Max(jaw.positions.Count, 1);
        return (Quaternion.AngleAxis(90f, axis) * middle).y >= middle.y ? 1f : -1f;
    }

    // ----- Saving -----

    // A part's mesh, saved as an asset (an existing file is refilled, so the prefabs' links to it stay).
    static Mesh SaveMesh(Part part, string path)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh();
        else mesh.Clear();
        mesh.name = Path.GetFileNameWithoutExtension(path);
        mesh.SetVertices(part.positions);
        mesh.SetNormals(part.normals);
        if (part.tangents.Count == part.positions.Count) mesh.SetTangents(part.tangents);
        if (part.uvs.Count == part.positions.Count) mesh.SetUVs(0, part.uvs);
        mesh.subMeshCount = part.faces.Length;
        for (int s = 0; s < part.faces.Length; s++) mesh.SetTriangles(part.faces[s], s, false);
        mesh.RecalculateBounds();
        if (isNew) AssetDatabase.CreateAsset(mesh, path);
        else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    // The trap's metal: <Tier> Bear Trap.mat, made in its color only if missing (so a color you changed stays).
    static Material MaterialFor(Tier tier)
    {
        string path = MaterialFolder + "/" + tier.Title + ".mat";
        Material look = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (look != null) return look;
        look = new Material(Shader.Find("Standard")) { name = tier.Title, color = tier.color };
        look.SetFloat("_Metallic", tier.metallic);
        look.SetFloat("_Glossiness", tier.smoothness);
        AssetDatabase.CreateAsset(look, path);
        return look;
    }

    // The trap's prefab, rebuilt in place (its file and top object stay, so the Building Data's link does).
    static GameObject SavePrefab(Tier tier, Model model, Mesh[] meshes, Material look, float hingeY, Vector3 axis,
                                 float frontTurn, float backTurn)
    {
        string path = PrefabFolder + "/" + tier.Title + ".prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        Scene preview = default;
        GameObject root;
        if (exists) root = PrefabUtility.LoadPrefabContents(path);
        else
        {
            preview = EditorSceneManager.NewPreviewScene();
            root = new GameObject(tier.Title);
            SceneManager.MoveGameObjectToScene(root, preview);
        }
        try
        {
            root.name = tier.Title;
            root.layer = IgnoreRaycastLayer;
            Transform oldModel = root.transform.Find("Model");
            if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

            // The catching box: a trigger as wide as the trap, half a meter tall.
            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 size = model.longIsX ? new Vector3(model.halfLong * 2f, CatchHeight, model.halfShort * 2f)
                                         : new Vector3(model.halfShort * 2f, CatchHeight, model.halfLong * 2f);
            box.size = size;
            box.center = new Vector3(0f, CatchHeight * 0.5f, 0f);

            Transform shape = NewChild("Model", root.transform);
            Material[] mats = new Material[Mathf.Max(1, meshes[0].subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = look;
            NewPiece("Base", shape, meshes[0], mats, Vector3.zero);
            Transform front = NewPiece("Jaw Front", shape, meshes[1], mats, new Vector3(0f, hingeY, 0f));
            Transform back = NewPiece("Jaw Back", shape, meshes[2], mats, new Vector3(0f, hingeY, 0f));

            BearTrap trap = root.GetComponent<BearTrap>();
            if (trap == null) trap = root.AddComponent<BearTrap>();
            SerializedObject so = new SerializedObject(trap);   // the names in quotes must match BearTrap's fields
            so.FindProperty("damage").floatValue = tier.damage;
            so.FindProperty("holdSeconds").floatValue = tier.stun;
            so.FindProperty("uses").intValue = tier.uses;
            so.FindProperty("playerDamage").floatValue = tier.damage;
            so.FindProperty("frontJaw").objectReferenceValue = front;
            so.FindProperty("backJaw").objectReferenceValue = back;
            so.FindProperty("hingeAxis").vector3Value = axis;
            so.FindProperty("frontTurn").floatValue = frontTurn;
            so.FindProperty("backTurn").floatValue = backTurn;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (root.GetComponent<StandOnGround>() == null)   // set only the first time, so your changes stay
            {
                SerializedObject feet = new SerializedObject(root.AddComponent<StandOnGround>());   // names must match StandOnGround's fields
                feet.FindProperty("footOffset").floatValue = 0f;   // the pivot is the bottom of the trap
                feet.FindProperty("sink").floatValue = 0.005f;
                feet.FindProperty("maxTilt").floatValue = 25f;
                feet.ApplyModifiedPropertiesWithoutUndo();
            }

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (exists) PrefabUtility.UnloadPrefabContents(root);
            else EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    static Transform NewPiece(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 at)
    {
        Transform piece = NewChild(name, parent);
        piece.localPosition = at;
        piece.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        piece.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
        return piece;
    }

    static Transform NewChild(string name, Transform parent)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    // The trap's Building Data (made the first time; picture = a photo of the trap): its name, prefab, Piece Type Trap,
    // recipe and Upgrade Of every time; its description when it's empty or one of this tool's.
    static BuildingData SaveBuildingData(Tier tier, GameObject prefab, BuildingData builtOn, List<string> report)
    {
        string path = DataFolder + "/" + tier.Title + ".asset";
        BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
        bool isNew = data == null;
        if (isNew)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, path);
        }
        SerializedObject so = new SerializedObject(data);   // the names in quotes must match BuildingData's fields
        so.FindProperty("displayName").stringValue = tier.Title;
        string words = so.FindProperty("description").stringValue;
        if (string.IsNullOrEmpty(words) || IsOurText(words)) so.FindProperty("description").stringValue = DescriptionFor(tier);
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("pieceType").enumValueIndex = (int)PieceType.Trap;
        so.FindProperty("upgradeOf").objectReferenceValue = builtOn;

        List<string> missing = new List<string>();
        SerializedProperty list = so.FindProperty("recipe");
        list.ClearArray();
        foreach ((string id, int amount) in tier.cost)
        {
            ItemData item = CostItem(id, report);
            if (item == null)
            {
                missing.Add(id);
                continue;
            }
            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty line = list.GetArrayElementAtIndex(list.arraySize - 1);
            line.FindPropertyRelative("item").objectReferenceValue = item;
            line.FindPropertyRelative("amount").intValue = amount;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);

        report.Add(tier.Title + (isNew ? " made: " : " updated: ") + RecipeText(tier) + ", " + Numbers(tier) +
                   (missing.Count > 0 ? " (ITEM NOT FOUND: " + string.Join(", ", missing) + ")" : ""));
        return data;
    }

    // What it says in the info box (its numbers are added from BearTrap): a trap that can be upgraded also says with
    // what ("Build an Iron Bear Trap on it..."); an upgrade's "Built on a Copper Bear Trap" line comes from its
    // Upgrade Of.
    static string DescriptionFor(Tier tier)
    {
        List<string> stronger = new List<string>();
        foreach (Tier other in Tiers) if (other.builtOn == tier.name) stronger.Add(other.name);
        if (stronger.Count == 0) return Catching;
        string names = stronger.Count == 1 ? stronger[0]
                     : string.Join(", ", stronger.GetRange(0, stronger.Count - 1)) + " or " + stronger[stronger.Count - 1];
        string a = "AEIOU".IndexOf(char.ToUpperInvariant(names[0])) >= 0 ? "an " : "a ";
        return Catching + " Build " + a + names + " " + Kind + " on it to make it stronger.";
    }

    // A description this tool wrote (now or before): it may be replaced with the new one.
    static bool IsOurText(string words)
    {
        if (System.Array.IndexOf(OldDescriptions, words) >= 0) return true;
        foreach (Tier tier in Tiers) if (words == DescriptionFor(tier)) return true;
        return false;
    }

    // The build menu's list: the traps together in the table's order (Copper first), where the first of them was
    // (else at the end). The other buildings keep their places. Saved only when it changed.
    static void SortBuildMenu(List<BuildingData> traps, List<string> report)
    {
        string path = FindPrefabPath(BuildMenuPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null || traps.Count == 0) return;   // no build menu yet: it lists them by itself
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            BuildMenu menu = root.GetComponent<BuildMenu>();
            if (menu == null) return;
            SerializedObject so = new SerializedObject(menu);
            SerializedProperty list = so.FindProperty("buildings");   // must match the field name in BuildMenu
            if (list == null) return;

            List<Object> before = new List<Object>();
            for (int i = 0; i < list.arraySize; i++) before.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);

            List<Object> after = new List<Object>();
            int at = -1;
            foreach (Object entry in before)
            {
                if (entry is BuildingData data && traps.Contains(data))
                {
                    if (at < 0) at = after.Count;   // the traps go where the first of them was
                    continue;
                }
                after.Add(entry);
            }
            if (at < 0) at = after.Count;
            after.InsertRange(at, traps);

            bool same = after.Count == before.Count;
            for (int i = 0; same && i < after.Count; i++) same = after[i] == before[i];
            if (same) return;

            list.arraySize = after.Count;
            for (int i = 0; i < after.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = after[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            report.Add("build menu: the traps listed " + string.Join(", ", traps.ConvertAll(t => t.DisplayName)));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // A recipe's item by its Item Id. Gold and Diamond are made when missing (copies of Turki's Iron, as Build Desert /
    // Snow Content make them), so the recipes don't wait for those tools.
    static ItemData CostItem(string id, List<string> report)
    {
        ItemData item = FindItem(id);
        if (item != null) return item;
        if (id == "Gold") return DesertBuilder.MakeGold(report);
        if (id == "Diamond") return SnowBuilder.MakeDiamond(report);
        return null;
    }

    // The Item Data with this Item Id (its file name), from any folder.
    static ItemData FindItem(string id)
    {
        foreach (string guid in AssetDatabase.FindAssets(id + " t:ItemData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == id) return AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }
        return null;
    }

    // "Iron Bear Trap + 4 Gold" / "4 Copper".
    static string RecipeText(Tier tier)
    {
        List<string> words = new List<string>();
        if (tier.IsUpgrade) words.Add(TitleOf(tier.builtOn));
        foreach ((string id, int amount) in tier.cost) words.Add(amount + " " + id);
        return string.Join(" + ", words);
    }

    // A tier's trap name: "Iron" -> "Iron Bear Trap".
    static string TitleOf(string tierName) => tierName + " " + Kind;

    // "damage 20, stun 3 s, 1 use".
    static string Numbers(Tier tier)
    {
        CultureInfo n = CultureInfo.InvariantCulture;
        return "damage " + tier.damage.ToString("0.#", n) + ", stun " + tier.stun.ToString("0.#", n) + " s, " +
               tier.uses + (tier.uses == 1 ? " use" : " uses");
    }
}
