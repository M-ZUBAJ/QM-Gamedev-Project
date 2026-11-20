/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly: the rock tools (Use Low Poly Rocks, Build Rock Break) and Build Plate Armor use it.
Purpose: Small helpers for tools that save meshes of their own (the first two moved here from RockBreakBuilder on
         20/11/2026, when the armor tool needed them too):
         - MeshIn: the mesh inside a model file (e.g. one of the pack's .dae rocks).
         - SaveCopy: a copy of a mesh with a new pivot and size, saved as an asset (an existing file is refilled, so
           whatever links to it keeps working).
         - SaveSkinnedCopy: a copy of an animated character's mesh (bones and all) with only the faces you choose, saved
           the same way (Build Plate Armor's skin without the faces under the gloves).
Requirement: MeshAssetHelper.cs <-x ArmorBuilder.cs, RockBreakBuilder.cs, RockBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using Unity.Collections;   // NativeArray (the bone weights)
using UnityEditor;
using UnityEngine;

public static class MeshAssetHelper
{
    // ===== My methods =====

    // The mesh inside a model file (e.g. one of the pack's .dae rocks), or null.
    public static Mesh MeshIn(string modelPath)
    {
        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (part is Mesh mesh) return mesh;
        }
        return null;
    }

    // A copy of a mesh with "newPivot" moved to (0, 0, 0) and everything scaled by "scale", saved as an asset at "path"
    // (an existing file is refilled, so links to it stay). The mesh is read here in the Editor, where Unity allows it
    // even when the model's Read/Write is off; the copy itself stays readable (a Mesh Collider given it while playing,
    // like RockLook's, needs that). Null = the mesh couldn't be read.
    public static Mesh SaveCopy(Mesh source, Vector3 newPivot, float scale, string path)
    {
        Vector3[] corners = source.vertices;
        if (corners.Length == 0)
        {
            Debug.LogWarning("The corners of " + source.name + " can't be read, so it wasn't copied.");
            return null;
        }
        for (int i = 0; i < corners.Length; i++) corners[i] = (corners[i] - newPivot) * scale;

        Mesh copy = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = copy == null;
        if (isNew) copy = new Mesh();
        else copy.Clear();

        copy.name = Path.GetFileNameWithoutExtension(path);
        copy.indexFormat = source.indexFormat;
        copy.SetVertices(corners);
        Vector3[] normals = source.normals;   // one size for all directions: the normals stay the same
        if (normals.Length == corners.Length) copy.SetNormals(normals);
        Vector2[] uvs = source.uv;            // the color sheet's colors
        if (uvs.Length == corners.Length) copy.SetUVs(0, uvs);
        Color32[] colors = source.colors32;
        if (colors.Length == corners.Length) copy.SetColors(colors);
        Vector4[] tangents = source.tangents;
        if (tangents.Length == corners.Length) copy.SetTangents(tangents);
        copy.subMeshCount = source.subMeshCount;
        for (int s = 0; s < source.subMeshCount; s++) copy.SetTriangles(source.GetTriangles(s), s, false);
        if (normals.Length != corners.Length) copy.RecalculateNormals();
        copy.RecalculateBounds();

        if (isNew) AssetDatabase.CreateAsset(copy, path);
        else EditorUtility.SetDirty(copy);
        return copy;
    }

    // A copy of an animated (skinned) mesh with every corner, bone weight and pose as they are, but only the faces in
    // "faces" (one list per sub-mesh, 3 corner numbers per face), saved as an asset at "path" (an existing file is
    // refilled through the Mesh API, so links to it stay; EditorUtility.CopySerialized lost a mesh's corners once, see
    // FirstPersonArmsHelper). The copy stays readable. Null = the mesh couldn't be read.
    public static Mesh SaveSkinnedCopy(Mesh source, List<int>[] faces, string path)
    {
        Vector3[] corners = source.vertices;
        int count = corners.Length;
        if (count == 0)
        {
            Debug.LogWarning("The corners of " + source.name + " can't be read, so it wasn't copied.");
            return null;
        }

        Mesh copy = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = copy == null;
        if (isNew) copy = new Mesh();
        else
        {
            copy.Clear();
            copy.ClearBlendShapes();
        }

        copy.name = Path.GetFileNameWithoutExtension(path);
        copy.indexFormat = source.indexFormat;
        copy.vertices = corners;
        Vector3[] normals = source.normals;
        if (normals.Length == count) copy.normals = normals;
        Vector4[] tangents = source.tangents;
        if (tangents.Length == count) copy.tangents = tangents;
        Color32[] colors = source.colors32;
        if (colors.Length == count) copy.colors32 = colors;
        List<Vector4> uvs = new List<Vector4>();
        for (int channel = 0; channel < 8; channel++)
        {
            source.GetUVs(channel, uvs);
            if (uvs.Count == count) copy.SetUVs(channel, uvs);
        }

        // Which bones move each corner, and how much: all of them (boneWeights alone keeps only the first four).
        NativeArray<byte> bonesPerCorner = source.GetBonesPerVertex();
        if (bonesPerCorner.Length == count) copy.SetBoneWeights(bonesPerCorner, source.GetAllBoneWeights());
        else
        {
            BoneWeight[] weights = source.boneWeights;
            if (weights.Length == count) copy.boneWeights = weights;
        }
        copy.bindposes = source.bindposes;

        // Shape keys (e.g. a face's expressions), if it has any.
        Vector3[] moved = new Vector3[count], turned = new Vector3[count], bent = new Vector3[count];
        for (int shape = 0; shape < source.blendShapeCount; shape++)
        {
            for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
            {
                source.GetBlendShapeFrameVertices(shape, frame, moved, turned, bent);
                copy.AddBlendShapeFrame(source.GetBlendShapeName(shape), source.GetBlendShapeFrameWeight(shape, frame), moved, turned, bent);
            }
        }

        copy.subMeshCount = faces.Length;
        for (int sub = 0; sub < faces.Length; sub++) copy.SetTriangles(faces[sub], sub, false);
        copy.bounds = source.bounds;   // the same corners: the same box

        if (isNew) AssetDatabase.CreateAsset(copy, path);
        else EditorUtility.SetDirty(copy);
        return copy;
    }
}
