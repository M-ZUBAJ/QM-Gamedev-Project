/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: The Player (next to PlayerInventory). Tools > Items > Build Plate Armor puts it there with every armor piece.
        A game scene saved without it (e.g. the tool ran while the title scene was open, so it couldn't reach the
        Player) gets one by itself when the scene loads, so the rule works in every game scene.
Unity Editor: Turn the rule on: select the GameRules object > Testing > Armor Test. Ticking it while playing gives the
              armor at once (then Tab > Gear to put it on, or Tab > Crafting > Armor to try the upgrades). From the
              title screen: New Game > Testing > Start with all armor.
Purpose: For testing armor: while the game rule Armor Test is on, the player gets one of each armor piece (the Copper,
         Iron, Gold and Diamond sets: 24) in the inventory, when the game starts or the moment the rule is turned on. Pieces
         the player already has, in the inventory or worn, aren't given twice, and turning the rule off takes nothing
         away. Tools come from the separate rule Tool Test (ToolTestKit). It gives its list's pieces first, then every
         other armor Item Data the game has loaded (the crafting list links them all), so a set added after the list
         was saved (e.g. Gold in the new world's scene) is given too, and a kit that added itself (empty list) gives
         them all.
Requirement: ArmorTestKit.cs x-> PlayerInventory.cs, ItemData.cs, GameRules.cs
             ArmorTestKit.cs o-> PlayerArmor.cs
             ArmorTestKit.cs <-x ArmorBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ArmorTestKit : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The armor given first while the game rule Armor Test is on (Tools > Items > Build Plate Armor fills it in). " +
             "Every other armor Item Data the game has loaded is given after it.")]
    [SerializeField] List<ItemData> armor = new List<ItemData>();

    bool wasOn;     // the rule's value last time we looked, so turning it on (not just being on) gives the armor
    bool started;   // the rule can be set before Start: the armor is given in Start, once everything is ready

    // ===== Unity messages (Unity calls these) =====

    // Static events outlive Play sessions when domain reload is off: listen for scenes once per Play session.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        SceneManager.sceneLoaded -= AddIfMissing;
        SceneManager.sceneLoaded += AddIfMissing;
    }

    // Unity calls this after a scene's objects woke up (Awake) but before they start (Start). A game scene saved
    // without a kit (Build Plate Armor couldn't reach its Player, e.g. the title scene was open) gets one on the
    // Player, so GameRules > Armor Test works in every game scene. The title screen has no PlayerInventory: nothing.
    static void AddIfMissing(Scene scene, LoadSceneMode mode)
    {
        PlayerInventory inventory = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<ArmorTestKit>(true) != null) return;   // the scene has its own kit
            if (inventory == null) inventory = root.GetComponentInChildren<PlayerInventory>(true);
        }
        if (inventory != null) inventory.gameObject.AddComponent<ArmorTestKit>();
    }

    void OnEnable()
    {
        GameRules.Changed += OnRulesChanged;
    }

    void OnDisable()
    {
        GameRules.Changed -= OnRulesChanged;
    }

    void Start()
    {
        started = true;
        OnRulesChanged();   // the rule may be on from the start
    }

    // ===== My methods =====

    void OnRulesChanged()
    {
        if (!started) return;   // Start gives them
        bool on = GameRules.ArmorTest;
        if (on && !wasOn) Give();
        wasOn = on;
    }

    // One of each armor piece the player doesn't have yet (in the inventory or worn).
    public void Give()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null) return;
        PlayerArmor worn = PlayerArmor.Current;

        foreach (ItemData piece in Pieces())
        {
            if (piece == null || inventory.Count(piece.Id) > 0 || (worn != null && worn.IsWorn(piece.Id))) continue;
            if (!inventory.Add(piece.Id, piece.DisplayName, piece.GetIcon(), 1, piece))
            {
                Debug.Log("Armor Test: the inventory is full, so not every test armor piece fit.");
                return;
            }
        }
    }

    // The armor to give: the list from Build Plate Armor first, then every other armor Item Data the game has loaded (the
    // crafting list links them all), so a set added later is given even by a list saved before it. The loaded ones go
    // weakest set first, each set from helmet to boots, like the crafting list.
    List<ItemData> Pieces()
    {
        List<ItemData> pieces = armor.FindAll(piece => piece != null);

        List<ItemData> found = new List<ItemData>();
        Dictionary<string, float> setTotal = new Dictionary<string, float>();   // a set's protection, all pieces together
        foreach (ItemData data in Resources.FindObjectsOfTypeAll<ItemData>())
        {
            if (data == null || !data.IsArmor || found.Contains(data)) continue;
            found.Add(data);
            string set = data.Armor.set ?? "";
            setTotal.TryGetValue(set, out float total);
            setTotal[set] = total + data.Armor.protection;
        }
        found.Sort((a, b) =>
        {
            string setA = a.Armor.set ?? "", setB = b.Armor.set ?? "";
            int order = setTotal[setA].CompareTo(setTotal[setB]);
            if (order == 0) order = string.CompareOrdinal(setA, setB);   // two sets just as strong stay apart
            return order != 0 ? order : a.Armor.slot.CompareTo(b.Armor.slot);
        });
        foreach (ItemData data in found)
        {
            if (!pieces.Contains(data)) pieces.Add(data);
        }
        return pieces;
    }
}
