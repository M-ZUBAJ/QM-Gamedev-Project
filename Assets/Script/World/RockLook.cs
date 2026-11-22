/*
First Date: 20/11/2026
Date: 22/11/2026
Author: Tony
Use by: A rock's top object: Turki's StoneNode, CopperNode and IronNode (and the StoneNode Desert look).
        Tools > World > Use Low Poly Rocks puts it there and fills in Shapes.
Unity Editor: Testing: Play: the rocks around you have different shapes, although they all come from the same prefab.
Purpose: Gives each rock one of the low-poly rock shapes when it appears (placed in the scene or spawned by
         ObjectScatter), picked from where it stands, so 140 rocks from one prefab don't all look alike, and the same rock
         always gets the same shape (the new world makes a far chunk's rocks again when the player comes back; it was a
         random pick until 22/11/2026). Its Mesh Filter shows that shape and its
         Mesh Collider takes the same shape (what the player bumps into and hits). Its color (the material) stays the
         prefab's. The shapes are made by the tool: the pack's rocks, all about the same size, with the pivot near the
         bottom so the rock sits a little into the ground.
Requirement: RockLook.cs x-> MeshFilter (Unity)
             RockLook.cs o-> MeshCollider (Unity)
             RockLook.cs <-x RockBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
public class RockLook : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The rock shapes to pick from, one for each rock by where it stands (Assets/Prefabs/ResourceNodes/Rocks/Shapes, " +
             "made by Tools > World > Use Low Poly Rocks). Empty = it keeps the shape it has.")]
    [SerializeField] List<Mesh> shapes = new List<Mesh>();

    // ===== Unity messages (Unity calls these) =====

    // Awake, not Start: so other scripts on the rock (RockBreak) see the shape it ends up with when they start.
    void Awake()
    {
        if (shapes.Count == 0) return;
        Mesh shape = shapes[ShapeFor(transform.position, shapes.Count)];
        if (shape == null) return;

        GetComponent<MeshFilter>().sharedMesh = shape;
        MeshCollider body = GetComponent<MeshCollider>();
        if (body != null) body.sharedMesh = shape;   // the player bumps into and hits the shape it shows
    }

    // ===== My methods =====

    // A shape picked from where the rock stands (since 22/11/2026, was random): the endless world makes a far chunk's
    // rocks again when the player comes back, and each one should look the same as before. Rocks a few centimeters
    // apart still get different shapes.
    static int ShapeFor(Vector3 at, int count)
    {
        unchecked
        {
            int hash = Mathf.RoundToInt(at.x * 10f) * 73856093 ^ Mathf.RoundToInt(at.y * 10f) * 19349663 ^ Mathf.RoundToInt(at.z * 10f) * 83492791;
            return (hash & int.MaxValue) % count;
        }
    }
}
