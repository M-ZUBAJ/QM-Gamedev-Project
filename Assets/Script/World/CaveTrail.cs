/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Every cave prefab's top object (Rock Cave Small / Rock Cave / Rock Cave Big, the desert and snow caves in
        Assets/Prefabs/World/Caves). Tools > World > Build Rock Caves puts it there, and Tools > Enemies > Build Human
        Enemies adds it to caves made before it existed.
Unity Editor: Radius = the cave floor's radius (the tool sets it from the cave's design). Select a cave in the Scene
              view to see its trail: the door just inside the entrance, the spot in front of it and the two side spots
              (yellow), and the floor (cyan).
              Testing: Play in the new world and watch a human that walked into a cave to mine: it walks out through
              the entrance instead of pushing against the back wall.
Purpose: An invisible trail through a cave's entrance. A cave is a ring of wall rocks with one gap at its front (the
         cave's +Z), so a human enemy (HumanBrain) that only steers around what's right in front of it could get stuck
         inside, or push against the back wall from outside. This gives it waypoints:
           - the way out (AddWayOut): the door spot just inside the gap, the spot a few meters in front of the cave, and
             a spot at the cave's side when where it's going is behind the cave;
           - the way in (AddWayIn): the same the other way round.
         Static: CaveTrail.Around(point) = the cave whose floor holds that point (null = out in the open). Caves
         register while they're switched on (the new world's chunks switch theirs on and off).
Requirement: CaveTrail.cs <-x RockCaveBuilder.cs
             CaveTrail.cs <-o HumanBrain.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CaveTrail : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The cave floor's radius (meters): inside the ring of wall rocks. Build Rock Caves sets it.")]
    [Min(1f)]
    [SerializeField] float radius = 7.5f;

    const float DoorInside = 1.5f;     // the door spot: this far inside the entrance
    const float FrontOutside = 4.5f;   // the spot in front: this far outside (the rubble outside leaves the front free)
    const float SideOutside = 7f;      // the side spots: this far outside the floor's edge (past the walls and rubble)
    const float FloorMargin = 0.3f;    // a point this far past the floor's edge still counts as inside (the walls' inner face)

    static readonly List<CaveTrail> all = new List<CaveTrail>();   // every cave switched on now

    public float Radius => radius * transform.lossyScale.x;
    public Vector3 Door => transform.TransformPoint(new Vector3(0f, 0f, radius - DoorInside));
    public Vector3 Front => transform.TransformPoint(new Vector3(0f, 0f, radius + FrontOutside));

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        all.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(Door, 0.4f);
        Gizmos.DrawSphere(Front, 0.4f);
        Gizmos.DrawLine(Door, Front);
        Gizmos.DrawSphere(Side(1f), 0.4f);
        Gizmos.DrawSphere(Side(-1f), 0.4f);
    }

    // ===== My methods =====

    // The cave whose floor holds this point (null = out in the open).
    public static CaveTrail Around(Vector3 point)
    {
        foreach (CaveTrail cave in all)
        {
            if (cave != null && cave.Contains(point)) return cave;
        }
        return null;
    }

    // The point is on this cave's floor (inside the ring of walls).
    public bool Contains(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        float r = Radius + FloorMargin;
        return d.sqrMagnitude <= r * r;
    }

    // Adds the way out toward "toward": the door (unless it's already past it), the spot in front, and a side spot
    // when "toward" is behind the cave (so it walks round the walls, not into them).
    public void AddWayOut(List<Vector3> trail, Vector3 toward)
    {
        trail.Add(Door);
        trail.Add(Front);
        if (Behind(toward)) trail.Add(Side(SideOf(toward)));
    }

    // Adds the way in from "from": a side spot when "from" is behind the cave, the spot in front (unless it's already
    // in the entrance), and the door.
    public void AddWayIn(List<Vector3> trail, Vector3 from)
    {
        if (Behind(from)) trail.Add(Side(SideOf(from)));
        if (Flat(from - Door) > Flat(Front - Door)) trail.Add(Front);
        trail.Add(Door);
    }

    // Not in front of the entrance: getting there means going round the walls.
    bool Behind(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point);
        return !(local.z > radius * 0.3f && Mathf.Abs(local.x) < local.z);
    }

    float SideOf(Vector3 point) => transform.InverseTransformPoint(point).x < 0f ? -1f : 1f;

    // A spot beside the cave (side = 1 right, -1 left), a little toward its front.
    Vector3 Side(float side) => transform.TransformPoint(new Vector3(side * (radius + SideOutside), 0f, radius * 0.3f));

    static float Flat(Vector3 v)
    {
        v.y = 0f;
        return v.magnitude;
    }
}
