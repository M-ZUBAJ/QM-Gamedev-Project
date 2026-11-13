/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor helper (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: No menu of its own: Tools > Player > Add First Person Hands and Tools > Player > Set Up Player Animations
              use it. The new meshes are saved in Assets/Prefabs/Player/FirstPersonArms. It always starts from the
              original meshes, so running those tools again is safe. Your real body (and its shadow) is never changed.
Purpose: Two fixes for the first-person arms, done on copies of their meshes (Arms, Hands, Gloves):
         - Cut at the elbow: only faces that move with a forearm, hand or finger are kept, like Minecraft's arms. The
           open end at the shoulder (where the hidden chest would join) can't swing into view any more.
         - Both sides: every face also gets a back face turned the other way, so looking into a sleeve or the cut end
           shows its inside instead of a see-through hole. It's done in the mesh, not with a special shader, so the
           normal materials keep working (also in WebGL).
Requirement: FirstPersonArmsHelper.cs x-> FirstPersonHands.cs, UIBuildHelper.cs
             FirstPersonArmsHelper.cs <-x FirstPersonHandsBuilder.cs, PlayerAnimationSetup.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;   // IndexFormat
using static UIBuildHelper;    // MakeFolders

public static class FirstPersonArmsHelper
{
    // ===== Fields =====
    const string MeshFolder = "Assets/Prefabs/Player/FirstPersonArms";

    // A face is kept if one of its corners moves at least this much with a forearm, hand or finger (0.5 = half).
    // Lower = more of the upper arm stays; higher = the cut comes closer to the wrist.
    const float KeepWeight = 0.5f;

    // The Humanoid bones the kept part starts at: these and everything under them (hands, fingers).
    static readonly string[] KeptFrom = { "LeftLowerArm", "RightLowerArm" };

    // ===== My methods =====

    // Gives the first-person copy's shown meshes (FirstPersonHands' Shown Parts) their cut, two-sided copies.
    // root = the hands copy: an opened prefab, or the copy Add First Person Hands is building. Returns a short report.
    public static string Apply(GameObject root)
    {
        Animator animator = root.GetComponentInChildren<Animator>(true);
        List<Transform> keptFrom = KeptBones(animator, root.transform);
        if (keptFrom.Count == 0) return "no forearm bones found (the character needs a Humanoid avatar), so nothing was changed";

        string[] shown = ShownParts(root.GetComponentInChildren<FirstPersonHands>(true));
        List<string> done = new List<string>();
        foreach (SkinnedMeshRenderer part in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!FirstPersonHands.IsHandPart(part.name, shown)) continue;
            Mesh original = OriginalMesh(part);
            if (original == null) continue;

            Mesh baked = Bake(original, part.bones, keptFrom, out int kept, out int total);
            if (baked == null)
            {
                done.Add(part.name + " (its mesh couldn't be read, left as it was)");
                continue;
            }
            Mesh saved = SaveMesh(baked, original.name);
            part.sharedMesh = saved;
            if (saved.vertexCount == 0)
            {
                Debug.LogError(part.name + "'s first-person mesh was saved EMPTY, so it won't show. Delete the files in " +
                               MeshFolder + " and run Tools > Player > Add First Person Hands again.");
            }
            done.Add(part.name + " (" + kept + " of " + total + " faces kept, both sides)");
        }
        return done.Count > 0 ? string.Join(", ", done) : "no arm or hand meshes found";
    }

    // FirstPersonHands' Shown Parts words (Arm, Hand, Glove by default).
    static string[] ShownParts(FirstPersonHands hands)
    {
        if (hands == null) return FirstPersonHands.DefaultParts;
        SerializedProperty list = new SerializedObject(hands).FindProperty("shownParts");
        if (list == null || list.arraySize == 0) return FirstPersonHands.DefaultParts;
        string[] words = new string[list.arraySize];
        for (int i = 0; i < words.Length; i++) words[i] = list.GetArrayElementAtIndex(i).stringValue;
        return words;
    }

    // The forearm bones, found by their Humanoid names in the avatar (works on an opened prefab too).
    static List<Transform> KeptBones(Animator animator, Transform root)
    {
        List<Transform> bones = new List<Transform>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return bones;
        foreach (HumanBone bone in animator.avatar.humanDescription.human)
        {
            if (System.Array.IndexOf(KeptFrom, bone.humanName) < 0) continue;
            Transform found = FindDeep(root, bone.boneName);
            if (found != null) bones.Add(found);
        }
        return bones;
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

    // The character's own mesh for this part: if it's already one of our copies, the one from the prefab it comes from.
    static Mesh OriginalMesh(SkinnedMeshRenderer part)
    {
        SkinnedMeshRenderer from = part;
        while (from != null && from.sharedMesh != null && AssetDatabase.GetAssetPath(from.sharedMesh).StartsWith(MeshFolder))
        {
            from = PrefabUtility.GetCorrespondingObjectFromSource(from);
        }
        return from != null ? from.sharedMesh : null;
    }

    // The cut, two-sided copy. Every corner is there twice: as it is (the front) and with its normal turned around (the
    // back). Each kept face is drawn with the front corners and again, turned around, with the back ones.
    static Mesh Bake(Mesh source, Transform[] bones, List<Transform> keptFrom, out int kept, out int total)
    {
        kept = 0;
        total = 0;
        Vector3[] corners = source.vertices;
        BoneWeight[] weights = source.boneWeights;
        int count = corners.Length;
        if (count == 0 || weights.Length != count) return null;

        // Which bones belong to the kept part (a forearm or anything under it).
        bool[] keepBone = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;
            foreach (Transform from in keptFrom) keepBone[i] |= bones[i] == from || bones[i].IsChildOf(from);
        }
        float KeptShare(int bone, float weight) => bone >= 0 && bone < keepBone.Length && keepBone[bone] ? weight : 0f;

        // How much each corner moves with the kept part (0 = upper arm / shoulder only, 1 = forearm / hand only).
        float[] share = new float[count];
        for (int i = 0; i < count; i++)
        {
            BoneWeight w = weights[i];
            share[i] = KeptShare(w.boneIndex0, w.weight0) + KeptShare(w.boneIndex1, w.weight1) +
                       KeptShare(w.boneIndex2, w.weight2) + KeptShare(w.boneIndex3, w.weight3);
        }

        Mesh mesh = new Mesh { name = source.name + " FP" };
        mesh.indexFormat = count * 2 > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = Twice(corners);
        mesh.boneWeights = Twice(weights);
        mesh.bindposes = source.bindposes;

        Vector3[] normals = source.normals;
        if (normals.Length == count)
        {
            Vector3[] both = Twice(normals);
            for (int i = count; i < both.Length; i++) both[i] = -both[i];   // the back faces face the other way
            mesh.normals = both;
        }
        Vector4[] tangents = source.tangents;
        if (tangents.Length == count)
        {
            Vector4[] both = Twice(tangents);
            for (int i = count; i < both.Length; i++) both[i].w = -both[i].w;
            mesh.tangents = both;
        }
        Color[] colors = source.colors;
        if (colors.Length == count) mesh.colors = Twice(colors);
        for (int channel = 0; channel < 4; channel++)
        {
            List<Vector2> uvs = new List<Vector2>();
            source.GetUVs(channel, uvs);
            if (uvs.Count != count) continue;
            uvs.AddRange(uvs.ToArray());
            mesh.SetUVs(channel, uvs);
        }

        mesh.subMeshCount = source.subMeshCount;
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            int[] faces = source.GetTriangles(sub);
            List<int> keep = new List<int>();
            for (int i = 0; i + 2 < faces.Length; i += 3)
            {
                total++;
                int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                if (Mathf.Max(share[a], Mathf.Max(share[b], share[c])) < KeepWeight) continue;   // upper arm, shoulder: cut away
                kept++;
                keep.Add(a);
                keep.Add(b);
                keep.Add(c);
                keep.Add(c + count);   // the same face from behind: the back corners, turned around
                keep.Add(b + count);
                keep.Add(a + count);
            }
            mesh.SetTriangles(keep, sub);
        }
        mesh.RecalculateBounds();
        return mesh;
    }

    static T[] Twice<T>(T[] items)
    {
        T[] both = new T[items.Length * 2];
        items.CopyTo(both, 0);
        items.CopyTo(both, items.Length);
        return both;
    }

    // Saves the copy as "<original> FP.asset", inside the same file if it exists (so the prefab's link to it stays).
    // An existing file is refilled part by part through the Mesh API. (EditorUtility.CopySerialized was used before: it
    // kept the faces but lost every corner and bone weight, so the arms vanished after Set Up Player Animations.)
    static Mesh SaveMesh(Mesh baked, string originalName)
    {
        MakeFolders(MeshFolder);
        string path = MeshFolder + "/" + originalName + " FP.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(baked, path);
            return baked;
        }
        CopyMesh(baked, existing);
        Object.DestroyImmediate(baked);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    // Everything a skinned mesh needs, copied from one mesh into another.
    static void CopyMesh(Mesh from, Mesh to)
    {
        to.Clear();
        to.name = from.name;
        to.indexFormat = from.indexFormat;
        to.vertices = from.vertices;
        int count = from.vertexCount;
        Vector3[] normals = from.normals;
        if (normals.Length == count) to.normals = normals;
        Vector4[] tangents = from.tangents;
        if (tangents.Length == count) to.tangents = tangents;
        Color[] colors = from.colors;
        if (colors.Length == count) to.colors = colors;
        List<Vector4> uvs = new List<Vector4>();
        for (int channel = 0; channel < 8; channel++)
        {
            from.GetUVs(channel, uvs);
            if (uvs.Count == count) to.SetUVs(channel, uvs);
        }
        to.boneWeights = from.boneWeights;   // which bones move each corner: without them the arm can't follow the hand
        to.bindposes = from.bindposes;
        to.subMeshCount = from.subMeshCount;
        for (int sub = 0; sub < from.subMeshCount; sub++) to.SetTriangles(from.GetTriangles(sub), sub);
        to.bounds = from.bounds;
    }
}
