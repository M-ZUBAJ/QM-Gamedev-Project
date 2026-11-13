using UnityEngine;
using System;

public class PlayerCoins : MonoBehaviour
{
    public static PlayerCoins Instance { get; private set; }

    public int Coins { get; private set; }

    // Lets the UI know when the coin total changes.
    public event Action<int> CoinsChanged;

    // [Tony] added (10/11/2026): when the player dies, the coins go back to what the player had at the start
    // (20 right now, from the test line in Awake; 0 once that line is gone). Untick to keep the coins after dying.
    // To undo: delete every line marked [Tony]. Log: Docs/TonyEditMate/Marko/10-11-2026 Coins Reset On Death, Perk Menu Closes, Punch.md
    [Tooltip("[Tony] On: dying puts the coins back to the starting amount.")]
    public bool resetOnDeath = true;                                                  // [Tony] added
    private int startCoins;                                                           // [Tony] added
    private PlayerHealth health;                                                      // [Tony] added

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one PlayerCoins component exists!");
            return;
        }

        Instance = this;


        ///temporary test code
        AddCoins(20);

        startCoins = Coins; // [Tony] added: what the player starts with (dying goes back to it)
    }

    void Start()                                                                      // [Tony] added
    {                                                                                 // [Tony] added
        health = GetComponent<PlayerHealth>();   // on the same Player                // [Tony] added
        if (health != null) health.Died += OnPlayerDied;                              // [Tony] added
    }                                                                                 // [Tony] added

    void OnDestroy()                                                                  // [Tony] added
    {                                                                                 // [Tony] added
        if (health != null) health.Died -= OnPlayerDied;                              // [Tony] added
    }                                                                                 // [Tony] added

    void OnPlayerDied()                                                               // [Tony] added
    {                                                                                 // [Tony] added
        if (resetOnDeath) ResetCoins();                                               // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added: back to the starting amount. The HUD's coin counter follows (CoinsChanged).
    public void ResetCoins()                                                          // [Tony] added
    {                                                                                 // [Tony] added
        Coins = startCoins;                                                           // [Tony] added
        Debug.Log("Coins reset to " + Coins + " (the player died).");                 // [Tony] added
        CoinsChanged?.Invoke(Coins);                                                  // [Tony] added
    }                                                                                 // [Tony] added

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;

        Debug.Log("Coins earned: " + amount +
                  " | Total coins: " + Coins);

        CoinsChanged?.Invoke(Coins);
    }


    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;

        if (Coins < amount)
        {
            Debug.Log("Not enough coins!");
            return false;
        }

        Coins -= amount;

        Debug.Log("Coins spent: " + amount +
                " | Total coins: " + Coins);

        CoinsChanged?.Invoke(Coins);

        return true;
    }

}
