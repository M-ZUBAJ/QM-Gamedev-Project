/*
First Date: 4/10/2026
Date: 11/10/2026
Author: Tony
Use by: Nothing to set up. Other scripts call BoundsUtil.Of(someObject) or BoundsUtil.NearestPoint(collider, point).
Unity Editor: Nothing to set up (code only, no component).
Purpose: Of: the box around everything you can see of an object (all its renderers, children too).
         Its center is the real middle of the meshes, which for objects made of several shapes
         is often NOT the object's position (pivot).
         NearestPoint: the point on a collider closest to a point, for ANY collider (see the method).
Requirement: BoundsUtil.cs <-x BlueprintPlacer.cs, Crosshair.cs, ItemGrabber.cs, ObjectLevitator.cs
*/

using UnityEngine;

public static class BoundsUtil
{
    // ===== My methods =====

    // A box around all the object's renderers (or a 1 x 1 x 1 box at its position if it has none).
    public static Bounds Of(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            bounds.Encapsulate(rends[i].bounds);
        }
        return bounds;
    }

    // The point on the collider closest to "from" (used to measure "how far is that object?").
    // Unity's Collider.ClosestPoint only works on Box, Sphere, Capsule and Convex Mesh colliders,
    // and logs an error for the others (a Mesh Collider without Convex ticked, Terrain).
    // For those, the box around the collider is close enough.
    public static Vector3 NearestPoint(Collider col, Vector3 from)
    {
        bool exact = col is BoxCollider || col is SphereCollider || col is CapsuleCollider
                     || (col is MeshCollider mesh && mesh.convex);
        return exact ? col.ClosestPoint(from) : col.bounds.ClosestPoint(from);
    }
}
