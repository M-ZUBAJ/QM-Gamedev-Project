/*
First Date: 10/11/2026
Date: 10/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly. The tools that size and place a model use it
              (Tools > Player > Add Character Model, Tools > Enemies > Build Monster Enemy).
Purpose: Measures the box around the model you actually SEE, from its real corners (vertices), in the world.
         Unity's own Renderer.bounds can be much bigger than an animated (skinned) model: that box is kept around
         one of its bones, and when that bone is turned the box grows. The RPG Monster Duo slime's box was 1.67 m
         tall and reached 0.37 m below its feet, so a tool using it made the slime small and floating.
         A tool calls MeshBounds.Of(model).
Requirement: MeshBounds.cs <-x EnemyBuilder.cs, FirstPersonHandsBuilder.cs, PlayerCharacterBuilder.cs, TierToolBuilder.cs, WildAnimalBuilder.cs
*/

using UnityEngine;

public static class MeshBounds
{
    // ===== My methods =====

    // The box around every switched-on mesh of the object, in the world. An animated (skinned) mesh is measured in
    // the pose it has right now. Nothing to measure = a box of size 0 at the object.
    public static Bounds Of(GameObject go)
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        bool found = false;
        Mesh posed = new Mesh();   // a scratch copy for animated meshes

        try
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())   // switched-off parts don't count
            {
                Mesh mesh = null;
                if (r is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    skinned.BakeMesh(posed, false);   // the mesh as its bones hold it now, in the renderer's own space
                    mesh = posed;
                }
                else if (r is MeshRenderer && r.TryGetComponent(out MeshFilter filter))
                {
                    mesh = filter.sharedMesh;
                }
                if (mesh == null) continue;

                Matrix4x4 toWorld = r.transform.localToWorldMatrix;
                foreach (Vector3 corner in mesh.vertices)
                {
                    Vector3 p = toWorld.MultiplyPoint3x4(corner);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    found = true;
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(posed);
        }

        if (!found) return new Bounds(go.transform.position, Vector3.zero);
        return new Bounds((min + max) * 0.5f, max - min);
    }
}
