/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open the scene (e.g. GameScene, the old world), then Tools > World > Add World Barrier. It asks first, and
              Ctrl+Z takes it back. Save the scene afterwards (Ctrl+S). Run it again to fit the barrier again (e.g. after
              the map's walls moved). Guide: Docs/World Barrier Setup.md
Purpose: Puts a glowing barrier (WorldBarrier) around the open scene's map, so players can see where the world ends:
           - where the map's own walls are: an object named "Boundaries" with box colliders on all four sides
             (Sanyam's, in GameScene) = on their inner faces. His walls are left as they are (three of them are
             triggers, which stop nothing; the barrier's own walls stop the player);
           - else just inside the edge of the terrain (the one with the most ground textures);
           - from below the lowest ground along that line to well above the highest.
         The new world (a scene with a World Generator) makes its own barrier every game, so it's skipped there.
         An existing barrier in the scene is fitted again instead of adding a second one.
Requirement: WorldBarrierBuilder.cs x-> WorldBarrier.cs, WorldGenerator.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WorldBarrierBuilder
{
    // ===== Fields =====
    const string ToolName = "Add World Barrier";
    const string BarrierName = "World Barrier";
    const string WallsName = "Boundaries";   // the map's own walls (Sanyam's, in GameScene), found by name
    const float Below = 10f;                 // the barrier starts this far under the lowest ground on its line
    const float Above = 60f;                 // and ends this far over the highest
    const float TerrainInset = 1f;           // without walls: this far inside the terrain's edge
    const float SampleStep = 2f;             // meters between two ground heights read along the line

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Add World Barrier", false, MenuOrder.AddWorldBarrier)]
    static void Add()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first.", "OK");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (Find<WorldGenerator>(scene) != null)
        {
            EditorUtility.DisplayDialog(ToolName, "\"" + scene.name + "\" has a World Generator (the new world): it makes " +
                                        "its own barrier around the world it makes, every game. Nothing to add here.", "OK");
            return;
        }
        if (!FindArea(scene, out Rect area, out string from))
        {
            EditorUtility.DisplayDialog(ToolName, "No map found in \"" + scene.name + "\": it needs a terrain, or walls " +
                                        "(box colliders) in an object named \"" + WallsName + "\".", "OK");
            return;
        }
        GroundAlong(scene, area, out float low, out float high);

        WorldBarrier existing = Find<WorldBarrier>(scene);
        string where = "x " + area.xMin.ToString("0") + " to " + area.xMax.ToString("0") + ", z " + area.yMin.ToString("0") +
                       " to " + area.yMax.ToString("0") + " (" + area.width.ToString("0") + " x " + area.height.ToString("0") + " m)";
        if (!EditorUtility.DisplayDialog(ToolName,
                (existing != null ? "Fits the scene's World Barrier again" : "Adds a World Barrier to \"" + scene.name + "\"") +
                ": a glowing force field that players see and can't walk through.\n\n" +
                "Where: " + from + ", " + where + ".\n" +
                "Height: from " + (low - Below).ToString("0") + " m to " + (high + Above).ToString("0") + " m.\n\n" +
                "Ctrl+Z takes it back. Save the scene afterwards.", existing != null ? "Fit Again" : "Add", "Cancel"))
            return;

        WorldBarrier barrier = existing;
        if (barrier == null)
        {
            GameObject go = new GameObject(BarrierName);
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, ToolName);
            barrier = go.AddComponent<WorldBarrier>();
        }
        else Undo.RecordObject(barrier.transform, ToolName);

        barrier.transform.SetPositionAndRotation(new Vector3(area.center.x, low - Below, area.center.y), Quaternion.identity);
        barrier.transform.localScale = Vector3.one;
        SerializedObject so = new SerializedObject(barrier);   // the names in quotes must match WorldBarrier's fields
        so.FindProperty("size").vector2Value = area.size;
        so.FindProperty("height").floatValue = high + Above - (low - Below);
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = barrier.gameObject;

        Debug.Log(ToolName + ": \"" + scene.name + "\" has a World Barrier along " + from + ", " + where + ". Save the scene " +
                  "(Ctrl+S), then Play and walk to the edge of the map.", barrier);
    }

    // ===== My methods =====

    // The inside of the map: between the inner faces of the walls in "Boundaries", or else the painted terrain's edge
    // (a meter in). "from" says which, for the dialog.
    static bool FindArea(Scene scene, out Rect area, out string from)
    {
        if (WallsArea(scene, out area))
        {
            from = "the inner faces of the map's walls (" + WallsName + ")";
            return true;
        }
        Terrain terrain = MainTerrain(scene);
        if (terrain != null)
        {
            Vector3 corner = terrain.transform.position, size = terrain.terrainData.size;
            area = Rect.MinMaxRect(corner.x + TerrainInset, corner.z + TerrainInset,
                                   corner.x + size.x - TerrainInset, corner.z + size.z - TerrainInset);
            from = "the edge of the terrain " + terrain.name;
            return true;
        }
        from = "";
        return false;
    }

    // The walls' inside: each wall's face that looks toward the middle of all of them. Needs a wall on every side.
    static bool WallsArea(Scene scene, out Rect area)
    {
        area = default;
        GameObject holder = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == WallsName) holder = root;
        }
        if (holder == null) return false;

        List<Bounds> walls = new List<Bounds>();
        foreach (BoxCollider box in holder.GetComponentsInChildren<BoxCollider>(true)) walls.Add(WorldBox(box));
        if (walls.Count < 4) return false;

        Bounds all = walls[0];
        foreach (Bounds wall in walls) all.Encapsulate(wall);
        Vector3 middle = all.center;

        float left = float.MinValue, right = float.MaxValue, back = float.MinValue, front = float.MaxValue;
        foreach (Bounds wall in walls)
        {
            bool alongZ = wall.size.x < wall.size.z;   // a west or east wall (long in z)
            if (alongZ && wall.center.x < middle.x) left = Mathf.Max(left, wall.max.x);
            else if (alongZ) right = Mathf.Min(right, wall.min.x);
            else if (wall.center.z < middle.z) back = Mathf.Max(back, wall.max.z);
            else front = Mathf.Min(front, wall.min.z);
        }
        bool everySide = left > float.MinValue && right < float.MaxValue && back > float.MinValue && front < float.MaxValue;
        if (!everySide || right <= left || front <= back) return false;
        area = Rect.MinMaxRect(left, back, right, front);
        return true;
    }

    // A box collider's box in world space (its 8 corners, so a turned or scaled wall works too).
    static Bounds WorldBox(BoxCollider box)
    {
        Transform t = box.transform;
        Bounds bounds = new Bounds(t.TransformPoint(box.center), Vector3.zero);
        Vector3 half = box.size / 2f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = box.center + new Vector3((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y,
                                                      (i & 4) == 0 ? -half.z : half.z);
            bounds.Encapsulate(t.TransformPoint(corner));
        }
        return bounds;
    }

    // The lowest and highest ground along the barrier's line (every terrain under it). No terrain there = 0 and 50 m.
    static void GroundAlong(Scene scene, Rect area, out float low, out float high)
    {
        List<Terrain> terrains = FindAll<Terrain>(scene);
        low = float.MaxValue;
        high = float.MinValue;
        Vector2[] corners = { new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin),
                              new Vector2(area.xMax, area.yMax), new Vector2(area.xMin, area.yMax) };
        for (int side = 0; side < 4; side++)
        {
            Vector2 a = corners[side], b = corners[(side + 1) % 4];
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / SampleStep));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 spot = Vector2.Lerp(a, b, i / (float)steps);
                foreach (Terrain terrain in terrains)
                {
                    if (terrain.terrainData == null || !Covers(terrain, spot)) continue;
                    float y = terrain.SampleHeight(new Vector3(spot.x, 0f, spot.y)) + terrain.transform.position.y;
                    low = Mathf.Min(low, y);
                    high = Mathf.Max(high, y);
                }
            }
        }
        if (low > high)
        {
            low = 0f;
            high = 50f;
        }
    }

    static bool Covers(Terrain terrain, Vector2 spot)
    {
        Vector3 corner = terrain.transform.position, size = terrain.terrainData.size;
        return spot.x >= corner.x && spot.x <= corner.x + size.x && spot.y >= corner.z && spot.y <= corner.z + size.z;
    }

    // The scene's main terrain: the one with the most ground textures (GameScene also has an unpainted top-level one).
    static Terrain MainTerrain(Scene scene)
    {
        Terrain best = null;
        int most = -1;
        foreach (Terrain terrain in FindAll<Terrain>(scene))
        {
            if (terrain.terrainData == null) continue;
            int count = 0;
            foreach (TerrainLayer layer in terrain.terrainData.terrainLayers) if (layer != null) count++;
            if (count <= most) continue;
            most = count;
            best = terrain;
        }
        return best;
    }

    // The first T in the scene (switched off ones too), or null.
    static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static List<T> FindAll<T>(Scene scene) where T : Component
    {
        List<T> all = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects()) all.AddRange(root.GetComponentsInChildren<T>(true));
        return all;
    }
}
