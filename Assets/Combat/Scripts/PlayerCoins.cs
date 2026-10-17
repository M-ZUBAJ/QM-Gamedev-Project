using UnityEngine;
using System;

public class PlayerCoins : MonoBehaviour
{
    public static PlayerCoins Instance { get; private set; }

    public int Coins { get; private set; }

    // Lets the UI know when the coin total changes.
    public event Action<int> CoinsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one PlayerCoins component exists!");
            return;
        }

        Instance = this;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;

        Debug.Log("Coins earned: " + amount +
                  " | Total coins: " + Coins);

        CoinsChanged?.Invoke(Coins);
    }
}
