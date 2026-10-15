using System.Collections;
using System.Collections.Generic;


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

        Sprite swordIcon = CreateSwordIcon();

        PlayerInventory.Instance.Add(
            "Sword",
            "Sword",
            swordIcon,
            1
        );

        Debug.Log("Sword added to inventory for testing.");
    }

    Sprite CreateSwordIcon()
    {
        int width = 128;
        int height = 64;

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(0.15f, 0.15f, 0.15f, 1f);

        texture.SetPixels(pixels);
        texture.Apply();

        // Temporary plain icon background.
        return Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f)
        );
    }
}
