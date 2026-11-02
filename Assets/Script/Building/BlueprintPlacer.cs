/*
First Date: 5/10/2026
Date: 2/11/2026
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
         Left click = place, right click = cancel, middle click = switch between Snap and Surface
         (keys set in KeyBindings), mouse wheel = turn it.
         Snap (the start mode): near a built piece, the blueprint locks into the spot where it fits against it
         (BuildSnap: walls on floors, floors and roofs on top of walls, furniture against walls...) and turns
         green. Nowhere to snap: it sits on the surface, as in Surface mode.
         The wheel turns it 90 degrees and it fits again in its new direction. Until the wheel is used,
         Snap picks the direction that fits best (a wall follows the floor edge); after that, the player's is kept.
         Surface: the blueprint sits on whatever surface the crosshair points at.
         The wheel turns it to the next of Directions fixed directions lined up with the world (16 = every 22.5 degrees).
         It turns red, and can't be placed, when it is outside an InteractionMachine's area,
         inside something, in the air, or the player doesn't have the recipe's materials.
         Placing it takes the materials from the inventory and builds the real building right away.
Requirement: BlueprintPlacer.cs x-> Blueprint.cs, BuildingData.cs, BuildSnap.cs, BuiltPiece.cs, BoundsUtil.cs, ClickClaim.cs, InteractionGate.cs, KeyBindings.cs, MenuState.cs, PlayerInventory.cs
             BlueprintPlacer.cs <-x BuildMenu.cs, BuildMenuUIBuilder.cs, BuildModeHUD.cs, PlayerStats.cs
*/

using UnityEngine;

// How a blueprint is placed. Middle click switches between them.
public enum PlaceMode
{
    Snap,      // locks onto built pieces (floors, walls, roofs, furniture); on the surface where there's nothing to snap to
    Surface,   // sits on whatever surface the crosshair points at
}

[DisallowMultipleComponent]
public class BlueprintPlacer : MonoBehaviour
{
    // ===== Fields =====
    [Range(2f, 50f)]
    [Tooltip("How far from the camera a blueprint can be placed.")]
    [SerializeField] float placeRange = 12f;

    [Range(4, 32)]
    [Tooltip("Surface mode: how many directions a blueprint can face, lined up with the world (0 = +Z, 90 = +X...). " +
             "Only 4, 8, 16 or 32: other numbers jump to the nearest one. " +
             "4 = 90 degrees per wheel step, 8 = 45, 16 = 22.5, 32 = 11.25. Snap mode always turns 90 degrees.")]
    [SerializeField] int directions = 32;

    [Tooltip("The mode placing starts in. Middle click switches between them while placing. " +
             "Snap = lock onto built floors / walls / roofs. Surface = sit on what the crosshair points at.")]
    [SerializeField] PlaceMode startMode = PlaceMode.Snap;

    [Range(0.2f, 5f)]
    [Tooltip("Snap mode: how close (meters) the crosshair must point to a snap spot for the blueprint to lock onto it. " +
             "Bigger = locks from further away.")]
    [SerializeField] float snapDistance = 1.5f;

    static BlueprintPlacer instance;   // the one on the player

    Camera cam;
    Blueprint ghost;                   // the blueprint being placed (null = not placing)
    int startFrame;                    // the frame placing started (that click must not place it straight away)
    float yaw;                         // the way the player turned the blueprint, in degrees
    Vector3 pivotToBottom;             // from the blueprint's position (pivot) to the middle of its bottom, unturned
    Vector3 pivotToCenter;             // from the pivot to the middle of the whole building, unturned
    Bounds ghostBox;                   // the blueprint's box in its own frame (for snapping)
    PlaceMode mode;                    // Snap or Surface right now
    bool snapped;                      // locked onto a built piece right now (green)
    bool turnedByPlayer;               // the wheel was used: Snap keeps the player's direction instead of choosing one
    bool valid;                        // can it be placed where it is now?
    string problem;                    // why not (shown on screen when the player tries)

    // While placing, this script owns the mouse through ClickClaim, so other scripts using the same buttons
    // (levitate, swing, hotbar wheel) step aside without knowing about building.
    public static bool IsPlacing { get; private set; }

    // The placing mode right now and whether the blueprint is locked onto a piece (e.g. the build mode hint shows it).
    public static PlaceMode Mode => instance != null ? instance.mode : PlaceMode.Snap;
    public static bool IsSnapped => instance != null && instance.snapped;

    // Fired each time a building is really built (paid for and placed), with its Building Data. E.g. PlayerStats counts them.
    public event System.Action<BuildingData> Built;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        instance = this;
        mode = startMode;
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
        if (ModePressed()) ToggleMode();

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) Turn(1);          // next direction
        else if (scroll < 0f) Turn(-1);    // previous direction

        if (PlacePressed()) TryPlace();
    }

    // LateUpdate: the camera has moved this frame, so the blueprint follows the crosshair exactly.
    void LateUpdate()
    {
        if (!IsPlacing || ghost == null || cam == null) return;

        Move();
        valid = Check(out problem);
        ghost.SetValid(valid, snapped);
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
        ghostBox = BuiltPiece.MeasureLocalBox(ghost.transform);

        yaw = Snap(cam != null ? cam.transform.eulerAngles.y : 0f);   // start at the direction closest to where you look
        snapped = false;
        turnedByPlayer = false;   // Snap chooses the best fitting direction until the wheel is used
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

        BuildingData built = ghost.Data;
        ghost.Build();
        ghost = null;
        Stop();
        Built?.Invoke(built);
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

    void ToggleMode()
    {
        mode = mode == PlaceMode.Snap ? PlaceMode.Surface : PlaceMode.Snap;
        Debug.Log("Placing mode: " + mode);
    }

    // Mouse wheel: turns the blueprint one step. Snap mode: 90 degrees, from the way it faces now, so a snapped
    // piece turns where it is and fits again in its new direction. Surface mode: to the next of the Directions.
    void Turn(int steps)
    {
        turnedByPlayer = true;   // from now on Snap keeps the player's direction instead of choosing one

        if (mode == PlaceMode.Snap)
        {
            float from = snapped ? ghost.transform.eulerAngles.y : yaw;   // snapped: lined up with the piece it's on
            yaw = Mathf.Repeat(from + steps * 90f, 360f);
        }
        else
        {
            yaw = Snap(yaw + steps * Step);
        }
    }

    // Puts the blueprint where the crosshair points: locked onto a built piece (Snap), or on the surface.
    void Move()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // from the screen center
        // Triggers are ignored: other blueprints' blue pieces don't count as ground.
        bool hits = Physics.Raycast(ray, out RaycastHit hit, placeRange,
                                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        // Snap mode: the closest spot where it fits against a built piece (BuildSnap), if the aim is near one.
        snapped = mode == PlaceMode.Snap &&
                  BuildSnap.Find(ghost.Data.Piece, ghostBox, ray, placeRange, hits, hit, snapDistance,
                                 yaw, turnedByPlayer, out pos, out rot);

        if (!snapped)
        {
            rot = Quaternion.Euler(0f, Snap(yaw), 0f);   // Snap again: Directions may be changed while playing

            if (hits)
            {
                // The middle of the bottom sits on the point the crosshair hits.
                pos = hit.point - rot * pivotToBottom;
            }
            else
            {
                // Nothing in range: the middle of the building floats at the end of the crosshair line (shown red).
                pos = ray.origin + ray.direction * placeRange - rot * pivotToCenter;
            }
        }

        ghost.transform.SetPositionAndRotation(pos, rot);
    }

    // Can it be placed here? If not, says why.
    bool Check(out string why)
    {
        why = null;

        if (!InteractionGate.CanInteractWith(BoundsUtil.Of(ghost.gameObject)))
            why = "No Interaction Machine in range";
        else if (ghost.Overlaps(snapped))   // snapped: touching the built pieces it locks onto is fine
            why = "Something is in the way";
        else if (!snapped && !ghost.IsSupported())   // snapped: held up by the piece it locks onto
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

    bool ModePressed()
    {
        return Input.GetKeyDown(KeyBindings.AutoPlace);   // middle click by default
    }
}
