/*
First Date: 11/10/2026
Date: 21/11/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerHealth).
Unity Editor: Set "Respawn Point" to where the player comes back (default 0, 0.5, 0: the middle of the world,
              just above the ground). Make sure that spot is on solid ground, not inside a wall.
              In the new (generated) world, WorldGenerator sets it to where the player starts (SetRespawnPoint), and
              with its New World On Death on, respawning makes a brand-new world instead (StartsNewWorld).
              With a DeathScreen in the scene, the player respawns when its Respawn button is clicked;
              without one, straight away.
              Testing: in Play mode, click PlayerHealth's ⋮ menu > "Test: Die" (or type 0 into its Health).
Purpose: When the player dies (PlayerHealth reaches 0), brings them back at the Respawn Point, like Minecraft:
         moved there, stopped (no falling speed kept), and full Health, Hunger and Stamina again.
         The inventory is kept. Fires Respawned afterwards (PlayerStats starts counting a new life).
         The moment the player dies, every open menu closes (inventory, build menu, crafting, perk choice...).
         In the new world (WorldGenerator > New World On Death) Respawn doesn't bring the player back: a brand-new world
         loads instead (Day 1, everything fresh, like a new game).
Requirement: PlayerRespawn.cs x-> PlayerHealth.cs, MenuState.cs
             PlayerRespawn.cs o-> PlayerHunger.cs, PlayerStamina.cs, WorldGenerator.cs, Rigidbody (Unity)
             PlayerRespawn.cs <-x DeathScreen.cs, PlayerStats.cs
             PlayerRespawn.cs <-o WorldGenerator.cs
*/

using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[DisallowMultipleComponent]
public class PlayerRespawn : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Where the player comes back after dying (world position). Y = 0.5 puts a 1 m tall shape on ground at height 0.")]
    [SerializeField] Vector3 respawnPoint = new Vector3(0f, 0.5f, 0f);

    PlayerHealth health;
    PlayerHunger hunger;     // can be missing: then hunger isn't refilled
    PlayerStamina stamina;   // can be missing too
    Rigidbody rb;            // can be missing: then only the Transform is moved
    bool respawnNextFrame;   // died with no death screen: respawn on the next Update

    // True = don't respawn by itself when the player dies; wait until Respawn() is called (DeathScreen's button).
    // DeathScreen turns this on by itself, so nothing to set up.
    public bool WaitForButton { get; set; }

    // Fired after every respawn. E.g. PlayerStats resets its numbers for the new life.
    public event System.Action Respawned;

    public Vector3 RespawnPoint => respawnPoint;

    // True in the new world with New World On Death on: Respawn loads a brand-new world instead of bringing the player
    // back here (DeathScreen keeps its page up until it does).
    public bool StartsNewWorld => WorldGenerator.NewWorldOnRespawn;

    // Moves the respawn point (e.g. WorldGenerator: the spot in the grassland where a new world starts the player).
    public void SetRespawnPoint(Vector3 point)
    {
        respawnPoint = point;
    }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        hunger = GetComponent<PlayerHunger>();
        stamina = GetComponent<PlayerStamina>();
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        health.Died += OnDied;    // PlayerHealth tells us when the player dies
    }

    void OnDisable()
    {
        health.Died -= OnDied;    // stop listening (e.g. the script was turned off or the player removed)
    }

    // No death screen: come back straight away, but one frame later, so every other script that reacts to dying
    // (e.g. ItemDropper dropping the items) still sees the player where they died, not at the Respawn Point.
    void Update()
    {
        if (respawnNextFrame)
        {
            respawnNextFrame = false;
            Respawn();
        }
    }

    // ===== My methods =====

    void OnDied()
    {
        MenuState.CloseAll();   // close the inventory, build menu, crafting... nothing stays open over the death
        if (!WaitForButton) respawnNextFrame = true;
    }

    // Moves the player to the Respawn Point and fills every stat back up.
    public void Respawn()
    {
        respawnNextFrame = false;   // e.g. the button was clicked: no second respawn

        // The new world (WorldGenerator > New World On Death): no coming back here, a brand-new world loads instead.
        if (StartsNewWorld)
        {
            WorldGenerator.Current.StartNewWorld();
            return;
        }
        if (rb != null)
        {
            rb.velocity = Vector3.zero;          // don't keep the speed of the fall that killed us
            rb.angularVelocity = Vector3.zero;
            rb.position = respawnPoint;          // tell physics right away, not at the next physics step
        }
        transform.position = respawnPoint;

        health.Revive();
        if (hunger != null) hunger.Eat(hunger.MaxHunger);         // Eat never goes above max = full
        if (stamina != null) stamina.Restore(stamina.MaxStamina); // also ends "tired"

        Debug.Log(gameObject.name + " respawned at " + respawnPoint + ".");
        Respawned?.Invoke();
    }
}
