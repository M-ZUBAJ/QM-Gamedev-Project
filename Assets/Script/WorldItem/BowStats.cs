/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: Not a script you put on an object. It is the "Bow" section inside the Item Data of a bow (Item Type Tool, Tool >
        Kind Bow), e.g. Assets/Config/Items/Tools/SpruceBow.
Unity Editor: Select a bow's Item Data > Item Type = Tool > Tool > Kind = Bow: the Bow section shows under it (Damage, Draw
              Seconds, Arrow Speed). Tools > Items > Build Bow And Arrows makes the Wooden Bow and the Spruce Bow and sets
              all three (it sets them again each time it runs). The numbers can be changed while playing.
Purpose: What makes one bow better than another:
           - Damage: added to the arrow's own Damage. A fully drawn shot hurts by the bow's Damage + the arrow's Damage
             (Wooden Bow 5 + Stone Arrow 20 = 25), half drawn = half.
           - Draw Seconds: how long it takes to draw it all the way (full damage and speed). Shorter = it shoots faster
             (Wooden Bow 1 s, Spruce Bow 0.6 s). The draw animation plays faster or slower to fit (BowShooter.DrawSpeed).
           - Arrow Speed: how fast its arrows fly when it's fully drawn. Faster = they fall less on the way, so they go
             farther and are easier to aim.
         BowShooter reads them from the bow in the selected hotbar slot.
Requirement: BowStats.cs <-x BowBuilder.cs, BowShooter.cs, ItemData.cs
*/

using UnityEngine;

// [Serializable] lets Unity show this inside Item Data as a fold-out section and save it there.
[System.Serializable]
public class BowStats
{
    // ===== Fields =====
    [Tooltip("Added to every arrow it shoots: a fully drawn shot hurts by this + the arrow's Damage (half drawn = half). " +
             "Wooden Bow 5, Spruce Bow 15 (a Wooden Sword hits for 25).")]
    [Min(0f)]
    public float damage = 5f;

    [Tooltip("Seconds of holding to draw it all the way: full damage and speed (Minecraft's bow: 1 second). Shorter = it " +
             "shoots faster. Wooden Bow 1, Spruce Bow 0.6. The draw animation plays faster or slower to fit.")]
    [Range(0.3f, 3f)]
    public float drawSeconds = 1f;

    [Tooltip("How fast its arrows fly when it's fully drawn, in meters per second (Minecraft's: about 50). Faster = they " +
             "fall less, so they go farther and straighter. Wooden Bow 45, Spruce Bow 60.")]
    [Range(10f, 120f)]
    public float arrowSpeed = 45f;
}
