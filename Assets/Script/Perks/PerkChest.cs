using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PerkChest : MonoBehaviour
{
    [Header("Chest Cost")]
    [Min(0)]
    [SerializeField] int minCost = 5;

    [Min(0)]
    [SerializeField] int maxCost = 15;

    [SerializeField] KeyCode interactKey = KeyCode.E;

    [Header("Interaction")]
    [Min(1f)]
    [SerializeField] float interactionRange = 3f;


    [Header("Opened Chest")]
    [SerializeField] bool despawnAfterOpened = true;

    [Min(0f)]
    [SerializeField] float despawnDelay = 0f; // how longer after opened chest should despawn

    bool opened;
    int openingCost;

    // [Tony] added (19/11/2026): lets Tony's InteractOutline hide the chest's yellow outline once it's opened.
    // Read only, nothing else changes. Log: Docs/TonyEditMate/Marko/19-11-2026 Chest On The Floor And Yellow Outline.md
    public bool IsOpened => opened; // [Tony] added

    // [Tony] added (19/11/2026): the perk panel can now be closed (E again, or walking away) and opened again, each
    // chest keeps its own 3 perks, and the coins are paid when a perk is picked. The getters are read only, for Tony's
    // prompt under the crosshair (ChestPrompt) and the price on the panel (PerkPanelInfo).
    // To undo: delete every line marked [Tony] below and put the commented "Original" code back.
    // Log: Docs/TonyEditMate/Marko/19-11-2026 Perk Panel, E Toggle And Pay On Pick.md
    public int OpeningCost => tier != null ? tier.Price : openingCost;   // [Tony] added: the price (21/11/2026: Tony's ChestTier's when the chest has one)
    public float InteractionRange => interactionRange;      // [Tony] added: how close E works
    public KeyCode InteractKey => interactKey;              // [Tony] added: the key (E)
    public static PerkChest Showing { get; private set; }   // [Tony] added: the chest whose perks are on screen (null = none)
    List<PerkData> choices;                                 // [Tony] added: this chest's 3 perks, rolled the first time it opens
    static int closedFrame = -1;                            // [Tony] added: the frame E closed the panel (nothing opens in it)
    const float CloseMargin = 0.5f;                         // [Tony] added: meters past Interaction Range before it closes

    // [Tony] added (21/11/2026): loot boxes. Tony's ChestTier on the chest (common, rare or epic box) sets the price,
    // which grows with every perk bought from that kind of box, and whether the epic box can reroll its perks.
    // A chest without one works exactly as before. Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
    ChestTier tier;                                         // [Tony] added: found in Start (can be missing)
    public ChestTier Tier => tier;                          // [Tony] added: for Tony's prompt and perk panel (the box's name)

    // [Tony] added (21/11/2026): the new chest system (Tony's ChestTier, GameRules > New Chests, picked on the New Game
    // page): a common or epic box rolls ONE random perk in Tony's pop-up (PerkRollUI: BUY or REROLL) instead of your
    // panel, every box can reroll, and the common and rare boxes offer the old common and rare perks together.
    // The old system works exactly as before. Log: Docs/TonyEditMate/Marko/21-11-2026 New Chest System.md
    bool rollSeen;                                          // [Tony] added: the roll was shown once (opening again shows the result at once)
    bool RandomRoll => tier != null && tier.RandomRoll;     // [Tony] added: this box rolls one perk (new system)
    List<PerkData> PerksNow() => tier != null ? tier.PerksFor(availablePerks) : availablePerks;   // [Tony] added: the perks it offers now



    [Header("Available Perks")]
    [SerializeField] List<PerkData> availablePerks = new List<PerkData>();
    // above line creates an editable list in the inspector, now we can add future perk assets without editing script

    void Start()
    {
        if (maxCost < minCost)
            maxCost = minCost;

        openingCost = Random.Range(minCost, maxCost + 1);

        tier = GetComponent<ChestTier>();                                             // [Tony] added (21/11/2026)
        // [Tony] edited (21/11/2026): the price comes from Tony's ChestTier when the chest has one. Original:
        // Debug.Log(name + " costs " + openingCost + " coins to open.");
        Debug.Log(name + " costs " + OpeningCost + " coins to open.");               // [Tony] edited
    }

    void Update()
    {
        if (opened) return;

        // [Tony] added (19/11/2026): while this chest's perks are on screen, E (wherever you look) or walking away
        // closes the panel. Nothing was paid yet, so the chest can be opened again, with the same 3 perks.
        // If the panel closed some other way (a perk was picked, the player died, the game was won), it's just forgotten.
        if (Showing == this)                                                          // [Tony] added
        {                                                                             // [Tony] added
            if (!PanelOpen()) Showing = null;                                         // [Tony] added
            else                                                                      // [Tony] added
            {                                                                         // [Tony] added
                // [Tony] edited (21/11/2026): while Tony's roll pop-up is still rolling, E skips the roll instead of
                // closing (PerkRollUI.BlocksClose). Was: if (Input.GetKeyDown(interactKey) || TooFar()) ClosePanel();
                if ((Input.GetKeyDown(interactKey) && !PerkRollUI.BlocksClose) || TooFar()) ClosePanel();   // [Tony] edited
                return;                                                               // [Tony] added
            }                                                                         // [Tony] added
        }                                                                             // [Tony] added
        // [Tony] added: another chest's panel is open, or E just closed one this frame, or another menu is open.
        if (Showing != null || closedFrame == Time.frameCount || MenuState.AnyOpen) return;   // [Tony] added

        if (!Input.GetKeyDown(interactKey)) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        // [Tony] replaced (19/11/2026): in first person the camera sits on the edge of the player's own ball, so this
        // Raycast often hit the player first and E did nothing (Turki's berries skip the player for the same reason).
        // InteractOutline.LooksAt skips the player's body, and the chest's yellow outline uses the same test, so the
        // outline shows exactly when E works. Log: Docs/TonyEditMate/Marko/19-11-2026 Chest On The Floor And Yellow Outline.md
        // Original:
        // if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        //     return;
        //
        // // Only open this chest when the player is looking directly at it.
        // if (hit.collider.transform != transform &&
        //     !hit.collider.transform.IsChildOf(transform))
        //     return;
        if (!InteractOutline.LooksAt(ray, interactionRange, transform)) return;   // [Tony] replaced

        TryOpen();
    }


    void TryOpen()
    {
        if (opened) return;

        if (PerkSelectionUI.Instance == null)
        {
            Debug.LogWarning("PerkSelectionUI was not found.");
            return;
        }

        // [Tony] replaced (21/11/2026): a chest may show 1 to 3 perks now (some perks can be picked only once, or only
        // after another one), so it needs at least one. Original:
        // if (availablePerks == null || availablePerks.Count < 3)
        // {
        //     Debug.LogWarning("The chest needs at least 3 available perks.");
        //     return;
        // }
        if (PerksNow() == null || PerksNow().Count == 0)                              // [Tony] replaced (21/11/2026: PerksNow(), was availablePerks)
        {                                                                             // [Tony] replaced
            Debug.LogWarning("The chest has nothing in its Available Perks list.");   // [Tony] replaced
            return;                                                                   // [Tony] replaced
        }                                                                             // [Tony] replaced

        if (PlayerCoins.Instance == null)
        {
            Debug.LogWarning("PlayerCoins was not found.");
            return;
        }

        // [Tony] replaced (19/11/2026): no coin check here any more. Looking at the perks is free; the coins are paid
        // when one is picked (OnPerkSelected), and Tony's PerkPanelInfo fades out the choices while you can't pay.
        // Original:
        // if (PlayerCoins.Instance.Coins < openingCost)
        // {
        //     Debug.Log("You need " + openingCost + " coins to open this chest.");
        //     return;
        // }

        // [Tony] edited (19/11/2026): the 3 perks are only rolled the first time this chest opens and kept in the new
        // "choices" field, so opening it again shows the same 3 (another chest rolls its own). Your rolling code is
        // unchanged, one step further in, inside the new "if". Original first line (a new list every time):
        // List<PerkData> choices = new List<PerkData>();
        // [Tony] added (21/11/2026): a perk picked somewhere else since this chest rolled (e.g. a once-a-life perk from
        // another chest) can't be offered any more, so the chest rolls again.
        if (choices != null && choices.Exists(perk => !PerkRules.CanOffer(perk))) choices = null;   // [Tony] added
        if (choices == null)                                                          // [Tony] added
        {                                                                             // [Tony] added
            List<PerkData> pool = new List<PerkData>(
                PerksNow().FindAll(perk => perk != null && PerkRules.CanOffer(perk))   // [Tony] edited (21/11/2026): only perks the player may get now, from the list it offers now (was: availablePerks.FindAll(perk => perk != null))
            );

            // [Tony] replaced (21/11/2026): fewer than 3 is fine now (1 or 2 cards show); none left = nothing to offer.
            // Original:
            // if (pool.Count < 3)
            // {
            //     Debug.LogWarning("The chest needs at least 3 valid perk assets.");
            //     return;
            // }
            if (pool.Count == 0)                                                      // [Tony] replaced
            {                                                                         // [Tony] replaced
                NotificationFeed.Show("Nothing left in this box for you.", null);     // [Tony] replaced
                return;                                                               // [Tony] replaced
            }                                                                         // [Tony] replaced

            choices = new List<PerkData>();                                           // [Tony] edited: the field, made once

            // Randomly select three different perks.
            for (int i = 0; i < (RandomRoll ? 1 : 3) && pool.Count > 0; i++)          // [Tony] edited (21/11/2026): stops when the pool runs out, a random box rolls 1 (was: i < 3)
            {
                int index = Random.Range(0, pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }                                                                             // [Tony] added

        // [Tony] added (21/11/2026): a random box (new system) shows its one perk in Tony's roll pop-up: the roll plays
        // the first time (it can be skipped), then BUY pays and gives it (BuyRolled), REROLL rolls again, and E or walking
        // away closes it without paying. No pop-up in the scene = your panel with that one card.
        if (RandomRoll && PerkRollUI.Instance != null)                                // [Tony] added
        {                                                                             // [Tony] added
            if (!PerkRollUI.Instance.Open(this, choices[0], PerksNow(), !rollSeen)) return;   // [Tony] added
            rollSeen = true;                                                          // [Tony] added
            Showing = this;                                                           // [Tony] added
            return;                                                                   // [Tony] added
        }                                                                             // [Tony] added

        // Show the UI before taking payment.
        bool shown = PerkSelectionUI.Instance.Show(
            choices,
            OnPerkSelected
        );

        if (!shown) return;

        Showing = this; // [Tony] added: this chest's perks are on screen (E or walking away closes them)

        // [Tony] replaced (19/11/2026): paying moved to OnPerkSelected, and the chest only counts as opened once a perk
        // is picked, so closing the panel wastes neither the coins nor the chest. Original:
        // if (!PlayerCoins.Instance.SpendCoins(openingCost))
        // {
        //     PerkSelectionUI.Instance.Hide();
        //     return;
        // }
        //
        // opened = true;

        Debug.Log("Chest opened! Choose your perk (" + OpeningCost + " coins)."); // [Tony] edited: the price added (21/11/2026: OpeningCost, ChestTier's price)
    }




    void OnPerkSelected(PerkData selectedPerk)
    {
        Showing = null; // [Tony] added: the panel closed when the perk was picked

        if (PerkManager.Instance == null)
        {
            Debug.LogError("PerkManager was not found.");
            return;
        }

        // [Tony] added (19/11/2026): the coins are paid now, when the perk is picked (before, when the panel opened).
        // Not enough = no perk, and the chest stays for later (PerkPanelInfo fades out the choices before that happens).
        // [Tony] edited (21/11/2026): the price now (OpeningCost: Tony's ChestTier's, it grows with every perk bought)
        // instead of the openingCost field, and once paid, the next box of this kind costs more (tier.Bought).
        int cost = OpeningCost;                                                       // [Tony] added (21/11/2026)
        if (cost > 0 && (PlayerCoins.Instance == null || !PlayerCoins.Instance.SpendCoins(cost)))   // [Tony] added (21/11/2026: cost, was openingCost)
        {                                                                             // [Tony] added
            Debug.Log("You need " + cost + " coins for a perk from this chest.");     // [Tony] added (21/11/2026: cost)
            return;                                                                   // [Tony] added
        }                                                                             // [Tony] added
        opened = true; // [Tony] added: moved here from TryOpen (the chest is used up now)
        if (tier != null) tier.Bought();                                              // [Tony] added (21/11/2026)

        PerkManager.Instance.ApplyPerk(selectedPerk);

        Debug.Log("Selected perk: " + selectedPerk.displayName);

        // Remove the chest after the player chooses a perk.
        if (despawnAfterOpened)
        {
            Destroy(gameObject, despawnDelay);
        }


    }

    // [Tony] added (19/11/2026): opening and closing the panel. Log: Docs/TonyEditMate/Marko/19-11-2026 Perk Panel, E Toggle And Pay On Pick.md
    bool PanelOpen()                                                                  // [Tony] added
    {                                                                                 // [Tony] added
        // [Tony] edited (21/11/2026): or Tony's roll pop-up shows this chest. Was:
        // return PerkSelectionUI.Instance != null && PerkSelectionUI.Instance.IsOpen;
        return (PerkSelectionUI.Instance != null && PerkSelectionUI.Instance.IsOpen) || PerkRollUI.IsOpenFor(this);   // [Tony] edited
    }                                                                                 // [Tony] added

    // [Tony] added: closes the panel (nothing is paid). Nothing opens again this frame, even though E is down.
    void ClosePanel()                                                                 // [Tony] added
    {                                                                                 // [Tony] added
        Showing = null;                                                               // [Tony] added
        closedFrame = Time.frameCount;                                                // [Tony] added
        // [Tony] edited (21/11/2026): your panel or Tony's roll pop-up, whichever is open. Was:
        // if (PanelOpen()) PerkSelectionUI.Instance.Hide();
        if (PerkSelectionUI.Instance != null && PerkSelectionUI.Instance.IsOpen) PerkSelectionUI.Instance.Hide();   // [Tony] edited
        if (PerkRollUI.IsOpenFor(this)) PerkRollUI.Instance.Close();                  // [Tony] added (21/11/2026)
    }                                                                                 // [Tony] added

    // [Tony] added: farther than Interaction Range (+ Close Margin) from the camera (where the E ray starts) to the
    // nearest point of the chest's box.
    bool TooFar()                                                                     // [Tony] added
    {                                                                                 // [Tony] added
        Camera cam = Camera.main;                                                     // [Tony] added
        if (cam == null) return false;                                                // [Tony] added
        Vector3 eye = cam.transform.position;                                         // [Tony] added
        Collider box = GetComponent<Collider>();                                      // [Tony] added
        Vector3 nearest = box != null && box.enabled ? box.ClosestPoint(eye) : transform.position;   // [Tony] added
        return Vector3.Distance(eye, nearest) > interactionRange + CloseMargin;       // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added: a chest that disappears while its perks are on screen takes the panel with it.
    void OnDestroy()                                                                  // [Tony] added
    {                                                                                 // [Tony] added
        if (Showing == this) ClosePanel();                                            // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added (21/11/2026): rerolling, for Tony's REROLL button on the perk panel. Only a chest whose ChestTier
    // allows it (the epic box; in the new chest system every box). The first reroll at a box costs a tenth of its price, each next one there twice as
    // much (ChestTier.RerollPrice). Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
    // [Tony] added (21/11/2026): in both chest systems REROLL only works once the player has the Reroll perk (an epic or
    // legendary box offers it): Tony's ChestTier.CanReroll checks that too. Log: Docs/TonyEditMate/Marko/21-11-2026 Reroll Perk, Keen Eye And Long Reach.md
    public bool RerollAllowed => tier != null && tier.CanReroll;                      // [Tony] added: show the button
    public int RerollCost => tier != null ? tier.RerollPrice : 0;                     // [Tony] added
    public bool RerollWouldChange => choices != null && OtherPerksLeft();             // [Tony] added: else it'd show the same perks

    // [Tony] added: pays and shows new perks (ones not on screen first). False = it can't (not allowed here, nothing
    // new could come up, or not enough coins); nothing is paid then.
    public bool TryReroll()                                                           // [Tony] added
    {                                                                                 // [Tony] added
        if (!RerollAllowed || !RerollWouldChange || Showing != this) return false;    // [Tony] added
        if (PlayerCoins.Instance == null || PerkSelectionUI.Instance == null) return false;   // [Tony] added
        if (!PlayerCoins.Instance.SpendCoins(RerollCost)) return false;               // [Tony] added
        tier.Rerolled();                                                              // [Tony] added: the next reroll here costs twice as much
        // [Tony] edited (21/11/2026): the list it offers now (was availablePerks), a random box rolls 1 perk (was 3) and
        // its roll plays again in Tony's pop-up (was always: PerkSelectionUI.Instance.Refresh(choices);).
        List<PerkData> pool = PerksNow().FindAll(perk => perk != null && PerkRules.CanOffer(perk));   // [Tony] edited
        choices = PerkRules.Pick(pool, RandomRoll ? 1 : 3, choices);                  // [Tony] edited
        if (RandomRoll && PerkRollUI.IsOpenFor(this)) PerkRollUI.Instance.Open(this, choices[0], PerksNow(), true);   // [Tony] edited
        else PerkSelectionUI.Instance.Refresh(choices);                               // [Tony] edited
        return true;                                                                  // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added: the list holds a perk the player may get that isn't on screen now.
    bool OtherPerksLeft()                                                             // [Tony] added
    {                                                                                 // [Tony] added
        List<PerkData> list = PerksNow();                                             // [Tony] edited (21/11/2026): the list it offers now (was availablePerks)
        return list != null &&                                                        // [Tony] edited
               list.Exists(perk => perk != null && PerkRules.CanOffer(perk) && !choices.Contains(perk));   // [Tony] edited
    }                                                                                 // [Tony] added

    // [Tony] added (21/11/2026): Tony's roll pop-up's BUY button: pays and gives the rolled perk, like picking it on your
    // panel (OnPerkSelected: the price, the box's burst, the perk, then the chest disappears).
    public bool CanBuyRolled => !opened && Showing == this && choices != null && choices.Count > 0;   // [Tony] added
    public void BuyRolled()                                                           // [Tony] added
    {                                                                                 // [Tony] added
        if (CanBuyRolled) OnPerkSelected(choices[0]);                                 // [Tony] added
    }                                                                                 // [Tony] added



    void GrantRandomPerk()
    {
        int perk = Random.Range(0, 4);

        switch (perk)
        {
            // Perk 1 :increase heath by 20 percent
            case 0:
                {
                    PlayerHealth health = FindFirstObjectByType<PlayerHealth>();

                    if (health != null)
                        health.IncreaseMaxHealth(0.20f);
                    else
                        Debug.LogWarning("PlayerHealth was not found.");
                    PlayerPerks.Record(ChestPerkType.MaxHealth);   // [Tony] added: the HUD shows the perk (PerkBar) (21/11/2026: ChestPerkType, was PerkType)
                    break;
                }

            // Perk 2 : Increase max stamina by 25 percent
            case 1:
                {
                    PlayerStamina stamina = FindFirstObjectByType<PlayerStamina>();

                    if (stamina != null)
                        stamina.IncreaseMaxStamina(25f);
                    else
                        Debug.LogWarning("PlayerStamina was not found.");
                    PlayerPerks.Record(ChestPerkType.MaxStamina);   // [Tony] added: the HUD shows the perk (PerkBar) (21/11/2026: ChestPerkType, was PerkType)
                    break;
                }

            // Perk 3 : increase Max move speed by 15 percent 
            case 2:
                {
                    PlayerController controller = FindFirstObjectByType<PlayerController>();

                    if (controller != null)
                        controller.IncreaseMoveSpeed(0.15f);
                    else
                        Debug.LogWarning("PlayerController was not found.");
                    PlayerPerks.Record(ChestPerkType.MoveSpeed);   // [Tony] added: the HUD shows the perk (PerkBar) (21/11/2026: ChestPerkType, was PerkType)
                    break;
                }

            // Perk 4: Unlock or upgrade the Fireball ability.
            case 3:
                {
                    PlayerFireball fireball =
                        FindFirstObjectByType<PlayerFireball>();

                    if (fireball != null)
                        fireball.AddFireballPerk();
                    else
                        Debug.LogWarning(
                            "PlayerFireball was not found on the player."
                        );
                    PlayerPerks.Record(ChestPerkType.Fireball);   // [Tony] added: the HUD shows the perk (PerkBar) (21/11/2026: ChestPerkType, was PerkType)

                    break;
                }


        }
    }
}
