/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to put on an object: PunchEffect (punches, tools, swords) and PerkEffects (the fireball, Thorns) call it
        when the player's hit lands. Other scripts can too: DamageNumbers.Show(point, 12f, false, true).
Unity Editor: Nothing to set. The player turns the numbers on / off on the DISPLAY page (DisplaySettings). Testing: Play,
              hit a tree or a slime: a white number pops up where it landed. With the Critical Strike perk (an epic box),
              a critical hit shows a big orange-red number with "!" and CRITICAL over it.
Purpose: Damage numbers, like many games: when a hit lands, the damage it really did (the target's health before minus
         after) pops up at the hit spot, rises and fades, always readable (drawn over everything, a dark outline, about
         the same size on screen near or far):
           - a normal hit: white;
           - a critical hit: bigger, orange-red, "CRITICAL" over the number and a "!" after it, popping harder and
             staying longer, so it's clear at a glance.
         Only while DisplaySettings.ShowDamage is on, and only on monsters and animals while Only monsters and animals is
         on. A hit that does nothing (a dodge, a turtle's shell: "BLOCKED!") shows no number.
Requirement: DamageNumbers.cs x-> FloatingText.cs, DisplaySettings.cs, EnemyCombat.cs, ResourceNode.cs
             DamageNumbers.cs <-x ArrowProjectile.cs, BearTrap.cs, PerkEffects.cs, PunchEffect.cs
*/

using System.Globalization;
using UnityEngine;

public static class DamageNumbers
{
    // ===== Fields =====
    static readonly Color NormalColor = Color.white;
    static readonly Color CriticalColor = new Color(1f, 0.38f, 0.12f);   // orange-red
    const float NormalSize = 3.2f;            // TextMeshPro size at NearDistance (3 = about a quarter of a meter)
    const float CriticalSize = 5.6f;
    const float NearDistance = 6f;            // meters: farther away the numbers grow, so they stay readable
    const float MostGrowth = 3f;              // ...up to 3 times their size
    const float Spread = 0.3f;                // meters: a little random offset, so quick hits don't cover each other

    // ===== My methods =====

    // Shows "amount" at "point" (if the player wants it: DisplaySettings). creature = a monster or an animal (for Only
    // monsters and animals). critical = a critical hit: big, orange-red, CRITICAL.
    public static void Show(Vector3 point, float amount, bool critical, bool creature)
    {
        if (amount < 0.05f || !DisplaySettings.ShowsDamageOn(creature)) return;

        string number = amount >= 10f || Mathf.Abs(amount - Mathf.Round(amount)) < 0.05f
            ? Mathf.RoundToInt(amount).ToString(CultureInfo.InvariantCulture)
            : amount.ToString("0.#", CultureInfo.InvariantCulture);   // small hits keep their tenth: 2.5
        Vector3 at = point + Jitter();
        float grow = Growth(at);
        if (critical)
            FloatingText.Show(at, "<size=45%>CRITICAL</size>\n" + number + "!", CriticalColor, CriticalSize * grow, 1.3f, 2.6f, 1.1f);
        else
            FloatingText.Show(at, number, NormalColor, NormalSize * grow, 0.9f, 1.6f, 0.7f);
    }

    // A target's health right now (a monster's EnemyCombat, or a tree's, rock's, ore's or animal's ResourceNode), and
    // whether it's a creature (a monster or an animal). NaN = it has no health (nothing to show).
    public static float HealthOf(Component target, out bool creature)
    {
        creature = false;
        if (target == null) return float.NaN;

        EnemyCombat enemy = target as EnemyCombat;
        if (enemy == null) enemy = target.GetComponentInParent<EnemyCombat>();
        if (enemy != null)
        {
            creature = true;
            return enemy.currentHealth;
        }

        ResourceNode node = target as ResourceNode;
        if (node == null) node = target.GetComponentInParent<ResourceNode>();
        if (node == null) return float.NaN;
        creature = node.resourceType == ResourceNode.ResourceType.Animal;
        return node.currentHealth;
    }

    // After a hurt: shows the damage it really did (health "before" it minus now). A destroyed target (a broken tree)
    // still counts this frame (Unity removes it at the end of the frame).
    public static void ShowDealt(Component target, float before, bool creature, Vector3 point, bool critical)
    {
        if (float.IsNaN(before) || target == null) return;
        float after = HealthOf(target, out _);
        if (float.IsNaN(after)) return;
        Show(point, before - after, critical, creature);
    }

    // A spot near the top of a target (for hits without a point, e.g. the fireball).
    public static Vector3 TopOf(Component target)
    {
        if (target == null) return Vector3.zero;
        Collider shape = target.GetComponentInParent<Collider>();
        if (shape == null) shape = target.GetComponentInChildren<Collider>();
        if (shape == null) return target.transform.position + Vector3.up;
        Bounds box = shape.bounds;
        return box.center + Vector3.up * box.extents.y * 0.6f;
    }

    // A small random offset across the view, so hits in a row don't stack on each other.
    static Vector3 Jitter()
    {
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        return right * Random.Range(-Spread, Spread) + Vector3.up * Random.Range(0f, Spread);
    }

    // Farther from the camera = bigger, so a number reads about the same on screen near or far.
    static float Growth(Vector3 at)
    {
        Camera cam = Camera.main;
        if (cam == null) return 1f;
        return Mathf.Clamp(Vector3.Distance(cam.transform.position, at) / NearDistance, 0.85f, MostGrowth);
    }
}
