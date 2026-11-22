using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // [Tony] added: EventSystem, to know if the mouse is over a menu / button

public class PlayerCombat : MonoBehaviour
{
    public Camera playerCamera;

    public float attackRange = 2.5f; // from how far away player can attack

    // [Tony] added (21/11/2026): how far the attack reaches now: Attack Range, longer with the Long Reach perk (Tony's
    // PerkEffects: +10 % per pick). Tony's crosshair uses it too: with the Keen Eye perk it turns yellow when what it
    // points at is this close (a click will hit it). To undo: delete these lines and put back the attack's raycast below.
    // Log: Docs/TonyEditMate/Marko/21-11-2026 Reroll Perk, Keen Eye And Long Reach.md
    public float AttackReach => attackRange * PerkEffects.ReachMultiplier;                    // [Tony] added

    [Header("Attack Cooldown")]
    [Min(0f)]
    public float attackCooldown = 0.5f; // half a second

    private float nextAttackTime;



    [Header("Enemy and Animal Damage")]
    public float handDamage = 10f;
    public float swordDamage = 25f;
    public float axeDamage = 10f;
    public float pickAxeDamage = 10f;

    [Header("Resource Damage")]
    public float handResourceDamage = 5f;
    public float swordResourceDamage = 7f;
    public float axeTreeDamage = 25f;
    public float axeStoneDamage = 5f;
    public float pickAxeTreeDamage = 5f;
    public float pickAxeStoneDamage = 25f;



    [Header("Stamina")]
    public float attackStaminaCost = 10f;
    private PlayerStamina playerStamina;
    private PunchCombo punchCombo;                                                                // [Tony] added (13/11/2026): Tony's bare-handed punch combos (can be missing)

    // finds the stamina component on the same Player Object
    void Awake()
    {
        playerStamina = GetComponent<PlayerStamina>();
        punchCombo = GetComponent<PunchCombo>();                                                  // [Tony] added
    }

    void Update()
    {
        // [Tony] added (20/10/2026): a left click on a menu is not an attack. Before this, clicking inventory slots,
        // tabs, crafting rows, build buttons or the Respawn button attacked too and used 10 stamina each time.
        // Skip while a menu is open (inventory, crafting, build menu, death screen), while another mode owns the mouse
        // (e.g. placing a building), or when the free mouse is over any UI. (While aiming the mouse is locked in the
        // middle of the screen, on the crosshair picture, so the UI check only runs while the mouse is free.)
        // To undo: delete the lines marked [Tony].
        if (MenuState.AnyOpen || ClickClaim.Claimed) return;                                                  // [Tony] added
        if (Cursor.lockState != CursorLockMode.Locked && EventSystem.current != null &&                       // [Tony] added
            EventSystem.current.IsPointerOverGameObject()) return;                                            // [Tony] added

        // [Tony] added (22/11/2026): a bow in the hand doesn't punch or swing: Tony's BowShooter shoots it instead (hold
        // the left mouse button to draw, let go to shoot an arrow; the arrow hurts where it lands, not this ray).
        // To undo: delete this line. Log: Docs/TonyEditMate/Marko/22-11-2026 Bow Shoots Instead Of Punching.md
        if (ToolRules.BowInHand) return;                                                                      // [Tony] added

        // [Tony] added (13/11/2026): with no tool in the hand, Tony's PunchCombo decides the punches: left click = a jab
        // (left, right, left...), right click = a heavy punch (left hook, heavy right, left hook...: more damage and stamina).
        // Tools still attack with left click, below. Without PunchCombo nothing changes.
        // Log: Docs/TonyEditMate/Marko/13-11-2026 Punch Combo.md
        if (punchCombo != null && punchCombo.ForBareHands)                                        // [Tony] added
        {                                                                                         // [Tony] added
            PlayerAction? punch = punchCombo.Next(Time.time >= nextAttackTime);                   // [Tony] added
            if (punch.HasValue) Attack(punch.Value);                                              // [Tony] added
            return;                                                                               // [Tony] added
        }                                                                                         // [Tony] added

        //if the player presses the left click and enough time has passed since last attack.
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }



    // [Tony] replaced (13/11/2026): void Attack()
    // punch = which punch Tony's PunchCombo chose (null = decide from the hand, as before). Attack() still works.
    void Attack(PlayerAction? punch = null)                                                       // [Tony] replaced
    {
        // Check stamina before attacking.
        if (playerStamina != null && playerStamina.Stamina <= 0f)
        {
            Debug.Log("Attack Failed: Not Enough Stamina !");
            return;
        }

        // [Tony] replaced (21/11/2026): nextAttackTime = Time.time + attackCooldown;
        // The rare box's Attack Speed perk (Tony's PerkEffects: +10 % per pick) makes the wait between attacks shorter.
        // To undo: put the line above back. Log: Docs/TonyEditMate/Marko/21-11-2026 New Chest System.md
        nextAttackTime = Time.time + attackCooldown / PerkEffects.AttackSpeedMultiplier;          // [Tony] replaced

        if (playerStamina != null)
        {
            playerStamina.Use(attackStaminaCost);
        }

        // [Tony] added (10/11/2026, changed 12/11/2026): what's in the hand decides the swing the character plays, even
        // when it hits nothing: nothing or a non-tool (wood, food...) = Punch, a sword or axe = Attack, a pickaxe = Mine.
        // Tools are Item Data with Item Type = Tool (Tony's tier tools: Tools > Items > Build Tier Tools). Does nothing
        // without Tony's character. To undo: delete the lines marked [Tony] and put back the lines in the [Tony] replaced
        // comments. Logs: Docs/TonyEditMate/Marko/10-11-2026 Coins Reset On Death, Perk Menu Closes, Punch.md,
        // Docs/TonyEditMate/Marko/12-11-2026 Punch Wind Burst.md, Docs/TonyEditMate/Marko/12-11-2026 Tier Tools.md
        PlayerInventory hands = PlayerInventory.Instance;                                         // [Tony] added
        ToolStats tool = ToolRules.ToolIn(hands != null ? hands.ActiveItem : null);               // [Tony] added (null = a bare hand)
        PlayerAction swing = punch ?? ToolRules.SwingFor(tool);                                   // [Tony] added (PunchCombo's punch, else from the hand)
        PlayerAnimator.Play(swing);                                                               // [Tony] added
        if (playerStamina != null && PunchCombo.IsHeavy(swing))                                   // [Tony] added: a heavy punch costs more stamina
            playerStamina.Use(attackStaminaCost * (PunchCombo.StaminaScale(swing) - 1f));         // [Tony] added

        // Fire a ray from the centre of the camera.
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        // [Tony] replaced (21/11/2026): if (Physics.Raycast(ray, out hit, attackRange))
        // The Long Reach perk makes the attack reach farther (AttackReach, at the top). To undo: put the line above back.
        if (Physics.Raycast(ray, out hit, AttackReach))                                           // [Tony] replaced
        {
            EnemyCombat enemy =
                hit.collider.GetComponentInParent<EnemyCombat>();

            ResourceNode resource =
                hit.collider.GetComponentInParent<ResourceNode>();

            // Find the currently equipped item.
            PlayerInventory inventory = PlayerInventory.Instance;

            string equippedItem = "";

            if (inventory != null &&
                inventory.ActiveItem != null &&
                !inventory.ActiveItem.IsEmpty)
            {
                // [Tony] replaced (12/11/2026): equippedItem = inventory.ActiveItem.id;
                // Tools now come in tiers (WoodenSword ... DiamondSword), so the tool's kind gives the name your damage
                // code below checks: "Sword", "Axe" or "PickAxe" ("" = not a tool: hand damage).
                equippedItem = ToolRules.WeaponName(tool);                                        // [Tony] replaced
            }

            float damage;

            // Damage against enemies.
            if (enemy != null)
            {
                damage = handDamage;

                if (equippedItem == "Sword")
                    damage = swordDamage;
                else if (equippedItem == "Axe")
                    damage = axeDamage;
                else if (equippedItem == "PickAxe")
                    damage = pickAxeDamage;

                // [Tony] added (12/11/2026): a better sword hits harder: +25 % per tier above Wood (ToolRules.Boost).
                damage *= ToolRules.Boost(tool, ToolKind.Sword);

                // Apply this particular sword's earned upgrade bonus.
                // Capture the ID of the sword used for this attack.
                // This is saved now because PunchEffect may apply the hit later.
                string swordId = null;

                if (inventory != null &&
                    inventory.ActiveItem != null &&
                    !inventory.ActiveItem.IsEmpty &&
                    ToolRules.IsSword(inventory.ActiveItem))
                {
                    swordId = inventory.ActiveItem.id;

                    if (WeaponUpgradeSystem.Instance != null)
                    {
                        damage *= WeaponUpgradeSystem.Instance.GetDamageMultiplier(swordId);
                    }
                }

                damage *= PunchCombo.Power(swing);

                // [Tony] added (21/11/2026): the epic box's Night Power perk (Tony's PerkEffects): +10 % damage per pick
                // at night. To undo: delete this line. Log: Docs/TonyEditMate/Marko/21-11-2026 New Chest System.md
                damage *= PerkEffects.DamageMultiplier;                                                     // [Tony] added

                // [Tony] added (21/11/2026): the epic box's Critical Strike perk (Tony's PerkEffects): a chance for this
                // hit to do double damage; the wind burst turns red. Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
                bool critical = PerkEffects.RollCritical();                                                 // [Tony] added
                if (critical) damage *= PerkEffects.CriticalMultiplier;                                     // [Tony] added

                // [Tony] replaced (12/11/2026): enemy.TakeDamage(damage);
                // Every swing now hurts when it lands: Tony's PunchEffect waits that swing's delay (punch, sword / axe or
                // pickaxe), then hurts the enemy; a punch also bursts wind on it. Without PunchEffect it hurts at once.
                // [Tony] edited (21/11/2026): + critical at the end (a red burst). Was:
                // PunchEffect.Hit(enemy, hit, () => enemy.TakeDamage(damage, swordId), swing);
                PunchEffect.Hit(enemy, hit, () => enemy.TakeDamage(damage, swordId), swing, critical);     // [Tony] replaced
            }
            // Damage against trees, stone and animals.
            else if (resource != null)
            {
                // [Tony] added (12/11/2026): ores need a good enough pickaxe (copper and iron: Stone, gold: Iron, diamond:
                // Gold; the ore's Item Data > Mined With). A hand, axe or sword can't break them at all.
                if (!ToolRules.CanMine(tool, resource))                                           // [Tony] added
                {                                                                                 // [Tony] added
                    ToolRules.TellCantMine(resource);                                             // [Tony] added: "Needs a Stone Pickaxe or better"
                    return;                                                                       // [Tony] added
                }                                                                                 // [Tony] added

                damage = handResourceDamage;

                switch (resource.resourceType)
                {
                    case ResourceNode.ResourceType.Tree:
                        if (equippedItem == "Sword")
                            damage = swordResourceDamage;
                        else if (equippedItem == "Axe")
                            damage = axeTreeDamage;
                        else if (equippedItem == "PickAxe")
                            damage = pickAxeTreeDamage;
                        break;

                    case ResourceNode.ResourceType.Stone:
                        if (equippedItem == "Sword")
                            damage = swordResourceDamage;
                        else if (equippedItem == "Axe")
                            damage = axeStoneDamage;
                        else if (equippedItem == "PickAxe")
                            damage = pickAxeStoneDamage;
                        break;

                    case ResourceNode.ResourceType.Animal:
                        damage = handDamage;

                        if (equippedItem == "Sword")
                            damage = swordDamage;
                        else if (equippedItem == "Axe")
                            damage = axeDamage;
                        else if (equippedItem == "PickAxe")
                            damage = pickAxeDamage;
                        break;
                }

                // [Tony] added (12/11/2026): a better tool is faster at its own job (+25 % per tier above Wood): the axe
                // at trees, the pickaxe at rock and ores, the sword at animals (ToolRules.Boost).
                damage *= ToolRules.Boost(tool, ToolRules.JobFor(resource));                      // [Tony] added
                damage *= PunchCombo.Power(swing);                                                // [Tony] added: a heavy punch hits harder (13/11/2026)
                damage *= PerkEffects.DamageMultiplier;                                           // [Tony] added (21/11/2026): Night Power perk, like above
                bool critical = PerkEffects.RollCritical();                                       // [Tony] added (21/11/2026): Critical Strike perk, like above
                if (critical) damage *= PerkEffects.CriticalMultiplier;                           // [Tony] added

                // [Tony] replaced (12/11/2026): resource.TakeDamage(damage);
                // Every swing now hurts when it lands, so a tree or rock that this swing breaks stays until the fist or
                // tool lands (a punch also bursts wind, even on a running deer).
                // [Tony] edited (21/11/2026): + critical at the end. Was:
                // PunchEffect.Hit(resource, hit, () => resource.TakeDamage(damage), swing);
                PunchEffect.Hit(resource, hit, () => resource.TakeDamage(damage), swing, critical);   // [Tony] replaced
            }
        }
    }



    // void Attack()
    // {
    //     // if the player stamina field exists and it is <=0
    //     if (playerStamina != null && playerStamina.Stamina <= 0f)
    //     {
    //         Debug.Log("Attack Failed: Not Enough Stamina !");
    //         return; // dont attack
    //     }

    //     if (playerStamina != null)
    //     {
    //         playerStamina.Use(attackStaminaCost);
    //     }

    //     // fires and invisible ray from the centre of the camera (where the crosshair is)
    //     Ray ray = playerCamera.ViewportPointToRay(
    //         new Vector3(0.5f, 0.5f, 0f)
    //     );

    //     RaycastHit hit;

    //     if (Physics.Raycast(ray, out hit, attackRange))
    //     {
    //         EnemyCombat enemy =
    //             hit.collider.GetComponentInParent<EnemyCombat>(); // lookds for enemy's health script

    //         ResourceNode resource =
    //             hit.collider.GetComponentInParent<ResourceNode>();// looks for ResourceNodes Health

    //         float damage = handDamage;

    //         // check if the player has another weapon equipped
    //         // if yes, use the damage of that weapon instead
    //         PlayerInventory inventory = PlayerInventory.Instance;

    //         if (inventory != null &&
    //             inventory.ActiveItem != null &&
    //             !inventory.ActiveItem.IsEmpty &&
    //             inventory.ActiveItem.id == "Sword")
    //         {
    //             damage = swordDamage;
    //         }

    //         // if the enemy is found, deal damage
    //         if (enemy != null)
    //         {
    //             enemy.TakeDamage(damage);
    //         }

    //         else if (resource != null)
    //         {
    //             resource.TakeDamage(damage);
    //         }

    //     }
    // }
}
