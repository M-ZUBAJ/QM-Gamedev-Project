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
    public int OpeningCost => openingCost;                  // [Tony] added: the price
    public float InteractionRange => interactionRange;      // [Tony] added: how close E works
    public KeyCode InteractKey => interactKey;              // [Tony] added: the key (E)
    public static PerkChest Showing { get; private set; }   // [Tony] added: the chest whose perks are on screen (null = none)
    List<PerkData> choices;                                 // [Tony] added: this chest's 3 perks, rolled the first time it opens
    static int closedFrame = -1;                            // [Tony] added: the frame E closed the panel (nothing opens in it)
    const float CloseMargin = 0.5f;                         // [Tony] added: meters past Interaction Range before it closes



    [Header("Available Perks")]
    [SerializeField] List<PerkData> availablePerks = new List<PerkData>();
    // above line creates an editable list in the inspector, now we can add future perk assets without editing script

    void Start()
    {
        if (maxCost < minCost)
            maxCost = minCost;

        openingCost = Random.Range(minCost, maxCost + 1);

        Debug.Log(name + " costs " + openingCost + " coins to open.");
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
                if (Input.GetKeyDown(interactKey) || TooFar()) ClosePanel();         // [Tony] added
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

        if (availablePerks == null || availablePerks.Count < 3)
        {
            Debug.LogWarning("The chest needs at least 3 available perks.");
            return;
        }

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
        if (choices == null)                                                          // [Tony] added
        {                                                                             // [Tony] added
            List<PerkData> pool = new List<PerkData>(
                availablePerks.FindAll(perk => perk != null)
            );

            if (pool.Count < 3)
            {
                Debug.LogWarning("The chest needs at least 3 valid perk assets.");
                return;
            }

            choices = new List<PerkData>();                                           // [Tony] edited: the field, made once

            // Randomly select three different perks.
            for (int i = 0; i < 3; i++)
            {
                int index = Random.Range(0, pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }
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

        Debug.Log("Chest opened! Choose your perk (" + openingCost + " coins)."); // [Tony] edited: the price added
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
        if (openingCost > 0 && (PlayerCoins.Instance == null || !PlayerCoins.Instance.SpendCoins(openingCost)))   // [Tony] added
        {                                                                             // [Tony] added
            Debug.Log("You need " + openingCost + " coins for a perk from this chest.");   // [Tony] added
            return;                                                                   // [Tony] added
        }                                                                             // [Tony] added
        opened = true; // [Tony] added: moved here from TryOpen (the chest is used up now)

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
        return PerkSelectionUI.Instance != null && PerkSelectionUI.Instance.IsOpen;   // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added: closes the panel (nothing is paid). Nothing opens again this frame, even though E is down.
    void ClosePanel()                                                                 // [Tony] added
    {                                                                                 // [Tony] added
        Showing = null;                                                               // [Tony] added
        closedFrame = Time.frameCount;                                                // [Tony] added
        if (PanelOpen()) PerkSelectionUI.Instance.Hide();                             // [Tony] added
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
                    PlayerPerks.Record(PerkType.MaxHealth);   // [Tony] added: the HUD shows the perk (PerkBar)
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
                    PlayerPerks.Record(PerkType.MaxStamina);   // [Tony] added: the HUD shows the perk (PerkBar)
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
                    PlayerPerks.Record(PerkType.MoveSpeed);   // [Tony] added: the HUD shows the perk (PerkBar)
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
                    PlayerPerks.Record(PerkType.Fireball);   // [Tony] added: the HUD shows the perk (PerkBar)

                    break;
                }


        }
    }
}
