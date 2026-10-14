/*
First Date: 4/10/2026
Date: 13/10/2026
Author: Tony
Use by: Create an empty GameObject called "GameRules" in the scene and add this script.
        (If you forget to add this script, one is created by itself with the default rules.)
Unity Editor: Tick / untick the rules in the Inspector (also works while playing).
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
         Adding a rule = new GameRule entry + bool field + a case in Get and Set + static property.
Requirement: GameRules.cs <-x AutoCollector.cs, DaylightCycle.cs, ItemDropper.cs
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

    

    static GameRules instance;   // the one GameRules in the scene

    // Other scripts subscribe to this to be told when any rule changes.
    public static event System.Action Changed;

    // Other scripts read the rules through these, e.g. GameRules.WorldInteraction
    public static bool DoDaylightCycle  => Get(GameRule.DoDaylightCycle);
    public static bool WorldInteraction => Get(GameRule.WorldInteraction);
    public static bool KeepInventory    => Get(GameRule.KeepInventory);
    public static bool AutoPickupDrops  => Get(GameRule.AutoPickupDrops);
    public static bool ClearItemOnDead  => Get(GameRule.ClearItemOnDead);

    // Finds the GameRules in the scene. If there isn't one, makes one with the default rules.
    static GameRules Current
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("GameRules").AddComponent<GameRules>();
            }
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
        }

        Changed?.Invoke();   // tell every listening script (if there are any)
    }
}
