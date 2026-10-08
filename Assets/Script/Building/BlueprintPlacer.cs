/*
First Date: 5/10/2026
Date: 8/10/2026
Author: Tony
Use by: Hierarchy: select PlayerSphere > Add Component > BlueprintPlacer (only one, on the player).
Unity Editor: Nothing to connect: the build menu (B) starts it with BlueprintPlacer.Begin(buildingData).
              Tools > UI > Build Build Menu UI adds it to the player for you.
              With an InteractionMachine rule in the game, blueprints outside its area are red.
              Why a blueprint is red is written to the Console when you try to place it.
              The blueprint follows the crosshair by the MIDDLE of the building's shapes, wherever its root object (pivot) is.
              Recommended: in the toolbar (top left of the Scene view), click "Pivot" to switch it to "Center".
              Unity's Move / Rotate tools then appear in the middle of the selected shapes, the same point this script uses.
              This is only a view setting and changes nothing in the game.
Purpose: Moves a new blueprint with the crosshair until the player puts it down.
         Left click = place, right click = cancel, mouse wheel = turn to the next direction
         (only Directions fixed directions lined up with the world: 16 = every 22.5 degrees),
         middle click = auto placement on / off (keys set in KeyBindings).
         Auto placement on: the blueprint sits on the surface the crosshair points at.
         Auto placement off: it floats at a fixed distance in front of you (e.g. to place it against a wall).
         It turns red, and can't be placed, when it is outside an InteractionMachine's area,
         inside something, in the air, or the player doesn't have the recipe's materials.
         Placing it takes the materials from the inventory and builds the real building right away.
Requirement: BlueprintPlacer.cs x-> Blueprint.cs, BuildingData.cs, BoundsUtil.cs, ClickClaim.cs, InteractionGate.cs, KeyBindings.cs, MenuState.cs, PlayerInventory.cs
             BlueprintPlacer.cs <-x BuildMenu.cs, BuildMenuUIBuilder.cs, BuildModeHUD.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class BlueprintPlacer : MonoBehaviour
{
    // ===== Fields =====
    [Range(2f, 50f)]
    [Tooltip("How far from the camera a blueprint can be placed.")]
    [SerializeField] float placeRange = 12f;

    [Range(4, 32)]
    [Tooltip("How many directions a blueprint can face, lined up with the world (0 = +Z, 90 = +X...). " +
             "Only 4, 8, 16 or 32: other numbers jump to the nearest one. " +
             "4 = 90 degrees per wheel step, 8 = 45, 16 = 22.5, 32 = 11.25.")]
    [SerializeField] int directions = 32;

    [Tooltip("Start with auto placement on (sit on the surface under the crosshair).")]
    [SerializeField] bool autoPlace = true;

    static BlueprintPlacer instance;   // the one on the player

    Camera cam;
    Blueprint ghost;                   // the blueprint being placed (null = not placing)
    int startFrame;                    // the frame placing started (that click must not place it straight away)
    float yaw;                         // the blueprint's turn, in degrees (always one of the allowed directions)
    float freeDistance;                // auto placement off: how far in front of the camera it floats
    Vector3 pivotToBottom;             // from the blueprint's position (pivot) to the middle of its bottom, unturned
    Vector3 pivotToCenter;             // from the pivot to the middle of the whole building, unturned
    bool valid;                        // can it be placed where it is now?
    string problem;                    // why not (shown on screen when the player tries)

    // While placing, this script owns the mouse through ClickClaim, so other scripts using the same buttons
    // (levitate, swing, hotbar wheel) step aside without knowing about building.
    public static bool IsPlacing { get; private set; }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cam = Camera.main;
    }

    void OnDisable()
    {
        Cancel();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear them
        IsPlacing = false;
    }

    // Unity calls this when a value is changed in the Inspector: Directions only allows 4, 8, 16 or 32.
    void OnValidate()
    {
        // Nearest power of two: log2(16) = 4 -> 2^4 = 16, log2(20) = 4.3 -> 2^4 = 16, log2(24) = 4.6 -> 2^5 = 32.
        int power = Mathf.RoundToInt(Mathf.Log(Mathf.Max(directions, 1), 2f));
        directions = Mathf.Clamp(1 << power, 4, 32);
    }

    void Update()
    {
        if (!IsPlacing) return;
        if (ghost == null) { Stop(); return; }   // destroyed by something else

        // A menu is open (the cursor is free): wait.
        if (MenuState.AnyOpen || Cursor.lockState != CursorLockMode.Locked) return;
        if (Time.frameCount == startFrame) return;   // the click that chose the building

        if (CancelPressed()) { Cancel(); return; }
        if (AutoPressed()) ToggleAuto();

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) yaw = Snap(yaw + Step);          // next direction
        else if (scroll < 0f) yaw = Snap(yaw - Step);     // previous direction

        if (PlacePressed()) TryPlace();
    }

    // LateUpdate: the camera has moved this frame, so the blueprint follows the crosshair exactly.
    void LateUpdate()
    {
        if (!IsPlacing || ghost == null || cam == null) return;

        Move();
        valid = Check(out problem);
        ghost.SetValid(valid);
    }

    // ===== My methods =====

    // Starts placing a new blueprint of this building (e.g. called by a build menu).
    public static void Begin(BuildingData data)
    {
        if (instance == null)
        {
            Debug.LogWarning("BlueprintPlacer: add it to the PlayerSphere.");
            return;
        }
        instance.StartPlacing(data);
    }

    // Stops placing and throws the blueprint away (e.g. B pressed while placing).
    public static void CancelPlacing()
    {
        if (instance != null) instance.Cancel();
    }

    void StartPlacing(BuildingData data)
    {
        if (data == null || data.Prefab == null) return;
        Cancel();   // only one at a time

        ghost = Blueprint.Create(data);

        // Measure where the building really is compared to its pivot (the prefab's root can be off to one side
        // of the shapes). Measured once at position 0 with no turn; Move() turns these with the blueprint.
        ghost.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Bounds b = BoundsUtil.Of(ghost.gameObject);
        pivotToCenter = b.center;
        pivotToBottom = new Vector3(b.center.x, b.min.y, b.center.z);

        yaw = Snap(cam != null ? cam.transform.eulerAngles.y : 0f);   // start at the direction closest to where you look
        freeDistance = placeRange * 0.5f;
        startFrame = Time.frameCount;
        IsPlacing = true;
        ClickClaim.Take(this);   // the mouse buttons and wheel are ours until placing ends
    }

    void TryPlace()
    {
        if (!valid)
        {
            Debug.Log("Can't place here: " + problem);
            return;
        }

        // Pay, then swap the blueprint for the real building.
        // (Checked again here: the inventory may have changed since the blueprint last turned blue.)
        if (!ghost.Data.TakeFrom(PlayerInventory.Instance))
        {
            Debug.Log("Can't build: missing " + ghost.Data.Missing(PlayerInventory.Instance));
            return;
        }

        ghost.Build();
        ghost = null;
        Stop();
    }

    void Cancel()
    {
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        if (IsPlacing) Stop();
    }

    void Stop()
    {
        IsPlacing = false;
        ClickClaim.Release(this);   // the click that ended placing still counts as used this frame
    }

    void ToggleAuto()
    {
        autoPlace = !autoPlace;

        // Turning it off: keep floating at the distance it is now, so it doesn't jump.
        if (!autoPlace)
        {
            float d = Vector3.Distance(cam.transform.position, ghost.transform.position);
            freeDistance = Mathf.Clamp(d, 2f, placeRange);
        }

        Debug.Log(autoPlace ? "Auto placement: ON" : "Auto placement: OFF");
    }

    // Puts the blueprint where the crosshair points.
    void Move()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // from the screen center
        Quaternion rot = Quaternion.Euler(0f, Snap(yaw), 0f);   // Snap again: Directions may be changed while playing
        Vector3 pos;

        // Triggers are ignored: other blueprints' blue pieces don't count as ground.
        if (autoPlace && Physics.Raycast(ray, out RaycastHit hit, placeRange,
                                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // The middle of the bottom sits on the point the crosshair hits.
            pos = hit.point - rot * pivotToBottom;
        }
        else
        {
            // The middle of the building floats on the crosshair line.
            Vector3 point = ray.origin + ray.direction * (autoPlace ? placeRange : freeDistance);
            pos = point - rot * pivotToCenter;
        }

        ghost.transform.SetPositionAndRotation(pos, rot);
    }

    // Can it be placed here? If not, says why.
    bool Check(out string why)
    {
        why = null;

        if (!InteractionGate.CanInteractWith(BoundsUtil.Of(ghost.gameObject)))
            why = "No Interaction Machine in range";
        else if (ghost.Overlaps())
            why = "Something is in the way";
        else if (!ghost.IsSupported())
            why = "Can't build in the air";
        else if (!ghost.Data.CanAfford(PlayerInventory.Instance))
            why = "Missing " + ghost.Data.Missing(PlayerInventory.Instance);

        return why == null;
    }

    // ----- Turning -----

    // Degrees between two allowed directions (16 directions = 22.5).
    float Step => 360f / directions;

    // The allowed direction closest to this angle, 0 - 360. The directions start at 0 (= +Z),
    // so +Z, +X, -Z and -X are always among them.
    float Snap(float angle)
    {
        return Mathf.Repeat(Mathf.Round(angle / Step) * Step, 360f);   // Repeat keeps it between 0 and 360
    }

    // The keys below are chosen in KeyBindings.
    bool PlacePressed()
    {
        return Input.GetKeyDown(KeyBindings.PlaceBuilding);
    }

    bool CancelPressed()
    {
        return Input.GetKeyDown(KeyBindings.CancelBuilding);
    }

    bool AutoPressed()
    {
        return Input.GetKeyDown(KeyBindings.AutoPlace);
    }
}
