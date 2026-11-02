/*
First Date: 31/10/2026
Date: 2/11/2026
Author: Tony
Use by: Nothing to set up. Blueprint.Build() adds it to every building it builds.
Unity Editor: Nothing to set. Select a built building while playing to see it (Data = which building it is).
Purpose: Marks a building that the player built, and remembers which Building Data it came from and its box
         (its size, measured once). Snap mode (BuildSnap) looks at these to lock a new blueprint next to them:
         e.g. a wall onto the edge of a built floor.
Requirement: BuiltPiece.cs x-> BuildingData.cs
             BuiltPiece.cs <-x Blueprint.cs, BlueprintPlacer.cs, BuildSnap.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BuiltPiece : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Which building this is. Filled in by Blueprint.Build().")]
    [SerializeField] BuildingData data;

    Bounds localBox;   // the box around its shapes, in its own frame (position + turn, no scale)

    static readonly List<BuiltPiece> all = new List<BuiltPiece>();   // every built piece in the scene

    public BuildingData Data => data;
    public PieceType Type => data != null ? data.Piece : PieceType.Other;
    public Bounds LocalBox => localBox;

    // Every built piece in the scene right now.
    public static IReadOnlyList<BuiltPiece> All => all;

    // Its frame: where it stands and which way it faces (only the turn around "up": buildings never tilt).
    public Vector3 Position => transform.position;
    public Quaternion Turn => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    public float Yaw => transform.eulerAngles.y;          // which way it faces, in degrees
    public Vector3 Center => ToWorld(localBox.center);    // the middle of its box, in the world

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    // Static lists outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        all.Clear();
    }

    // ===== My methods =====

    // Called by Blueprint.Build() right after building it.
    public void Setup(BuildingData buildingData)
    {
        data = buildingData;
        localBox = MeasureLocalBox(transform);
    }

    // A point in this piece's own frame -> the world.
    public Vector3 ToWorld(Vector3 local)
    {
        return Position + Turn * local;
    }

    // A point in the world -> this piece's own frame.
    public Vector3 ToLocal(Vector3 world)
    {
        return Quaternion.Inverse(Turn) * (world - Position);
    }

    // The box around every shape of "root", in root's own frame (its position and turn, but NOT its scale):
    // e.g. a 2 x 0.25 x 2 floor gives a box 2 x 0.25 x 2 wherever it stands and whichever way it faces.
    public static Bounds MeasureLocalBox(Transform root)
    {
        Quaternion back = Quaternion.Inverse(Quaternion.Euler(0f, root.eulerAngles.y, 0f));
        Vector3 origin = root.position;
        bool first = true;
        Bounds box = new Bounds(Vector3.zero, Vector3.one);

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            // The mesh's own box (tight), turned and scaled with its shape; else Unity's world box.
            MeshFilter mf = r.GetComponent<MeshFilter>();
            Bounds mb = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : r.bounds;
            Transform space = mf != null && mf.sharedMesh != null ? r.transform : null;

            for (int i = 0; i < 8; i++)   // the 8 corners of the box
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 world = space != null ? space.TransformPoint(corner) : corner;
                Vector3 local = back * (world - origin);

                if (first) { box = new Bounds(local, Vector3.zero); first = false; }
                else box.Encapsulate(local);
            }
        }
        return box;
    }
}
