/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerInventory, PlayerHealth, PlayerHunger and PlayerStamina).
Unity Editor: Make an item edible in its Item Data: Item Type = Consumable, then set its stats (see Consumable.cs).
              The key is set in KeyBindings (Consume, default right mouse button).
              Testing: Play, put food in a hotbar slot, select it (1 - 8), right click and watch the bars.
Purpose: Eat / drink the item in hand, like Minecraft: right click with food selected in the hotbar uses up 1
         and changes the player's Health, Hunger and Stamina by the item's Consumable numbers
         (positive = gives, negative = takes away).
         Does nothing with an empty hand, a non-food item, a menu open, while placing a blueprint, or when dead.
         A missing stat script (e.g. no PlayerStamina) just skips that stat.
Requirement: ItemConsumer.cs x-> PlayerInventory.cs, ItemData.cs, Consumable.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs
             ItemConsumer.cs o-> PlayerHealth.cs, PlayerHunger.cs, PlayerStamina.cs
*/

using UnityEngine;
using UnityEngine.Events;   // UnityEvent

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class ItemConsumer : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Called every time something is eaten, with its Item Data. E.g. a crunch sound.")]
    [SerializeField] UnityEvent<ItemData> onConsumed;

    PlayerInventory inv;
    PlayerHealth health;     // each can be missing: that stat is just skipped
    PlayerHunger hunger;
    PlayerStamina stamina;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        hunger = GetComponent<PlayerHunger>();
        stamina = GetComponent<PlayerStamina>();
    }

    void Update()
    {
        if (MenuState.AnyOpen || MenuState.KeysCaptured) return;   // a menu has the keyboard and mouse
        if (ClickClaim.Claimed) return;                            // e.g. right click cancels a blueprint instead
        if (!Input.GetKeyDown(KeyBindings.Consume)) return;

        TryConsumeHand();
    }

    // ===== My methods =====

    // Eats the item in hand if it's food. Returns true if something was eaten.
    public bool TryConsumeHand()
    {
        PlayerInventory.Slot hand = inv.ActiveItem;
        if (hand == null || hand.IsEmpty) return false;   // nothing selected / empty slot

        ItemData data = hand.data;
        if (data == null)
        {
            // Usually the item's prefab has no ItemPickup, or its ItemPickup's Data is empty.
            Debug.LogWarning("Can't eat \"" + hand.name + "\": it has no Item Data. Give its prefab an ItemPickup " +
                             "with Data = its Item Data (Tools > Items > Make Item From Selection), then pick it up again.");
            return false;
        }
        if (!data.IsConsumable) return false;   // not food (a Material like Wood)
        if (health != null && health.IsDead) return false;      // the dead don't eat

        inv.TakeOneFromHand();
        Apply(data.Consumable);
        onConsumed?.Invoke(data);
        return true;
    }

    // Changes each stat by the item's number. The stat scripts keep values between 0 and max.
    void Apply(Consumable food)
    {
        if (health != null)
        {
            if (food.health > 0f) health.Heal(food.health);
            else if (food.health < 0f) health.TakeDamage(-food.health);   // minus a minus = a positive damage amount
        }

        if (hunger != null)
        {
            if (food.hunger > 0f) hunger.Eat(food.hunger);
            else if (food.hunger < 0f) hunger.LoseFood(-food.hunger);
        }

        if (stamina != null)
        {
            if (food.stamina > 0f) stamina.Restore(food.stamina);
            else if (food.stamina < 0f) stamina.Use(-food.stamina);
        }
    }
}
