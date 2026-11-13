/*
First Date: 4/10/2026
Date: 12/11/2026
Author: Tony
Use by: Create an empty GameObject called "GameRules" in the scene and add this script.
        (If you forget to add this script, one is created by itself with the default rules.)
Unity Editor: Tick / untick the rules in the Inspector (also works while playing).
              Item Icon Style only shows while Overwrite Item Icon is ticked, and Notification Side only while
              Show Notifications is ticked (GameRulesEditor).
Purpose: On / off rules for the world, like Minecraft's /gamerule.
         Other scripts ask for a rule like this: if (GameRules.DoDaylightCycle) ...
         - DoDaylightCycle (default on): time of day moves (DaylightCycle). Off = time stops.
         - WorldInteraction (default off): the ball can grab / levitate anywhere,
           without being inside an InteractionMachine's radius.
         - KeepInventory (default off): off = dying drops every item where the player died (ItemDropper),
           on = the player keeps everything.
         - AutoPickupDrops (default off): on = DROPPED items (thrown from the inventory or dropped on death) fly into
           the inventory when the player comes near them (AutoCollector); off = they need aiming + F.
         - ClearItemOnDead (default off): on = dying deletes every item (nothing drops); off = they drop (ItemDropper).
           No effect while KeepInventory is on.
         - DropItem (default off): off = the player can't throw items out of the inventory (clicking outside the
           window with a stack on the cursor does nothing). On = they can (ItemDropper). Dying still drops items
           (that's KeepInventory / ClearItemOnDead).
         - OverwriteItemIcon (default off): off = every item shows its own icon (its Item Data's Icon).
           On = every item is shown the same way, chosen in Item Icon Style (ItemIconRules does the work):
             Auto Icon          = a photo of the item's prefab
             Label              = no picture, the item's prefab name in capitals (like Marko's "SWORD" slots)
             Static Image       = the item's own still picture (its Item Data > Icon > Image)
             Aseprite Animation = the item's own Aseprite animation (its Item Data > Icon > Aseprite)
           An item without that picture / animation falls back to Auto Icon.
         - ShowNotifications (default on): messages like "+3 Wood" or "+5 Coins" pop up on the side of the screen
           when the player gets items or coins (NotificationFeed). Off = no messages.
           Notification Side (dropdown) = which side of the screen they show on: Left or Right.
         - ToolTest (default off): on = the player gets one of every tool (pickaxe, axe and sword in wood, stone, iron,
           gold and diamond: 15) in the inventory, at the start or the moment it's ticked (ToolTestKit, for testing).
         Adding a rule = new GameRule entry + bool field + a case in Get and Set + static property.
Requirement: GameRules.cs <-x AutoCollector.cs, DaylightCycle.cs, GameRulesEditor.cs, GameSetup.cs, HeldItemUI.cs, ItemDropper.cs, ItemIconRules.cs, NewGameDefaultsSync.cs, NotificationFeed.cs, SlotUI.cs, ToolTestKit.cs
*/

using UnityEngine;

// Every rule. A future rules menu can show one row per entry, in this order.
public enum GameRule
{
    DoDaylightCycle,
    WorldInteraction,
    KeepInventory,
    AutoPickupDrops,
    ClearItemOnDead,
    OverwriteItemIcon,
    DropItem,
    ShowNotifications,
    ToolTest,
}

// Which side of the screen the "+3 Wood" messages show on while Show Notifications is on (a dropdown).
public enum NotificationSide
{
    Left,
    Right,
}

// How every item is shown while the rule Overwrite Item Icon is on. One at a time (a dropdown).
public enum ItemIconStyle
{
    AutoIcon,            // a photo of the item's prefab
    Label,               // the item's prefab name as text, e.g. "SWORD" (good for a prototype without art)
    StaticImage,         // the item's own still picture
    AsepriteAnimation,   // the item's own Aseprite animation
}

[DisallowMultipleComponent]   // only one GameRules per scene
public class GameRules : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("On: the time of day moves (sun goes around). Off: time stops where it is.")]
    [SerializeField] bool doDaylightCycle = true;

    [Tooltip("On: a body with no hands (the ball, simple shapes) can grab and levitate anywhere. " +
             "Off: only inside an InteractionMachine's radius.")]
    [SerializeField] bool worldInteraction = false;

    [Tooltip("On: the player keeps all items when dying. Off (like Minecraft): every item drops where the player died.")]
    [SerializeField] bool keepInventory = false;

    [Tooltip("On: when the player dies, every item is GONE (nothing is dropped). Off: items drop on the ground. " +
             "If Keep Inventory is on, this rule has no effect (the player keeps everything).")]
    [SerializeField] bool clearItemOnDead = false;

    [Tooltip("On: dropped items are collected by walking near them (AutoCollector's Drop Pickup Radius). " +
             "Off: dropped items need aiming + F, like any Pickup item.")]
    [SerializeField] bool autoPickupDrops = false;

    [Tooltip("On: the player can throw items out of the inventory (click outside the window with a stack on the cursor). " +
             "Off: they can't; the stack stays on the cursor and goes back into the inventory when it closes. " +
             "Dying still drops items (see Keep Inventory).")]
    [SerializeField] bool dropItem = false;

    [Header("Item Icons")]
    [Tooltip("Off: every item shows its own icon (set in its Item Data). " +
             "On: every item is shown the same way, chosen in Item Icon Style below.")]
    [SerializeField] bool overwriteItemIcon = false;

    [Tooltip("How every item is shown while Overwrite Item Icon is on: Auto Icon = a photo of its prefab, " +
             "Label = its prefab name as text, Static Image / Aseprite Animation = its own picture / animation " +
             "(items without one get Auto Icon).")]
    [SerializeField] ItemIconStyle itemIconStyle = ItemIconStyle.AutoIcon;

    [Header("Notifications")]
    [Tooltip("On: messages like \"+3 Wood\" or \"+5 Coins\" pop up on the side of the screen when the player gets " +
             "items (harvesting, picking up, crafting) or coins (defeating an enemy). Off: no messages.")]
    [SerializeField] bool showNotifications = true;

    [Tooltip("Which side of the screen the messages show on.")]
    [SerializeField] NotificationSide notificationSide = NotificationSide.Left;

    [Header("Testing")]
    [Tooltip("On: the player gets one of every tool (pickaxe, axe and sword in wood, stone, iron, gold and diamond: 15) " +
             "in the inventory, at the start or the moment this is ticked. Tools already owned aren't given twice. " +
             "Turning it off takes nothing away.")]
    [SerializeField] bool toolTest = false;

    static GameRules instance;   // the one GameRules in the scene

    // Other scripts subscribe to this to be told when any rule changes.
    public static event System.Action Changed;

    // Other scripts read the rules through these, e.g. GameRules.WorldInteraction
    public static bool DoDaylightCycle  => Get(GameRule.DoDaylightCycle);
    public static bool WorldInteraction => Get(GameRule.WorldInteraction);
    public static bool KeepInventory    => Get(GameRule.KeepInventory);
    public static bool AutoPickupDrops  => Get(GameRule.AutoPickupDrops);
    public static bool ClearItemOnDead  => Get(GameRule.ClearItemOnDead);
    public static bool OverwriteItemIcon => Get(GameRule.OverwriteItemIcon);
    public static bool DropItem         => Get(GameRule.DropItem);
    public static bool ShowNotifications => Get(GameRule.ShowNotifications);
    public static bool ToolTest         => Get(GameRule.ToolTest);

    // The dropdown under Overwrite Item Icon (only used while that rule is on).
    public static ItemIconStyle ItemIconStyle => Current.itemIconStyle;

    // The dropdown under Show Notifications (only used while that rule is on).
    public static NotificationSide NotificationSide => Current.notificationSide;

    // Finds the GameRules in the scene. If there isn't one, makes one with the default rules.
    static GameRules Current
    {
        get
        {
            // The scene's GameRules may not have run its Awake yet (e.g. a menu asks while the scene is still loading).
            // Look for it first: making a new one then would win, and the scene's one (with your settings) would be removed.
            if (instance == null) instance = FindFirstObjectByType<GameRules>();
            if (instance == null) instance = new GameObject("GameRules").AddComponent<GameRules>();
            return instance;
        }
    }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        // A second GameRules would be confusing (which rules win?), so remove it.
        if (instance != null && instance != this)
        {
            Debug.LogWarning("There is more than one GameRules in the scene. Removing the extra one.");
            Destroy(this);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance != this) return;

        instance = null;   // static values outlive the scene, so clear them
        Changed = null;
    }

    // Unity calls this when a box is ticked in the Inspector, so listening scripts update while playing.
    void OnValidate()
    {
        if (Application.isPlaying) Changed?.Invoke();
    }

    // ===== My methods =====

    // The value of a rule right now.
    public static bool Get(GameRule rule)
    {
        GameRules r = Current;
        switch (rule)
        {
            case GameRule.DoDaylightCycle:  return r.doDaylightCycle;
            case GameRule.WorldInteraction: return r.worldInteraction;
            case GameRule.KeepInventory:    return r.keepInventory;
            case GameRule.AutoPickupDrops:  return r.autoPickupDrops;
            case GameRule.ClearItemOnDead:  return r.clearItemOnDead;
            case GameRule.OverwriteItemIcon: return r.overwriteItemIcon;
            case GameRule.DropItem:         return r.dropItem;
            case GameRule.ShowNotifications: return r.showNotifications;
            case GameRule.ToolTest:         return r.toolTest;
        }
        return false;   // a new GameRule with no field yet
    }

    // Changes a rule (e.g. from a future rules menu or cheat command).
    public static void Set(GameRule rule, bool value)
    {
        GameRules r = Current;
        switch (rule)
        {
            case GameRule.DoDaylightCycle:  r.doDaylightCycle = value;  break;
            case GameRule.WorldInteraction: r.worldInteraction = value; break;
            case GameRule.KeepInventory:    r.keepInventory = value;    break;
            case GameRule.AutoPickupDrops:  r.autoPickupDrops = value;  break;
            case GameRule.ClearItemOnDead:  r.clearItemOnDead = value;  break;
            case GameRule.OverwriteItemIcon: r.overwriteItemIcon = value; break;
            case GameRule.DropItem:         r.dropItem = value;          break;
            case GameRule.ShowNotifications: r.showNotifications = value; break;
            case GameRule.ToolTest:         r.toolTest = value;          break;
        }

        Changed?.Invoke();   // tell every listening script (if there are any)
    }

    // Changes the Item Icon Style dropdown from code (e.g. a settings menu later).
    public static void SetItemIconStyle(ItemIconStyle style)
    {
        Current.itemIconStyle = style;
        Changed?.Invoke();
    }

    // Moves the notifications to the other side from code (e.g. a settings menu later).
    public static void SetNotificationSide(NotificationSide side)
    {
        Current.notificationSide = side;
        Changed?.Invoke();
    }
}
