/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: A rock's top object: Turki's StoneNode, CopperNode and IronNode (and the StoneNode Desert look).
        Tools > World > Use Low Poly Rocks puts it there and fills in Shapes.
Unity Editor: Testing: Play: the rocks around you have different shapes, although they all come from the same prefab.
Purpose: Gives each rock one of the low-poly rock shapes at random when it appears (placed in the scene or spawned by
         ObjectScatter), so 140 rocks from one prefab don't all look alike. Its Mesh Filter shows that shape and its
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
    [Tooltip("The rock shapes to pick from, one at random for each rock (Assets/Prefabs/ResourceNodes/Rocks/Shapes, " +
             "made by Tools > World > Use Low Poly Rocks). Empty = it keeps the shape it has.")]
    [SerializeField] List<Mesh> shapes = new List<Mesh>();

    // ===== Unity messages (Unity calls these) =====

    // Awake, not Start: so other scripts on the rock (RockBreak) see the shape it ends up with when they start.
    void Awake()
    {
        if (shapes.Count == 0) return;
        Mesh shape = shapes[Random.Range(0, shapes.Count)];
        if (shape == null) return;

        GetComponent<MeshFilter>().sharedMesh = shape;
        MeshCollider body = GetComponent<MeshCollider>();
        if (body != null) body.sharedMesh = shape;   // the player bumps into and hits the shape it shows
    }
}
