
using UnityEngine;

public class GiveSwordForTesting : MonoBehaviour
{
    void Start()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("PlayerInventory was not found.");
            return;
        }

        PlayerInventory.Instance.Add(
            "Sword",
            "Sword",
            null,
            1
        );

        Debug.Log("Sword added to inventory for testing.");
    }
}
