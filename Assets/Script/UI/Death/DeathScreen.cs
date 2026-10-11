/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Drag this script onto DeadCanvas (the top object of the death page).
Unity Editor: Nothing to connect. It finds these children by their NAME, so keep the names:
                StatusText    = a Text (TextMeshPro or legacy) where the stats are written
                RespawnButton = the Button that brings the player back
              Leave DeadCanvas switched ON in the Hierarchy: this script hides it by itself when the game starts
              (it turns off the Canvas component, not the object, so it can still hear the player die).
              Give DeadCanvas a higher Sort Order than the HUD so it covers it.
              The scene needs an EventSystem for the button (Tools > UI > Build Inventory UI makes one).
              Testing: in Play mode, click PlayerHealth's ⋮ menu > "Test: Die", then click Respawn.
Purpose: When the player dies, shows the death page with what they did this life (PlayerStats),
         frees the mouse and freezes the player. Clicking Respawn brings them back (PlayerRespawn)
         and hides the page again.
Requirement: DeathScreen.cs x-> PlayerHealth.cs, PlayerRespawn.cs, MenuState.cs, Canvas (Unity)
             DeathScreen.cs o-> PlayerStats.cs
*/

using TMPro;            // TMP_Text (TextMeshPro text)
using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: which UI object is selected
using UnityEngine.UI;             // Button, Text

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class DeathScreen : MonoBehaviour
{
    // ===== Fields =====
    // The children this script looks for. If you rename them in the Hierarchy, rename them here too.
    const string StatusName = "StatusText";
    const string ButtonName = "RespawnButton";

    Canvas canvas;
    TMP_Text statusTmp;     // the stats text: TextMeshPro...
    Text statusLegacy;      // ...or the old UI Text, whichever StatusText has
    Button respawnButton;

    PlayerHealth health;    // found in the scene by itself
    PlayerRespawn respawn;
    PlayerStats stats;      // can be missing: then the page shows no numbers

    public bool IsShown => canvas != null && canvas.enabled;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        canvas = GetComponent<Canvas>();

        Transform status = FindChild(transform, StatusName);
        if (status != null)
        {
            statusTmp = status.GetComponent<TMP_Text>();
            statusLegacy = status.GetComponent<Text>();
        }
        else Debug.LogWarning("DeathScreen: no child named \"" + StatusName + "\". The stats won't show.", this);

        Transform button = FindChild(transform, ButtonName);
        if (button != null) respawnButton = button.GetComponent<Button>();
        if (respawnButton != null) respawnButton.onClick.AddListener(OnRespawnClicked);   // no Inspector wiring needed
        else Debug.LogWarning("DeathScreen: no Button named \"" + ButtonName + "\". The player can't respawn.", this);

        canvas.enabled = false;   // hidden until the player dies
    }

    void Start()
    {
        health = FindFirstObjectByType<PlayerHealth>();
        respawn = FindFirstObjectByType<PlayerRespawn>();
        stats = FindFirstObjectByType<PlayerStats>();

        if (health == null || respawn == null)
        {
            Debug.LogWarning("DeathScreen: the Player needs PlayerHealth and PlayerRespawn.", this);
            return;
        }

        respawn.WaitForButton = true;   // we decide when to respawn now (the button), not PlayerRespawn
        health.Died += Show;
    }

    // While shown, keep the numbers up to date (the last hit is counted a frame after dying).
    void Update()
    {
        if (IsShown) WriteStats();
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= Show;
        if (respawn != null) respawn.WaitForButton = false;   // without us, respawn straight away again
        MenuState.SetOpen(this, false);                       // static values outlive the scene, so clear them
        MenuState.SetKeysCaptured(this, false);
    }

    // ===== My methods =====

    void Show()
    {
        canvas.enabled = true;
        WriteStats();

        MenuState.SetOpen(this, true);          // the camera, building, levitating... pause
        MenuState.SetKeysCaptured(this, true);  // moving, jumping, eating, the hotbar... stop
        Cursor.lockState = CursorLockMode.None; // free the mouse to click Respawn
        Cursor.visible = true;
    }

    void OnRespawnClicked()
    {
        // Only while the page is showing. (Unity's UI also "clicks" the selected button when Space or Enter is pressed,
        // so without this, jumping after respawning could press Respawn again and teleport the player back.)
        if (!IsShown) return;

        // Forget the clicked button, so Space / Enter can't press it again later.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (respawn != null) respawn.Respawn();

        canvas.enabled = false;
        MenuState.SetOpen(this, false);
        MenuState.SetKeysCaptured(this, false);
        Cursor.lockState = CursorLockMode.Locked;   // back to aiming
        Cursor.visible = false;
    }

    void WriteStats()
    {
        string text = stats != null ? stats.Summary() : "";
        if (statusTmp != null) statusTmp.text = text;
        else if (statusLegacy != null) statusLegacy.text = text;
    }

    // Finds a child (or grandchild...) by name, anywhere under "parent". null if there is none.
    static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform deeper = FindChild(child, childName);
            if (deeper != null) return deeper;
        }
        return null;
    }
}
