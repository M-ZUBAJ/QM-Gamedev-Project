
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

        PlayerInventory.Instance.Add(
            "Axe",
            "Axe",
            null,
            1
        );

        Debug.Log("Axe added to inventory for testing.");

        PlayerInventory.Instance.Add(
            "PickAxe",
            "PickAxe",
            null,
            1
        );

        Debug.Log("PickAxe added to inventory for testing.");
    }
}
